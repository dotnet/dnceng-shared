// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Octokit;

namespace Microsoft.DotNet.GitHub.Authentication;

public interface IGitHubClientFactory
{
    IGitHubClient CreateGitHubClient(string token);
    IGitHubClient CreateGitHubClient(string token, AuthenticationType type);
}
