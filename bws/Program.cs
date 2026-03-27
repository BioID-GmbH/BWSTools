using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Bws
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // define possible arguments
            var fileArgument = new Argument<FileInfo>("file") { Description = "The image-file to process." };
            var filesArgument = new Argument<FileInfo[]>("files") { Description = "List of image-files to process." };
            var videoArgument = new Argument<FileInfo>("video") { Description = "The video-file to process." };

            // and options
            var hostOption = new Option<string>("--host") { Required = true, Description = "URL of the BWS to call." };
            var clientOption = new Option<string>("--clientid") { Required = true, Description = "Your BWS Client Identifier (needed for JWT Bearer authentication)." };
            var keyOption = new Option<string>("--key") { Required = true, Description = "Your base64 encoded signing key (needed for JWT Bearer authentication)." };
            var photoOption = new Option<FileInfo>("--photo") { Required = true, Description = "ID photo input file." };
            var disableliveOption = new Option<bool>("--disablelive") { Description = "Disable liveness detection with PhotoVerify API." };
            var challengeOption = new Option<string>("--challenge") { Description = "Optional head motion direction for the challenge response liveness detection mode (right, left, up, down). Requires two live images." };
            var classOption = new Option<long>("--classid", "-i") { Description = "Unique class ID of the person associated with the biometric template.", Required = true };
            var tagsOption = new Option<string[]>("--tags") { AllowMultipleArgumentsPerToken = true, Description = "Tags associated with a biometric template." };
            var restApiOption = new Option<bool>("--rest", "-r") { Description = "Use RESTful API call (instead of gRPC)." };
            var deadlineOption = new Option<int>("--deadline", "-d") { Description = "An optional deadline for the call (gRPC only) in milliseconds." };
            var verbosityOption = new Option<Verbosity>("--verbosity", "-v") { Description = "The output verbosity mode.", DefaultValueFactory = _ => Verbosity.Normal };

            // define available sub-commands
            var healthCheckCommand = new Command("healthcheck", "Call into the gRPC health check API.")
            {
                hostOption, restApiOption, verbosityOption
            };
            var livedetectCommand = new Command("livedetect", "Call into the LivenessDetection API, requires one (passive live detection) or two (active live detection) live images.")
            {
                filesArgument, hostOption, clientOption, keyOption, challengeOption, restApiOption, deadlineOption, verbosityOption
            };
            var videoLivedetectCommand = new Command("videolivedetect", "Call into the VideoLivenessDetection API, requires a video file as input.")
            {
                videoArgument, hostOption, clientOption, keyOption, restApiOption, deadlineOption, verbosityOption
            };
            var photoVerifyCommand = new Command("photoverify", "Call into the PhotoVerify API, requires one or two live images and one photo.")
            {
                filesArgument, hostOption, clientOption, keyOption, photoOption, disableliveOption, challengeOption, restApiOption, deadlineOption, verbosityOption
            };
            var enrollmentCommand = new Command("enroll", "Call into the FaceEnrollment API with one or more images.")
            {
                filesArgument, hostOption, clientOption, keyOption, classOption, restApiOption, deadlineOption, verbosityOption
            };
            var verifyCommand = new Command("verify", "Call into the FaceVerification API, providing a single image.")
            {
                fileArgument, hostOption, clientOption, keyOption, classOption, restApiOption, deadlineOption, verbosityOption
            };
            var searchCommand = new Command("search", "Call into the Face Search API, requires one or more image files.")
            {
                filesArgument, hostOption, clientOption, keyOption, tagsOption, restApiOption, deadlineOption, verbosityOption
            };
            var setTagsCommand = new Command("settags", "Associate tags with a biometric template.")
            {
                hostOption, clientOption, keyOption, classOption, tagsOption, restApiOption, deadlineOption, verbosityOption
            };
            var getTemplateCommand = new Command("gettemplate", "Fetch the status of a biometric template (together with enrolled thumbs, if available).")
            {
                hostOption, clientOption, keyOption, classOption, restApiOption, deadlineOption, verbosityOption
            };
            var deleteTemplateCommand = new Command("deletetemplate", "Delete a biometric template.")
            {
                hostOption, clientOption, keyOption, classOption, restApiOption, deadlineOption, verbosityOption
            };

            // set the command handlers for our APIs
            var cb = new ConnectionBinder(hostOption, clientOption, keyOption, restApiOption, deadlineOption);

            healthCheckCommand.SetHandler(HealthCheckAsync, hostOption, restApiOption, verbosityOption);
            livedetectCommand.SetHandler(LiveDetectionAsync, cb, filesArgument, challengeOption, verbosityOption);
            videoLivedetectCommand.SetHandler(VideoLiveDetectionAsync, cb, videoArgument, verbosityOption);
            photoVerifyCommand.SetHandler(PhotoVerifyAsync, cb, filesArgument, photoOption, disableliveOption, challengeOption, verbosityOption);
            enrollmentCommand.SetHandler(FaceEnrollmentAsync, cb, filesArgument, classOption, verbosityOption);
            verifyCommand.SetHandler(FaceVerificationAsync, cb, fileArgument, classOption, verbosityOption);
            searchCommand.SetHandler(FaceSearchAsync, cb, filesArgument, tagsOption, verbosityOption);
            setTagsCommand.SetHandler(SetTagsAsync, cb, classOption, tagsOption, verbosityOption);
            getTemplateCommand.SetHandler(GetTemplateStatusAsync, cb, classOption, verbosityOption);
            deleteTemplateCommand.SetHandler(DeleteTemplateAsync, cb, classOption, verbosityOption);

            var rootCommand = new RootCommand("BWS 3 command-line interface.");
            rootCommand.Subcommands.Add(healthCheckCommand);
            rootCommand.Subcommands.Add(livedetectCommand);
            rootCommand.Subcommands.Add(videoLivedetectCommand);
            rootCommand.Subcommands.Add(photoVerifyCommand);
            rootCommand.Subcommands.Add(enrollmentCommand);
            rootCommand.Subcommands.Add(verifyCommand);
            rootCommand.Subcommands.Add(searchCommand);
            rootCommand.Subcommands.Add(setTagsCommand);
            rootCommand.Subcommands.Add(getTemplateCommand);
            rootCommand.Subcommands.Add(deleteTemplateCommand);

            return await rootCommand.Parse(args).InvokeAsync();
        }

        // Call HealthCheck API
        internal static async Task<int> HealthCheckAsync(string host, bool restApi, Verbosity verbosity)
        {
            return restApi
                ? await BwsRest.HealthCheckAsync(host, verbosity).ConfigureAwait(false)
                : await BwsGrpc.HealthCheckAsync(host, verbosity).ConfigureAwait(false);
        }

        // Call LivenessDetection API, see https://developer.bioid.com/bws/grpc/livenessdetection
        internal static async Task<int> LiveDetectionAsync(Connection connection, FileInfo[] files, string tag, Verbosity verbosity)
        {
            return connection.RestApi
                   ? await BwsRest.LiveDetectionAsync(connection, files, tag, verbosity).ConfigureAwait(false)
                   : await BwsGrpc.LiveDetectionAsync(connection, files, tag, verbosity).ConfigureAwait(false);
        }

        // Call VideoLivenessDetection API, see https://developer.bioid.com/bws/grpc/videolivenessdetection
        internal static async Task<int> VideoLiveDetectionAsync(Connection connection, FileInfo video, Verbosity verbosity)
        {
            return connection.RestApi
                    ? await BwsRest.VideoLiveDetectionAsync(connection, video, verbosity).ConfigureAwait(false)
                    : await BwsGrpc.VideoLiveDetectionAsync(connection, video, verbosity).ConfigureAwait(false);
        }

        // Call PhotoVerify API, see https://developer.bioid.com/bws/grpc/photoverify
        internal static async Task<int> PhotoVerifyAsync(Connection connection, FileInfo[] files, FileInfo photo, bool disableLive, string tag, Verbosity verbosity)
        {
            return connection.RestApi
                    ? await BwsRest.PhotoVerifyAsync(connection, files, photo, disableLive, tag, verbosity).ConfigureAwait(false)
                    : await BwsGrpc.PhotoVerifyAsync(connection, files, photo, disableLive, tag, verbosity).ConfigureAwait(false);
        }

        // Call Enroll API, see https://developer.bioid.com/bws/face/enroll 
        internal static async Task<int> FaceEnrollmentAsync(Connection connection, FileInfo[] files, long classId, Verbosity verbosity)
        {
            return connection.RestApi
                ? await BwsRest.FaceEnrollmentAsync(connection, files, classId, verbosity).ConfigureAwait(false)
                : await BwsGrpc.FaceEnrollmentAsync(connection, files, classId, verbosity).ConfigureAwait(false);
        }

        // Call Verify API, see https://developer.bioid.com/bws/face/verify
        internal static async Task<int> FaceVerificationAsync(Connection connection, FileInfo file, long classId, Verbosity verbosity)
        {
            return connection.RestApi
                ? await BwsRest.FaceVerificationAsync(connection, file, classId, verbosity).ConfigureAwait(false)
                : await BwsGrpc.FaceVerificationAsync(connection, file, classId, verbosity).ConfigureAwait(false);
        }

        // Call Search API, see https://developer.bioid.com/bws/face/search
        internal static async Task<int> FaceSearchAsync(Connection connection, FileInfo[] files, string[] tags, Verbosity verbosity)
        {
            return connection.RestApi
                ? await BwsRest.FaceSearchAsync(connection, files, tags, verbosity).ConfigureAwait(false)
                : await BwsGrpc.FaceSearchAsync(connection, files, tags, verbosity).ConfigureAwait(false);
        }

        // Call SetTemplateTags API, see https://developer.bioid.com/bws/face/settemplatetags
        internal static async Task<int> SetTagsAsync(Connection connection, long classId, string[] tags, Verbosity verbosity)
        {
            return connection.RestApi
                ? await BwsRest.SetTagsAsync(connection, classId, tags, verbosity).ConfigureAwait(false)
                : await BwsGrpc.SetTagsAsync(connection, classId, tags, verbosity).ConfigureAwait(false);
        }

        // Call GetTemplateStatus API, see https://developer.bioid.com/bws/face/gettemplatestatus
        internal static async Task<int> GetTemplateStatusAsync(Connection connection, long classId, Verbosity verbosity)
        {
            return connection.RestApi
                ? await BwsRest.GetTemplateStatusAsync(connection, classId, verbosity).ConfigureAwait(false)
                : await BwsGrpc.GetTemplateStatusAsync(connection, classId, verbosity).ConfigureAwait(false);
        }

        // Call DeleteTemplate API, see https://developer.bioid.com/bws/face/deletetemplate
        internal static async Task<int> DeleteTemplateAsync(Connection connection, long classId, Verbosity verbosity)
        {
            return connection.RestApi
                ? await BwsRest.DeleteTemplateAsync(connection, classId, verbosity).ConfigureAwait(false)
                : await BwsGrpc.DeleteTemplateAsync(connection, classId, verbosity).ConfigureAwait(false);
        }

    }

    /// <summary>
    /// Represents various levels of detail for message output in the console.
    /// </summary>
    public enum Verbosity { Quiet, Minimal, Normal, Detailed, Diagnostic }


    /// <summary>
    /// Represents a connection configuration to the BWS api.
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// The host URL of the BWS api.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// The clientID used for authentication with the BWS api.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The client key used to authenticate to the BWS api.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// A value that indicates whether the request should be made via gRPC or REST.
        /// The default value is set to gRPC.
        /// </summary>
        public bool RestApi { get; set; } = false;

        /// <summary>
        /// The timeout deadline for API requests in milliseconds
        /// </summary>
        public int Deadline { get; set; } = -1;
    }

    /// <summary>
    /// Binds commandline options to a <see cref="Connection"/> object for use in configuring a BWS API connection.
    /// </summary>
    /// <param name="hostOption">The commandline option for the host URL.</param>
    /// <param name="clientOption">The commandline option for the client ID.</param>
    /// <param name="keyOption">The commandline option for the client key.</param>
    /// <param name="restApiOption">The commandline option for enabling the REST API mode.</param>
    /// <param name="deadlineOption">The commandline option for setting the timeout deadline.</param>
    public class ConnectionBinder(Option<string> hostOption, Option<string> clientOption, Option<string> keyOption, Option<bool> restApiOption, Option<int> deadlineOption)
    {
        private readonly Option<string> _hostOption = hostOption;
        private readonly Option<string> _clientOption = clientOption;
        private readonly Option<string> _keyOption = keyOption;
        private readonly Option<bool> _restApiOption = restApiOption;
        private readonly Option<int> _deadlineOption = deadlineOption;

        /// <summary>
        /// Creates a new <see cref="Connection"/> instance by extracting the values from the parsed command line options.
        /// </summary>
        /// <param name="result">The result of the command line parsing, used to retrieve the option values.</param>
        /// <returns>A fully configured <see cref="Connection"/> object.</returns>
        public Connection GetBoundValue(ParseResult result) =>
            new()
            {
                Host = result.GetValue(_hostOption)!,
                ClientId = result.GetValue(_clientOption)!,
                Key = result.GetValue(_keyOption)!,
                RestApi = result.GetValue(_restApiOption),
                Deadline = result.GetValue(_deadlineOption)
            };
    }
}

