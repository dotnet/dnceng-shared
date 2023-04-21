// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Internal.Health;

public interface IHealthReportProvider
{
    Task UpdateStatusAsync(string serviceName, string instance, string subStatusName, HealthStatus status, string message);
    Task<IList<HealthReport>> GetAllStatusAsync(string serviceName);
}
