// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.DotNet.Web.Authentication.Tests.Controllers;

[Route("test-auth/anonymous")]
public class AnonymousAttributeController : ControllerBase
{
    [AllowAnonymous]
    [Route("no")]
    public IActionResult NoAttribute()
    {
        return Ok("Anonymous:No:Value");
    }

    [AllowAnonymous]
    [Route("anonymous")]
    public IActionResult AnonymousAttribute()
    {
        return Ok("Anonymous:Anonymous:Value");
    }

    [Authorize, AllowAnonymous]
    [Route("any")]
    public IActionResult Any()
    {
        return Ok("Anonymous:Any:Value");
    }

    [Authorize(Roles = "ActionRole"), AllowAnonymous]
    [Route("role")]
    public IActionResult RoleAttribute()
    {
        return Ok("Anonymous:Role:Value");
    }
}
