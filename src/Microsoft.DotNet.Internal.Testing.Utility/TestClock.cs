// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.Internal.Testing.Utility;

public class TestClock : TimeProvider, Extensions.Internal.ISystemClock
{
    public static readonly DateTime BaseTime = DateTime.Parse("2001-02-03T16:05:06Z");
    public DateTimeOffset UtcNow { get; set; } = BaseTime;
}
