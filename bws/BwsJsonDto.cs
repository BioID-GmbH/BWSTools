﻿// Copyright (c) 2024 BioID GmbH
//
// Implementation of the JSON data transfer objects as defined with the BWS 3
// protobuf messages and used by the RESTful JSON APIs.
// For a description of the elements please refer to the BWS 3 API reference
// at https://developer.bioid.com/BWS/NewBws

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bws.Json
{
    /// <summary>
    /// The <c>LivenessDetection</c> request object. See also
    /// <seealso href="https://developer.bioid.com/bws/restful/livenessdetection">
    /// RESTful JSON API LivenessDetection</seealso>
    /// </summary>
    public class LivenessDetectionRequest
    {
        /// <summary>
        /// A list of one or two live images. If a second image is provided, the
        /// tags of these images can optionally be used for challenge-response.
        /// </summary>
        public List<ImageData> LiveImages { get; set; } = [];
    }

    /// <summary>
    /// The <c>LivenessDetection</c> response object. See also
    /// <seealso href="https://developer.bioid.com/bws/restful/livenessdetection">
    /// RESTful JSON API LivenessDetection</seealso> or 
    /// <seealso href="https://developer.bioid.com/bws/restful/videolivenessdetection">
    /// RESTful JSON API VideoLivenessDetection</seealso>.
    /// </summary>
    public class LivenessDetectionResponse
    {
        /// <summary>
        /// The status of the BWS job that processed the request.
        /// </summary>
        public JobStatus Status { get; set; }
        /// <summary>
        /// A list of errors that might have occurred while the request has been processed.
        /// </summary>
        public List<JobError> Errors { get; set; } = [];
        /// <summary>
        /// The calculated image properties for each of the processed images.
        /// </summary>
        public List<ImageProperties> ImageProperties { get; set; } = [];
        /// <summary>
        /// The liveness decision made by BWS.
        /// </summary>
        public bool Live { get; set; }
        /// <summary>
        /// An informative liveness score (a value between 0.0 and 1.0) that
        /// reflects the confidence level of the live decision.
        /// The higher the score, the more likely the person is a live person. 
        /// </summary>
        public double LivenessScore { get; set; }
    }

    /// <summary>
    /// The <c>VideoLivenessDetection</c> request object. See also
    /// <seealso href="https://developer.bioid.com/bws/restful/videolivenessdetection">
    /// RESTful JSON API VideoLivenessDetection</seealso>
    /// </summary>
    public class VideoLivenessDetectionRequest
    {
        /// <summary>
        /// The binary input video data, base64 encoded.
        /// </summary>
        public string Video { get; set; } = string.Empty;
    }

    /// <summary>
    /// The <c>PhotoVerify</c> request object. See also
    /// <seealso href="https://developer.bioid.com/bws/grpc/photoverify">
    /// RESTful JSON API PhotoVerify</seealso>
    /// </summary>
    public class PhotoVerifyRequest
    {
        /// <summary>
        /// A list of one or two live images. If two images are provided and 
        /// liveness detection is not disabled, the tags of the second image
        /// can optionally be used for challenge-response.
        /// </summary>
        public List<ImageData> LiveImages { get; set; } = [];
        /// <summary>
        /// The ID-photo image.
        /// </summary>
        public required string Photo { get; set; }
        /// <summary>
        /// By default this API automatically calls into the <c>LivenessDetection</c>
        /// API with the provided<see cref="LiveImages"/>. If you do not want to
        /// perform a liveness detection at all, simply set this flag to <c>true</c>.
        /// </summary>
        public bool DisableLivenessDetection { get; set; }
    }

    /// <summary>
    /// The <c>PhotoVerify</c> response object. See also
    /// <seealso href="https://developer.bioid.com/bws/grpc/photoverify">
    /// RESTful JSON API PhotoVerify</seealso>.
    /// </summary>
    public class PhotoVerifyResponse
    {
        /// <summary>
        /// The status of the BWS job that processed the request.
        /// </summary>
        public JobStatus Status { get; set; }
        /// <summary>
        /// A list of errors that might have occurred while the request has been processed.
        /// </summary>
        public List<JobError> Errors { get; set; } = [];
        /// <summary>
        /// The calculated image properties for each of the provided live images
        /// in the given order.
        /// </summary>
        public List<ImageProperties> ImageProperties { get; set; } = [];
        /// <summary>
        /// The calculated image properties of the provided ID photo.
        /// </summary>
        public ImageProperties PhotoProperties { get; set; } = new();
        /// <summary>
        /// The actual level of accuracy the specified photo complies with.
        /// We recommend not to accept verified persons with low verification levels.
        /// </summary>
        public AccuracyLevel VerificationLevel { get; set; }
        /// <summary>
        /// An informative verification score (a value between 0.0 and 1.0) that
        /// reflects the verification level. The higher the score, the more
        /// likely the live images and ID photo belong to the same person.
        /// If this score is exactly 0.0, it has not been calculated.
        /// </summary>
        public double VerificationScore { get; set; }
        /// <summary>
        /// The liveness decision mabe by BWS in case liveness detection is not disabled.
        /// </summary>
        public bool Live { get; set; }
        /// <summary>
        /// An informative liveness score (a value between 0.0 and 1.0) that
        /// reflects the confidence level of the live decision.
        /// The higher the score, the more likely the person is a live person. 
        /// </summary>
        public double LivenessScore { get; set; }
        /// <summary>
        /// Verification accuracy levels.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter<AccuracyLevel>))]
        public enum AccuracyLevel { NOT_RECOGNIZED = 0, LEVEL_1, LEVEL_2, LEVEL_3, LEVEL_4, LEVEL_5 }
    }

    /// <summary>
    /// Each API call returns a <see href="https://developer.bioid.com/bws/grpc/jobstatus">JobStatus</see>
    /// to indicate, whether the job completed execution successfully or aborted due to any reason.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<JobStatus>))]
    public enum JobStatus { SUCCEEDED = 0, FAULTED = 1, CANCELLED = 2 }

    /// <summary>
    /// An <see href="https://developer.bioid.com/bws/grpc/JobError">error</see>
    /// reported by an API call during job processing.
    /// </summary>
    public class JobError
    {
        /// <summary>
        ///  The error-code identifying the reported error message.
        ///  The code is intended to uniquely identify this error.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;
        /// <summary>
        /// The error message describing the error. 
        /// This is a plain text message. It is in english by default.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// The <see href="https://developer.bioid.com/bws/grpc/ImageData">ImageData</see>
    /// object is used to send captured live images or loaded photo images to
    /// the BioID Web Service.
    /// Optionally some tags can be associated with each uploaded image.
    /// </summary>
    public class ImageData
    {
        /// <summary>
        /// The binary image data, base64 encoded.
        /// </summary>
        public required string Image { get; set; }
        /// <summary>
        /// Depending on the API, an image can be tagged to allow different usage scenarios.
        /// </summary>
        public List<string> Tags { get; set; } = [];
    }

    /// <summary>
    /// The <see href="https://developer.bioid.com/bws/grpc/ImageProperties">ImageProperties</see>
    /// object contains the properties of an image as calculated by the
    /// BioID Web Service for a single input image of video frame.
    /// </summary>
    public class ImageProperties
    {
        /// <summary>
        /// Rotation of the input image.
        /// </summary>
        public int Rotated { get; set; }
        /// <summary>
        /// List of faces found in the image.
        /// </summary>
        public List<Face> Faces { get; set; } = [];
        /// <summary>
        /// An optionally calculated quality assessment score.
        /// </summary>
        public double QualityScore { get; set; }
        /// <summary>
        /// List of quality checks and other checks performed.
        /// </summary>
        public List<QualityAssessment> QualityAssessments { get; set; } = [];
        /// <summary>
        /// Optional frame number.
        /// </summary>
        public int FrameNumber { get; set; }
    }

    /// <summary>
    /// Description of a <see href="https://developer.bioid.com/bws/grpc/QualityAssessment">
    /// face image quality check</see> that has been performed.
    /// </summary>
    public class QualityAssessment
    {
        /// <summary>
        /// The quality check performed.
        /// </summary>
        public string Check { get; set; } = string.Empty;
        /// <summary>
        /// The outcome of the quality check. This is a score int the range between
        /// 0.0 and 1.0. The higher the value, the better the check was passed.
        /// </summary>
        public double Score { get; set; }
        /// <summary>
        /// A text with additional info about this quality assessment.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// The <see cref="https://developer.bioid.com/bws/grpc/ImageProperties">Face</see>
    /// object describes some landmarks of a found face within an image together
    /// with some optional scores generated by additional DCNNs.
    /// </summary>
    /// <remarks>
    /// Note that the face image is typically mirrored, i.e. the right eye is
    /// on the left side of the image and vice versa!
    /// </remarks>
    public class Face
    {
        /// <summary>
        /// Position of the center of the left eye.
        /// </summary>
        public required PointD LeftEye { get; set; }
        /// <summary>
        /// Position of the center of the right eye.
        /// </summary>
        public required PointD RightEye { get; set; }
        /// <summary>
        /// A score in the range ]0.0, 1.0] for the probability that the
        /// detected face is from a live person. The higher the score, the more
        /// likely the person is a live person. A value of 0.0 indicates, that
        /// a liveness detection was not performed on this face yet (or failed).
        /// </summary>
        public double TextureLivenessScore { get; set; }
        /// <summary>
        /// A score in the range ]0.0, 1.0] for the probability that the face
        /// motion (calculated with the help of previous images) is a natural
        /// 3D motion. The higher the score, the more likely the motion is in 3D.
        /// A value of 0.0 indicates that this score has not been calculated.
        /// </summary>
        public double MotionLivenessScore { get; set; }
        /// <summary>
        /// Calculated movement direction of this face relative to the position
        /// of this face in a previous image in the range ]0, 360] degrees.
        /// A value of 0.0 indicates that no movement direction has been calculated.
        /// </summary>
        public double MovementDirection { get; set; }
    }

    /// <summary>
    /// Represents an ordered pair of double precision floating point x- and
    /// y-coordinates that defines a point in a two-dimensional plane.
    /// </summary>
    public class PointD
    {
        /// <summary>
        /// The x-coordinate of the point.
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// The y-coordinate of the point.
        /// </summary>
        public double Y { get; set; }
    }

    //--------------------------------------------------------
    // Face Recognition
    //--------------------------------------------------------

    /// <summary>
    /// The <c>Enroll</c> response object. See also
    /// <seealso href="https://developer.bioid.com/bws/restful/enroll">
    /// JSON Web API Enroll</seealso>.
    /// </summary>
    public class FaceEnrollmentResponse
    {
        /// <summary>
        /// The status of the BWS job that processed the request.
        /// </summary>
        public JobStatus Status { get; set; }
        /// <summary>
        /// A list of errors that might have occurred while the request has been processed.
        /// </summary>
        public List<JobError> Errors { get; set; } = [];
        /// <summary>
        /// The calculated image properties for each of the provided live images
        /// in the given order.
        /// </summary>
        public List<ImageProperties> ImageProperties { get; set; } = [];
        /// <summary>
        /// The actual action performed.
        /// </summary>
        public EnrollmentAction PerformedAction { get; set; }
        /// <summary>
        /// Number of newly enrolled images.
        /// </summary>
        public int EnrolledImages { get; set; }
        /// <summary>
        /// The FaceTemplateStatus of the generated face template without any face thumbnails.
        /// </summary>
        public FaceTemplateStatus TemplateStatus { get; set; } = new();
        /// <summary>
        /// Possible enrollment actions.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter<EnrollmentAction>))]
        public enum EnrollmentAction { NONE = 0, NEW_TEMPLATE_CREATED, TEMPLATE_UPDATED, TEMPLATE_UPGRADED, ENROLLMENT_FAILED = -1 }
    }

    /// <summary>
    /// The <c>Verify</c> response object. See also
    /// <seealso href="https://developer.bioid.com/bws/restful/verify">
    /// JSON Web API Verify</seealso>.
    /// </summary>
    public class FaceVerificationResponse
    {
        /// <summary>
        /// The status of the BWS job that processed the request.
        /// </summary>
        public JobStatus Status { get; set; }
        /// <summary>
        /// A list of errors that might have occurred while the request has been processed.
        /// </summary>
        public List<JobError> Errors { get; set; } = [];
        /// <summary>
        /// The calculated image properties for the provided image.
        /// </summary>
        public ImageProperties ImageProperties { get; set; } = new();
        /// <summary>
        /// Verification decission made by BWS.
        /// </summary>
        public bool Verified { get; set; }
        /// <summary>
        /// The calculated verification score (a value between 0.0 and 1.0) that
        /// led to the verified decision. The higher the score, the more likely
        /// the face in the image and the template referenced by the class ID
        /// belong to the same person.
        /// </summary>
        public double Score { get; set; }
    }

    /// <summary>
    /// The <c>Search</c> request object. See also
    /// <seealso href="https://developer.bioid.com/bws/restful/search">
    /// JSON Web API Search</seealso>
    /// </summary>
    public class FaceSearchRequest
    {
        /// <summary>
        /// An array of one or more base64 encoded facial images of the persons
        /// to search for. Each image has to contain exactly a single face.
        /// </summary>
        public List<ImageData> Images { get; set; } = [];

        /// <summary>
        /// List of tags that need to be assigned to the biometric templates to
        /// search. Only templates that have all of these tags applied are considered
        /// in the search. If no tag is specified, all available templates are searched.
        /// </summary>
        public List<string> Tags { get; set; } = [];
    }

    /// <summary>
    /// The <c>Search</c> request object. See also
    /// <seealso href="https://developer.bioid.com/bws/restful/search">
    /// JSON Web API Search</seealso>
    /// </summary>
    public class FaceSearchResponse
    {
        /// <summary>
        /// The status of the BWS job that processed the request.
        /// </summary>
        public JobStatus Status { get; set; }
        /// <summary>
        /// A list of errors that might have occurred while the request has been processed.
        /// </summary>
        public List<JobError> Errors { get; set; } = [];
        /// <summary>
        /// The calculated image properties for each of the provided images
        /// in the given order.
        /// </summary>
        public List<ImageProperties> ImageProperties { get; set; } = [];
        /// <summary>
        /// For each input image a <see cref="SearchResult"/> is created and
        /// returned in the order of the provided input images
        /// </summary>
        public List<SearchResult> Result { get; set; } = [];
    }

    /// <summary>
    /// Face recognition one-to-many search result.
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// A sorted list (by score) of <see cref="TemplateMatchResult"/>s.
        /// </summary>
        public List<TemplateMatchResult> Matches { get; set; } = [];
    }

    /// <summary>
    /// A single one-to-many match result.
    /// </summary>
    public class TemplateMatchResult
    {
        /// <summary>
        /// Class ID of the person the matched template belongs to.
        /// </summary>
        public long ClassId { get; set; }
        /// <summary>
        /// The calculated similarity score for this class in the range between
        /// 0.0 and 1.0. The higher the score, the more likely the template
        /// matches the calculated feature vector.
        /// </summary>
        public double Score { get; set; }
    }

    /// <summary>
    /// The status of a biometric face template as managed by BWS.
    /// </summary>
    public class FaceTemplateStatus
    {
        /// <summary>
        /// Unique class ID associated with the template.
        /// </summary>
        public long ClassId { get; set; }

        /// <summary>
        /// Is there a template stored for the this class?
        /// </summary>
        public bool Available { get; set; }

        /// <summary>
        /// When has the template been generated the last time.
        /// </summary>
        public string Enrolled { get; set; } = string.Empty;

        /// <summary>
        /// List of tags stored with the template.
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// The version of the encoder used to calculate the feature vectors for this template.
        /// </summary>
        public int EncoderVersion { get; set; }

        /// <summary>
        /// The number of feature vectors (created from the enrolled face images)
        /// that have been calculated, stored and used to generate the template.
        /// </summary>
        public int FeatureVectors { get; set; }

        /// <summary>
        /// The number of thumbnails (created from the enrolled face images)
        /// that have been stored with the template.
        /// </summary>
        public int ThumbnailsStored { get; set; }

        /// <summary>
        /// If thumbnails have been stored and they have been requested (with a
        /// call to <c>GetTemplateStatus</c>), those are returned with this field.
        /// </summary>
        public List<Thumbnail> Thumbnails { get; set; } = [];

        public class Thumbnail
        {
            /// <summary>
            /// Timestamp when the image was enrolled.
            /// </summary>
            public string Enrolled { get; set; } = string.Empty;

            /// <summary>
            /// The thumbnail serialized as base64 PNG.
            /// </summary>
            public string Image { get; set; } = string.Empty;
        }
    }
}
