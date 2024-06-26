// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.DotNet.Kusto;

public static class KustoServiceCollectionExtensions
{
    public static IServiceCollection AddKustoClientProvider(this IServiceCollection services, string sectionName)
    {
        services.AddSingleton<IKustoClientProvider, KustoClientProvider>();
        services.Configure<KustoOptions>(sectionName, (o, s) =>
        {
            s.Bind(o);
        });
        return services;
    }

    public static IServiceCollection AddKustoClientProvider(this IServiceCollection services, Action<KustoOptions> configure)
    {
        services.AddSingleton<IKustoClientProvider, KustoClientProvider>();
        services.Configure(configure);
        return services;
    }
}
