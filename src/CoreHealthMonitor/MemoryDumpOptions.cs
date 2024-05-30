// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CoreHealthMonitor;

public class MemoryDumpOptions
{
    public string ContainerUri { get; set; }
    public string[] IgnoreDumpPatterns { get; set; }
    public string ManagedIdentityId { get; set; }
}
