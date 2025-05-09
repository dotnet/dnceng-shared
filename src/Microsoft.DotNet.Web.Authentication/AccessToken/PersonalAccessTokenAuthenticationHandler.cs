// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.Web.Authentication.AccessToken;

public static class PersonalAccessTokenUtilities
{
    public static int TokenIdByteCount => sizeof(int);
    public static int CalculateTokenSizeForPasswordSize(int passwordSize) => TokenIdByteCount + passwordSize;

    public static string EncodeToken(int tokenId, byte[] password)
    {
        byte[] tokenIdBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(tokenId));
        byte[] outputBytes = tokenIdBytes.Concat(password).ToArray();
        return WebEncoders.Base64UrlEncode(outputBytes);
    }

    public static string EncodePasswordBytes(byte[] passwordBytes)
    {
        return WebEncoders.Base64UrlEncode(passwordBytes);
    }
}

public class PersonalAccessTokenAuthenticationHandler<TUser> :
    AuthenticationHandler<PersonalAccessTokenAuthenticationOptions<TUser>> where TUser : class
{
    public PersonalAccessTokenAuthenticationHandler(
        IOptionsMonitor<PersonalAccessTokenAuthenticationOptions<TUser>> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IPasswordHasher<TUser> passwordHasher,
        SignInManager<TUser> signInManager) : base(options, logger, encoder)
    {
        PasswordHasher = passwordHasher;
        SignInManager = signInManager;
    }

    public IPasswordHasher<TUser> PasswordHasher { get; }
    public SignInManager<TUser> SignInManager { get; }

    public new PersonalAccessTokenEvents<TUser> Events
    {
        get => (PersonalAccessTokenEvents<TUser>) base.Events;
        set => base.Events = value;
    }

    public int TokenByteCount => PersonalAccessTokenUtilities.CalculateTokenSizeForPasswordSize(Options.PasswordSize);

    protected override Task<object> CreateEventsAsync()
    {
        return Task.FromResult<object>(new PersonalAccessTokenEvents<TUser>());
    }

    private byte[] GeneratePassword()
    {
        var bytes = new byte[Options.PasswordSize];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return bytes;
    }

    private (int tokenId, string password)? DecodeToken(string input)
    {
        byte[] tokenBytes = WebEncoders.Base64UrlDecode(input);
        if (tokenBytes.Length != TokenByteCount)
        {
            return null;
        }

        int tokenId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(tokenBytes, 0));
        string password = WebEncoders.Base64UrlEncode(tokenBytes, PersonalAccessTokenUtilities.TokenIdByteCount, Options.PasswordSize);
        return (tokenId, password);
    }

    public async Task<(int id, string value)> CreateToken(TUser user, string name)
    {
        byte[] passwordBytes = GeneratePassword();
        string password = PersonalAccessTokenUtilities.EncodePasswordBytes(passwordBytes);
        string hash = PasswordHasher.HashPassword(user, password);
        var context = new SetTokenHashContext<TUser>(Context, user, name, hash);
        int tokenId = await Events.SetTokenHash(context);
        return (tokenId, PersonalAccessTokenUtilities.EncodeToken(tokenId, passwordBytes));
    }

    public async Task<TUser> VerifyToken(string token)
    {
        (int tokenId, string password)? decoded = DecodeToken(token);
        if (!decoded.HasValue)
        {
            return null;
        }

        (int tokenId, string password) = decoded.Value;

        var context = new GetTokenHashContext<TUser>(Context, tokenId);
        await Events.GetTokenHash(context);
        if (!context.Succeeded)
        {
            return null;
        }

        string hash = context.Hash;
        TUser user = context.User;

        PasswordVerificationResult result = PasswordHasher.VerifyHashedPassword(user, hash, password);

        if (result == PasswordVerificationResult.Success ||
            result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            return user;
        }

        return null;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            string token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.NoResult();
            }

            TUser user = await VerifyToken(token);

            if (user == null)
            {
                return AuthenticateResult.NoResult();
            }

            ClaimsPrincipal principal = await SignInManager.CreateUserPrincipalAsync(user);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            var context = new PersonalAccessTokenValidatePrincipalContext<TUser>(
                Context,
                Scheme,
                Options,
                ticket,
                user);
            await Events.ValidatePrincipal(context);
            if (context.Principal == null)
            {
                return AuthenticateResult.Fail("No principal.");
            }

            return AuthenticateResult.Success(
                new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name));
        }
        catch (Exception)
        {
            return AuthenticateResult.NoResult();
        }
    }

    private string GetToken()
    {
        string authorization = Request.Headers["Authorization"];

        if (!string.IsNullOrEmpty(authorization))
        {
            string authPrefix = Options.TokenName + " ";

            if (authorization.StartsWith(authPrefix))
            {
                return authorization.Substring(authPrefix.Length).Trim();
            }
        }

        return Events.GetTokenFromRequest(Request);
    }
}
