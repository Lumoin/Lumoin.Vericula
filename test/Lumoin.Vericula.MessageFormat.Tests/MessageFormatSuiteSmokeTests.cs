using System.Collections.Immutable;
using System.Text.Json;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Smoke tests over the vendored suite as a whole: the loader finds every file, merges every case
/// completely, and the suite's shape matches what the vendoring step recorded at copy time. These
/// tests guard the harness itself; <see cref="MessageFormatSuiteCaseTests"/> asserts each case against the parser.
/// </summary>
[TestClass]
public sealed class MessageFormatSuiteSmokeTests
{
    /// <summary>Every vendored file's resource logical name paired with its exact case count, recorded when the suite was vendored.</summary>
    private static readonly Dictionary<string, int> ExpectedCaseCountsByFile = new(StringComparer.Ordinal)
    {
        [WellKnownSuiteResources.BidiJson] = 27,
        [WellKnownSuiteResources.DataModelErrorsJson] = 23,
        [WellKnownSuiteResources.FallbackJson] = 8,
        [WellKnownSuiteResources.FunctionsCurrencyJson] = 12,
        [WellKnownSuiteResources.FunctionsDateJson] = 7,
        [WellKnownSuiteResources.FunctionsDatetimeJson] = 7,
        [WellKnownSuiteResources.FunctionsIntegerJson] = 13,
        [WellKnownSuiteResources.FunctionsNumberJson] = 41,
        [WellKnownSuiteResources.FunctionsOffsetJson] = 16,
        [WellKnownSuiteResources.FunctionsPercentJson] = 13,
        [WellKnownSuiteResources.FunctionsStringJson] = 9,
        [WellKnownSuiteResources.FunctionsTimeJson] = 6,
        [WellKnownSuiteResources.PatternSelectionJson] = 22,
        [WellKnownSuiteResources.SyntaxErrorsJson] = 133,
        [WellKnownSuiteResources.SyntaxJson] = 114,
        [WellKnownSuiteResources.UOptionsJson] = 10
    };

    /// <summary>
    /// Every error type string that ever appears in an <c>expErrors</c> entry across the whole suite,
    /// at the <c>LDML48.2</c> tag this project vendors: twelve of the schema's thirteen known types.
    /// The suite exercises no case whose expected error is <see cref="WellKnownSuiteErrorTypes.BadVariantKey"/>.
    /// </summary>
    private static readonly HashSet<string> ExpectedObservedErrorTypes = new(StringComparer.Ordinal)
    {
        WellKnownSuiteErrorTypes.SyntaxError,
        WellKnownSuiteErrorTypes.VariantKeyMismatch,
        WellKnownSuiteErrorTypes.MissingFallbackVariant,
        WellKnownSuiteErrorTypes.MissingSelectorAnnotation,
        WellKnownSuiteErrorTypes.DuplicateDeclaration,
        WellKnownSuiteErrorTypes.DuplicateOptionName,
        WellKnownSuiteErrorTypes.DuplicateVariant,
        WellKnownSuiteErrorTypes.UnresolvedVariable,
        WellKnownSuiteErrorTypes.UnknownFunction,
        WellKnownSuiteErrorTypes.BadSelector,
        WellKnownSuiteErrorTypes.BadOperand,
        WellKnownSuiteErrorTypes.BadOption
    };

    /// <summary>The suite carries exactly 461 cases across its 16 vendored files.</summary>
    [TestMethod]
    public void LoadsExactlyFourHundredSixtyOneCases()
    {
        Assert.HasCount(461, MessageFormatSuite.AllCases);
    }

    /// <summary>Every vendored file contributes exactly the case count recorded when the suite was copied.</summary>
    [TestMethod]
    public void LoadsEachFilesExactCaseCount()
    {
        var actualCountsByFile = MessageFormatSuite.AllCases
            .GroupBy(testCase => testCase.File, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.HasCount(ExpectedCaseCountsByFile.Count, actualCountsByFile, "A file was not loaded, or an unexpected file was.");

        foreach((string file, int expectedCount) in ExpectedCaseCountsByFile)
        {
            Assert.IsTrue(actualCountsByFile.TryGetValue(file, out int actualCount), $"'{file}' contributed no case.");
            Assert.AreEqual(expectedCount, actualCount, $"'{file}' contributed {actualCount} case(s), expected {expectedCount}.");
        }
    }

    /// <summary>Every case has a non-empty locale and source text once its file's defaults are merged in.</summary>
    [TestMethod]
    public void EveryCaseHasALocaleAndASourceAfterMerging()
    {
        foreach(SuiteCase testCase in MessageFormatSuite.AllCases)
        {
            Assert.IsFalse(string.IsNullOrEmpty(testCase.Locale), $"{MessageFormatSuite.DisplayName(testCase)} has no locale.");
            Assert.IsNotNull(testCase.Src, $"{MessageFormatSuite.DisplayName(testCase)} has no src.");
        }
    }

    /// <summary>
    /// Every case sets at least one of <c>exp</c>, <c>expParts</c> or <c>expErrors</c>, on itself or on
    /// its file's <c>defaultTestProperties</c>, matching the schema's own <c>anyExp</c> requirement.
    /// Checked against each file's raw, unmerged JSON rather than the loader's merged
    /// <see cref="SuiteCase"/>: the schema treats a present-but-empty <c>expErrors</c> (the built-in
    /// formatters' <c>currency</c>, <c>date</c>, <c>datetime</c>, <c>percent</c> and <c>time</c> cases
    /// often assert only "formats without error", via an empty array carried on the file's defaults)
    /// as a real assertion, which <see cref="SuiteCase.ExpErrors"/> cannot distinguish from "asserts
    /// nothing" once it has collapsed both to the same empty <see cref="ImmutableArray{T}"/>.
    /// </summary>
    [TestMethod]
    public void EveryCaseHasAtLeastOneExpectation()
    {
        var casesWithNoExpectation = new List<string>();

        foreach(string resourceName in MessageFormatSuite.TestFileResourceNames(typeof(MessageFormatSuite).Assembly))
        {
            SuiteFileDocument document = JsonSerializer.Deserialize(
                MessageFormatSuite.ReadResourceText(resourceName), MessageFormatSuiteJsonContext.Default.SuiteFileDocument)!;

            for(int index = 0; index < document.Tests.Length; index++)
            {
                if(!SetsAnExpectation(document.Tests[index]) && !SetsAnExpectation(document.DefaultTestProperties))
                {
                    casesWithNoExpectation.Add($"{resourceName}#{index + 1}");
                }
            }
        }

        Assert.HasCount(0, casesWithNoExpectation, string.Join(", ", casesWithNoExpectation));
    }

    /// <summary>Determines whether a raw case (or a file's raw defaults) sets any of the schema's three assertion properties, regardless of whether the value it sets is itself empty.</summary>
    /// <param name="testCase">The raw case or defaults object to inspect; <see langword="null"/> for a file with no defaults.</param>
    /// <returns><see langword="true"/> if <paramref name="testCase"/> is not <see langword="null"/> and sets <c>exp</c>, <c>expParts</c> or <c>expErrors</c>; otherwise, <see langword="false"/>.</returns>
    private static bool SetsAnExpectation(SuiteRawCase? testCase)
    {
        return testCase is not null && (testCase.Exp is not null || testCase.ExpParts is not null || testCase.ExpErrors is not null);
    }

    /// <summary>No vendored case sets the suite's development-time <c>only</c> flag.</summary>
    [TestMethod]
    public void NoCaseSetsTheOnlyFlag()
    {
        SuiteCase[] onlyCases = [.. MessageFormatSuite.AllCases.Where(testCase => testCase.Only)];

        Assert.HasCount(0, onlyCases, string.Join(", ", onlyCases.Select(MessageFormatSuite.DisplayName)));
    }

    /// <summary>
    /// The suite exercises exactly twelve of the schema's thirteen error types: every case's
    /// <c>expErrors</c> entries, taken together, name every static and runtime type except
    /// <see cref="WellKnownSuiteErrorTypes.BadVariantKey"/>, which no case at this tag expects.
    /// </summary>
    [TestMethod]
    public void ObservedErrorTypesAreExactlyTheTwelveTheSuiteExercises()
    {
        var actualTypes = new HashSet<string>(
            MessageFormatSuite.AllCases.SelectMany(testCase => testCase.ExpErrors), StringComparer.Ordinal);

        Assert.IsTrue(actualTypes.SetEquals(ExpectedObservedErrorTypes), string.Join(", ", actualTypes.Order(StringComparer.Ordinal)));
    }

    /// <summary>The vendored <c>LICENSE</c> resource decodes to the Unicode License V3 text.</summary>
    [TestMethod]
    public void LicenseResourceDecodesToUnicodeLicenseV3()
    {
        Assert.Contains("UNICODE LICENSE V3", MessageFormatSuite.License, StringComparison.Ordinal);
    }
}
