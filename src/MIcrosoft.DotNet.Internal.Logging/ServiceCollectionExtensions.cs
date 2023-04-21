// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.Internal.Logging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOperationTracking(
        this IServiceCollection collection,
        Action<OperationManagerOptions> configure)
    {
        collection.AddSingleton<OperationManager>();
        collection.AddOptions();
        collection.Configure(configure);
        return collection;
    }
}
