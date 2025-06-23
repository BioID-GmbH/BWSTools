using System;
using System.IO;
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
    /// Includes functionality for health checks, livenessdetection, 
    /// videolivenessdetection,photoverify and facerecognition.
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
                using var call = client.LivenessDetectionAsync(request, deadline: deadline);

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
        /// <param name="video">The input video file for video liveness detection.</param>
        /// <param name="verbosity">The verbosity level for output.</param>
        /// <returns>An integer indicating success (0) or failure (1).</returns>
        internal static async Task<int> VideoLiveDetectionAsync(Connection connection, FileInfo video, Verbosity verbosity)
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
                var request = new VideoLivenessDetectionRequest { Video = ByteString.CopyFrom(File.ReadAllBytes(video.FullName)) };

                // Set deadline timeout for the connection to the server.
                DateTime? deadline = connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline);
                // Call BWS
                using var call = client.VideoLivenessDetectionAsync(request, deadline: deadline);

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
                using var call = client.PhotoVerifyAsync(request, deadline: connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline));

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

        /// <summary>
        /// Performs biometric enrollment of a single class using one or more images of the person. 
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="files">An array of input image files for face enrollment.</param>
        /// <param name="classId">A unique class ID for the person will be associated with the biometric template.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>An integer indicating success (0) or failure (1).</returns>
        internal static async Task<int> FaceEnrollmentAsync(Connection connection, FileInfo[] files, long classId, Verbosity verbosity)
        {
            try
            {
                // create the gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling Face-Enrollment at {channel.Target}..."); }
                var client = new FaceRecognition.FaceRecognitionClient(channel);

                {
                    // creates request
                    var request = new FaceEnrollmentRequest { ClassId = classId };
                    foreach (var file in files) { request.Images.Add(new ImageData { Image = ByteString.CopyFrom(File.ReadAllBytes(file.FullName)) }); }
                    // call BWS
                    using var call = client.EnrollAsync(request, deadline: connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline));
                    FaceEnrollmentResponse response = await call.ResponseAsync.ConfigureAwait(false);

                    // output
                    if (verbosity >= Verbosity.Minimal)
                    {
                        Console.WriteLine($"Server response       : {call.GetStatus()}");
                        Console.WriteLine($"Job status            : {response.Status}");
                        Console.WriteLine($"Action                : {response.PerformedAction}");
                        ConsoleOutput.DumpErrors(response.Errors);
                    }
                    if (verbosity >= Verbosity.Normal)
                    {
                        Console.WriteLine($"Template Class-ID  : {response.TemplateStatus.ClassId}");
                        Console.WriteLine($"Template available : {response.TemplateStatus.Available}");
                        Console.WriteLine($"Template enrolled  : {response.TemplateStatus.Enrolled}");
                        Console.WriteLine($"Template tags      : {response.TemplateStatus.Tags}");
                        Console.WriteLine($"Template version   : {response.TemplateStatus.EncoderVersion}");
                        Console.WriteLine($"Template features  : {response.TemplateStatus.FeatureVectors}");
                        Console.WriteLine($"Template thumbnails: {response.TemplateStatus.ThumbnailsStored}");
                    }
                    if (verbosity >= Verbosity.Detailed)
                    {
                        ConsoleOutput.DumpMetadata(call.GetTrailers(), "Response Trailers");
                        ConsoleOutput.DumpImageProperties(response.ImageProperties);
                        ConsoleOutput.DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
                    }
                }
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity > Verbosity.Minimal)
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
        /// Performs a one-to-one comparison of the uploaded face image with a stored biometric template 
        /// to verify whether the individual is the person they claim to be.
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="file">An input image that is used for the verification.</param>
        /// <param name="classId">A unique class ID of the person associated with the biometric template.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>An integer indicating success (0) or failure (1).</returns>
        internal static async Task<int> FaceVerificationAsync(Connection connection, FileInfo file, long classId, Verbosity verbosity)
        {
            try
            {
                // create the gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key, 1000));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling Face-Verification at {channel.Target}..."); }
                var client = new FaceRecognition.FaceRecognitionClient(channel);

                // creates request
                var request = new FaceVerificationRequest
                {
                    Image = new ImageData { Image = ByteString.CopyFrom(File.ReadAllBytes(file.FullName)) },
                    ClassId = classId
                };

                // call BWS
                using var call = client.VerifyAsync(request, deadline: connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline));
                FaceVerificationResponse response = await call.ResponseAsync.ConfigureAwait(false);

                // output
                if (verbosity >= Verbosity.Minimal)
                {
                    Console.WriteLine($"Server response: {call.GetStatus()}");
                    Console.WriteLine($"Job status     : {response.Status}");
                    Console.WriteLine($"Verified       : {response.Verified}");
                    Console.WriteLine($"Score          : {response.Score}");
                    ConsoleOutput.DumpErrors(response.Errors);
                }
                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
                }
                if (verbosity >= Verbosity.Detailed)
                {
                    ConsoleOutput.DumpMetadata(call.GetTrailers(), "Response Trailers");
                    Console.WriteLine($"ImageProperties: {response.ImageProperties}");
                }
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity > Verbosity.Minimal)
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
        /// Performs a one-to-many comparison of the uploaded face images with stored biometric templates.
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="files">An array of input image files for the FaceSearch.</param>
        /// <param name="tags">A list of assigned tags that are being searched for in the biometric templates.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> FaceSearchAsync(Connection connection, FileInfo[] files, string[] tags, Verbosity verbosity)
        {
            try
            {
                // create the gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key, 1000));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling Face-Search at {channel.Target}..."); }
                var client = new FaceRecognition.FaceRecognitionClient(channel);

                // creates request
                var request = new FaceSearchRequest();
                foreach (var file in files)
                {
                    var img = new ImageData { Image = ByteString.CopyFrom(File.ReadAllBytes(file.FullName)) };
                    img.Tags.Add(file.Name);
                    request.Images.Add(img);
                }
                request.Tags.AddRange(tags);
                using var call = client.SearchAsync(request, deadline: connection.Deadline <= 0 ? null : DateTime.UtcNow.AddMilliseconds(connection.Deadline));
                FaceSearchResponse response = await call.ResponseAsync.ConfigureAwait(false);

                // output
                if (verbosity >= Verbosity.Minimal)
                {
                    Console.WriteLine($"Server response : {call.GetStatus()}");
                    Console.WriteLine($"Job status      : {response.Status}");
                    Console.WriteLine($"Searched persons: {response.Result.Count}");
                    foreach (var r in response.Result) { Console.WriteLine($"Matches: {r.Matches}"); }
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
                if (verbosity > Verbosity.Minimal)
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
        /// Set the specified tags to a biometric template with specified class ID. 
        /// Tags can be used to group various classes together, allowing for more targeted searches 
        /// during one-to-many comparisons. Any existing tags will be overwritten with the new tags provided.
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="classId">The unique class ID of the biometric face template to associate with the given tags.</param>
        /// <param name="tags">A list of tags to assign to the template.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> SetTagsAsync(Connection connection, long classId, string[] tags, Verbosity verbosity)
        {
            try
            {
                // create the gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling SetTemplateTags at {channel.Target}..."); }
                var client = new FaceRecognition.FaceRecognitionClient(channel);
                var request = new SetTemplateTagsRequest { ClassId = classId };
                request.Tags.AddRange(tags);
                // call BWS
                using var call = client.SetTemplateTagsAsync(request);
                SetTemplateTagsResponse response = await call.ResponseAsync.ConfigureAwait(false);
                // output
                if (verbosity >= Verbosity.Minimal) { Console.WriteLine($"Server response: {call.GetStatus()}"); }
                return 0;
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity > Verbosity.Minimal) { ConsoleOutput.DumpMetadata(ex.Trailers, "Response Trailers"); }
                return 1;
            }
            catch
            {
                ConsoleOutput.WriteError("Unexpected error calling service.");
                throw;
            }
        }

        /// <summary>
        /// Fetches the biometric face template status of an enrolled person, if available. 
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="classId">The unique class ID of the enrolled person whose template status is to be fetched.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> GetTemplateStatusAsync(Connection connection, long classId, Verbosity verbosity)
        {
            try
            {
                // create the gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling GetTemplateStatus at {channel.Target}..."); }
                var client = new FaceRecognition.FaceRecognitionClient(channel);
                var request = new FaceTemplateStatusRequest { ClassId = classId, DownloadThumbnails = true };
                // call BWS
                using var call = client.GetTemplateStatusAsync(request);
                FaceTemplateStatus response = await call.ResponseAsync.ConfigureAwait(false);
                // output
                if (verbosity >= Verbosity.Minimal)
                {
                    Console.WriteLine($"Server response       : {call.GetStatus()}");
                    Console.WriteLine($"Template Class-ID     : {response.ClassId}");
                    Console.WriteLine($"Template available    : {response.Available}");
                }
                if (verbosity >= Verbosity.Normal)
                {
                    Console.WriteLine($"Template enrolled  : {response.Enrolled}");
                    Console.WriteLine($"Template tags      : {response.Tags}");
                    Console.WriteLine($"Template version   : {response.EncoderVersion}");
                    Console.WriteLine($"Template features  : {response.FeatureVectors}");
                    Console.WriteLine($"Template thumbnails: {response.ThumbnailsStored}");
                    Console.WriteLine($"Thumbnails         : {response.Thumbnails.Count}");
                    ConsoleOutput.DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
                }
                if (verbosity >= Verbosity.Detailed)
                {
                    ConsoleOutput.DumpMetadata(call.GetTrailers(), "Response Trailers");
                    foreach (var thumb in response.Thumbnails)
                    {
                        Console.WriteLine($"- thumbnail with {thumb.Image.Length} bytes enrolled at {thumb.Enrolled}");
                    }
                }
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity > Verbosity.Minimal)
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
        /// Deletes all information associated with the provided class ID, including the biometric face templates 
        /// and any associated tags. This operation permanently removes the data from the system.
        /// </summary>
        /// <param name="connection">Connection parameters including host and authentication credentials.</param>
        /// <param name="classId">The unique class ID of the biometric face templates to be deleted.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> DeleteTemplateAsync(Connection connection, long classId, Verbosity verbosity)
        {
            try
            {
                // create the gRPC channel
                using GrpcChannel channel = Authentication.CreateAuthenticatedChannel(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling DeleteTemplate at {channel.Target}..."); }
                var client = new FaceRecognition.FaceRecognitionClient(channel);
                var request = new DeleteTemplateRequest { ClassId = classId };
                // call BWS
                using var call = client.DeleteTemplateAsync(request);
                var response = await call.ResponseAsync.ConfigureAwait(false);
                // output
                if (verbosity >= Verbosity.Minimal)
                {
                    Console.WriteLine($"Server response: {call.GetStatus()}");
                }
                if (verbosity >= Verbosity.Diagnostic)
                {
                    ConsoleOutput.DumpMetadata(await call.ResponseHeadersAsync.ConfigureAwait(false), "Response Headers");
                    ConsoleOutput.DumpMetadata(call.GetTrailers(), "Response Trailers");
                }
                return 0;
            }
            catch (RpcException ex)
            {
                ConsoleOutput.WriteError($"gRPC error from calling service: {ex.Status.StatusCode} - '{ex.Status.Detail}'");
                if (verbosity > Verbosity.Minimal)
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
        }
    }
}
