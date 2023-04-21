// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using JetBrains.Annotations;

namespace Microsoft.DotNet.Internal.Health;

public interface IServiceHealthReporter<[UsedImplicitly] T> : IHealthReporter
{
}
