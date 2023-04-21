// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.Internal.DependencyInjection;

internal class ResolvingLazy<T> : Lazy<T>
{
    public ResolvingLazy(IServiceProvider services) : base(services.GetRequiredService<T>)
    {
    }
}
