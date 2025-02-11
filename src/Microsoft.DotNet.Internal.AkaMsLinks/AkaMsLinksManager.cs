// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.DotNet.Services.Utility;

namespace Microsoft.DotNet.Internal.AkaMsLinks;

/// <summary>
///     A single aka.ms link.
/// </summary>
public record AkaMsLink
{
    /// <summary>
    /// Target of the link
    /// </summary>
    public required string TargetUrl { get; set; }

    /// <summary>
    /// Short url of the link. Should only include the fragment element of the url, not the full aka.ms
    /// link.
    /// </summary>
    public required string ShortUrl { get; set; }

    /// <summary>
    /// Description of the link.
    /// </summary>
    public required string Description { get; set; }
}

public interface IAkaMsLinksManager
{
    public Task CreateOrUpdateLinksAsync(
        IEnumerable<AkaMsLink> links,
        string linkOwners,
        string linkCreatedOrUpdatedBy,
        string linkGroupOwner);

    public Task DeleteLinksAsync(List<string> linksToDelete);
}

public class AkaMsLinksManager: IAkaMsLinksManager
{
    private const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

    internal const string ApiBaseUrl = "https://redirectionapi.trafficmanager.net/api/aka"; // Base url for aka.ms API
    internal const string Endpoint = "https://microsoft.onmicrosoft.com/redirectionapi"; // Token scope endpoint for aka.ms

    internal static string ApiTargetUrl => $"{ApiBaseUrl}/1/{TenantId}";

    /// <summary>
    ///     Aka.ms max links per batch request. There are two maximums:
    ///         - Number of links per batch (300)
    ///         - Max content size per request (50k)
    ///     It's really easy to go over 50k after content encoding is done if the
    ///     maximum number of links per requests is reached. So we limit the max size
    ///     to 100 which is typically ~70% of the overall allowable size. This has plenty of
    ///     breathing room if the link targets were to get a lot larger.
    /// </summary>
    private const int BulkApiBatchSize = 100;

    private ExponentialRetry _retryHandler;
    private TokenCredential _credential;
    private IHttpClientFactory _clientFactory;
    private ILogger _log;

    public AkaMsLinksManager(TokenCredential credential, ILogger logger, IHttpClientFactory clientFactory)
    {
        _retryHandler = ExponentialRetry.Default;
        _credential = credential;
        _log = logger;
        _clientFactory = clientFactory;
    }

    private async Task<HttpClient> CreateClient()
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext([$"{Endpoint}/.default"]), default);

        HttpClient httpClient = _clientFactory.CreateClient(nameof(AkaMsLinksManager));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        return httpClient;
    }

    /// <summary>
    /// Create or update one or more links
    /// </summary>
    /// <param name="links">Set of links to create or update</param>
    /// <param name="linkCreatedOrUpdatedBy">The alias of the link creator. Must be valid</param>
    /// <param name="linkGroupOwner">SG owner of the link</param>
    /// <param name="linkOwners">Semicolon delimited list of link owners.</param>
    /// <returns>Async task</returns>
    public async Task CreateOrUpdateLinksAsync(
        IEnumerable<AkaMsLink> links,
        string linkOwners,
        string linkCreatedOrUpdatedBy,
        string linkGroupOwner)
    {
        _log.LogInformation("Creating/Updating {linkCount} aka.ms links.", links.Count());

        (IReadOnlyCollection<AkaMsLink> linksToCreate, IReadOnlyCollection<AkaMsLink> linksToUpdate) = await BucketLinksAsync(links);

        if (linksToCreate.Any())
        {
            await CreateOrUpdateLinkBatchAsync(linksToCreate, linkOwners, linkCreatedOrUpdatedBy, linkGroupOwner, update: false);
        }
        if (linksToUpdate.Any())
        {
            await CreateOrUpdateLinkBatchAsync(linksToUpdate, linkOwners, linkCreatedOrUpdatedBy, linkGroupOwner, update: true);
        }

        _log.LogInformation("Completed creating/updating {linkCount} aka.ms links.", links.Count());
    }

    /// <summary>
    /// Bucket links by whether they exist or not.
    /// </summary>
    /// <param name="links">Links to bucket.</param>
    /// <returns>Tuple of links to create and links to update.</returns>
    private async Task<(IReadOnlyCollection<AkaMsLink> linksToCreate, IReadOnlyCollection<AkaMsLink> linksToUpdate)> BucketLinksAsync(
        IEnumerable<AkaMsLink> links)
    {
        var linksToCreate = new ConcurrentBag<AkaMsLink>();
        var linksToUpdate = new ConcurrentBag<AkaMsLink>();

        using (HttpClient client = await CreateClient())
        using (var clientThrottle = new SemaphoreSlim(8, 8))
        {
            await Task.WhenAll(links.Select(async link =>
            {
                try
                {
                    await clientThrottle.WaitAsync();

                    await RequestWithRetry(async () =>
                    {
                        var response = await client.GetAsync($"{ApiTargetUrl}/{link.ShortUrl}");

                        // If 200, then the link should be updated, if 400, then it should be
                        // created
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                                linksToUpdate.Add(link);
                                break;
                            case HttpStatusCode.NotFound:
                                linksToCreate.Add(link);
                                break;
                            default:
                                response.EnsureSuccessStatusCode();
                                break;
                        }
                    },
                    retryMessage: $"Failed to bucket aka.ms/{link.ShortUrl}");
                }
                finally
                {
                    clientThrottle.Release();
                }
            }));
        }

        return (linksToCreate, linksToUpdate);
    }

    /// <summary>
    /// Create or update links.
    /// </summary>
    /// <param name="links">Set of links to create or update</param>
    /// <param name="linkCreatedOrUpdatedBy">The alias of the link creator. Must be valid</param>
    /// <param name="linkGroupOwner">SG owner of the link</param>
    /// <param name="linkOwners">Semicolon delimited list of link owners.</param>
    /// <param name="update">If true, existing links will be overwritten.</param>
    /// <returns>Async task</returns>
    private async Task CreateOrUpdateLinkBatchAsync(IReadOnlyCollection<AkaMsLink> links, string linkOwners,
        string linkCreatedOrUpdatedBy, string linkGroupOwner, bool update)
    {
        // Batch these up by the max batch size
        var linkBatches = new List<IEnumerable<AkaMsLink>>();

        IEnumerable<AkaMsLink> remainingLinks = links;
        while (remainingLinks.Any())
        {
            linkBatches.Add(remainingLinks.Take(BulkApiBatchSize));
            remainingLinks = remainingLinks.Skip(BulkApiBatchSize);
        }

        await Task.WhenAll(linkBatches.Select(async batch =>
        {
            _log.LogInformation("{action} batch of {linksCount} aka.ms links.", update ? "Updating" : "Creating", links.Count());

            using (HttpClient client = await CreateClient())
            {
                string newOrUpdatedLinksJson =
                    GetCreateOrUpdateLinkJson(linkOwners, linkCreatedOrUpdatedBy, linkGroupOwner, update, links);

                await RequestWithRetry(async () =>
                {
                    var requestMessage = new HttpRequestMessage(update ? HttpMethod.Put : HttpMethod.Post,
                            $"{ApiTargetUrl}/bulk")
                    {
                        Content = new StringContent(newOrUpdatedLinksJson, Encoding.UTF8, "application/json")
                    };

                    _log.LogInformation("Sending {action} request for batch of {linksCount} aka.ms links.", update ? "update" : "create", links.Count());

                    HttpResponseMessage response = await client.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();
                },
                retryMessage: "Failed to create/update aka.ms links");

                _log.LogInformation("Completed aka.ms create/update for batch {Count} links.", links.Count());
            }
        }));

    }

    private async Task RequestWithRetry(Func<Task> function, string retryMessage)
    {
        await _retryHandler.RetryAsync(
            function,
            ex =>
            {
                _log.LogError("{retryMessage}: {errorMessage}", retryMessage, ex.Message);
            },
            ex =>
            {
                if (ex is HttpRequestException httpEx)
                {
                    // 400, 401, and 403 indicate auth failure or bad requests that should not be retried.
                    // Check for auth failures/bad request on POST (400, 401, and 403).
                    // Aka.MS will return 500 on invalid JWT token and should be treated as an auth failure.
                    if (httpEx.StatusCode == HttpStatusCode.BadRequest ||
                        httpEx.StatusCode == HttpStatusCode.Unauthorized ||
                        httpEx.StatusCode == HttpStatusCode.Forbidden ||
                        httpEx.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        if (httpEx.StatusCode == HttpStatusCode.InternalServerError)
                        {
                            _log.LogWarning("AkaMS will return status code 500 if the JWT token is invalid.");
                        }

                        return false;
                    }
                }
                return true;
            });
    }

    /// <summary>
    /// Get the json needed to create or update links.
    /// </summary>
    /// <param name="linkOwners">Link owners. Semicolon delimited list of aliases</param>
    /// <param name="linkCreatedOrUpdatedBy">Aliases of link creator and updator</param>
    /// <param name="linkGroupOwner">Alias of group owner. Can be empty</param>
    /// <param name="overwrite">If true, overwrite existing links, otherwise fail if they already exist.</param>
    /// <param name="batchOfLinksToCreateOrUpdate">Links to create/update</param>
    /// <returns>String representation of the link creation content</returns>
    internal static string GetCreateOrUpdateLinkJson(string linkOwners, string linkCreatedOrUpdatedBy, string linkGroupOwner,
        bool update, IEnumerable<AkaMsLink> batchOfLinksToCreateOrUpdate)
    {
        if (update)
        {
            return JsonSerializer.Serialize(batchOfLinksToCreateOrUpdate.Select(link =>
            {
                return new
                {
                    shortUrl = link.ShortUrl,
                    owners = linkOwners,
                    targetUrl = link.TargetUrl,
                    lastModifiedBy = linkCreatedOrUpdatedBy,
                    description = link.Description,
                    groupOwner = linkGroupOwner,
                    isAllowParam = true
                };
            }));
        }
        else
        {
            return JsonSerializer.Serialize(batchOfLinksToCreateOrUpdate.Select(link =>
            {
                return new
                {
                    shortUrl = link.ShortUrl,
                    owners = linkOwners,
                    targetUrl = link.TargetUrl,
                    lastModifiedBy = linkCreatedOrUpdatedBy,
                    description = link.Description,
                    groupOwner = linkGroupOwner,

                    // Create specific items
                    createdBy = linkCreatedOrUpdatedBy,
                    isVanity = !string.IsNullOrEmpty(link.ShortUrl),
                    isAllowParam = true
                };
            }));
        }
    }

    /// <summary>
    /// Delete one or more aka.ms links
    /// </summary>
    /// <param name="linksToDelete">Links to delete. Should not be prefixed with 'aka.ms'</param>
    /// <returns>Async task</returns>
    public async Task DeleteLinksAsync(List<string> linksToDelete)
    {
        // The bulk hard-delete APIs do not have short-url forms (only identity), so they must be
        // deleted individually. Use a semaphore to avoid excessive numbers of concurrent API calls

        using (HttpClient client = await CreateClient())
        {
            using (var clientThrottle = new SemaphoreSlim(8, 8))
            {
                await Task.WhenAll(linksToDelete.Select(async link =>
                {
                    try
                    {
                        await clientThrottle.WaitAsync();

                        await RequestWithRetry(async () =>
                        {
                            var response = await client.DeleteAsync($"{ApiTargetUrl}/harddelete/{link}");
                            if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound)
                            {
                                return;
                            }
                            response.EnsureSuccessStatusCode();
                        },
                        retryMessage: $"Failed to delete aka.ms/{link}");
                    }
                    finally
                    {
                        clientThrottle.Release();
                    }
                }));
            }
        }
    }
}
