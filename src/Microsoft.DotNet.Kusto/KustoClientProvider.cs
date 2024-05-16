// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using Kusto.Data.Results;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;

namespace Microsoft.DotNet.Kusto;

public sealed class KustoClientProvider : IKustoClientProvider, IDisposable
{
    private readonly IOptionsMonitor<KustoOptions> _options;
    private readonly object _updateLock = new object();
    private ICslQueryProvider _kustoQueryProvider;
    private readonly IDisposable _monitor;

    public KustoClientProvider(IOptionsMonitor<KustoOptions> options)
    {
        _options = options;
        _monitor = options.OnChange(ClearProviderCache);
    }

    public KustoClientProvider(IOptionsMonitor<KustoOptions> options, ICslQueryProvider provider)
    {
        _options = options;
        _kustoQueryProvider = provider;
    }

    private void ClearProviderCache(KustoOptions arg1, string arg2)
    {
        lock (_updateLock)
        {
            _kustoQueryProvider = null;
        }
    }

    public ICslQueryProvider GetProvider()
    {
        var value = _kustoQueryProvider;
        if (value != null)
            return value;
        lock (_updateLock)
        {
            value = _kustoQueryProvider;
            if (value != null)
                return value;

            _kustoQueryProvider = value = KustoClientFactory.CreateCslQueryProvider(GetKustoConnectionStringBuilder());
            return value;
        }
    }

    private string DatabaseName => _options.CurrentValue.Database;
    private string KustoClusterUri => _options.CurrentValue.KustoClusterUri;
    private string ManagedIdentityId => _options.CurrentValue.ManagedIdentityId;

    private KustoConnectionStringBuilder GetKustoConnectionStringBuilder()
    {
        if (string.IsNullOrEmpty(KustoClusterUri))
        {
            throw new ArgumentException($"{nameof(KustoOptions.KustoClusterUri)} is not configured in app settings");
        }
        if (string.IsNullOrEmpty(DatabaseName))
        {
            throw new ArgumentException($"{nameof(KustoOptions.Database)} is not configured in app settings");
        }

        KustoConnectionStringBuilder kcsb = new(KustoClusterUri);

        if (string.IsNullOrEmpty(ManagedIdentityId))
        {
            return kcsb.WithAadSystemManagedIdentity();
        }
        return kcsb.WithAadUserManagedIdentity(ManagedIdentityId);
    }

    public async Task<IDataReader> ExecuteKustoQueryAsync(KustoQuery query)
    {
        using var client = GetProvider();
        var properties = KustoHelpers.BuildClientRequestProperties(query);

        string text = KustoHelpers.BuildQueryString(query);

        try
        {
            return await client.ExecuteQueryAsync(
                DatabaseName,
                text,
                properties);
        }
        catch (SemanticException)
        {
            return null;
        }
    }

    /// <summary>
    /// Use this method to receive large quantities of data from Kusto in a "stream". 
    /// This method assumes the query is returning a single schema result set. 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<object[]> ExecuteStreamableKustoQuery(KustoQuery query)
    {
        using var client = GetProvider();
        var properties = KustoHelpers.BuildClientRequestProperties(query);
        properties.SetOption(ClientRequestProperties.OptionResultsProgressiveEnabled, true);

        string text = KustoHelpers.BuildQueryString(query);

        ProgressiveDataSet pDataSet = await client.ExecuteQueryV2Async(
            DatabaseName,
            text,
            properties);

        int tableCompletionCount = 0;

        using IEnumerator<ProgressiveDataSetFrame> frames = pDataSet.GetFrames();

        while (frames.MoveNext())
        {
            var frame = frames.Current;

            switch (frame.FrameType)
            {
                case FrameType.TableFragment:
                {
                    var content = frame as ProgressiveDataSetDataTableFragmentFrame;
                    while (GetNextRow(content, out object[] row))
                    {
                        yield return row;
                    }
                }
                    break;

                case FrameType.DataTable:
                {
                    // Note from documentation: we can't skip processing the data -- we must consume it.
                    var content = frame as ProgressiveDataSetDataTableFrame;
                    var reader = content.TableData;
                    while (reader.Read())
                    {
                        var writer = new System.IO.StringWriter();
                        reader.WriteAsText("", true, writer,
                            firstOnly: false,
                            markdown: false,
                            includeWithHeader: "ColumnType",
                            includeHeader: true);
                    }
                }
                    break;

                case FrameType.TableCompletion:
                    tableCompletionCount++;
                    break;

                case FrameType.DataSetHeader:
                case FrameType.TableHeader:
                case FrameType.TableProgress:
                case FrameType.DataSetCompletion:
                case FrameType.LastInvalid:
                default:
                    break;
            }

            if (tableCompletionCount > 1)
            {
                throw new ArgumentException("This method does not support multiple data result sets.");
            }
        }

        bool GetNextRow(ProgressiveDataSetDataTableFragmentFrame frame, out object[] row)
        {
            row = new object[frame.FieldCount];
            return frame.GetNextRecord(row);
        }
    }

    public void Dispose()
    {
        _kustoQueryProvider?.Dispose();
        _monitor?.Dispose();
    }
}
