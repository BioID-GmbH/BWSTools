using System.Collections.Generic;
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
    /// JSON serialization context for <see cref="Json.LivenessDetectionResponse"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
    [JsonSerializable(typeof(Json.LivenessDetectionResponse))]
    public partial class LivenessDetectionResponseContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for <see cref="Json.PhotoVerifyRequest"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Json.PhotoVerifyRequest))]
    internal partial class PhotoVerifyRequestContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for <see cref="Json.PhotoVerifyResponse"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
    [JsonSerializable(typeof(Json.PhotoVerifyResponse))]
    internal partial class PhotoVerifyResponseContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for <see cref="Json.VideoLivenessDetectionRequest"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Json.VideoLivenessDetectionRequest))]
    internal partial class VideoLivenessDetectionRequestContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// List of json serialization context for FaceEnrollment requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(List<Json.ImageData>))]
    internal partial class FaceEnrollRequestContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for <see cref="Json.FaceEnrollmentResponse"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Json.FaceEnrollmentResponse))]
    internal partial class FaceEnrollResponseContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// List of json serialization context for FaceVerification requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(List<Json.ImageData>))]
    internal partial class FaceVerifyRequestContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for <see cref="Json.FaceVerificationResponse"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Json.FaceVerificationResponse))]
    internal partial class FaceVerifyResponseContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for <see cref="Json.FaceSearchRequest"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Json.FaceSearchRequest))]
    internal partial class FaceSearchRequestContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for <see cref="Json.FaceSearchResponse"/> requests.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
    [JsonSerializable(typeof(Json.FaceSearchResponse))]
    internal partial class FaceSearchResponseContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for a list of strings.
    /// </summary>
    [JsonSerializable(typeof(List<string>))]
    public partial class ListStringContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// JSON serialization context for <see cref="Json.FaceTemplateStatus"/> resonse.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true)]
    [JsonSerializable(typeof(Json.FaceTemplateStatus))]
    internal partial class FaceTemplateStatusContext : JsonSerializerContext
    {
    }
}
