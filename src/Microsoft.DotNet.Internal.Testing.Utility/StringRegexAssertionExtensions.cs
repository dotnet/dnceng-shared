// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using AwesomeAssertions;
using AwesomeAssertions.Primitives;

namespace Microsoft.DotNet.Internal.Testing.Utility;

public static class StringRegexAssertionExtensions{
    public static AndConstraint<StringAssertions> MatchRegex(
        this StringAssertions stringAssertion,
        Regex regularExpression,
        string because = "",
        params object[] becauseArgs
    )
    {
        stringAssertion.Subject.Should().NotBeNull(
            $"Expected string to match regex {regularExpression}{(because != string.Empty ? " because " + because : string.Empty)}, but it was <null>.");

        regularExpression.IsMatch(stringAssertion.Subject).Should().BeTrue(
            $"Expected string to match regex {regularExpression}{(because != string.Empty ? " because " + because : string.Empty)}, but {stringAssertion.Subject} does not match.");

        return new AndConstraint<StringAssertions>(stringAssertion);
    }
}
