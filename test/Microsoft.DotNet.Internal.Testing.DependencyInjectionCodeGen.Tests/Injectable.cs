// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Internal.Testing.DependencyInjectionCodeGen.Tests;

public class Injectable : IDisposable, IAsyncDisposable
{
    public Injectable(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public bool IsSyncDisposeCalled { get; private set; }
    public bool IsAsyncDisposeCalled { get; private set; }

    public void Dispose()
    {
        IsSyncDisposeCalled = true;
    }

    public ValueTask DisposeAsync()
    {
        IsAsyncDisposeCalled = true;
        return default;
    }
}
