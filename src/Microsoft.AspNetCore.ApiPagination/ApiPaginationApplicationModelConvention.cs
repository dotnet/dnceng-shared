// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.ApiPagination;

public class ApiPaginationApplicationModelConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (ControllerModel controller in application.Controllers)
        {
            foreach (ActionModel action in controller.Actions)
            {
                var paginatedAttribute = action.ActionMethod.GetCustomAttribute<PaginatedAttribute>();
                if (paginatedAttribute != null)
                {
                    foreach (ParameterModel param in paginatedAttribute.CreateParameterModels())
                    {
                        param.Action = action;
                        action.Parameters.Add(param);
                    }
                }
            }
        }
    }
}
