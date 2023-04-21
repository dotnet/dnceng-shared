// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DncEng.CommandLineLib;

public interface IConsoleBackend
{
    TextWriter Out { get; }
    TextWriter Error { get; }
    TextReader In { get; }
    void SetColor(ConsoleColor color);
    void ResetColor();
    Task<string> PromptAsync(string message);
    bool IsInteractive { get; }
}
