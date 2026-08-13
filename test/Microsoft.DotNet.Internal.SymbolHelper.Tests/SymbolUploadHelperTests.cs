// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using Azure.Core;
using Microsoft.SymbolStore;
using Moq;
using NUnit.Framework;

namespace Microsoft.DotNet.Internal.SymbolHelper.Tests;

[TestFixture]
public class SymbolUploadHelperTests
{
    private static readonly ConstructorInfo s_constructor = typeof(SymbolUploadHelper).GetConstructor(
        BindingFlags.NonPublic | BindingFlags.Instance,
        binder: null,
        [typeof(ITracer), typeof(string), typeof(SymbolPublisherOptions), typeof(string)],
        modifiers: null)!;
    private static readonly MethodInfo s_validateArchiveEntryPath = typeof(SymbolUploadHelper).GetMethod(
        "GetValidatedArchiveEntryPath",
        BindingFlags.NonPublic | BindingFlags.Static)!;
    private string _workingDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _ = Directory.CreateDirectory(_workingDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_workingDirectory, recursive: true);
    }

    [TestCase("../../../evil.dll")]
    [TestCase(@"..\..\..\evil.dll")]
    [TestCase(@"../..\evil.dll")]
    [TestCase("/evil.dll")]
    [TestCase(@"\evil.dll")]
    [TestCase(@"C:\evil.dll")]
    [TestCase("C:/evil.dll")]
    [TestCase("C:evil.dll")]
    [TestCase(@"\\server\share\evil.dll")]
    [TestCase("//server/share/evil.dll")]
    [TestCase(@"..\root-evil\evil.dll")]
    [TestCase("../../outside/")]
    [TestCase(@"..\..\outside\")]
    public void ArchivePathsOutsideExtractionRootAreRejected(string entryName)
    {
        string extractionRoot = Path.Combine(_workingDirectory, "root");

        Action validate = () => ValidateArchiveEntryPath(extractionRoot, entryName);

        validate.Should().Throw<InvalidDataException>();
    }

    [TestCase("nested/../safe.dll", "safe.dll")]
    [TestCase(@"nested\safe.exe", @"nested\safe.exe")]
    [TestCase("nested/safe.pdb", @"nested\safe.pdb")]
    public void ArchivePathsWithinExtractionRootAreAccepted(string entryName, string expectedRelativePath)
    {
        string extractionRoot = Path.Combine(_workingDirectory, "root");

        string result = ValidateArchiveEntryPath(extractionRoot, entryName);

        string platformRelativePath = expectedRelativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        result.Should().Be(Path.Combine(extractionRoot, platformRelativePath));
    }

    [TestCase("nested/")]
    [TestCase(@"nested\")]
    public void DirectoryEntriesWithinExtractionRootAreAccepted(string entryName)
    {
        string extractionRoot = Path.Combine(_workingDirectory, "root");

        string result = ValidateArchiveEntryPath(extractionRoot, entryName);

        result.Should().Be(Path.Combine(extractionRoot, "nested") + Path.DirectorySeparatorChar);
    }

    [Test]
    public void CaseOnlyPathReentryUsesPlatformFileSystemSemantics()
    {
        string extractionRoot = Path.Combine(_workingDirectory, "root");
        const string entryName = @"..\ROOT\safe.dll";

        Action validate = () => ValidateArchiveEntryPath(extractionRoot, entryName);

        if (OperatingSystem.IsWindows())
        {
            validate.Should().NotThrow();
        }
        else
        {
            validate.Should().Throw<InvalidDataException>();
        }
    }

    [Test]
    public async Task MaliciousPackageIsRejectedBeforeAnyFileIsExtracted()
    {
        SymbolUploadHelper helper = CreateHelper();
        using MemoryStream package = CreatePackage(
            ("safe.dll", [1]),
            ("../outside.dll", [2]));

        int result = await helper.AddPackageToRequest("request", "malicious.nupkg", package);

        result.Should().Be(-1);
        File.Exists(Path.Combine(_workingDirectory, "outside.dll")).Should().BeFalse();
        Directory.GetFiles(_workingDirectory, "safe.dll", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Test]
    public async Task ValidNestedSymbolsAreProcessed()
    {
        SymbolUploadHelper helper = CreateHelper();
        using MemoryStream package = CreatePackage(
            ("lib/nested.dll", [1]),
            (@"tools\nested.exe", [2]),
            ("symbols/nested.pdb", [3]));

        int result = await helper.AddPackageToRequest("request", "valid.nupkg", package);

        result.Should().Be(0);
    }

    private SymbolUploadHelper CreateHelper()
    {
        var options = new SymbolPublisherOptions(
            "dnceng",
            Mock.Of<TokenCredential>(),
            isDryRun: true);
        return (SymbolUploadHelper)s_constructor.Invoke(
            [Mock.Of<ITracer>(), Path.Combine(_workingDirectory, "sym"), options, _workingDirectory]);
    }

    private static string ValidateArchiveEntryPath(string extractionRoot, string entryName)
    {
        try
        {
            return (string)s_validateArchiveEntryPath.Invoke(null, [extractionRoot, entryName])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static MemoryStream CreatePackage(params (string Name, byte[] Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach ((string name, byte[] content) in entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name);
                using Stream entryStream = entry.Open();
                entryStream.Write(content);
            }
        }
        stream.Position = 0;
        return stream;
    }
}
