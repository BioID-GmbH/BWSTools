using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace BioID.JwtGenerator
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // define possible options and arguments
            var subOption = new Option<string>("--sub", "The subject of the generated JWT (your BWS client ID or your email, ...).") { IsRequired = true }; subOption.AddAlias("-s");
            var keyOption = new Option<string>("--key", "A base64 encoded signing key. Should have a length of at least 64 bytes.") { IsRequired = true }; keyOption.AddAlias("-k");
            var expiryOption = new Option<int>("--expiry", "The expiration time of the generated token in minutes."); expiryOption.SetDefaultValue(5);
            var issOption = new Option<string>("--iss", "The issuer of the generated JWT. Defaults to the subject.");
            var audOption = new Option<string>("--aud", "The audience for the generated token."); audOption.SetDefaultValue("BWS");
            var algOption = new Option<string>("--alg", "The cryptographic algorithm used for the JWS, see https://datatracker.ietf.org/doc/html/rfc7518#section-3");
            algOption.SetDefaultValue(SecurityAlgorithms.HmacSha256);

            // define command and handler
            var rootCommand = new RootCommand("BioID JWT generator mainly for use with BWS 3.")
            {
                subOption, keyOption, expiryOption, issOption, audOption, algOption
            };
            rootCommand.SetHandler(GenerateToken, subOption, keyOption, expiryOption, issOption, audOption, algOption);

            // finally invoke the command
            return await rootCommand.InvokeAsync(args);
        }

        internal static void GenerateToken(string subject, string key, int expireMinutes, string issuer, string audience, string algorithm)
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
