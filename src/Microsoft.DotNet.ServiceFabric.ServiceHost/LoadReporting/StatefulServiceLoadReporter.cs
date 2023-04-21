// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Fabric;

namespace Microsoft.DotNet.ServiceFabric.ServiceHost;

public class StatefulServiceLoadReporter : IServiceLoadReporter
{
    private readonly IStatefulServicePartition _partition;

    public StatefulServiceLoadReporter(IStatefulServicePartition partition)
    {
        _partition = partition;
    }

    public void ReportLoad(string name, int value)
    {
        _partition.ReportLoad(new[] {new LoadMetric(name, value)});
    }
}
