// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.ServiceFabric.ServiceHost;

public static class ServiceFabricHelpers
{
    public static bool RunningInServiceFabric()
    {
        string fabricApplication = Environment.GetEnvironmentVariable("Fabric_ApplicationName");
        return !string.IsNullOrEmpty(fabricApplication);
    }
}
