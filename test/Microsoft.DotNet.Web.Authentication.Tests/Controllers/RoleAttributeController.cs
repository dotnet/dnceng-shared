// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.DotNet.Web.Authentication.Tests.Controllers;

[Route("test-auth/role")]
[Authorize(Roles = "ControllerRole")]
public class RoleAttributeController : ControllerBase
{
    [Route("no")]
    public IActionResult NoAttribute()
    {
        return Ok("Role:No:Value");
    }

    [AllowAnonymous]
    [Route("anonymous")]
    public IActionResult AnonymousAttribute()
    {
        return Ok("Role:Anonymous:Value");
    }

    [Authorize]
    [Route("any")]
    public IActionResult Any()
    {
        return Ok("Role:Any:Value");
    }

    [Authorize(Roles = "ActionRole")]
    [Route("role")]
    public IActionResult RoleAttribute()
    {
        return Ok("Role:Role:Value");
    }
}
