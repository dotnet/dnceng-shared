// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Identity;

namespace Microsoft.DotNet.Internal.Credentials;

public static class CredentialResolver
{
    private const string FederatedAssertionScope = "api://AzureADTokenExchange/.default";

    /// <summary>
    /// Creates a credential based on parameters provided.
    /// </summary>
    public static TokenCredential CreateCredential(CredentialResolverOptions options)
    {
        // 1. BAR or Entra token that can directly be used to authenticate against a service
        if (!string.IsNullOrEmpty(options.Token))
        {
            return new ResolvedCredential(options.Token!);
        }

        // 2. Cross-tenant federated credential: a managed identity in the source tenant
        //    produces a client assertion used to authenticate as an app registration in the
        //    target tenant. Requires both a ManagedIdentityId and FederatedCredential.
        if (options.FederatedCredential is not null)
        {
            if (string.IsNullOrEmpty(options.ManagedIdentityId))
            {
                throw new InvalidOperationException(
                    $"{nameof(CredentialResolverOptions.ManagedIdentityId)} must be set when " +
                    $"{nameof(CredentialResolverOptions.FederatedCredential)} is configured.");
            }

            return CreateFederatedCredential(options.ManagedIdentityId!, options.FederatedCredential);
        }

        // 3. Managed identity (for server-to-server scenarios - e.g. PCS->Maestro)
        if (!string.IsNullOrEmpty(options.ManagedIdentityId))
        {
            return CreateManagedIdentityCredential(options.ManagedIdentityId!);
        }

        // 4. Azure CLI authentication setup by the caller (for CI scenarios)
        return new AzureCliCredential();
    }

    private static ManagedIdentityCredential CreateManagedIdentityCredential(string managedIdentityId)
    {
        return managedIdentityId == "system"
            ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(managedIdentityId));
    }

    private static ClientAssertionCredential CreateFederatedCredential(
        string managedIdentityId,
        FederatedCredentialOptions federated)
    {
        if (string.IsNullOrEmpty(federated.AppId))
        {
            throw new InvalidOperationException($"{nameof(FederatedCredentialOptions.AppId)} must be configured.");
        }
 
        if (string.IsNullOrEmpty(federated.TenantId))
        {
            throw new InvalidOperationException($"{nameof(FederatedCredentialOptions.TenantId)} must be configured.");
        }

        ManagedIdentityCredential assertionCredential = CreateManagedIdentityCredential(managedIdentityId);
        TokenRequestContext assertionRequest = new([FederatedAssertionScope]);

        return new ClientAssertionCredential(
            federated.TenantId,
            federated.AppId,
            async cancellationToken =>
            {
                AccessToken token = await assertionCredential.GetTokenAsync(assertionRequest, cancellationToken);
                return token.Token;
            });
    }
}
