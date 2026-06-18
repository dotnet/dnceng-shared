// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Internal.Credentials;

/// <summary>
/// Options describing a cross-tenant federated credential exchange.
///
/// A <see cref="Azure.Identity.ManagedIdentityCredential"/> in the source tenant is used
/// to obtain an assertion token for the <c>api://AzureADTokenExchange</c> audience, which
/// is then used as a client assertion to authenticate as <see cref="AppId"/> in
/// <see cref="TenantId"/> via a <see cref="Azure.Identity.ClientAssertionCredential"/>.
/// </summary>
public class FederatedCredentialOptions
{
    /// <summary>
    /// Client ID of the App Registration to authenticate as in the target tenant.
    /// This is typically the same client ID as the source-tenant app registration.
    /// </summary>
    public required string AppId { get; init; }

    /// <summary>
    /// Tenant ID the resulting <see cref="Azure.Identity.ClientAssertionCredential"/> authenticates against.
    /// </summary>
    public required string TenantId { get; init; }
}
