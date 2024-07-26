// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DncEng.Configuration.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.ServiceFabric.ServiceHost;

public class ServiceHostWebSiteOptions
{
    public IReadOnlyCollection<string> Urls { get; set; } = new[] { "http://localhost:8080/" };

    public bool CaptureStartupErrors { get; set; } = true;
}

public static class ServiceHostWebSite<TStartup>
    where TStartup : class
{
    /// <summary>
    ///     This is the entry point of the service host process.
    /// </summary>
    [PublicAPI]
    public static void Run(string serviceTypeName, ServiceHostWebSiteOptions options = null)
    {
        options ??= new();

        if (ServiceFabricHelpers.RunningInServiceFabric())
        {
            ServiceFabricMain(serviceTypeName);
        }
        else
        {
            NonServiceFabricMain(options);
        }
    }

    private static void NonServiceFabricMain(ServiceHostWebSiteOptions options)
    {
        new WebHostBuilder()
            .UseKestrel(o =>
                // Default 32k, which isn't enough for oauth cookies from GitHub for people with many teams/claims
                o.Limits.MaxRequestHeadersTotalSize = 65536)
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddDefaultJsonConfiguration(context.HostingEnvironment, serviceProvider: null);
            })
            .ConfigureServices(ServiceHost.ConfigureDefaultServices)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IServiceLoadReporter>(new EmptyServiceLoadReporter());
            })
            .ConfigureLogging(
                builder =>
                {
                    builder.AddFilter(level => level > LogLevel.Debug);
                    builder.AddConsole();
                })
            .UseStartup<TStartup>()
            .UseUrls(string.Join(";", options.Urls))
            .CaptureStartupErrors(true)
            .Build()
            .Run();
    }

    private static void ServiceFabricMain(string serviceTypeName)
    {
        ServiceHost.Run(
            host => host.RegisterStatelessWebService<TStartup>(serviceTypeName,
            hostBuilder =>
            {
                hostBuilder.ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddDefaultJsonConfiguration(context.HostingEnvironment, serviceProvider: null);
                });
            }));
    }
}
