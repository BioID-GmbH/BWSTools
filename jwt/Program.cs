using System.CommandLine;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BioID.JwtGenerator
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // define possible options and arguments
            var subOption = new Option<string>("--sub", "-s") { Description = "The subject of the generated JWT (your BWS client ID or your email, ...).",  Required = true };
            var keyOption = new Option<string>("--key", "-k") { Description = "A base64 encoded signing key. Should have a length of at least 64 bytes.", Required = true }; 
            var expiryOption = new Option<int>("--expiry") { Description = "The expiration time of the generated token in minutes.", DefaultValueFactory = _ => 5 };
            var issOption = new Option<string>("--iss") { Description = "The issuer of the generated JWT. Defaults to the subject." };
            var audOption = new Option<string>("--aud") { Description = "The audience for the generated token.", DefaultValueFactory = _ => "BWS" };
            var algOption = new Option<string>("--alg") { Description = "The cryptographic algorithm used for the JWS, see https://datatracker.ietf.org/doc/html/rfc7518#section-3", DefaultValueFactory = _ => SecurityAlgorithms.HmacSha256 };

            // define command and handler
            var rootCommand = new RootCommand("BioID JWT generator mainly for use with BWS 3.")
            {
                subOption, keyOption, expiryOption, issOption, audOption, algOption
            };

            rootCommand.SetAction(parseResult =>
            {
                var subject = parseResult.GetValue(subOption) ?? "";
                var key = parseResult.GetValue(keyOption) ?? "";
                var expiry = parseResult.GetValue(expiryOption);
                var issuer = parseResult.GetValue(issOption);
                var audience = parseResult.GetValue(audOption) ?? "";
                var algorithm = parseResult.GetValue(algOption) ?? "";

                GenerateToken(subject, key, expiry, issuer, audience, algorithm);
            });

            // finally invoke the command
            return await rootCommand.Parse(args).InvokeAsync();
        }

        internal static void GenerateToken(string subject, string key, int expireMinutes, string? issuer, string audience, string algorithm)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(key));
                var credentials = new SigningCredentials(securityKey, algorithm);
                var claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = subject };
                var descriptor = new SecurityTokenDescriptor { Claims = claims, Issuer = string.IsNullOrWhiteSpace(issuer) ? subject : issuer, Audience = audience, SigningCredentials = credentials };
                var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = true, TokenLifetimeInMinutes = expireMinutes };
                string jwt = handler.CreateToken(descriptor);
                Console.WriteLine(jwt);
            }

            catch(Exception ex) 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
