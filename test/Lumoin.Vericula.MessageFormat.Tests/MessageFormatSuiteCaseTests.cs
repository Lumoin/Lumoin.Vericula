using System.Collections.Immutable;
using System.Reflection;
using Lumoin.Vericula.MessageFormat.Diagnostics;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Runs one test per case in the vendored Unicode MessageFormat 2.0 conformance suite, driven by
/// <see cref="MessageFormatSuite.AllCases"/> through <see cref="DynamicDataAttribute"/>, against
/// <see cref="MessageFormatReader.TryParse(string)"/>, asserting the full slice-1 data model assertion:
/// an empty static-expectation set means the case must parse to a valid, diagnostic-free model; a set
/// containing <see cref="WellKnownMessageFormatDiagnostics.SyntaxError"/> means the model must be null
/// with only syntax-error diagnostics; any other non-empty set means the model must be non-null and the
/// set of distinct diagnostic ids <see cref="MessageFormatReader.TryParse(string)"/> reports must equal
/// the expected set exactly.
/// </summary>
[TestClass]
public sealed class MessageFormatSuiteCaseTests
{
    /// <summary>Asserts one case against the three-way data model assertion above, on the static subset of its <c>expErrors</c>.</summary>
    /// <param name="testCase">One merged suite case, supplied by <see cref="Cases"/>.</param>
    [TestMethod]
    [DynamicData(nameof(Cases), DynamicDataDisplayName = nameof(GetDisplayName))]
    public void MatchesTheFullDataModelAssertion(SuiteCase testCase)
    {
        ImmutableArray<string> expectedIds = [.. testCase.ExpErrors
            .Where(WellKnownSuiteErrorTypes.IsStatic)
            .Select(WellKnownSuiteErrorTypes.ToDiagnosticId)];

        MessageParseResult result = MessageFormatReader.TryParse(testCase.Src);

        if(expectedIds.IsEmpty)
        {
            Assert.IsNotNull(result.Message, $"{MessageFormatSuite.DisplayName(testCase)} was expected to parse to a valid model.");
            Assert.IsTrue(result.Diagnostics.IsEmpty,
                $"{MessageFormatSuite.DisplayName(testCase)} reported {result.Diagnostics.Length} unexpected diagnostic(s): {string.Join(", ", result.Diagnostics.Select(d => d.Id))}.");

            return;
        }

        if(expectedIds.Contains(WellKnownMessageFormatDiagnostics.SyntaxError, StringComparer.Ordinal))
        {
            Assert.IsNull(result.Message, $"{MessageFormatSuite.DisplayName(testCase)} was expected to fail to parse.");
            Assert.IsFalse(result.Diagnostics.IsEmpty, $"{MessageFormatSuite.DisplayName(testCase)} reported no diagnostic.");
            Assert.IsTrue(
                result.Diagnostics.All(diagnostic => WellKnownMessageFormatDiagnostics.IsSyntaxError(diagnostic.Id)),
                $"{MessageFormatSuite.DisplayName(testCase)} reported a non-syntax-error diagnostic: {string.Join(", ", result.Diagnostics.Select(d => d.Id))}.");

            return;
        }

        Assert.IsNotNull(result.Message, $"{MessageFormatSuite.DisplayName(testCase)} was expected to parse to a model despite its data model diagnostics.");

        var actualIds = new HashSet<string>(result.Diagnostics.Select(diagnostic => diagnostic.Id), StringComparer.Ordinal);
        var expectedIdSet = new HashSet<string>(expectedIds, StringComparer.Ordinal);

        Assert.IsTrue(actualIds.SetEquals(expectedIdSet),
            $"{MessageFormatSuite.DisplayName(testCase)} reported [{string.Join(", ", actualIds.Order(StringComparer.Ordinal))}], expected [{string.Join(", ", expectedIdSet.Order(StringComparer.Ordinal))}].");
    }

    /// <summary>Supplies every vendored suite case as a one-element <see cref="DynamicDataAttribute"/> row.</summary>
    /// <returns>Every case in <see cref="MessageFormatSuite.AllCases"/>, each wrapped for <see cref="DynamicDataAttribute"/>.</returns>
    public static IEnumerable<object[]> Cases()
    {
        return MessageFormatSuite.AllCases.Select(static testCase => new object[] { testCase });
    }

    /// <summary>Builds the test-explorer name for one <see cref="Cases"/> row, via <see cref="MessageFormatSuite.DisplayName(SuiteCase)"/>.</summary>
    /// <param name="methodInfo">The test method the row belongs to; unused, but required by <see cref="DynamicDataAttribute"/>'s display-name delegate shape.</param>
    /// <param name="data">The row's arguments: exactly one <see cref="SuiteCase"/>.</param>
    /// <returns>The case's display name.</returns>
    public static string GetDisplayName(MethodInfo methodInfo, object?[] data)
    {
        return MessageFormatSuite.DisplayName((SuiteCase)data[0]!);
    }
}
