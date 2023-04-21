// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.DotNet.Web.Authentication.Tests;

[Route("test-auth/any")]
[Authorize]
public class AnyAttributeController : ControllerBase
{
    [Route("no")]
    public IActionResult NoAttribute()
    {
        return Ok("Any:No:Value");
    }

    [AllowAnonymous]
    [Route("anonymous")]
    public IActionResult AnonymousAttribute()
    {
        return Ok("Any:Anonymous:Value");
    }

    [Authorize]
    [Route("any")]
    [HttpGet]
    public IActionResult Any()
    {
        return Ok("Any:Any:Value");
    }

    [Authorize(Roles = "ActionRole")]
    [Route("role")]
    public IActionResult RoleAttribute()
    {
        return Ok("Any:Role:Value");
    }
}
