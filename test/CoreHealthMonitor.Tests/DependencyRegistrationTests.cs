// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using FluentAssertions;
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
                    Program.Configure(s);
                },
                out string message,
                additionalScopedTypes: new[] {typeof(CoreHealthMonitorService)}
            )
            .Should()
            .BeTrue(message);
    }
}
