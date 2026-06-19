// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Identity;

namespace Microsoft.DotNet.Internal.Credentials;

/// <summary>
/// Credential suitable for local development.
/// </summary>
public class DevCredential : ChainedTokenCredential
{
    public DevCredential() : base(
        new AzureCliCredential(),
        new AzureDeveloperCliCredential(),
        new VisualStudioCredential(),
        new VisualStudioCodeCredential(),
        new EnvironmentCredential())
    {
    }
}
