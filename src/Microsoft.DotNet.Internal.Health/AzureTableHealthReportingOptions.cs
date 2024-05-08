// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Internal.Health;

public class AzureTableHealthReportingOptions
{
    public string ConnectionString { get; set; }
    public string TableName { get; set; }
    public string ManagedIdentityClientId { get; set; }
}
