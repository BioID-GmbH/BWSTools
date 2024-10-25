# BWSTools
Here are some useful tools for using the BioID Web Service 3 (BWS 3).

## BWS CLI
This is a simple BWS command-line interface that can be used to easily test BWS 3 installations.

## JWT - JSON Web Tokens
JSON Web Tokens are an open, industry standard RFC 7519 method for representing claims securely between two parties.
The JWT tool can be used to create JSON web tokens for authentication with various BWS 3 services:
* BWS Management API: Use your username as subject and your personal API key as signing key (you find this information in the [BWS Portal][BWSPortal] with your user profile)
* BWS 3 Client: Use the desired client-ID as subject and one of the client keys associated with this client as signing key (you find this information in the [BWS Portal][BWSPortal] with your BWS 3 client)

[BWSPortal]: https://bwsportal.bioid.com/
