// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.DotNet.Internal.Testing.Utility;

public class TestClock : ISystemClock, Microsoft.AspNetCore.Authentication.ISystemClock
{
    public static readonly DateTime BaseTime = DateTime.Parse("2001-02-03T16:05:06Z");
    public DateTimeOffset UtcNow { get; set; } = BaseTime;
}
