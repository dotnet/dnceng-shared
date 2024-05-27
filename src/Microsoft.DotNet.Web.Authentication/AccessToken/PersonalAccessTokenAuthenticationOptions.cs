// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;

namespace Microsoft.DotNet.Web.Authentication.AccessToken;

public class PersonalAccessTokenAuthenticationOptions<TUser> : AuthenticationSchemeOptions
{
    public const int DefaultPasswordSize = 16;

    public new PersonalAccessTokenEvents<TUser> Events
    {
        get => (PersonalAccessTokenEvents<TUser>) base.Events;
        set => base.Events = value;
    }

    public int PasswordSize { get; set; } = DefaultPasswordSize;

    public string TokenName { get; set; } = "Bearer";
}
