// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Security.Utilities;

namespace Microsoft.DotNet.Web.Authentication.AccessToken;

public static class PersonalAccessTokenUtilities
{
    internal const string VersionTwoTokenPrefix = "dnp2.";

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

    internal static string EncodeVersionTwoToken(int tokenId, string password)
    {
        if (tokenId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenId));
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("The token password must not be empty.", nameof(password));
        }

        if (password.Contains('.'))
        {
            throw new ArgumentException("The token password must not contain the token separator.", nameof(password));
        }

        return $"{VersionTwoTokenPrefix}{tokenId.ToString(CultureInfo.InvariantCulture)}.{password}";
    }

    internal static bool TryDecodeToken(
        string input,
        int legacyPasswordSize,
        out int tokenId,
        out string password)
    {
        tokenId = default;
        password = null;

        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        if (input.StartsWith(VersionTwoTokenPrefix, StringComparison.Ordinal))
        {
            return TryDecodeVersionTwoToken(input, out tokenId, out password);
        }

        try
        {
            byte[] tokenBytes = WebEncoders.Base64UrlDecode(input);
            if (tokenBytes.Length != CalculateTokenSizeForPasswordSize(legacyPasswordSize))
            {
                return false;
            }

            tokenId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(tokenBytes, 0));
            password = WebEncoders.Base64UrlEncode(tokenBytes, TokenIdByteCount, legacyPasswordSize);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryDecodeVersionTwoToken(
        string input,
        out int tokenId,
        out string password)
    {
        tokenId = default;
        password = null;

        int tokenIdStart = VersionTwoTokenPrefix.Length;
        int passwordSeparator = input.IndexOf('.', tokenIdStart);
        if (passwordSeparator <= tokenIdStart || passwordSeparator == input.Length - 1)
        {
            return false;
        }

        if (input.IndexOf('.', passwordSeparator + 1) >= 0)
        {
            return false;
        }

        if (!int.TryParse(
                input.AsSpan(tokenIdStart, passwordSeparator - tokenIdStart),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out tokenId) ||
            tokenId < 0)
        {
            tokenId = default;
            return false;
        }

        password = input.Substring(passwordSeparator + 1);
        return true;
    }
}

public class PersonalAccessTokenAuthenticationHandler<TUser> :
    AuthenticationHandler<PersonalAccessTokenAuthenticationOptions<TUser>> where TUser : class
{
    internal const string HisV2ProviderSignature = "DNHP";

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

    /// <summary>
    /// Gets the decoded byte count of a legacy personal access token.
    /// </summary>
    public int TokenByteCount => PersonalAccessTokenUtilities.CalculateTokenSizeForPasswordSize(Options.PasswordSize);

    protected override Task<object> CreateEventsAsync()
    {
        return Task.FromResult<object>(new PersonalAccessTokenEvents<TUser>());
    }

    private static string GeneratePassword()
    {
        return IdentifiableSecrets.GenerateCommonAnnotatedKey(
            base64EncodedSignature: HisV2ProviderSignature,
            customerManagedKey: false,
            platformReserved: null,
            providerReserved: null,
            longForm: false);
    }

    private (int tokenId, string password)? DecodeToken(string input)
    {
        if (!PersonalAccessTokenUtilities.TryDecodeToken(
                input,
                Options.PasswordSize,
                out int tokenId,
                out string password))
        {
            return null;
        }

        return (tokenId, password);
    }

    public async Task<(int id, string value)> CreateToken(TUser user, string name)
    {
        string password = GeneratePassword();
        string hash = PasswordHasher.HashPassword(user, password);
        var context = new SetTokenHashContext<TUser>(Context, user, name, hash);
        int tokenId = await Events.SetTokenHash(context);
        return (tokenId, PersonalAccessTokenUtilities.EncodeVersionTwoToken(tokenId, password));
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
