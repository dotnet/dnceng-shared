// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.Internal.Health;

public class MachineNameInstanceAccessor : IInstanceAccessor
{
    public string GetCurrentInstanceName()
    {
        return Environment.MachineName;
    }
}
