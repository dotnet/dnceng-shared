// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.DncEng.CommandLineLib.Authentication;

public interface ITokenCredentialProvider
{
    public Task<TokenCredential> GetCredentialAsync();
}
