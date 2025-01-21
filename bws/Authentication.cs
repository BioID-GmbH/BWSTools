using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bws
{
    internal static class Authentication
    {
        /// <summary>
        /// Creates an instance of an authenticated HttpClient with the specified host and token.
        /// The HttpClient is configured with a Bearer token for authorization. 
        /// "Reference-Number" header for tracking purposes.
        /// </summary>
        /// <param name="host">The base URI for the HttpClient.</param>
        /// <param name="token">The Bearer token used for authentication.</param>
        /// <returns>An HttpClient instance with authentication and custom headers configured.</returns>
        public static HttpClient CreateAuthenticatedClient(Uri host, string token)
        {
            var httpClient = new HttpClient { BaseAddress = host };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpClient.DefaultRequestHeaders.Add("Reference-Number", $"bws-cli-restful-{DateTime.UtcNow.Ticks}");
            return httpClient;
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
        public static GrpcChannel CreateAuthenticatedChannel(Uri host, string token)
        {
            // We want to use JWT Bearer token authentication for each call (i.e. CallCredentials)
            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                metadata.Add("Reference-Number", $"bws-cli-grpc-{DateTime.UtcNow.Ticks}");
                metadata.Add("Authorization", $"Bearer {token}");
                return Task.CompletedTask;
            });

            // create the GRPC channel with our call credentials
            bool insecure = host.Scheme != Uri.UriSchemeHttps;
            // Create the GRPC channel with our call credentials
            return GrpcChannel.ForAddress(host, new GrpcChannelOptions
            {
                // TLS is highly recommended, but in case of an insecure connection we allow the use of the JWT nonetheless
                Credentials = ChannelCredentials.Create(insecure ? ChannelCredentials.Insecure : ChannelCredentials.SecureSsl, credentials),
                UnsafeUseInsecureChannelCallCredentials = insecure,
                // defaults to 4 MB, which might be not enough to receive images
                MaxReceiveMessageSize = int.MaxValue
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
        public static string GenerateToken(string clientId, string key, int expireMinutes = 20)
        {
            var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = clientId };
            var descriptor = new SecurityTokenDescriptor { Claims = claims, Issuer = clientId, Audience = "BWS", SigningCredentials = credentials };
            var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = true, TokenLifetimeInMinutes = expireMinutes };
            string jwt = handler.CreateToken(descriptor);
            return jwt;
        }
    }
}
