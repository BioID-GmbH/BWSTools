using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BioID.Services;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

internal class Program
{
    public enum Verbosity { Quiet, Minimal, Normal, Detailed, Diagnostic }

    static async Task<int> Main(string[] args)
    {
        // define possible options and arguments
        var hostOption = new Option<string>("--host", "URL of the BWS to call.") { IsRequired = true };
        var clientOption = new Option<string>("--clientid", "Your BWS Client Identifier (needed for JWT Bearer authentication).") { IsRequired = true };
        var keyOption = new Option<string>("--key", "Your base64 encoded signing key (needed for JWT Bearer authentication).") { IsRequired = true };
        var filesArgument = new Argument<FileInfo[]>("files", "List of (image-/video-) files to process.");
        var photoOption = new Option<FileInfo>("--photo", "ID photo input file.") { IsRequired = true };
        var disableliveOption = new Option<bool>("--disablelive", "Disable liveness detection with PhotoVerify API.");
        var challengeOption = new Option<string>("--challenge", "Optional head motion direction for the challenge response liveness detection mode (right, left, up, down). Requires two live images.");
        var verbosityOption = new Option<Verbosity>(aliases: ["--verbosity", "-v"], description: "The output verbosity mode.", getDefaultValue: () => Verbosity.Normal);

        // define available sub-commands
        var livedetectCommand = new Command("livedetect", "Call into the LivenessDetection API, requires one (passive live detection) or two (active live detection) live images.")
        {
            hostOption, clientOption, keyOption, filesArgument, challengeOption, verbosityOption
        };
        var videoLivedetectCommand = new Command("videolivedetect", "Call into the VideoLivenessDetection API, requires a video file as input.")
        {
            hostOption, clientOption, keyOption, filesArgument, verbosityOption
        };
        var photoVerifyCommand = new Command("photoverify", "Call into the PhotoVerify API, requires one or two live images and one photo.")
        {
            hostOption, clientOption, keyOption, photoOption, filesArgument, disableliveOption, challengeOption, verbosityOption
        };
        var healthCheckCommand = new Command("healthcheck", "Call into the gRPC health check API.")
        {
            hostOption, verbosityOption
        };

        var rootCommand = new RootCommand("BWS 3 command-line interface.");
        rootCommand.AddCommand(livedetectCommand);
        rootCommand.AddCommand(videoLivedetectCommand);
        rootCommand.AddCommand(photoVerifyCommand);
        rootCommand.AddCommand(healthCheckCommand);

        // set the command handlers for our APIs
        livedetectCommand.SetHandler(LiveDetectionAsync, hostOption, clientOption, keyOption, filesArgument, challengeOption, verbosityOption);
        videoLivedetectCommand.SetHandler(VideoLiveDetectionAsync, hostOption, clientOption, keyOption, filesArgument, verbosityOption);
        photoVerifyCommand.SetHandler(PhotoVerifyAsync, hostOption, clientOption, keyOption, filesArgument, disableliveOption, challengeOption, photoOption, verbosityOption);
        healthCheckCommand.SetHandler(HealthCheckAsync, hostOption, verbosityOption);

        return await rootCommand.InvokeAsync(args);
    }

    // call LivenessDetection API, see https://developer.bioid.com/bws/grpc/livenessdetection
    internal static async Task LiveDetectionAsync(string host, string clientId, string key, FileInfo[] files, string tag, Verbosity verbosity)
    {
        // create the gRPC channel
        using GrpcChannel channel = CreateAuthenticatedChannel(new Uri(host), GenerateToken(clientId, key));
        if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling LivenessDetection at {channel.Target}..."); }

        // create request
        var client = new BioIDWebService.BioIDWebServiceClient(channel);
        var request = new LivenessDetectionRequest();
        foreach (var file in files)
        {
            request.LiveImages.Add(new ImageData { Image = ByteString.CopyFrom(File.ReadAllBytes(file.FullName)) });
        }
        // tag is applied only to second image
        if (request.LiveImages.Count > 1 && !string.IsNullOrWhiteSpace(tag)) { request.LiveImages[1].Tags.Add(tag); }

        // call BWS
        var call = client.LivenessDetectionAsync(request);
        LivenessDetectionResponse response = await call.ResponseAsync.ConfigureAwait(false);

        // output
        if (verbosity > Verbosity.Quiet)
        {
            Console.WriteLine($"Server response: {response.Status}");
            Console.WriteLine($"Live: {response.Live} ({response.LivenessScore:F2})");
            DumpErrors(response.Errors);
        }
        if (verbosity > Verbosity.Minimal)
        {
            DumpMetadata(call.GetTrailers(), "Response Trailers");
        }
        if (verbosity > Verbosity.Normal)
        {
            DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
            DumpImageProperties(response.ImageProperties);
        }
    }

    // call VideoLivenessDetection API, see https://developer.bioid.com/bws/grpc/videolivenessdetection
    internal static async Task VideoLiveDetectionAsync(string host, string clientId, string key, FileInfo[] files, Verbosity verbosity)
    {
        // create the gRPC channel
        using GrpcChannel channel = CreateAuthenticatedChannel(new Uri(host), GenerateToken(clientId, key));
        if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling VideoLivenessDetection at {channel.Target}..."); }

        // create request
        var client = new BioIDWebService.BioIDWebServiceClient(channel);
        var request = new VideoLivenessDetectionRequest
        {
            Video = ByteString.CopyFrom(File.ReadAllBytes(files.First().FullName))
        };

        // call BWS
        var call = client.VideoLivenessDetectionAsync(request);
        LivenessDetectionResponse response = await call.ResponseAsync.ConfigureAwait(false);

        // output
        if (verbosity > Verbosity.Quiet)
        {
            Console.WriteLine($"Server response: {response.Status}");
            Console.WriteLine($"Live: {response.Live} ({response.LivenessScore:F2})");
            DumpErrors(response.Errors);
        }
        if (verbosity > Verbosity.Minimal)
        {
            DumpMetadata(call.GetTrailers(), "Response Trailers");
        }
        if (verbosity > Verbosity.Normal)
        {
            DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
            DumpImageProperties(response.ImageProperties);
        }
    }

    // call PhotoVerify API, see https://developer.bioid.com/bws/grpc/photoverify
    internal static async Task PhotoVerifyAsync(string host, string clientId, string key, FileInfo[] files, bool disablelive, string tag, FileInfo photo, Verbosity verbosity)
    {
        // create the gRPC channel
        using GrpcChannel channel = CreateAuthenticatedChannel(new Uri(host), GenerateToken(clientId, key));
        if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling PhotoVerify at {channel.Target}..."); }

        // create request
        var client = new BioIDWebService.BioIDWebServiceClient(channel);
        var request = new PhotoVerifyRequest
        {
            DisableLivenessDetection = disablelive,
            Photo = ByteString.CopyFrom(File.ReadAllBytes(photo.FullName))
        };
        foreach (var file in files)
        {
            request.LiveImages.Add(new ImageData { Image = ByteString.CopyFrom(File.ReadAllBytes(file.FullName)) });
        }
        // tag is applied only to second image
        if (request.LiveImages.Count > 1 && !string.IsNullOrWhiteSpace(tag)) { request.LiveImages[1].Tags.Add(tag); }

        // call BWS
        var call = client.PhotoVerifyAsync(request);
        PhotoVerifyResponse response = await call.ResponseAsync.ConfigureAwait(false);

        // output
        if (verbosity > Verbosity.Quiet)
        {
            Console.WriteLine($"Server response: {response.Status}");
            Console.WriteLine($"VerificationLevel: {response.VerificationLevel} ({response.VerificationScore})");
            Console.WriteLine($"Live: {response.Live} ({response.LivenessScore:F2})");
            DumpErrors(response.Errors);
        }
        if (verbosity > Verbosity.Minimal)
        {
            DumpMetadata(call.GetTrailers(), "Response Trailers");
        }
        if (verbosity > Verbosity.Normal)
        {
            DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
            DumpImageProperties(response.ImageProperties);
            Console.WriteLine($"PhotoProperties: {response.PhotoProperties}");
        }
    }

    // call HealthCheck API
    internal static async Task HealthCheckAsync(string host, Verbosity verbosity)
    {
        // create the gRPC channel
        using GrpcChannel channel = GrpcChannel.ForAddress(host);
        var client = new Health.HealthClient(channel);

        // perform liveness probe
        var check = await client.CheckAsync(new HealthCheckRequest { Service = "liveness" }).ConfigureAwait(false);
        if (verbosity > Verbosity.Quiet)
        {
            Console.WriteLine($"BWS gRPC liveness-check @ {host}: {check.Status}");
        }
        if (verbosity > Verbosity.Normal)
        {
            // perform readiness probe
            check = await client.CheckAsync(new HealthCheckRequest { Service = "readiness" }).ConfigureAwait(false);
            Console.WriteLine($"BWS gRPC readiness-check @ {host}: {check.Status}");
        }
    }

    /// <summary>
    /// Create a gRPC channel with call credentials.
    /// </summary>
    /// <remarks>
    /// This implementation uses the provided token for authentication, i.e. 
    /// the JWT must be valid as long as the created channel is in use. 
    /// An alternate implementation could create a new token each time the call
    /// credentials are retrieved.
    /// </remarks>
    /// <param name="host">The URL of the BWS host to call.</param>
    /// <param name="token">The JWT used for authentication.</param>
    /// <returns>The created gRPC channel.</returns>
    static GrpcChannel CreateAuthenticatedChannel(Uri host, string token)
    {
        // we want to use JWT Bearer token authentication for each call (i.e. CallCredentials)
        var credentials = CallCredentials.FromInterceptor((context, metadata) =>
        {
            metadata.Add("Reference-Number", $"bws-cli-{DateTime.UtcNow.Ticks}");
            metadata.Add("Authorization", $"Bearer {token}");
            return Task.CompletedTask;
        });

        bool insecure = host.Scheme != Uri.UriSchemeHttps;
        // create the GRPC channel with our call credentials
        return GrpcChannel.ForAddress(host, new GrpcChannelOptions
        {
            // TLS is highly recommended, but in case of an insecure connection we allow the use of the JWT nonetheless
            Credentials = ChannelCredentials.Create(insecure ? ChannelCredentials.Insecure : ChannelCredentials.SecureSsl, credentials),
            UnsafeUseInsecureChannelCallCredentials = insecure
        });
    }

    /// <summary>
    /// Generate a JWT for client authentication at the BioID Web Service.
    /// </summary>
    /// <param name="clientId">The ID of the BWS client to authenticate.</param>
    /// <param name="key">The symmetric key associated with the BWS client that
    /// is used to sign the JWT.</param>
    /// <param name="expireMinutes">Optional expiry of the genereated token in minutes.</param>
    /// <returns>A string containing the created JWT.</returns>
    static string GenerateToken(string clientId, string key, int expireMinutes = 5)
    {
        var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(key));
        var claims = new Dictionary<string, object> { ["sub"] = clientId, };
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = claims,
            Issuer = clientId,
            Audience = "BWS",
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512),
        };
        var handler = new JsonWebTokenHandler
        {
            SetDefaultTimesOnTokenCreation = true,
            TokenLifetimeInMinutes = expireMinutes
        };
        string jwt = handler.CreateToken(descriptor);
        return jwt;
    }

    static void DumpMetadata(Metadata entries, string title)
    {
        if (entries.Count != 0)
        {
            Console.WriteLine($"{title}:");
            foreach (var entry in entries)
            {
                Console.WriteLine($"  {entry.Key}: {entry.Value}");
            }
        }
    }

    static void DumpErrors(IEnumerable<JobError> errors)
    {
        if (errors.Any())
        {
            Console.WriteLine("Errors:");
            foreach (var error in errors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"  {error.ErrorCode}: {error.Message}");
                Console.ResetColor();
            }
        }
    }

    static void DumpImageProperties(IEnumerable<ImageProperties> imageProperties)
    {
        foreach (ImageProperties properties in imageProperties) { Console.WriteLine($"ImageProperties: {properties}"); }
    }
}
