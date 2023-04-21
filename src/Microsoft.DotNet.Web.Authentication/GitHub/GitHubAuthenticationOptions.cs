// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.OAuth;

namespace Microsoft.DotNet.Web.Authentication.GitHub;

public class GitHubAuthenticationOptions : OAuthOptions
{
    public GitHubAuthenticationOptions()
    {
        AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        TokenEndpoint = "https://github.com/login/oauth/access_token";
    }
}
