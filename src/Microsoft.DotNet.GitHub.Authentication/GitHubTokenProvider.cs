// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Services.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace Microsoft.DotNet.GitHub.Authentication;

public class GitHubTokenProvider : IGitHubTokenProvider
{
    private readonly IInstallationLookup _installationLookup;
    private readonly IGitHubAppTokenProvider _tokens;
    private readonly IOptions<GitHubClientOptions> _gitHubClientOptions;
    private readonly ConcurrentDictionary<long, AccessToken> _tokenCache;
    private readonly ILogger<GitHubTokenProvider> _logger;
    private readonly ExponentialRetry _retry;
    private readonly SemaphoreSlim _semaphore = new(1);

    public GitHubTokenProvider(
        IInstallationLookup installationLookup,
        IGitHubAppTokenProvider tokens,
        IOptions<GitHubClientOptions> gitHubClientOptions,
        ILogger<GitHubTokenProvider> logger,
        ExponentialRetry retry)
    {
        _installationLookup = installationLookup;
        _tokens = tokens;
        _gitHubClientOptions = gitHubClientOptions;
        _logger = logger;
        _retry = retry;
        _tokenCache = new ConcurrentDictionary<long, AccessToken>();
    }

    public async Task<string> GetTokenForInstallationAsync(long installationId)
    {
        if (TryGetCachedToken(installationId, out AccessToken cachedToken))
        {
            _logger.LogInformation("Cached token obtained for GitHub installation {installationId}. Expires at {tokenExpiresAt}.", installationId, cachedToken.ExpiresAt);
            return cachedToken.Token;
        }

        await _semaphore.WaitAsync();
        try
        {
            return await _retry.RetryAsync(async () =>
            {
                if (TryGetCachedToken(installationId, out cachedToken))
                {
                    _logger.LogInformation("Cached token obtained for GitHub installation {installationId}. Expires at {tokenExpiresAt}.", installationId, cachedToken.ExpiresAt);
                    return cachedToken.Token;
                }

                string jwt = _tokens.GetAppToken();
                var appClient = new GitHubClient(_gitHubClientOptions.Value.ProductHeader)
                {
                    Credentials = new Credentials(jwt, AuthenticationType.Bearer)
                };

                AccessToken token = await appClient.GitHubApps.CreateInstallationToken(installationId);

                _logger.LogInformation("New token obtained for GitHub installation {installationId}. Expires at {tokenExpiresAt} UTC.",
                    installationId,
                    token.ExpiresAt.UtcDateTime);
                UpdateTokenCache(installationId, token);
                return token.Token;
            },
            ex => _logger.LogError(ex, "Failed to get a github token for installation id {installationId}, retrying", installationId),
            ex => ex is ApiException exception && exception.StatusCode == HttpStatusCode.InternalServerError);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void InvalidateTokenCacheAsync(long installationId)
    {
        if (_tokenCache.TryRemove(installationId, out AccessToken _))
        {
            _logger.LogInformation("Token cache invalidated for GitHub installation {installationId}.", installationId);
        }
    }

    public string GetTokenForApp()
    {
        return _tokens.GetAppToken();
    }

    public string GetTokenForApp(string name)
    {
        return _tokens.GetAppToken(name);
    }

    public async Task<string> GetTokenForRepository(string repositoryUrl)
    {
        return await GetTokenForInstallationAsync(await _installationLookup.GetInstallationId(repositoryUrl));
    }

    private bool TryGetCachedToken(long installationId, out AccessToken cachedToken)
    {
        if (!_tokenCache.TryGetValue(installationId, out cachedToken))
        {
            return false;
        }

        // If the cached token will expire in less than 30 minutes we won't use it,
        // Instead GetTokenForInstallationAsync will generate a new one and update the cache
        if (cachedToken.ExpiresAt.UtcDateTime.Subtract(DateTime.UtcNow).TotalMinutes < 15)
        {
            cachedToken = null;
            return false;
        }

        return true;
    }

    private void UpdateTokenCache(long installationId, AccessToken accessToken)
    {
        _tokenCache.AddOrUpdate(installationId, accessToken, (installation, token) => token = accessToken);
    }
}
