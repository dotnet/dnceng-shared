// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.DncEng.Configuration.Extensions;
using Microsoft.DotNet.Internal.Health;
using Microsoft.DotNet.ServiceFabric.ServiceHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreHealthMonitor;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public sealed class CoreHealthMonitorService : IServiceImplementation
{
    private readonly IInstanceHealthReporter<CoreHealthMonitorService> _health;
    private readonly IOptions<DriveMonitorOptions> _driveOptions;
    private readonly ILogger<CoreHealthMonitorService> _logger;

    public CoreHealthMonitorService(
        IInstanceHealthReporter<CoreHealthMonitorService> health,
        IOptions<DriveMonitorOptions> driveOptions,
        ILogger<CoreHealthMonitorService> logger,
        ISystemClock clock)
    {
        _health = health;
        _driveOptions = driveOptions;
        _logger = logger;
    }

    public async Task<TimeSpan> RunAsync(CancellationToken cancellationToken)
    {
        await ScanDriveFreeSpaceAsync().ConfigureAwait(false);

        return TimeSpan.FromMinutes(5);
    }

    private async Task ScanDriveFreeSpaceAsync()
    {
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed)
            {
                _logger.LogInformation("Skipping drive {DriveName} of type {DriveType}", drive.Name, drive.DriveType);
                continue;
            }

            if (!drive.IsReady)
            {
                _logger.LogWarning("Drive {DriveName} reports !IsReady", drive.Name);
                continue;
            }

            long threshold;
            long freeSpace;
            try
            {
                threshold = _driveOptions.Value.MinimumFreeSpaceBytes;
                freeSpace = drive.AvailableFreeSpace;
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Failed to get drive space information for drive {DriveName}", drive.Name);
                break;
            }

            if (freeSpace < threshold)
            {
                await _health.UpdateStatusAsync(
                        "DriveSpace:" + drive.Name,
                        HealthStatus.Error,
                        $"Available drive space on {drive.Name} is at {freeSpace:N} below threshold of {threshold:N} bytes"
                    )
                    .ConfigureAwait(false);
            }
            else
            {
                await _health.UpdateStatusAsync(
                        "DriveSpace:" + drive.Name,
                        HealthStatus.Healthy,
                        $"Available drive space on {drive.Name} is at {freeSpace:N} above threshold of {threshold:N} bytes"
                    )
                    .ConfigureAwait(false);
            }
        }
    }

    public static void Configure(IServiceCollection services)
    {
        services.AddDefaultJsonConfiguration();
        services.Configure<DriveMonitorOptions>("DriveMonitoring", ((o, s) => s.Bind(o)));
    }
}
