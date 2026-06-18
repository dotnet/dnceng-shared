// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Internal.Credentials;

public class CredentialResolverOptions
{
    /// <summary>
    /// Whether to include interactive login flows
    /// </summary>
    public bool DisableInteractiveAuth { get; set; }

    /// <summary>
    /// Token to use directly instead of authenticating.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Managed Identity to use for the auth
    /// </summary>
    public string? ManagedIdentityId { get; set; }

    /// <summary>
    /// If set, a cross-tenant federated credential is produced: a
    /// <see cref="Azure.Identity.ManagedIdentityCredential"/> (configured via <see cref="ManagedIdentityId"/>)
    /// is used to obtain a client assertion against <c>api://AzureADTokenExchange</c>, which then backs
    /// a <see cref="Azure.Identity.ClientAssertionCredential"/> for the configured app and tenant.
    /// </summary>
    public FederatedCredentialOptions? FederatedCredential { get; set; }
}
