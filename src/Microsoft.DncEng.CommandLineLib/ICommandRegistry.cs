// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.DncEng.CommandLineLib;

public interface ICommandRegistry
{
    IReadOnlyDictionary<string, Type> GetValidCommandAtScope(Type scope = null);
}
