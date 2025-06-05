// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using Castle.DynamicProxy;
using AwesomeAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using NUnit.Framework;

namespace Microsoft.DotNet.ServiceFabric.ServiceHost.Tests;

[TestFixture]
public class LoggingServiceInterceptorTests
{
    [Test]
    public void LogsCorrectUrl()
    {
        var telemetryChannel = new FakeChannel();
        var config = new TelemetryConfiguration()
        {
            TelemetryChannel = telemetryChannel
        };
        var client = new TelemetryClient(config);

        StatelessServiceContext ctx = MockBuilder.StatelessServiceContext();

        var gen = new ProxyGenerator();
        var impl = (IFakeService) gen.CreateInterfaceProxyWithTargetInterface(
            typeof(IFakeService),
            new Type[0],
            Mock.Of<IFakeService>(),
            new LoggingServiceInterceptor(ctx, client));

        impl.TestServiceMethod();
        client.Flush();
        RequestTelemetry requestTelemetry = telemetryChannel.Telemetry.OfType<RequestTelemetry>().FirstOrDefault();
        requestTelemetry.Should().NotBeNull();
        (requestTelemetry.Success ?? true).Should().BeTrue();
        requestTelemetry.Url.AbsoluteUri.Should().StartWith(ctx.ServiceName.AbsoluteUri);
        requestTelemetry.Url.AbsoluteUri.Should().Contain(nameof(IFakeService));
        telemetryChannel.Telemetry.OfType<ExceptionTelemetry>().Should().BeEmpty();
    }

    [Test]
    public void ExceptionLogsFailedRequest()
    {
        var telemetryChannel = new FakeChannel();
        var config = new TelemetryConfiguration()
        {
            TelemetryChannel = telemetryChannel
        };
        var client = new TelemetryClient(config);

        StatelessServiceContext ctx = MockBuilder.StatelessServiceContext();

        Mock<IFakeService> fakeService = new Mock<IFakeService>();
        fakeService.Setup(s => s.TestServiceMethod()).Throws(new InvalidOperationException("Test Exception Text"));

        var gen = new ProxyGenerator();
        var impl = (IFakeService) gen.CreateInterfaceProxyWithTargetInterface(
            typeof(IFakeService),
            new Type[0],
            fakeService.Object,
            new LoggingServiceInterceptor(ctx, client));
            
        var ex = (((Func<object>)(() => impl.TestServiceMethod()))).Should().Throw<InvalidOperationException>().Which;
        ex.Message.Should().Be("Test Exception Text");
            
        client.Flush();
        List<RequestTelemetry> requestTelemetries =
            telemetryChannel.Telemetry.OfType<RequestTelemetry>().ToList();
        requestTelemetries.Should().ContainSingle();
        RequestTelemetry requestTelemetry = requestTelemetries[0];
        requestTelemetry.Success.Should().BeFalse();
        ExceptionTelemetry exceptionTelemetry = telemetryChannel.Telemetry.OfType<ExceptionTelemetry>().FirstOrDefault();
        exceptionTelemetry.Should().NotBeNull();
        exceptionTelemetry.Exception.Should().BeSameAs(ex);
    }
}
