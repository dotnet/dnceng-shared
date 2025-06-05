// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using AwesomeAssertions.Json;
using NUnit.Framework;

namespace Microsoft.DotNet.Internal.Health.Tests;

public class AzureTableHealthReportProviderTests
{     
    /// <summary>
    /// This functionality is bit implementation specific, but we need to make sure we handle weird values
    /// because AzureTables doesn't allow for some characters
    /// </summary>
    /// <param name="input">String to check in all escaped strings</param>
    [TestCase("basic", "basic")]
    [TestCase("with space", "with space")]
    [TestCase("with:colon", "with:colon:colon")]
    [TestCase("with/slash", "with:slash:slash")]
    [TestCase("with\\backslash", "with:back:backslash")]
    [TestCase("with#hash", "with:hash:hash")]
    [TestCase("with?question", "with:question:question")]
    [TestCase("with|pipe", "with:pipe:pipe")]
    public void CheckEscaping(string input, string expected)
    {
        AzureTableHealthReportProvider.EscapeKeyField(input).Should().BeEquivalentTo(expected);
    }

    [TestCase("basic", "basic")]
    [TestCase("with space", "with space")]
    [TestCase("with:colon:colon", "with:colon")]
    [TestCase("with:slash:slash", "with/slash")]
    [TestCase("with:back:backslash", "with\\backslash")]
    [TestCase("with:hash:hash", "with#hash")]
    [TestCase("with:question:question", "with?question")]
    [TestCase("with:pipe:pipe", "with|pipe")]
    public void CheckUnescaping(string input, string expected)
    {
        AzureTableHealthReportProvider.UnescapeKeyField(input).Should().BeEquivalentTo(expected);
    }
}
