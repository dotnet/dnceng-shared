// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Services.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;

namespace Microsoft.DotNet.GitHub.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGitHubTokenProvider(this IServiceCollection services)
    {
        services.TryAddSingleton<IGitHubAppTokenProvider, GitHubAppTokenProvider>();
        services.TryAddSingleton<IGitHubTokenProvider, GitHubTokenProvider>();
        services.TryAddSingleton<ExponentialRetry>();
        services.TryAddTransient<ISystemClock, SystemClock>();
        services.TryAddTransient<IInstallationLookup, InMemoryCacheInstallationLookup>();
        return services;
    }
}
