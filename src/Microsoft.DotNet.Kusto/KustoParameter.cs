// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.DotNet.Kusto;

public class KustoParameter
{
    public KustoParameter(string name, object value, KustoDataType type)
    {
        Name = name;
        Type = type;
        Value = value;
    }

    public string Name { get; }
    public KustoDataType Type { get; }
    public object Value { get; set; }
}
