// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Internal.Health;

public enum HealthStatus
{
    Invalid = 0,
    Healthy,
    Warning,
    Error,

    Unknown,
}
