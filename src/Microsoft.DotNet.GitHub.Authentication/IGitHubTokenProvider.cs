// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.DotNet.GitHub.Authentication;

public interface IGitHubTokenProvider
{
    Task<string> GetTokenForInstallationAsync(long installationId);
    Task<string> GetTokenForRepository(string repositoryUrl);
    string GetTokenForApp();
    string GetTokenForApp(string name);
    void InvalidateTokenCacheAsync(long installationId);
}

public static class GitHubTokenProviderExtensions
{
    public static Task<string> GetTokenForRepository(this IGitHubTokenProvider provider, string organization, string repository)
    {
        return provider.GetTokenForRepository(GitHubHelper.GetRepositoryUrl(organization, repository));
    }
}
