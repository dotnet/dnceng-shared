// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.GitHub.Authentication;
using Microsoft.DotNet.Services.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;

namespace Microsoft.DotNet.GitHub.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubTokenProvider(this IServiceCollection services)
    {
        return services
            .TryAddSingleton<IGitHubAppTokenProvider, GitHubAppTokenProvider>()
            .TryAddSingleton<IGitHubTokenProvider, GitHubTokenProvider>()
            .TryAddSingleton<ExponentialRetry>()
            .TryAddTransient<ISystemClock, SystemClock>()
            .TryAddTransient<IInstallationLookup, InMemoryCacheInstallationLookup>();
    }
}
