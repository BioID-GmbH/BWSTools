using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using BioID.Services;
using Grpc.Core;
using static Grpc.Core.Metadata;

namespace Bws
{
    /// <summary>
    /// Provides utility methods to display metadata, HTTP headers, errors, and other details on the console.
    /// </summary>
    internal static class ConsoleOutput
    {
        /// <summary>
        /// Read the gRPC response headers and display them in the console.
        /// </summary>
        /// <param name="entries">A collection of grpc metadata consisting of key-value pairs.</param>
        /// <param name="title">A title to be displayed in console before the metadata.</param>
        public static void DumpMetadata(Metadata entries, string title)
        {
            if (entries != null && entries.Count != 0)
            {
                Console.WriteLine($"{title}:");
                foreach (var entry in entries)
                {
                    Console.WriteLine($"  {entry.Key}: {entry.Value}");
                }
            }
        }
        public static void DumpHeaders(HttpResponseHeaders headers, string title)
        {
            if (headers.Any())
            {
                Console.WriteLine($"{title}:");
                foreach (var entry in headers)
                {
                    Console.WriteLine($"  {entry.Key}: {string.Join(" - ", entry.Value)}");
                }
            }
        }

        /// <summary>
        /// Outputs a collection of errors to the console if any errors are present.
        /// Each error is displayed with its error code and corresponding message.
        /// </summary>
        /// <param name="errors">A collection of <see cref="JobError"/> objects to be output.</param>
        public static void DumpErrors(IEnumerable<JobError> errors)
        {
            if (errors != null && errors.Any())
            {
                Console.WriteLine("Errors:");
                foreach (var error in errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"  {error.ErrorCode}: {error.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("No errors.");
            }
        }

        /// <summary>
        /// Outputs a collection of errors to the console if any errors are present.
        /// Each error is displayed with its error code and corresponding message.
        /// </summary>
        /// <param name="errors">A collection of <see cref="Json.JobError"/> objects to be output.</param>
        public static void DumpErrors(IEnumerable<Json.JobError> errors)
        {
            if (errors != null && errors.Any())
            {
                Console.WriteLine("Errors:");
                foreach (var error in errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"  {error.ErrorCode}: {error.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("No errors.");
            }
        }

        /// <summary>
        /// Writes a single error message to the console in red text.
        /// </summary>
        /// <param name="error">The error message to display.</param>
        public static void WriteError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(error);
            Console.ResetColor();
        }

        /// <summary>
        /// Outputs a collection of image properties as calculated by the BioID Web Service to the console.
        /// </summary>
        /// <param name="imageProperties">A collection of <see cref="ImageProperties"/> objects to be output</param>
        public static void DumpImageProperties(IEnumerable<ImageProperties> imageProperties)
        {
            foreach (ImageProperties properties in imageProperties) { Console.WriteLine($"ImageProperties: {properties}"); }
        }


        /// <summary>
        /// Outputs a collection of template search results to the console.
        /// </summary>
        /// <param name="matchResults">A collection of one-to-many face recognition search results.</param>
        public static void DumpTemplateMatches(IEnumerable<Json.SearchResult> matchResults)
        {
            foreach (var match in matchResults.SelectMany(matches => matches.Matches))
            {
                Console.WriteLine($"Match: [ClassId: {match.ClassId}] - [Score: {match.Score}]");
            }
        }
    }
}
