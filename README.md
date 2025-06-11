# BWSTools

BWSTools is a command-line tool for interacting with the **BioID Web Service 3 (BWS 3)**. It includes:

- **BWS CLI**: A CLI tool for calling BWS 3 APIs.
- **JWT**: A helper tool to generate JWT tokens for BWS 3 authentication.

## 📋 Table of Contents

- [BWS CLI](#bwscli)
  - [Usage](#usage)
  - [Commands](#commands)
    - [gRPC API](#grpc)
      - [livedetect](#livedetect)
      - [videolivedetect](#videolivedetect)
      - [photoverify](#photoverify)
    - [Face Recognition API](#facerecognition)
      - [enroll](#enroll)
      - [verify](#verify)
      - [search](#search)
      - [settags](#settags)
      - [gettemplate](#gettemplate)
      - [classcount](#classcount)
      - [deletetemplate](#deletetemplate)      
- [JWT Tool](#jwt)

<h2 id="bwscli">🧪 BWS CLI</h2>
This is a simple command-line tool for the BioID Web Service, designed to easily test BWS 3 installations.
To use the BWS CLI tool and successfully call BWS APIs, you will need:

- the corresponding **Client ID**, and
- the **base64-encoded client key (JWT Token)**.

If you do not already have these credentials, the [JWT](#jwt) explains in detail how to generate a suitable token.
<h4 id="usage">Usage</h4>

The general syntax for running the bws cli tool is:
```bash
bws [command] [options]
```
#### 📚 Commands
The CLI tool supports the following commands:

- **`healthcheck`**: Call the health check API. The HealthCheck API provides real-time health status of the BWS service API.

    | Options           | Description               | Required |
    |-------------------|---------------------------|----------|
    | `--host`          | URL of the BWS to call    | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls     | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |

    Examples:
    + gRPC healthcheck:
    ```bash
    bws healthcheck --host https://bwsapiendpoint
    ```
    + RESTful healthcheck:
    ```bash
    bws healthcheck --rest --host https://bwsapiendpoint
    ```
<h3 id="grpc">gRPC API (LivenessDetection, VideoLivenessDetection, PhotoVerify)</h3>

- <h4 id="livedetect">livedetect</h4>

   Call the LivenessDetection API, requiring one (passive live detection) or two (active live detection) live images.

    | Arguments         | Description                                                                         |
    |-------------------|-------------------------------------------------------------------------------------|
    | `files`           | List of image files to process.

    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No      |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | ❌ No      |
    | `--challenge`     | Optional head motion direction for the challenge response liveness detection mode (right, left, up, down). Requires two live images  | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |

      Examples:
    + Passive liveness detection with *diagnostic* output level:
    ```bash
    bws livedetect --host https://bwsapiendpoint -v Diagnostic yourfilepath --clientid yourbwsclientid --key yourbwssecret
    ```
    + Active liveness detection via RESTful API:
    ```bash
    bws livedetect --host https://bwsapiendpoint --rest yourfilepath1 yourfilepath2 --clientid yourbwsclientid --key yourbwssecret
    ```
---
- <h4 id="videolivedetect">videolivedetect</h4>
    Call the VideoLivenessDetection API to analyze a video file and detect if the content contains a live subject.

    | Arguments       | Description                                                                       |
    |-----------------|-----------------------------------------------------------------------------------|
    | `files`         | video file to process
   
    | Options           | Description                                                          | Required |
    |-------------------|----------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                               | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                           | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                      | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                | ❌ No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)             | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |

    Examples:
    + videolivenessdetection:
    ```bash
    bws videolivedetect --host https://bwsapiendpoint yourfilepath --clientid yourbwsclientid --key yourbwssecret
    ```
---
- <h4 id="photoverify">photoverify</h4>
    Calls the PhotoVerify API, which requires one or two live images and one ID photo, to verify whether the given photo matches a specific verification criterion.

    | Arguments       | Description                                                                           |
    |-----------------|---------------------------------------------------------------------------------------|
    | `files`         | List of image files to process.


    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--photo`         | ID photo input file                                                      | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | ❌ No       |
    | `--disablelive`   | Disable liveness detection with PhotoVerify API                          | ❌ No       |
    | `--challenge`     | Optional head motion direction for the challenge response liveness detection mode (right, left, up, down). Requires two live images  | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |

    Examples:
    + photoverify via RESTful API:
    ```bash
    bws photoverify --host https://bwsapiendpoint --rest yourfilepath1 yourfilepath2 --photo youridphotopath --clientid yourbwsclientid --key yourbwssecret
    ```

<h3 id="facerecognition">Face Recognition API (Enroll, Verify, Search, SetTemplateTags, GetTemplateStatus, GetClassCount, DeleteTemplate)</h3>
<p>The FaceRecognition APIs, which compares the facial characteristics of a person with a stored version of those characteristics.
It can compare against a single biometric template for user verification purposes or against multiple templates to search for a specific user within a set of persons.</p>  
 
- <h4 id="enroll">enroll</h4>
    Call into the FaceEnrollment API, requires one or more images.

    | Arguments       | Description                                                                           |
    |-----------------|---------------------------------------------------------------------------------------|
    | `files`         | List of image files to process.


    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |
    | `--classid` `-i`  | A unique class ID of the person associated with the template             | ❌ No       |

    Examples:
    + Biometric enrollment of a single class with *diagnostic* output level via RESTful API:
    ```bash
    bws --enroll yourfilepath --host https://bwsapiendpoint -v Diagnostic -r --clientid yourbwsclientid --key yourbwssecret --classid yourclassid
    ```
- <h4 id="verify">verify</h4>
    Call into the FaceVerification API, requires exactly one image.

    | Arguments       | Description                                                                           |
    |-----------------|---------------------------------------------------------------------------------------|
    | `files`         | List of image files to process.


    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |
    | `--classid` `-i`  | A unique class ID of the person associated with template                 | ❌ No       |

    Examples:
    + One-to-one comparison of the uploaded face image with *diagnostic* output level via RESTful API:
    ```bash
    bws --verify yourfilepath --host https://bwsapiendpoint -v Diagnostic -r --clientid yourbwsclientid --key yourbwssecret --classid yourclassid
    ```

- <h4 id="search">search</h4>
    Call into the Face Search API, requires one or more images.

    | Arguments       | Description                                                                           |
    |-----------------|---------------------------------------------------------------------------------------|
    | `files`         | List of image files to process.


    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | ❌ No      |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |
    | `--tags`          | A list of tags associated with a biometric template.                     | ❌ No      |

    Examples:
    + One-to-many comparison of the uploaded face image with *diagnostic* output level via RESTful API:
    ```bash
    bws --search yourfilepath --host https://bwsapiendpoint -v Diagnostic -r --clientid yourbwsclientid --key yourbwssecret --tags yourtags
    ```
- <h4 id="settags">settags</h4>
    Associate tags with a biometric template.

    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |
    | `--classid` `-i`  | A unique class ID of the person associated with template                 | ❌ No       |
    | `--tags`          | A list of tags associated with a biometric template.                     | ❌ No       |

    Examples:
    + Set tags to template with *diagnostic* output level via RESTful API:
    ```bash
    bws --settags --host https://bwsapiendpoint -v Diagnostic -r --clientid yourbwsclientid --key yourbwssecret --classid yourclassid --tags yourtags
    ```
- <h4 id="gettemplate">gettemplate</h4>
    Fetch the status of a biometric template (together with enrolled thumbs, if available).

    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |
    | `--classid` `-i`  | A unique class ID of the person associated with template                 | ❌ No       |

    Examples:
    + Get status of face template with *diagnostic* output level via RESTful API:
    ```bash
    bws --gettemplate --host https://bwsapiendpoint -v Diagnostic -r --clientid yourbwsclientid --key yourbwssecret --classid yourclassid
    ```
- <h4 id="classcount">classcount</h4>
    Fetch the number of enrolled classes.

    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |
    | `--tags`          | A list of tags associated with a biometric template.                     | ❌ No       |

    Examples:
    + Get the number of classes with *diagnostic* output level via RESTful API:
    ```bash
    bws --classcount --host https://bwsapiendpoint -v Diagnostic -r --clientid yourbwsclientid --key yourbwssecret --tags yourtags
    ```

- <h4 id="deletetemplate">deletetemplate</h4>
    Delete a biometric template.

    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | ✅ Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | ✅ Yes      |
    | `--key`           | Your base64 encoded signing key                                          | ✅ Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | ❌ No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | ❌ No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | ❌ No       |
    | `--classid` `-i`  | A unique class ID of the person associated with template                 | ❌ No       |

    Examples:
    + Deletes all information associated with the provided class ID:
    ```bash
    bws --deletetemplate --host https://bwsapiendpoint --clientid yourbwsclientid --key yourbwssecret --classid yourclassid
    ```

<h2 id="jwt">🔐 JWT - JSON Web Tokens</h2>

JSON Web Tokens are an open, industry-standard RFC 7519 method for securely representing claims between two parties.
The JWT tool can be used to create JSON web tokens for authentication with various BWS 3 services:
* BWS Management API: Use your username as subject and your personal API key as signing key (this information can be found in the [BWS Portal][BWSPortal] under your user profile)
* BWS 3 Client: Use the desired client-ID as the subject and one of the client keys associated with this client as the signing key (this information can be found in the [BWS Portal][BWSPortal] under your BWS 3 client)

## Before you start, you need access to a BWS 3 client

> #### If you do not have access, follow these steps
>
> - You need a **BioID Account** with a **confirmed** email address. If you don’t have one, [create a BioID account][bioidaccountregister].
> - Once you’ve created your BioID account, you can create a free [trial subscription][trial] or the BioID Web Service (BWS 3).
> - After signing in to the BWS Portal and creating the trial subscription using the wizard, you’ll need to create a ***BWS 3 client***.
> - The client can be created using the client creation wizard.

### Technical information about
- [**BioID Web Service (BWS 3)**][BWS3]

[bioidaccountregister]: https://account.bioid.com/Account/Register "Register a BioID account"
[BWSPortal]: https://bwsportal.bioid.com/
[trial]: https://bwsportal.bioid.com/ "Create a free trial subscription"
[BWS3]: https://developer.bioid.com/BWS/NewBws

