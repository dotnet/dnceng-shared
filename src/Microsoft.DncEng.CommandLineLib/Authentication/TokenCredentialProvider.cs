// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.DncEng.Configuration.Extensions;

namespace Microsoft.DncEng.CommandLineLib.Authentication;

public class TokenCredentialProvider : ITokenCredentialProvider
{
    private readonly IConsole _console;
    private readonly InteractiveTokenCredentialProvider _interactiveTokenCredentialProvider;

    public TokenCredentialProvider(IConsole console, InteractiveTokenCredentialProvider interactiveTokenCredentialProvider)
    {
        _console = console;
        _interactiveTokenCredentialProvider = interactiveTokenCredentialProvider;
    }

    public async Task<TokenCredential> GetCredentialAsync()
    {
        var creds = new List<TokenCredential>
        {
            new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                TenantId = ConfigurationConstants.MsftAdTenantId,
            })
        };

        if (_console.IsInteractive)
        {
            creds.Add(await _interactiveTokenCredentialProvider.GetCredentialAsync());
        }

        return new ChainedTokenCredential(creds.ToArray());
    }
}
