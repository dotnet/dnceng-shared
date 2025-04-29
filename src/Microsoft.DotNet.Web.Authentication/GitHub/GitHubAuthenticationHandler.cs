// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace Microsoft.DotNet.Web.Authentication.GitHub;

public class GitHubAuthenticationHandler : OAuthHandler<GitHubAuthenticationOptions>
{
    private readonly GitHubClaimResolver _claimResolver;

    public GitHubAuthenticationHandler(
        GitHubClaimResolver claimResolver,
        IOptionsMonitor<GitHubAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
        _claimResolver = claimResolver;
    }

    protected override async Task<AuthenticationTicket> CreateTicketAsync(
        ClaimsIdentity identity,
        AuthenticationProperties properties,
        OAuthTokenResponse tokens)
    {
        string accessToken = tokens.AccessToken;
        (IEnumerable<Claim> claims, User user) = await _claimResolver.GetUserInformation(accessToken, Context.RequestAborted);
        identity.AddClaims(claims);

        var context = new OAuthCreatingTicketContext(
            new ClaimsPrincipal(identity),
            properties,
            Context,
            Scheme,
            Options,
            Backchannel,
            tokens,
            default);
        await Options.Events.CreatingTicket(context);
        return new AuthenticationTicket(context.Principal, context.Properties, context.Scheme.Name);
    }
}
