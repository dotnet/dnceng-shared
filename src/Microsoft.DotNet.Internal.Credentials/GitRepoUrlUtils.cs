// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Internal.Credentials;

public enum GitRepoType
{
    GitHub,
    AzureDevOps,
    Local,
    None
}

/// <summary>
/// Minimal URL parsing helpers needed by <see cref="RemoteTokenProvider"/>.
/// Kept intentionally small; richer helpers live alongside the consumers.
/// </summary>
public static class GitRepoUrlUtils
{
    private const string GitHubComString = "github.com";

    public static GitRepoType ParseTypeFromUri(string pathOrUri)
    {
        if (!Uri.TryCreate(pathOrUri, UriKind.RelativeOrAbsolute, out Uri? parsedUri))
        {
            return GitRepoType.None;
        }

        if (!parsedUri.IsAbsoluteUri)
        {
            return pathOrUri.IndexOfAny(Path.GetInvalidPathChars()) == -1
                ? GitRepoType.Local
                : GitRepoType.None;
        }

        return parsedUri switch
        {
            { IsFile: true } => GitRepoType.Local,
            { Scheme: "https" or "http", Host: GitHubComString } => GitRepoType.GitHub,
            { Scheme: "https" or "http", Host: "dev.azure.com" } => GitRepoType.AzureDevOps,
            { Scheme: "https" or "http", Host: var host } when host.EndsWith("visualstudio.com") => GitRepoType.AzureDevOps,
            _ => GitRepoType.None,
        };
    }
}
