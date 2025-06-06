// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using AwesomeAssertions;
using Microsoft.DotNet.Internal.DependencyInjection.Testing;
using Microsoft.DotNet.ServiceFabric.ServiceHost;
using NUnit.Framework;

namespace CoreHealthMonitor.Tests;

[TestFixture]
public class DependencyRegistrationTests
{
    [Test]
    public void AreDependenciesRegistered()
    {
        DependencyInjectionValidation.IsDependencyResolutionCoherent(
                s =>
                {
                    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "XUNIT");
                    ServiceHost.ConfigureDefaultServices(s);
                    CoreHealthMonitorService.Configure(s);
                },
                out string message,
                additionalScopedTypes: new[] {typeof(CoreHealthMonitorService)}
            )
            .Should()
            .BeTrue(message);
    }
}
