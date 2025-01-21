# BWSTools
Here are some useful tools for using the BioID Web Service 3 (BWS 3).

## BWS CLI
This is a simple command-line tool for the BioID Web Service, designed to easily test BWS 3 installations.

### Usage
The general syntax for running the bws cli tool is:
```bash
bws [command] [options]
```
#### Commands
The CLI tool supports the following commands:
- `healthcheck`: Call the health check API. The HealthCheck API provides real-time health status of the BWS service API.

    | Options           | Description               | Required |
    |-------------------|---------------------------|----------|
    | `--host`          | URL of the BWS to call    | Yes      |
    | `--rest` `-r`     | Use RESTful API calls     | No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | No       |

    **Examples:**
    + gRPC healtcheck:
    ```bash
    bws healthcheck --host https://bwsapiendpoint
    ```
    + RESTful healtcheck:
    ```bash
    bws healthcheck --rest --host https://bwsapiendpoint
    ```
- `livedetect`: Call the LivenessDetection API, requiring one (passive live detection) or two (active live detection) live images.

    | Arguments         | Description                                                                                     |
    |-------------------|-------------------------------------------------------------------------------------------------|
    | `files`           | List of image-files to process.

    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | Yes      |
    | `--key`           | Your base64 encoded signing key                                          | Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | No       |
    | `--challenge`     | Optional head motion direction for the challenge response liveness detection mode (right, left, up, down). Requires two live images  | No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | No       |

      **Examples:**
    + Passive liveness detection with *diagnostic* output level:
    ```bash
    bws livedetect --host https://bwsapiendpoint -v Diagnostic yourfilepath --clientid yourbwsclientid --key yourbwssecret
    ```
    + Active liveness detection via RESTful API:
    ```bash
    bws livedetect --host https://bwsapiendpoint --rest yourfilepath1 yourfilepath2 --clientid yourbwsclientid --key yourbwssecret
    ```

- `videolivedetect`: Call the VideoLivenessDetection API to analyze a video file and detect if the content contains a live subject.

    | Arguments       | Description                                                                                     |
    |-----------------|-------------------------------------------------------------------------------------------------|
    | `files`         | video-file to process.
   
    | Options           | Description                                                          | Required |
    |-------------------|----------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                               | Yes      |
    | `--clientid`      | Your BWS Client Identifier                                           | Yes      |
    | `--key`           | Your base64 encoded signing key                                      | Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                | No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)             | No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | No       |

    **Examples:**
    + videolivenessdetection:
    ```bash
    bws videolivedetect --host https://bwsapiendpoint yourfilepath --clientid yourbwsclientid --key yourbwssecret
    ```

- `photoverify`: Calls the PhotoVerify API, which requires one or two live images and one ID photo, to verify whether the given photo matches a specific verification criterion.

    | Arguments       | Description                                                                                     |
    |-----------------|-------------------------------------------------------------------------------------------------|
    | `files`         | List of image-files to process.


    | Options           | Description                                                              | Required |
    |-------------------|--------------------------------------------------------------------------|----------|
    | `--host`          | URL of the BWS to call                                                   | Yes      |
    | `--clientid`      | Your BWS Client Identifier                                               | Yes      |
    | `--key`           | Your base64 encoded signing key                                          | Yes      |
    | `--photo`         | ID photo input file                                                      | Yes      |
    | `--rest` `-r`     | Use RESTful API calls                                                    | No       |
    | `--deadline` `-d` | Optional deadline for the call timeout (in milliseconds)                 | No       |
    | `--disablelive`   | Disable liveness detection with PhotoVerify API                          | No       |
    | `--challenge`     | Optional head motion direction for the challenge response liveness detection mode (right, left, up, down). Require two live images  | No       |
    | `--verbosity` `-v`| The output verbosity mode [default: Normal] (Detailed, Diagnostic, Minimal, Normal, Quiet) | No       |

    **Examples:**
    + photoverify via RESTful API:
    ```bash
    bws photoverify --host https://bwsapiendpoint --rest yourfilepath1 yourfilepath2 --photo youridphotopath --clientid yourbwsclientid --key yourbwssecret
    ```


## JWT - JSON Web Tokens
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

