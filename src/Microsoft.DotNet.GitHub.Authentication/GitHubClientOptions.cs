// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Octokit;

namespace Microsoft.DotNet.GitHub.Authentication;

public class GitHubClientOptions
{
    public ProductHeaderValue ProductHeader { get; set; }
    public string[] AllowOrgs { get; set; }
}
