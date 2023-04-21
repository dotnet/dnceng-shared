// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.DotNet.Internal.DependencyInjection;

public class SingleClientFactory<T> : IClientFactory<T>
{
    private readonly RefCountedObject<T> _client;

    public SingleClientFactory(T client)
    {
        _client = new RefCountedObject<T>(client);
    }
        
    public Reference<T> GetClient(string name)
    {
        return new Reference<T>(_client);
    }
}
