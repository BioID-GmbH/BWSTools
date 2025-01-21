using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BioID.Services;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;

namespace Bws
{
    /// <summary>
    /// Provides utility methods for performing gRPC calls to various bws operations.
    /// Includes functionality for health checks, livenessdetection, videolivenessdetection and photoverify.
    /// </summary>
    internal static class BwsGrpc
    {
        /// <summary>
        /// Performs a health check on the specified gRPC service.
        /// Outputs detailed information based on the verbosity level.
        /// </summary>
        /// <param name="host">The host address of the gRPC service.</param>
        /// <param name="verbosity">The verbosity level for output.</param>
        /// <returns>An integer indicating success (0) or failure (1).</returns>
        internal static async Task<int> HealthCheckAsync(string host, Verbosity verbosity)
        {
            try
            {
                // Create the gRPC channel to communicate with the host
                using var channel = GrpcChannel.ForAddress(new Uri(host));
                var client = new Health.HealthClient(channel);

                // liveness check
                var call = client.CheckAsync(new HealthCheckRequest { Service = "liveness" });
                var check = await call.ResponseAsync.ConfigureAwait(false);
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"gRPC service liveness-check @ {host}: {check?.Status}"); }

                // readiness check
                call = client.CheckAsync(new HealthCheckRequest { Service = "readiness" });

                // Read the service response
                check = await call.ResponseAsync.ConfigureAwait(false);
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"gRPC service readiness-check @ {host}: {check?.Status}"); }

                // Output additional metadata based on verbosity level
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
                }
                if (verbosity >= Verbosity.Detailed)
                {
                    ConsoleOutput.DumpMetadata(call.GetTrailers(), "Response Trailers");
                }
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(ex.Trailers, "Response Trailers");
                }
                return 1;
            }
            catch
            {
                ConsoleOutput.WriteError("Unexpected error calling service.");
                throw;
            }
            return 0;
        }

        /// <summary>
        /// Perform livenessdetection on a set of images.
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="files">The input image files for liveness detection.</param>
        /// <param name="tag">Optional tag applied to the second image.
        /// The tag is necessary for the challenge-response mechanism to specify the movement direction.</param>
        /// <param name="verbosity">The verbosity level for output.</param>
        /// <returns>An integer indicating success (0) or failure (1).</returns>
        internal static async Task<int> LiveDetectionAsync(Connection connection, FileInfo[] files, string tag, Verbosity verbosity)
        {
            try
            {
                // Create the authenticated gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling LivenessDetection at {channel.Target}..."); }

                // Prepare the request with live images
                // Create BWS client 
                var client = new BioIDWebService.BioIDWebServiceClient(channel);
                // Create liveness detection request
                var request = new LivenessDetectionRequest();
                foreach (var file in files)
                {
                    // Add live images to request
                    request.LiveImages.Add(new ImageData { Image = ByteString.CopyFrom(File.ReadAllBytes(file.FullName)) });
                }
                // Tag is applied only to second image
                if (request.LiveImages.Count > 1 && !string.IsNullOrWhiteSpace(tag)) { request.LiveImages[1].Tags.Add(tag); }

                // Set deadline timeout for the connection to the server.
                DateTime? deadline = connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline);
                // Call BWS
                var call = client.LivenessDetectionAsync(request, deadline: deadline);

                // Reading the call response
                LivenessDetectionResponse response = await call.ResponseAsync.ConfigureAwait(false);

                // Output
                if (verbosity >= Verbosity.Minimal)
                {
                    Console.WriteLine($"Server response: {call.GetStatus()}");
                    Console.WriteLine($"Job status: {response.Status}");
                    Console.WriteLine($"Live: {response.Live} ({response.LivenessScore})");
                    ConsoleOutput.DumpErrors(response.Errors);
                }
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
                }
                if (verbosity >= Verbosity.Detailed)
                {
                    ConsoleOutput.DumpMetadata(call.GetTrailers(), "Response Trailers");
                    ConsoleOutput.DumpImageProperties(response.ImageProperties);
                }
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(ex.Trailers, "Response Trailers");
                }
                return 1;
            }
            catch
            {
                ConsoleOutput.WriteError("Unexpected error calling service.");
                throw;
            }
            return 0;
        }

        /// <summary>
        /// Performs videolivenessdetection for a video file.
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="files">The input video file for video liveness detection.</param>
        /// <param name="verbosity">The verbosity level for output.</param>
        /// <returns>An integer indicating success (0) or failure (1).</returns>
        internal static async Task<int> VideoLiveDetectionAsync(Connection connection, FileInfo[] files, Verbosity verbosity)
        {
            try
            {
                // Create the authenticated gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling VideoLivenessDetection at {channel.Target}..."); }

                // Prepare video file request
                // Create BWS client
                var client = new BioIDWebService.BioIDWebServiceClient(channel);
                // Create videolivenessdetetction request
                var request = new VideoLivenessDetectionRequest { Video = ByteString.CopyFrom(File.ReadAllBytes(files.First().FullName)) };

                // Set deadline timeout for the connection to the server.
                DateTime? deadline = connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline);
                // Call BWS
                var call = client.VideoLivenessDetectionAsync(request, deadline: deadline);

                // Reading the call response
                LivenessDetectionResponse response = await call.ResponseAsync.ConfigureAwait(false);

                // Output
                if (verbosity >= Verbosity.Minimal)
                {
                    Console.WriteLine($"Server response: {call.GetStatus()}");
                    Console.WriteLine($"Job status: {response.Status}");
                    Console.WriteLine($"Live: {response.Live} ({response.LivenessScore})");
                    ConsoleOutput.DumpErrors(response.Errors);
                }
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
                }
                if (verbosity >= Verbosity.Detailed)
                {
                    ConsoleOutput.DumpMetadata(call.GetTrailers(), "Response Trailers");
                    ConsoleOutput.DumpImageProperties(response.ImageProperties);
                }
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(ex.Trailers, "Response Trailers");
                }
                return 1;
            }
            catch
            {
                ConsoleOutput.WriteError("Unexpected error calling service.");
                throw;
            }
            return 0;
        }

        /// <summary>
        /// Performs photo verification by comparing live images to a specified ID photo.
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="files">Array of file paths for live images to process.</param>
        /// <param name="photo">The file path for the ID photo to verify against.</param>
        /// <param name="disableLive">Boolean flag to disable liveness detection.</param>
        /// <param name="tag">Optional tag applied to the second image.
        /// The tag is necessary for the challenge-response mechanism to specify the movement direction.</param>
        /// <param name="verbosity">The verbosity level for output.</param>
        /// <returns>An integer indicating success (0) or failure (1).</returns>

        internal static async Task<int> PhotoVerifyAsync(Connection connection, FileInfo[] files, FileInfo photo, bool disableLive, string tag, Verbosity verbosity)
        {
            // Validate the input parameters to ensure the operation has the necessary data.
            if (files.Length == 0 || string.IsNullOrEmpty(photo.FullName)) { throw new ArgumentException("PhotoVerify requires at least one or more live images and one ID photo."); }
            try
            {
                // Create the authenticated gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling PhotoVerify at {channel.Target}..."); }

                // Creates photoverify request
                var client = new BioIDWebService.BioIDWebServiceClient(channel);
                var request = new PhotoVerifyRequest
                {
                    DisableLivenessDetection = disableLive,
                    Photo = ByteString.CopyFrom(File.ReadAllBytes(photo.FullName))
                };
                foreach (var file in files)
                {
                    // Add live images to request
                    request.LiveImages.Add(new ImageData { Image = ByteString.CopyFrom(File.ReadAllBytes(file.FullName)) });
                }
                // Tag is applied only to second image
                if (request.LiveImages.Count > 1 && !string.IsNullOrWhiteSpace(tag)) { request.LiveImages[1].Tags.Add(tag); }

                // Set deadline timeout for the connection to the server.
                DateTime? deadline = connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline);
                // Call BWS
                var call = client.PhotoVerifyAsync(request, deadline: connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline));

                // Reading the call response
                PhotoVerifyResponse response = await call.ResponseAsync.ConfigureAwait(false);

                // Output
                if (verbosity >= Verbosity.Minimal)
                {
                    Console.WriteLine($"Server response: {call.GetStatus()}");
                    Console.WriteLine($"Job status: {response.Status}");
                    Console.WriteLine($"VerificationLevel: {response.VerificationLevel} ({response.VerificationScore})");
                    Console.WriteLine($"Live: {response.Live} ({response.LivenessScore})");
                    ConsoleOutput.DumpErrors(response.Errors);
                }
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
                }
                if (verbosity >= Verbosity.Detailed)
                {
                    ConsoleOutput.DumpMetadata(call.GetTrailers(), "Response Trailers");
                    ConsoleOutput.DumpImageProperties(response.ImageProperties);
                }
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(ex.Trailers, "Response Trailers");
                }
                return 1;
            }
            catch
            {
                ConsoleOutput.WriteError("Unexpected error calling service.");
                throw;
            }
            return 0;
        }
    }
}
