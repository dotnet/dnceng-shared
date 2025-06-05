// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;

namespace Microsoft.DotNet.ServiceFabric.ServiceHost.Tests;

[TestFixture]
public class ExtensionsTests
{
    [Test]
    public async Task NonCancelableThrows()
    {
        await (((Func<Task>)(() => CancellationToken.None.AsTask()))).Should().ThrowExactlyAsync<ArgumentException>();
    }

    [Test]
    public void PostCancellationChangesStateCompleted()
    {
        var source = new CancellationTokenSource();
        Task task = source.Token.AsTask();
        task.IsCompleted.Should().BeFalse();
        source.Cancel();
        task.IsCompleted.Should().BeTrue();
    }

    [Test]
    public void PreCancelledReturnsCompleted()
    {
        var source = new CancellationTokenSource();
        source.Cancel();
        Task task = source.Token.AsTask();
        task.IsCompleted.Should().BeTrue();
    }

    [Test]
    public void WithoutCancellingNotComplete()
    {
        var source = new CancellationTokenSource();
        Task task = source.Token.AsTask();
        task.IsCompleted.Should().BeFalse();
    }
}
