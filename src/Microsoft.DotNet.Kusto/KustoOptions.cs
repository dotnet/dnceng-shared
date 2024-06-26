// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Kusto;

public class KustoOptions
{
    public string KustoClusterUri { get; set; }
    public string KustoIngestionUri { get; set; }
    public string Database { get; set; }
    public string ManagedIdentityId { get; set; }
    // For local development, use the Azure CLI for authentication
    public bool UseAzCliAuthentication { get; set; }
}
