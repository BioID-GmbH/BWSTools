using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bws
{
    /// <summary>
    /// Provides methods for interacting with the BWS RESTful web API, including health checks, livenessdetection, 
    /// videolivenessdetection, photoverify and facerecognition apis.
    /// </summary>
    internal static class BwsRest
    {
        /// <summary>
        /// Performs health checks for the specified host by calling the live and readiness endpoints.
        /// Outputs detailed information based on the verbosity level.
        /// </summary>
        /// <param name="host">The base URL of the BWS RESTful API.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> HealthCheckAsync(string host, Verbosity verbosity)
        {
            try
            {
                // Create an HTTP client with the specified host as the base address.
                var httpClient = new HttpClient { BaseAddress = new Uri(host) };

                // Perform the liveness health check.
                var req = new HttpRequestMessage(HttpMethod.Get, "/livez");
                var response = await httpClient.SendAsync(req).ConfigureAwait(false);
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Live http health-check: {responseBody}"); }

                // Perform the readiness health check.
                req = new HttpRequestMessage(HttpMethod.Get, "/readyz");
                response = await httpClient.SendAsync(req).ConfigureAwait(false);
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Ready http health-check: {responseBody}"); }

                if (verbosity >= Verbosity.Normal)
                {
                    ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Perform liveness detection on a set of images.
        /// </summary>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="files">The input image files for liveness detection.</param>
        /// <param name="tag">An optional tag to apply to the second image.
        /// The tag is necessary for the challenge-response mechanism to specify the movement direction.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> LiveDetectionAsync(Connection connection, FileInfo[] files, string tag, Verbosity verbosity)
        {
            try
            {
                // Create an authenticated HTTP client for the connection.
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/v1/livenessdetection";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling {url} at {connection.Host} ..."); }

                // Build the request payload with input images and optional tag.
                var request = new Json.LivenessDetectionRequest();
                foreach (var file in files)
                {
                    var img = new Json.ImageData { Image = Convert.ToBase64String(File.ReadAllBytes(file.FullName)) };
                    if (request.LiveImages.Count > 0 && !string.IsNullOrWhiteSpace(tag)) { img.Tags.Add(tag); }
                    request.LiveImages.Add(img);
                }

                // Call BWS.
                // Send the POST request to the API and handle the response.
                var response = await httpClient.PostAsync(url, JsonContent.Create(request, LivenessDetectionRequestContext.Default.LivenessDetectionRequest)).ConfigureAwait(false);

                // Handle successful and failed responses based on the status code.
                if (response.IsSuccessStatusCode)
                {
                    if (verbosity >= Verbosity.Minimal)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadFromJsonAsync(LivenessDetectionResponseContext.Default.LivenessDetectionResponse).ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine($"Live: {responseContent.Live} ({responseContent.LivenessScore})");
                            ConsoleOutput.DumpErrors(responseContent.Errors);
                            if (verbosity >= Verbosity.Normal)
                            {
                                ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                            }
                            if (verbosity >= Verbosity.Detailed)
                            {
                                Console.WriteLine("Response Content:");
                                Console.WriteLine(JsonSerializer.Serialize(responseContent, LivenessDetectionResponseContext.Default.LivenessDetectionResponse));
                            }
                        }
                    }
                }
                else
                {
                    // Log server responses when the request fails.
                    if (verbosity >= Verbosity.Minimal)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine(responseContent);
                        }
                        if (verbosity >= Verbosity.Normal)
                        {
                            ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Performs videolivenessdetection for a video file.
        /// </summary>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="video">The input video file for video liveness detection.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> VideoLiveDetectionAsync(Connection connection, FileInfo video, Verbosity verbosity)
        {
            try
            {
                // Create an authenticated HTTP client for the connection.
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/v1/videolivenessdetection";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling {url} at {connection.Host} ..."); }

                // Build the request payload with the video file.
                var request = new Json.VideoLivenessDetectionRequest { Video = Convert.ToBase64String(File.ReadAllBytes(video.FullName)) };

                // Call BWS
                // Send the POST request to the API and handle the response.
                var response = await httpClient.PostAsync(url, JsonContent.Create(request, VideoLivenessDetectionRequestContext.Default.VideoLivenessDetectionRequest)).ConfigureAwait(false);

                // Handle successful and failed responses based on the status code.
                if (response.IsSuccessStatusCode)
                {
                    if (verbosity >= Verbosity.Minimal)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadFromJsonAsync(LivenessDetectionResponseContext.Default.LivenessDetectionResponse).ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine($"Live: {responseContent.Live} ({responseContent.LivenessScore})");
                            ConsoleOutput.DumpErrors(responseContent.Errors);
                            if (verbosity >= Verbosity.Normal)
                            {
                                ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                            }
                            if (verbosity >= Verbosity.Detailed)
                            {
                                Console.WriteLine("Response Content:");
                                Console.WriteLine(JsonSerializer.Serialize(responseContent, LivenessDetectionResponseContext.Default.LivenessDetectionResponse));
                            }
                        }
                    }
                }
                else
                {
                    // Log server responses when the request fails.
                    if (verbosity >= Verbosity.Minimal)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine(responseContent);
                        }
                        if (verbosity >= Verbosity.Normal)
                        {
                            ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Performs photo verification by comparing live images to a specified ID photo.
        /// </summary>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="files">The input image files for photoverify.</param>
        /// <param name="photo">The ID photo file to verify against.</param>
        /// <param name="disableLive">Indicates whether to disable liveness detection.</param>
        /// <param name="tag">An optional tag to apply to the images.
        /// The tag is necessary for the challenge-response mechanism to specify the movement direction.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> PhotoVerifyAsync(Connection connection, FileInfo[] files, FileInfo photo, bool disableLive, string tag, Verbosity verbosity)
        {
            // Validate the input parameters to ensure the operation has the necessary data.
            if (files.Length == 0 || string.IsNullOrEmpty(photo.FullName)) { throw new ArgumentException("PhotoVerify requires at least one or more live images and one ID photo."); }
            try
            {
                // Create an authenticated HTTP client for the connection.
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/v1/photoverify";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling {url} at {connection.Host} ..."); }

                // Build the request payload with the ID photo and live images.
                var request = new Json.PhotoVerifyRequest
                {
                    Photo = Convert.ToBase64String(File.ReadAllBytes(photo.FullName)),
                    DisableLivenessDetection = disableLive
                };
                foreach (var file in files)
                {
                    var img = new Json.ImageData { Image = Convert.ToBase64String(File.ReadAllBytes(file.FullName)) };
                    if (request.LiveImages.Count > 0 && !string.IsNullOrWhiteSpace(tag)) { img.Tags.Add(tag); }
                    request.LiveImages.Add(img);
                }

                // Call BWS
                // Send the POST request to the API and handle the response.
                var response = await httpClient.PostAsync(url, JsonContent.Create(request, PhotoVerifyRequestContext.Default.PhotoVerifyRequest)).ConfigureAwait(false);

                // handle response
                if (response.IsSuccessStatusCode)
                {
                    if (verbosity >= Verbosity.Minimal)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadFromJsonAsync(PhotoVerifyResponseContext.Default.PhotoVerifyResponse).ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine($"VerificationLevel: {responseContent.VerificationLevel} ({responseContent.VerificationScore})");
                            Console.WriteLine($"Live: {responseContent.Live} ({responseContent.LivenessScore})");
                            ConsoleOutput.DumpErrors(responseContent.Errors);
                            if (verbosity >= Verbosity.Normal)
                            {
                                ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                            }
                            if (verbosity >= Verbosity.Detailed)
                            {
                                Console.WriteLine("Response Content:");
                                Console.WriteLine(JsonSerializer.Serialize(responseContent, PhotoVerifyResponseContext.Default.PhotoVerifyResponse));
                            }
                        }
                    }
                }
                else
                {
                    // Log server responses when the request fails.
                    if (verbosity >= Verbosity.Minimal)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine(responseContent);
                        }
                        if (verbosity >= Verbosity.Normal)
                        {
                            ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Performs biometric enrollment of a single class using one or more images of the person. 
        /// </summary>
        /// <remarks>
        /// This method creates and stores a new biometric face template,
        /// which can be used to create, update, or upgrade existing templates
        /// </remarks>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="files">An array of input image files for face enrollment.</param>
        /// <param name="classId">A unique class ID for the person will be associated with the biometric template.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> FaceEnrollmentAsync(Connection connection, FileInfo[] files, long classId, Verbosity verbosity)
        {
            try
            {
                // create the HTTP client
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/face/v1/enroll/{classId}";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling POST {url} at {connection.Host} ..."); }

                // create request
                var enrollRequest = new List<Json.ImageData>();
                foreach (var file in files)
                {
                    enrollRequest.Add(new Json.ImageData { Image = Convert.ToBase64String(File.ReadAllBytes(file.FullName)) });
                }
                // call BWS
                var response = await httpClient.PostAsync(url, JsonContent.Create(enrollRequest, FaceEnrollRequestContext.Default.ListImageData)).ConfigureAwait(false);

                // handle response
                if (verbosity >= Verbosity.Minimal)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadFromJsonAsync(FaceEnrollResponseContext.Default.FaceEnrollmentResponse).ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine($"PerformedAction: {responseContent.PerformedAction}");
                            ConsoleOutput.DumpErrors(responseContent.Errors);
                            if (verbosity >= Verbosity.Normal)
                            {
                                ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                            }
                            if (verbosity >= Verbosity.Detailed)
                            {
                                Console.WriteLine("Response Content:");
                                Console.WriteLine(JsonSerializer.Serialize(responseContent, FaceEnrollResponseContext.Default.FaceEnrollmentResponse));
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (responseContent != null) { Console.WriteLine(responseContent); }
                        if (verbosity >= Verbosity.Normal) { ConsoleOutput.DumpHeaders(response.Headers, "Response Headers"); }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Performs a one-to-one comparison of the uploaded face image with a stored biometric template 
        /// to verify whether the individual is the person they claim to be.
        /// </summary>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="file">An input image that is used for the verification.</param>
        /// <param name="classId">A unique class ID of the person associated with the biometric template.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> FaceVerificationAsync(Connection connection, FileInfo file, long classId, Verbosity verbosity)
        {
            try
            {
                // create the HTTP client
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/face/v1/verify/{classId}";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling POST {url} at {connection.Host} ..."); }

                // create request
                var verifyRequest = new Json.ImageData { Image = Convert.ToBase64String(File.ReadAllBytes(file.FullName)) };

                // call BWS
                var response = await httpClient.PostAsync(url, JsonContent.Create(verifyRequest, FaceVerifyRequestContext.Default.ImageData)).ConfigureAwait(false);

                // handle response
                if (verbosity >= Verbosity.Minimal)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadFromJsonAsync(FaceVerifyResponseContext.Default.FaceVerificationResponse).ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine($"Verified: {responseContent.Verified} ({responseContent.Score})");
                            ConsoleOutput.DumpErrors(responseContent.Errors);
                            if (verbosity >= Verbosity.Normal)
                            {
                                ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                            }
                            if (verbosity >= Verbosity.Detailed)
                            {
                                Console.WriteLine("Response Content:");
                                Console.WriteLine(JsonSerializer.Serialize(responseContent, FaceVerifyResponseContext.Default.FaceVerificationResponse));
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (responseContent != null) { Console.WriteLine(responseContent); }
                        if (verbosity >= Verbosity.Normal) { ConsoleOutput.DumpHeaders(response.Headers, "Response Headers"); }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Performs a one-to-many comparison of the uploaded face images with stored biometric templates 
        /// in order to find matching persons in the database.
        /// </summary>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="files">An array of input image files for the FaceSearch.</param>
        /// <param name="tags">A list of assigned tags that are being searched for in the biometric templates.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> FaceSearchAsync(Connection connection, FileInfo[] files, string[] tags, Verbosity verbosity)
        {
            try
            {
                // create the HTTP client
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = "/api/face/v1/search";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling POST {url} at {connection.Host} ..."); }

                // create request
                var request = new Json.FaceSearchRequest();
                foreach (var file in files)
                {
                    request.Images.Add(new Json.ImageData { Image = Convert.ToBase64String(File.ReadAllBytes(file.FullName)) });
                }
                request.Tags.AddRange(tags);
                // call BWS
                var response = await httpClient.PostAsync(url, JsonContent.Create(request, FaceSearchRequestContext.Default.FaceSearchRequest)).ConfigureAwait(false);

                // handle response
                if (verbosity >= Verbosity.Minimal)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadFromJsonAsync(FaceSearchResponseContext.Default.FaceSearchResponse).ConfigureAwait(false);
                        if (responseContent != null)
                        {
                            Console.WriteLine($"Searched persons: {responseContent.Result.Count}");
                            ConsoleOutput.DumpTemplateMatches(responseContent.Result);
                            //foreach (var r in responseContent.Result) { Console.WriteLine($"Matches: {r.Matches}"); }
                            ConsoleOutput.DumpErrors(responseContent.Errors);
                            if (verbosity >= Verbosity.Normal)
                            {
                                ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                            }
                            if (verbosity >= Verbosity.Detailed)
                            {
                                Console.WriteLine("Response Content:");
                                Console.WriteLine(JsonSerializer.Serialize(responseContent, FaceSearchResponseContext.Default.FaceSearchResponse));
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (responseContent != null) { Console.WriteLine(responseContent); }
                        if (verbosity >= Verbosity.Normal) { ConsoleOutput.DumpHeaders(response.Headers, "Response Headers"); }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Set the specified tags to a biometric template with specified class ID. 
        /// Tags can be used to group various classes together, allowing for more targeted searches 
        /// during one-to-many comparisons. Any existing tags will be overwritten with the new tags provided.
        /// </summary>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="classId">The unique class ID of the biometric face template to associate with the given tags.</param>
        /// <param name="tags">A list of tags to assign to the template.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> SetTagsAsync(Connection connection, long classId, string[] tags, Verbosity verbosity)
        {
            try
            {
                // create the HTTP client
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/face/v1/template/{classId}/tags";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling PUT {url} at {connection.Host} ..."); }

                // create request
                var request = new List<string>(tags);

                // call BWS
                var response = await httpClient.PutAsync(url, JsonContent.Create(request, ListStringContext.Default.ListString)).ConfigureAwait(false);

                if (verbosity >= Verbosity.Minimal)
                {
                    Console.WriteLine($"Server response: {response.StatusCode}");
                    if (verbosity >= Verbosity.Normal)
                    {
                        ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Fetches the biometric face template status of an enrolled person, if available. 
        /// </summary>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="classId">The unique class ID of the enrolled person whose template status is to be fetched.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> GetTemplateStatusAsync(Connection connection, long classId, Verbosity verbosity)
        {
            try
            {
                // create the HTTP client
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/face/v1/template/status/{classId}";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling GET {url} at {connection.Host} ..."); }

                // call BWS
                var response = await httpClient.GetAsync(url).ConfigureAwait(false);

                if (verbosity >= Verbosity.Minimal)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        if (verbosity >= Verbosity.Normal)
                        {
                            var responseContent = await response.Content.ReadFromJsonAsync(FaceTemplateStatusContext.Default.FaceTemplateStatus).ConfigureAwait(false);
                            if (responseContent != null)
                            {
                                Console.WriteLine("Response Content:");
                                Console.WriteLine(JsonSerializer.Serialize(responseContent, FaceTemplateStatusContext.Default.FaceTemplateStatus));
                            }
                            ConsoleOutput.DumpHeaders(response.Headers, "Response Headers");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Server response: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (responseContent != null) { Console.WriteLine(responseContent); }
                        if (verbosity >= Verbosity.Normal) { ConsoleOutput.DumpHeaders(response.Headers, "Response Headers"); }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Deletes all information associated with the provided class ID, including the biometric face templates 
        /// and any associated tags. This operation permanently removes the data from the system.
        /// </summary>
        /// <param name="connection">The connection parameters for the BWS RESTful API.</param>
        /// <param name="classId">The unique class ID of the biometric face templates to be deleted.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> DeleteTemplateAsync(Connection connection, long classId, Verbosity verbosity)
        {
            try
            {
                // create the HTTP client
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/face/v1/template/{classId}";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling DELETE {url} at {connection.Host} ..."); }

                // call BWS
                var response = await httpClient.DeleteAsync(url).ConfigureAwait(false);

                if (verbosity >= Verbosity.Minimal)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"Server response: {response.StatusCode}");
                    if (responseContent != null) { Console.WriteLine(responseContent); }
                    if (verbosity >= Verbosity.Normal) { ConsoleOutput.DumpHeaders(response.Headers, "Response Headers"); }
                }
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteError($"BWS RESTful web API call failed: {ex.Message}");
                return 1;
            }
            return 0;
        }
    }
}
