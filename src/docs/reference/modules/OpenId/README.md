# OpenID (`OrchardCore.OpenId`)

## OpenID Connect Module

`OrchardCore.OpenId` provides the following features:

- Core Components
- Authorization Server
- Management Interface
- Token Validation
- OIDC Client

## Core Components

Registers the core components used by the OpenID module.

## Management Interface

Allows adding, editing and removing the registered applications.

## Authorization Server

Enables authentication of external applications using the OpenID Connect/OAuth 2.0 standards.  
It is based on the [`OpenIddict`](https://github.com/openiddict/openiddict-core) library allowing.  
Orchard Core to act as identity provider to support token authentication without the need of an external identity provider.  

- Orchard Core can also be used as an identity provider for centralizing the user access permissions to external applications.
- Orchard Core services.
  - The authorization server feature maintains its own private JWT/validation handler instance for the userinfo API endpoint. This way, you don't have to enable the token validation feature for current tenant.

Flows supported: [code/implicit/hybrid flows](http://openid.net/specs/openid-connect-core-1_0.html) and [client credentials/resource owner password grants](https://tools.ietf.org/html/rfc6749).

### Configuration

Configuration can be set through the _OpenID Connect_ settings menu in the admin dashboard and also through a recipe step.

Available settings are:

- Token Format:
  - Data Protection: this format - enabled by default - uses non-standard opaque tokens encrypted by the ASP.NET Core Data Protection stack.
  - Json Web Token: this format uses signed JWT standard tokens. The tokens are encrypted by default but access token encryption can be turned off
to allow third-party resource servers to use the JWT tokens produced by the Orchard OpenID server.
- Authority: Orchard URL used by Orchard to act as an identity server.
- Signing Certificate Store Location: CurrentUser/LocalMachine <https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.storelocation(v=vs.110).aspx>
- Signing Certificate Store Name: AddressBook/AuthRootCertificateAuthority/Disallowed/My/Root/TrustedPeople/TrustedPublisher <https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.storename(v=vs.110).aspx>
- Encryption Certificate Thumbprint: the thumbprint of the signing certificate (it is recommended to not use same certificate that is being used for SSL).
- Encryption Certificate Store Location: CurrentUser/LocalMachine <https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.storelocation(v=vs.110).aspx>
- Encryption Certificate Store Name: AddressBook/AuthRootCertificateAuthority/Disallowed/My/Root/TrustedPeople/TrustedPublisher <https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.storename(v=vs.110).aspx>
- Encryption Certificate Thumbprint: the thumbprint of the encryption certificate (it is recommended to not use same certificate that is being used for SSL).
- Enable Token Endpoint.
- Enable Authorization Endpoint.
- Enable Logout Endpoint.
- Enable User Info Endpoint.
- Allow Password Flow: It requires that the Token Endpoint is enabled. More info at <https://tools.ietf.org/html/rfc6749#section-1.3.3> 

Typescript request example using axios:

``` typescript 
  const data: any =
  {
        username: username,
        password: password,
        // must read from config
        client_id: 'client_id',
        client_secret: 'client_secret',
        grant_type: 'password',
        scope: 'profile email phone roles'
  }

  let formData = []
  for (let it in data) {
    // eslint-disable-next-line no-prototype-builtins
    if (data.hasOwnProperty(it)) {
      formData.push(encodeURIComponent(it) + '=' + encodeURIComponent(data[it]))
    }
  } 
  
  var res = axios.request({
        url: ApiUrl + '/connect/token',
        method: 'post',
        data: formData.join('&'),
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
  })
  
  return res
```
Complete example at <https://github.com/JiZhouSoft/Jizhousoft.OrchardCore.Sample/blob/cd1fd0c42893c6d0607f8642451d1a98649b9769/vue-ts-admin/src/api/users.ts#L10>

!!! note
    For `client_secret` and `client_id` as confidential information, do not send it directly through the browser application. The sample code here is only used as a demonstration to send an authentication request through the client.
Generally speaking, we should rewrite an `action` which only accept the username and password parameters, and then returns the verification result.

- Allow Client Credentials Flow: It requires that the Token Endpoint is enabled. More info at <https://tools.ietf.org/html/rfc6749#section-1.3.4>
- Allow Authorization Code Flow: It requires that the Authorization and Token Endpoints are enabled. More info at <http://openid.net/specs/openid-connect-core-1_0.html#CodeFlowAuth>
- Allow Implicit Flow: It requires that the Authorization Endpoint is enabled. More info at <http://openid.net/specs/openid-connect-core-1_0.html#ImplicitFlowAuth>
- Allow Refresh Token Flow: It allows to refresh access token using a refresh token. It can be used in combination with Password Flow, Authorization Code Flow and Hybrid Flow. More info at <http://openid.net/specs/openid-connect-core-1_0.html#RefreshTokens>

A sample of OpenID Connect Settings recipe step:

```json
{
      "name": "OpenIdServerSettings",
      "TestingModeEnabled": false,
      "AccessTokenFormat": "JsonWebToken", // JsonWebToken or DataProtection
      "Authority": "https://www.orchardproject.net",
      "SigningCertificateStoreLocation": "LocalMachine", //More info: https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.storelocation(v=vs.110).aspx
      "SigningCertificateStoreName": "My", //More info: https://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.storename(v=vs.110).aspx
      "SigningCertificateThumbprint": "27CCA66EF38EF46CD9022431FB1FF0F2DF5CA1D7",
      "EncryptionCertificateStoreLocation": "LocalMachine",
      "EncryptionCertificateStoreName": "My",
      "EncryptionCertificateThumbprint": "BC34460ABEA2D576EA68E8FFCFEEB3F45C94FB0F",
      "EnableTokenEndpoint": true,
      "EnableAuthorizationEndpoint": false,
      "EnableLogoutEndpoint": true,
      "EnableUserInfoEndpoint": true,
      "AllowPasswordFlow": true,
      "AllowClientCredentialsFlow": false,
      "AllowAuthorizationCodeFlow": false,
      "AllowRefreshTokenFlow": false,
      "AllowImplicitFlow": false
}
```

### Client OpenID Connect Apps Configuration

OpenID Connect apps can be set through OpenID Connect Apps menu in the admin dashboard (through the Management Interface feature) 
and also through a recipe step.

OpenID Connect apps require the following configuration.

- Id: Unique identifier.
- Client Id: Client identifier of the application. It has to be provided by a client when requesting a valid token.
- Display Name: Display name associated with the current application.
- Type: There are two options:
  - Confidential: Confidential applications MUST send their client secret when communicating with the token and revocation endpoints. This guarantees that only the legit client can exchange an authorization code or get a refresh token.
  - Public: Public applications don't use client secret on their communications.
- Client Secret: Client secret is a password associated with the application. It will be required when the application is configured as Confidential.
- Flows: If general OpenID Connect settings allow this flow, an app can also enable this flow.
  - Allow Password Flow: It requires that the Token Endpoint is enabled. More info at <https://tools.ietf.org/html/rfc6749#section-1.3.3>
  - Allow Client Credentials Flow: It requires that the Token Endpoint is enabled. More info at <https://tools.ietf.org/html/rfc6749#section-1.3.4>
  - Allow Authorization Code Flow: It requires that the Authorization and Token Endpoints are enabled. More info at <http://openid.net/specs/openid-connect-core-1_0.html#CodeFlowAuth>
  - Allow Implicit Flow: It requires that the Authorization Endpoint is enabled. More info at <http://openid.net/specs/openid-connect-core-1_0.html#ImplicitFlowAuth>
  - Allow Refresh Token Flow: It allows to refresh access token using a refresh token. It can be used in combination with Password Flow, Authorization Code Flow and Hybrid Flow. More info at <http://openid.net/specs/openid-connect-core-1_0.html#RefreshTokens>
- Normalized RoleNames: This configuration is only required if Client Credentials Flow is enabled. It determines the roles assigned to the app when it is authenticated using that flow.
- Redirect Options: Those options are only required when Implicit Flow, Authorization Code Flow or Allow Hybrid Flow is required.
- Logout Redirect Uri: logout callback URL.
- Redirect Uri: callback URL.
- Skip Consent: sets whether a consent form has to be completed by the user after log in.
- Advanced Parameters: Allows setting additional parameters that can be sent with the authorize request. Note: The default parameters are set from the options above.

A sample of OpenID Connect App recipe step:

```json
{
      "name": "openidapplication",
      "ClientId": "openidtest",
      "DisplayName": "Open Id Test",
      "Type": "Confidential",
      "ClientSecret": "MyPassword",
      "EnableTokenEndpoint": true,
      "EnableAuthorizationEndpoint": false,
      "EnableLogoutEndpoint": true,
      "EnableUserInfoEndpoint": true,
      "AllowPasswordFlow": true,
      "AllowClientCredentialsFlow": false,
      "AllowAuthorizationCodeFlow": false,
      "AllowRefreshTokenFlow": false,
      "AllowImplicitFlow": false
}
```

### OpenID Connect Scopes Configuration

Scopes can be set through OpenID Connect Scopes menu in the admin dashboard (through the Management Interface feature) and also through a recipe step.

OpenID Connect Scopes require the following configuration.

| Property | Description |
| -------- | ----------- |
| Name | Unique name of the scope. |
| Display Name | Display name associated with the current scope. |
| Description | Describe how this scope is used in the system. |
| Tenants | Build the audience based on tenants names. |
| Additional resources | Build the audience based on the space separated strings provided. |

A sample of OpenID Connect Scope recipe step:

```json
    {
      "name": "OpenIdScope",
      "Description": "A scope to provide audience for remote clients",
      "DisplayName": "External Audience Scope",
      "ScopeName": "custom_scope",
      "Resources": "my_recipient"
    }
```

### Configuring Certificates

#### Windows / IIS

Several tools are available for generating a signing certificate on Windows and/or IIS, for example:

- IIS Server Manager _(offers limited control)_
    1. Server Certificates
    2. Create Self-Signed Certificate
- PowerShell _(offers full control)_
    1. `New-SelfSignedCertificate`, for example:

```powershell
# See https://technet.microsoft.com/en-us/itpro/powershell/windows/pkiclient/new-selfsignedcertificate

New-SelfSignedCertificate `
    -Subject "connect.example.com" `
    -FriendlyName "Example.com Signing Certificate" `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -KeySpec Signature `
    -KeyUsage DigitalSignature `
    -KeyUsageProperty Sign `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1") `
    -KeyExportPolicy NonExportable `
    -KeyAlgorithm RSA `
    -KeyLength 4096 `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddDays(825) `
    -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider"
```

**This snippet must be run as admin.** It generates a 4096-bit signing certificate, stores it in the machine store and returns the certificate's thumbprint, which you need in the OpenID Connect Settings recipe or when exporting the certificate through PowerShell. _You should update this example according to your requirements!_

In multi-node environments consider creating the certificate with `-KeyExportPolicy Exportable`, then export the certificate (PFX) to a secure location, using the MMC Certificates Snap-In or PowerShell `Export-PfxCertificate`, and subsequently import the certificate on each node as non-exportable, which is the default when using `Import-PfxCertificate`. For example:

```powershell
# See https://technet.microsoft.com/en-us/itpro/powershell/windows/pkiclient/export-pfxcertificate
# Run this on the machine where the certificate was generated:

$mypwd = ConvertTo-SecureString -String "MySecretPassword123" -Force -AsPlainText

Export-PfxCertificate -FilePath C:\securelocation\connect.example.com.pfx cert:\localMachine\my\thumbprintfromnewselfsignedcertificate -Password $mypwd

# See https://technet.microsoft.com/en-us/itpro/powershell/windows/pkiclient/import-pfxcertificate
# Run this on the target node:

$mypwd = ConvertTo-SecureString -String "MySecretPassword123" -Force -AsPlainText

Import-PfxCertificate -FilePath C:\securelocation\connect.example.com.pfx cert:\localMachine\my -Password $mypwd
```

**Important:** In order for the `OrchardCore.OpenId` module to use the certificate's keys for signing, it requires `Read` access to the certificate in the store. This can be granted in various ways, for example:

- `MMC.exe`
    1. Add Snap-In 'Certificates' for Computer Account
    2. Right-Click relevant certificate and select All Tasks, Manage Private Keys
    3. Add the relevant identity (e.g. IIS AppPool\PoolName)
        - Add
        - Advanced
        - Locations: Choose iis server machine name
        - Find Now
        - Search Results: Choose your iisServerMachineName\IIS_IUSRS (just one example)
        - OK
    4. Check Allow Read under Permissions
- `WinHttpCertCfg.exe` (grants Full Control)
    1. For example: `winhttpcertcfg -g -c LOCAL_MACHINE\My -s connect.example.com -a AppPoolIdentityName` <https://msdn.microsoft.com/en-us/library/windows/desktop/aa384088(v=vs.85).aspx>

### Use the Certificate in Azure

To use the certificate on an Azure hosted site.

1. Upload the certificate to the 'TLS/SSL settings' page of the site Azure portal page.
2. Add a new entry to the Azure site setting page with the following:
    - Key: WEBSITE_LOAD_CERTIFICATES
    - Value: [Thumbprint of the certificate]
3. Select the certificate under `CurrentUser` > `My` certificate store.

## Token Validation

- Validates tokens issued by the Orchard OpenID server
  - Configure the validation feature to transparently use the server configuration of another tenant, which has the authorization server feature enabled.
- Validates token by a remote server supporting JWT and OpenID Connect discovery.

Token Validation require the following configuration.

| Property | Description |
| -------- | ----------- |
| Authorization server tenant | The tenant that runs OpenID Connect Server. If none is selected, then the following properties must be provided. |
| Authority | The address of the remote OpenID Connect server that issued the token. |
| Audience | Defines the intended recipient of the token that must be checked. |

A sample of Token Validation Settings recipe step:

```json
    {
      "name": "OpenIdValidationSettings",
      "Audience": "my_recipient",
      "Authority": "https://idp.domain.com"
    }
```

## OIDC Client

Authenticates users from an external OpenID Connect identity provider. 
If the site allows to register new users, a local user is linked and the external login is linked.
If an "email" claim is received, and a local user is found, then the external login is linked to that account, after authenticating.

### OpenId Configuration

Configuration can be set through the _OpenID Connect_ settings menu in the admin dashboard and also through a recipe step.

Available settings are:

- Display Name: Display name of the IdP. It is shown in the login form.
- Authority: Authority to use when making OpenIdConnect calls.
- ClientId: The `client_id` part of the query.
- CallbackPath: The request path within the application's base path where the user agent will be returned after sign out from the identity provider. See `post_logout_redirect_uri` from <http://openid.net/specs/openid-connect-session-1_0.html#RedirectionAfterLogout>
- SignedOut CallbackPath: the callback endpoint for signout. Defaults to `/signout-callback-oidc`.
- SignedOut Redirect Uri: The URI where the user agent will be redirected to after application is signed out from the identity provider. The redirect will happen after the `SignedOutCallbackPath` is invoked.
- Scopes: Extra scopes except openid and profile.
- Response Mode: Configure Response Mode see: <http://openid.net/specs/openid-connect-core-1_0.html#ImplicitAuthResponse>. If fragment or query only Code Authentication Flow is allowed.
- Supported Flows: Select one of the OIDC flows:
  - Code Authentication Flow (see: <http://openid.net/specs/openid-connect-core-1_0.html#CodeFlowAuth>)
  - Hybrid Authentication Flow (see: <http://openid.net/specs/openid-connect-core-1_0.html#HybridAuthRequest>)
    - Use `code id_token` response type (example: <http://openid.net/specs/openid-connect-core-1_0.html#code-id_token-tokenExample>)
    - Use `code id_token token` response type (example: <http://openid.net/specs/openid-connect-core-1_0.html#code-id_token-tokenExample>)
    - Use `code token` response type (example: <http://openid.net/specs/openid-connect-core-1_0.html#code-tokenExample>)
  - Implicit Authentication Flow (see: <http://openid.net/specs/openid-connect-core-1_0.html#ImplicitAuthRequest>)
    - Use `id_token` response type (example: <http://openid.net/specs/openid-connect-core-1_0.html#id_tokenExample>)
    - Use `id_token token` response type (example: <http://openid.net/specs/openid-connect-core-1_0.html#id_token-tokenExample>)
- Client Secret: It is used with one of the 'confidential' flows, code or hybrid.

A sample of OpenID Connect Client Settings recipe step:

```json
{
      "name": "OpenIdClientSettings",
      "Authority": "http://localhost:44300/t1",
      "DisplayName": "Orchard (t1) IdP",
      "ClientId": "orchard_t2", 
      "CallbackPath": "/signin-oidc",
      "SignedOutCallbackPath": "/signout-callback-oidc",
      "Scopes": "email phone",
      "ResponseMode": "form_post",
      "ResponseType": "code id_token",
      "ClientSecret": "secret"
}
```
