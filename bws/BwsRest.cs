using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bws
{
    /// <summary>
    /// Provides methods for interacting with the BWS RESTful web API, including health checks, livenessdetection, 
    /// videolivenessdetection and photoverify apis.
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
        /// <param name="files">The input video file for video liveness detection.</param>
        /// <param name="verbosity">Specifies the verbosity level for logging the results.</param>
        /// <returns>Returns 0 on success, or 1 if an exception occurs.</returns>
        internal static async Task<int> VideoLiveDetectionAsync(Connection connection, FileInfo[] files, Verbosity verbosity)
        {
            try
            {
                // Create an authenticated HTTP client for the connection.
                using HttpClient httpClient = Authentication.CreateAuthenticatedClient(new Uri(connection.Host), Authentication.GenerateToken(connection.ClientId, connection.Key));
                string url = $"/api/v1/videolivenessdetection";
                if (verbosity > Verbosity.Quiet) { Console.WriteLine($"Calling {url} at {connection.Host} ..."); }

                // Build the request payload with the video file.
                var request = new Json.VideoLivenessDetectionRequest { Video = Convert.ToBase64String(File.ReadAllBytes(files.First().FullName)) };

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
        /// <param name="files">The input image files for liveness detection.</param>
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
    }
}
