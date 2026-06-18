// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Internal.Credentials;

namespace Microsoft.DotNet.Kusto;

public class KustoOptions
{
    public string KustoClusterUri { get; set; }
    public string KustoIngestionUri { get; set; }
    public string Database { get; set; }
    public string ManagedIdentityId { get; set; }
    // For local development, use the Azure CLI for authentication
    public bool UseAzCliAuthentication { get; set; }

    /// <summary>
    /// When set, authentication is performed by exchanging a managed identity assertion
    /// (using <see cref="ManagedIdentityId"/>) for a token issued to the configured app
    /// in the configured tenant. Enables cross-tenant access from a source-tenant managed
    /// identity to a target-tenant Kusto cluster via federated identity credentials.
    /// </summary>
    public FederatedCredentialOptions FederatedCredential { get; set; }
}
