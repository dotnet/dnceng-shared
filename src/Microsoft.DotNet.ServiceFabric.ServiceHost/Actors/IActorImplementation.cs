// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Microsoft.DotNet.ServiceFabric.ServiceHost.Actors;

public interface IActorImplementation
{
    void Initialize(ActorId actorId, IActorStateManager stateManager, IReminderManager reminderManager);
}
