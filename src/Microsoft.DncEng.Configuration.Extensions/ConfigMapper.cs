// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;

namespace Microsoft.DncEng.Configuration.Extensions;

public static class ConfigMapper
{
    private static readonly ConditionalWeakTable<IConfiguration, TelemetryClient> Clients = new ConditionalWeakTable<IConfiguration, TelemetryClient>();
    public static TelemetryClient GetTelemetryClient(IConfiguration config)
    {
        if (!Clients.TryGetValue(config, out var client))
        {
            client = CreateTelemetryClient();
            Clients.Add(config, client);
        }

        return client;
    }

    private static TelemetryClient CreateTelemetryClient()
    {
        var config = new TelemetryConfiguration()
        {
            ConnectionString = GetApplicationConnectionString()
        };
        return new TelemetryClient(config);
    }

    private static string GetApplicationConnectionString()
    {
        string envVar = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING");
        if (string.IsNullOrEmpty(envVar))
        {
            return "InstrumentationKey=00000000-0000-0000-0000-000000000000";
        }

        return envVar;
    }
}
