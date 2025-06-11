using System.CommandLine;
using System.CommandLine.Binding;
using System.IO;
using System.Threading.Tasks;

namespace Bws
{
    class Program
    {
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
            var restApiOption = new Option<bool>(["--rest", "-r"], "Use RESTful API calls.");
            var deadlineOption = new Option<int>(["--deadline", "-d"], "An optional deadline for the call in milliseconds.");
            var classOption = new Option<long>(["--classid", "-i"], "A unique class ID of the person associated with a template.");
            var tagsOption = new Option<string[]>("--tags", "Tags associated with a biometric template.") { AllowMultipleArgumentsPerToken = true };

            // define available sub-commands
            var livedetectCommand = new Command("livedetect", "Call into the LivenessDetection API, requires one (passive live detection) or two (active live detection) live images.")
            {
                hostOption, clientOption, keyOption, restApiOption, deadlineOption, filesArgument, challengeOption, verbosityOption
            };
            var videoLivedetectCommand = new Command("videolivedetect", "Call into the VideoLivenessDetection API, requires a video file as input.")
            {
                hostOption, clientOption, keyOption, restApiOption,deadlineOption, filesArgument, verbosityOption
            };
            var photoVerifyCommand = new Command("photoverify", "Call into the PhotoVerify API, requires one or two live images and one photo.")
            {
                hostOption, clientOption, keyOption, restApiOption,deadlineOption, photoOption, filesArgument, disableliveOption, challengeOption, verbosityOption
            };
            var healthCheckCommand = new Command("healthcheck", "Call into the gRPC health check API.")
            {
                hostOption, restApiOption, verbosityOption
            };
            var enrollmentCommand = new Command("enroll", "Call into the FaceEnrollment API, requires one or more images.")
            {
                hostOption, clientOption, keyOption, restApiOption, deadlineOption, verbosityOption, filesArgument, classOption
            };
            var verifyCommand = new Command("verify", "Call into the FaceVerification API, requires exactly one image.")
            {
                hostOption, clientOption, keyOption, restApiOption, deadlineOption, verbosityOption, filesArgument, classOption
            };
            var searchCommand = new Command("search", "Call into the Face Search API, requires one or more images.")
            {
                hostOption, clientOption, keyOption, restApiOption, deadlineOption, verbosityOption, filesArgument, tagsOption
            };
            var setTagsCommand = new Command("settags", "Associate tags with a biometric template.")
            {
                hostOption, clientOption, keyOption, restApiOption, deadlineOption, verbosityOption, classOption, tagsOption
            };
            var getTemplateCommand = new Command("gettemplate", "Fetch the status of a biometric template (together with enrolled thumbs, if available).")
            {
                hostOption, clientOption, keyOption, restApiOption, deadlineOption, verbosityOption, classOption
            };
            var getClassCountCommand = new Command("classcount", "Fetch the number of enrolled classes.")
            {
                hostOption, clientOption, keyOption, restApiOption, verbosityOption, tagsOption
            };
            var deleteTemplateCommand = new Command("deletetemplate", "Delete a biometric template.")
            {
                hostOption, clientOption, keyOption, restApiOption, deadlineOption, verbosityOption, classOption
            };

            // set the command handlers for our APIs
            var cb = new ConnectionBinder(hostOption, clientOption, keyOption, restApiOption, deadlineOption);
            livedetectCommand.SetHandler(LiveDetectionAsync, cb, filesArgument, challengeOption, verbosityOption);
            videoLivedetectCommand.SetHandler(VideoLiveDetectionAsync, cb, filesArgument, verbosityOption);
            photoVerifyCommand.SetHandler(PhotoVerifyAsync, cb, filesArgument, photoOption, disableliveOption, challengeOption, verbosityOption);
            healthCheckCommand.SetHandler(HealthCheckAsync, hostOption, restApiOption, verbosityOption);
            enrollmentCommand.SetHandler(FaceEnrollmentAsync, cb, filesArgument, classOption, verbosityOption);
            verifyCommand.SetHandler(FaceVerificationAsync, cb, filesArgument, classOption, verbosityOption);
            searchCommand.SetHandler(FaceSearchAsync, cb, filesArgument, tagsOption, verbosityOption);
            setTagsCommand.SetHandler(SetTagsAsync, cb, classOption, tagsOption, verbosityOption);
            getTemplateCommand.SetHandler(GetTemplateStatusAsync, cb, classOption, verbosityOption);
            getClassCountCommand.SetHandler(GetClassCountAsync, cb, tagsOption, verbosityOption);
            deleteTemplateCommand.SetHandler(DeleteTemplateAsync, cb, classOption, verbosityOption);

            var rootCommand = new RootCommand("BWS 3 command-line interface.");
            rootCommand.AddCommand(livedetectCommand);
            rootCommand.AddCommand(videoLivedetectCommand);
            rootCommand.AddCommand(photoVerifyCommand);
            rootCommand.AddCommand(healthCheckCommand);
            rootCommand.AddCommand(enrollmentCommand);
            rootCommand.AddCommand(verifyCommand);
            rootCommand.AddCommand(searchCommand);
            rootCommand.AddCommand(setTagsCommand);
            rootCommand.AddCommand(getTemplateCommand);
            rootCommand.AddCommand(getClassCountCommand);
            rootCommand.AddCommand(deleteTemplateCommand);

            return await rootCommand.InvokeAsync(args);
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
        internal static async Task<int> VideoLiveDetectionAsync(Connection connection, FileInfo[] files, Verbosity verbosity)
        {
            return connection.RestApi
                    ? await BwsRest.VideoLiveDetectionAsync(connection, files, verbosity).ConfigureAwait(false)
                    : await BwsGrpc.VideoLiveDetectionAsync(connection, files, verbosity).ConfigureAwait(false);
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
        internal static async Task<int> FaceVerificationAsync(Connection connection, FileInfo[] files, long classId, Verbosity verbosity)
        {
            return connection.RestApi
                ? await BwsRest.FaceVerificationAsync(connection, files, classId, verbosity).ConfigureAwait(false)
                : await BwsGrpc.FaceVerificationAsync(connection, files, classId, verbosity).ConfigureAwait(false);
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

        // Call GetClassCount API, see https://developer.bioid.com/bws/face/getclasscount
        internal static async Task<int> GetClassCountAsync(Connection connection, string[] tags, Verbosity verbosity)
        {
            return connection.RestApi
                ? await BwsRest.GetClassCountAsync(connection, tags, verbosity).ConfigureAwait(false)
                : await BwsGrpc.GetClassCountAsync(connection, tags, verbosity).ConfigureAwait(false);
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
    class ConnectionBinder(Option<string> hostOption, Option<string> clientOption, Option<string> keyOption, Option<bool> restApiOption, Option<int> deadlineOption) : BinderBase<Connection>
    {
        private readonly Option<string> _hostOption = hostOption;
        private readonly Option<string> _clientOption = clientOption;
        private readonly Option<string> _keyOption = keyOption;
        private readonly Option<bool> _restApiOption = restApiOption;
        private readonly Option<int> _deadlineOption = deadlineOption;

        /// <summary>
        /// Creates a <see cref="Connection"/> object based on the provided binding context and commandline options.
        /// </summary>
        /// <param name="bindingContext">The context containing parsed commandline arguments.</param>
        /// <returns>A configured <see cref="Connection"/> object.</returns>
        protected override Connection GetBoundValue(BindingContext bindingContext) =>
            new()
            {
                Host = bindingContext.ParseResult.GetValueForOption(_hostOption)!,
                ClientId = bindingContext.ParseResult.GetValueForOption(_clientOption)!,
                Key = bindingContext.ParseResult.GetValueForOption(_keyOption)!,
                RestApi = bindingContext.ParseResult.GetValueForOption(_restApiOption),
                Deadline = bindingContext.ParseResult.GetValueForOption(_deadlineOption)
            };
    }
}

