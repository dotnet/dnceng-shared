// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.GitHub.Authentication;

public static class GitHubHelper
{
    public static string GetRepositoryUrl(string organization, string repository) => $"https://github.com/{organization}/{repository}";
}
