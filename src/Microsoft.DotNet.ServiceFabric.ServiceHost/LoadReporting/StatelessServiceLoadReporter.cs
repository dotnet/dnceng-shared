// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Fabric;

namespace Microsoft.DotNet.ServiceFabric.ServiceHost;

public class StatelessServiceLoadReporter : IServiceLoadReporter
{
    private readonly IStatelessServicePartition _partition;

    public StatelessServiceLoadReporter(IStatelessServicePartition partition)
    {
        _partition = partition;
    }

    public void ReportLoad(string name, int value)
    {
        _partition.ReportLoad(new[] {new LoadMetric(name, value)});
    }
}
