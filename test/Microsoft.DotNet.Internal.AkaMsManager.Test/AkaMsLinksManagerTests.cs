// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Internal.Testing.Utility;
using Azure.Core;
using NUnit.Framework;

namespace Microsoft.DotNet.Internal.AkaMsManager.Test;

public class AkaMsLinksManagerTests
{
    private const string _linkOwners = "linkOwner1;linkOwner2";
    private const string _createdBy = "linkOwner1";
    private const string _groupOwner = "linkGroupOwner";

    private static readonly MockHttpClientFactory _clientFactory = new MockHttpClientFactory();
    private static readonly AkaMsLinksManager _manager = new AkaMsLinksManager(
        new TestTokenCredential("test-token"), new NUnitLogger(), _clientFactory);

    private static readonly AkaMsLink _validLink = new()
    {
        TargetUrl = "targetUrl",
        ShortUrl = "shortUrl",
        Description = "description",
    };

    [Test]
    public async Task LinkCreatedTest()
    {
        _clientFactory.AddCannedResponse($"{AkaMsLinksManager.ApiTargetUrl}/{_validLink.ShortUrl}", "", System.Net.HttpStatusCode.NotFound);

        var requestBody = AkaMsLinksManager.GetCreateOrUpdateLinkJson(_linkOwners, _createdBy, _groupOwner, false, [_validLink]);
        _clientFactory.AddCannedResponse(
            $"{AkaMsLinksManager.ApiTargetUrl}/bulk",
            requestBody,
            System.Net.HttpStatusCode.OK,
            HttpMethod.Post);

        await _manager.CreateOrUpdateLinksAsync([_validLink], _linkOwners, _createdBy, _groupOwner);

        _clientFactory.VerifyAll();
    }

    [Test]
    public async Task LinkUpdatedTest()
    {
        _clientFactory.AddCannedResponse($"{AkaMsLinksManager.ApiTargetUrl}/{_validLink.ShortUrl}", "");

        var requestBody = AkaMsLinksManager.GetCreateOrUpdateLinkJson(_linkOwners, _createdBy, _groupOwner, true, [_validLink]);
        _clientFactory.AddCannedResponse(
            $"{AkaMsLinksManager.ApiTargetUrl}/bulk",
            requestBody,
            System.Net.HttpStatusCode.OK,
            HttpMethod.Put);

        await _manager.CreateOrUpdateLinksAsync([_validLink], _linkOwners, _createdBy, _groupOwner);

        _clientFactory.VerifyAll();
    }

    [Test]
    public async Task LinksBucketedTest()
    {
        List<AkaMsLink> links = [];
        for (int i = 0; i < 5; i++)
        {
            links.Add(new AkaMsLink()
            {
                TargetUrl = "targetUrl",
                ShortUrl = $"shortUrl{i}",
                Description = "description",
            });
        }

        List<AkaMsLink> linksToCreate = links[..2];
        foreach (var link in linksToCreate)
        {
            _clientFactory.AddCannedResponse($"{AkaMsLinksManager.ApiTargetUrl}/{link.ShortUrl}", "", System.Net.HttpStatusCode.NotFound);
        }

        var createRequestBody = AkaMsLinksManager.GetCreateOrUpdateLinkJson(_linkOwners, _createdBy, _groupOwner, false, linksToCreate);
        _clientFactory.AddCannedResponse(
            $"{AkaMsLinksManager.ApiTargetUrl}/bulk",
            createRequestBody,
            System.Net.HttpStatusCode.OK,
            HttpMethod.Post);

        List<AkaMsLink> linksToUpdate = links[2..];
        foreach (var link in linksToUpdate)
        {
            _clientFactory.AddCannedResponse($"{AkaMsLinksManager.ApiTargetUrl}/{link.ShortUrl}", "");
        }

        var updateRequestBody = AkaMsLinksManager.GetCreateOrUpdateLinkJson(_linkOwners, _createdBy, _groupOwner, true, linksToUpdate);
        _clientFactory.AddCannedResponse(
            $"{AkaMsLinksManager.ApiTargetUrl}/bulk",
            updateRequestBody,
            System.Net.HttpStatusCode.OK,
            HttpMethod.Put);

        await _manager.CreateOrUpdateLinksAsync(links, _linkOwners, _createdBy, _groupOwner);

        _clientFactory.VerifyAll();
    }
}

public class TestTokenCredential(string token) : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(token, DateTimeOffset.MaxValue);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new AccessToken(token, DateTimeOffset.MaxValue));
    }
}
