// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.Internal.Health;

public sealed class AzureTableHealthReportProvider : IHealthReportProvider
{
    private readonly ILogger<AzureTableHealthReportProvider> _logger;
    private readonly TableClient _tableClient;

    public AzureTableHealthReportProvider(
        IOptionsMonitor<AzureTableHealthReportingOptions> options,
        ILogger<AzureTableHealthReportProvider> logger)
    {
        _logger = logger;

        if (string.IsNullOrEmpty(options.CurrentValue.StorageAccountTablesUri))
        {
            throw new ArgumentException($"{nameof(AzureTableHealthReportingOptions.StorageAccountTablesUri)} missing in {AzureTableHealthReportingOptions.HealthReportSettingsSection} section of app settings");
        }
        if (string.IsNullOrEmpty(options.CurrentValue.TableName))
        {
            throw new ArgumentException($"{nameof(AzureTableHealthReportingOptions.TableName)} missing in {AzureTableHealthReportingOptions.HealthReportSettingsSection} section of app settings");
        }

        DefaultAzureCredential credential = string.IsNullOrEmpty(options.CurrentValue.ManagedIdentityClientId)
            ? new()
            : new(new DefaultAzureCredentialOptions { ManagedIdentityClientId = options.CurrentValue.ManagedIdentityClientId });
        TableServiceClient tableServiceClient = new (new Uri(options.CurrentValue.StorageAccountTablesUri), credential);
        _tableClient = tableServiceClient.GetTableClient(options.CurrentValue.TableName);
    }

    private static string GetRowKey(string instance, string subStatus) => EscapeKeyField(instance ?? "") + "|" + EscapeKeyField(subStatus);
    private static (string instance, string subStatus) ParseRowKey(string rowKey)
    {
        var parts = rowKey.Split('|');
        var subStatus = UnescapeKeyField(parts[1]);
        if (string.IsNullOrEmpty(parts[0]))
            return (null, subStatus);
        return (UnescapeKeyField(parts[0]), subStatus);
    }

    public static string EscapeKeyField(string value) =>
        value.Replace(":", "\0")
            .Replace("|", ":pipe:")
            .Replace("\\", ":back:")
            .Replace("/", ":slash:")
            .Replace("#", ":hash:")
            .Replace("?", ":question:")
            .Replace("\0", ":colon:");

    public static string UnescapeKeyField(string value) =>
        value.Replace(":colon:", "\0")
            .Replace(":pipe:", "|")
            .Replace(":back:", "\\")
            .Replace(":slash:", "/")
            .Replace(":hash:", "#")
            .Replace(":question:", "?")
            .Replace("\0", ":");

    public async Task UpdateStatusAsync(string serviceName, string instance, string subStatusName, HealthStatus status, string message)
    {
        string partitionKey = EscapeKeyField(serviceName);
        string rowKey = GetRowKey(instance, subStatusName);

        try
        {
            await _tableClient.UpsertEntityAsync(new HealthReportTableEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Status = status,
                Message = message
            });
        }
        catch (Exception e)
        {
            // Crashing out a service trying to report health isn't useful, log that we failed and move on
            _logger.LogError(e, "Unable to update health status for {service}/{instance}|{subStatus}", serviceName, instance, subStatusName);
        }
    }

    public async Task<IList<HealthReport>> GetAllStatusAsync(string serviceName)
    {
        string partitionKey = EscapeKeyField(serviceName);

        AsyncPageable<HealthReportTableEntity> tableEntities = _tableClient.QueryAsync<HealthReportTableEntity>(x => x.PartitionKey == partitionKey);

        List<HealthReport> healthReports = new();
        await foreach (var page in tableEntities.AsPages())
        {
            healthReports.AddRange(page.Values.Select(entity =>
            {
                var (instance, subStatus) = ParseRowKey(entity.RowKey);
                return new HealthReport(
                    serviceName,
                    instance,
                    subStatus,
                    entity.Status,
                    entity.Message,
                    entity.Timestamp.Value
                );
            }));
        }
        return healthReports;
    }

    private class ValueList<T>
    {
        [JsonPropertyName("value")]
        public T[] Value { get; set; }
    }

    private class Entity
    {
        public DateTimeOffset? Timestamp { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public HealthStatus Status { get; set; }
        public string Message { get; set; }
        public string RowKey { get; set; }
    }
}
