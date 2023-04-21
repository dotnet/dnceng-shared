// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Data;
using System.Collections.Generic;

namespace Microsoft.DotNet.Kusto;

public interface IKustoClientProvider
{
    Task<IDataReader> ExecuteKustoQueryAsync(KustoQuery query);

    IAsyncEnumerable<object[]> ExecuteStreamableKustoQuery(KustoQuery query);
}
