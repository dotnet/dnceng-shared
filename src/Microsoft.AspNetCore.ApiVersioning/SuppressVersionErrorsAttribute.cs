// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.AspNetCore.ApiVersioning;

/// <summary>
///   This attribute marks the attributed type as "versioned" so the roslyn analyzer won't warn about it.
///   This should only be used on types that never change and are shared by all api versions, like error models.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SuppressVersionErrorsAttribute : Attribute
{
}
