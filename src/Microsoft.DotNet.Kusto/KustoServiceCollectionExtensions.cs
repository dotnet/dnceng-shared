// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.DotNet.Kusto;

public static class KustoServiceCollectionExtensions
{
    public static IServiceCollection AddKustoClientProvider(this IServiceCollection services, string sectionName, string ManagedIdentityId = null)
    {
        services.AddSingleton<IKustoClientProvider, KustoClientProvider>();
        services.Configure<KustoClientProviderOptions>(sectionName, (o, s) =>
        {
            s.Bind(o);
            if (!string.IsNullOrEmpty(o.ManagedIdentityId))
            {
                o.ManagedIdentityId = ManagedIdentityId;
            }
        });
        return services;
    }

    public static IServiceCollection AddKustoClientProvider(this IServiceCollection services, Action<KustoClientProviderOptions> configure)
    {
        services.AddSingleton<IKustoClientProvider, KustoClientProvider>();
        services.Configure(configure);
        return services;
    }
}
