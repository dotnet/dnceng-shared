// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.ApiVersioning.Swashbuckle;

[PublicAPI]
public interface ISwaggerVersioningScheme
{
    void Apply(OpenApiOperation operation, OperationFilterContext context, string version);
}
