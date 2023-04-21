// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.ServiceFabric.ServiceHost;

public interface IServiceImplementation
{
    Task<TimeSpan> RunAsync(CancellationToken cancellationToken);
}
