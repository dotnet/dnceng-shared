// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.GitHub.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.GitHub.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubTokenProvider(this IServiceCollection services)
    {
        return services
            .AddSingleton<IGitHubAppTokenProvider, GitHubAppTokenProvider>()
            .AddSingleton<IGitHubTokenProvider, GitHubTokenProvider>();
    }
}
