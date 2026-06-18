// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Identity;

namespace Microsoft.DotNet.Internal.Credentials;

/// <summary>
/// Helpers for creating <see cref="TokenCredential"/> instances based on a managed identity.
/// </summary>
public static class ManagedIdentityCredentialFactory
{
    /// <summary>
    /// The well-known scope used to obtain assertion tokens for workload identity federation /
    /// federated identity credentials.
    /// </summary>
    public const string FederatedAssertionScope = "api://AzureADTokenExchange/.default";

    /// <summary>
    /// Sentinel value indicating the system-assigned managed identity should be used.
    /// </summary>
    public const string SystemAssignedId = "system";

    /// <summary>
    /// Creates a <see cref="ManagedIdentityCredential"/> for the system-assigned or user-assigned MI.
    /// </summary>
    /// <param name="managedIdentityId">
    /// "system" for the system-assigned identity, or a client ID GUID for a user-assigned one.
    /// </param>
    public static ManagedIdentityCredential CreateManagedIdentityCredential(string managedIdentityId)
        => managedIdentityId == SystemAssignedId
            ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(managedIdentityId));

    /// <summary>
    /// Creates a federated <see cref="ClientAssertionCredential"/> that uses a managed identity in the
    /// current tenant to obtain a token assertion, and exchanges it for a token in
    /// <paramref name="tenantId"/> for the app registration identified by <paramref name="appId"/>.
    /// </summary>
    public static TokenCredential CreateFederatedCredential(string tenantId, string appId, string managedIdentityId)
    {
        ManagedIdentityCredential assertionCredential = CreateManagedIdentityCredential(managedIdentityId);
        TokenRequestContext assertionRequest = new([FederatedAssertionScope]);

        return new ClientAssertionCredential(
            tenantId,
            appId,
            async cancellationToken =>
            {
                AccessToken token = await assertionCredential.GetTokenAsync(assertionRequest, cancellationToken);
                return token.Token;
            });
    }
}
