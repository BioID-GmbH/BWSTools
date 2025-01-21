using System.Text.Json.Serialization;

/// <summary>
/// Provides JSON serialization contexts for various request and response types used in the BWS API. 
/// The source generator is used because the reflection mechanism is not applicable in AOT (Ahead-of-Time) compilation.
/// </summary>
namespace Bws
{

    /// <summary>
    /// JSON serialization context for <see cref="Json.LivenessDetectionRequest"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Json.LivenessDetectionRequest))]
    public partial class LivenessDetectionRequestContext : JsonSerializerContext
    {
    }

    /// <summary>
    ///  JSON serialization context for <see cref="Json.LivenessDetectionResponse"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
    [JsonSerializable(typeof(Json.LivenessDetectionResponse))]
    public partial class LivenessDetectionResponseContext : JsonSerializerContext
    {
    }

    /// <summary>
    ///  JSON serialization context for <see cref="Json.PhotoVerifyRequest"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Json.PhotoVerifyRequest))]
    internal partial class PhotoVerifyRequestContext : JsonSerializerContext
    {
    }

    /// <summary>
    ///  JSON serialization context for <see cref="Json.PhotoVerifyResponse"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
    [JsonSerializable(typeof(Json.PhotoVerifyResponse))]
    internal partial class PhotoVerifyResponseContext : JsonSerializerContext
    {
    }

    /// <summary>
    ///  JSON serialization context for <see cref="Json.VideoLivenessDetectionRequest"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Json.VideoLivenessDetectionRequest))]
    internal partial class VideoLivenessDetectionRequestContext : JsonSerializerContext
    {
    }


}
