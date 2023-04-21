// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.ServiceFabric.ServiceHost;

public class EmptyServiceLoadReporter : IServiceLoadReporter
{
    public void ReportLoad(string name, int value)
    {
    }
}
