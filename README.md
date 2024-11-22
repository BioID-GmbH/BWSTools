# BWSTools
Here are some useful tools for using the BioID Web Service 3 (BWS 3).

## BWS CLI
This is a simple BWS command-line interface that can be used to easily test BioID Web Service (BWS 3) installations.

## JWT - JSON Web Tokens
JSON Web Tokens are an open, industry standard RFC 7519 method for representing claims securely between two parties.
The JWT tool can be used to create JSON web tokens for authentication with various BWS 3 services:
* BWS Management API: Use your username as subject and your personal API key as signing key (you find this information in the [BWS Portal][BWSPortal] with your user profile)
* BWS 3 Client: Use the desired client-ID as subject and one of the client keys associated with this client as signing key (you find this information in the [BWS Portal][BWSPortal] with your BWS 3 client)

## Before you start, you need access to a BWS 3 client

> #### If you do not have access, follow these steps
>
> - You need a **BioID Account** with a **confirmed** email address. If you don’t have one, [create a BioID account][bioidaccountregister].
> - You can create a free [trial subscription][trial] of the BioID Web Service (BWS 3) once you've created your BioID account.
> - After you have signed in to the BWS Portal and created the trial subscription with the help of a wizard, you still need to create a ***BWS 3 client***.
> - The client can be created with the help of a creation wizard.

### Technical information about
- [**BioID Web Service (BWS 3)**][BWS3]

[bioidaccountregister]: https://account.bioid.com/Account/Register "Register a BioID account"
[BWSPortal]: https://bwsportal.bioid.com/
[trial]: https://bwsportal.bioid.com/ "Create a free trial subscription"
[BWS3]: https://developer.bioid.com/BWS/NewBws
