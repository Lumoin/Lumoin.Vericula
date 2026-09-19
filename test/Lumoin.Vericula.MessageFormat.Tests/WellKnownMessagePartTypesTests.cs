using Lumoin.Vericula.MessageFormat.Evaluation;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Pins the exact spelling of every <see cref="WellKnownMessagePartTypes"/> value against the vendored
/// conformance suite's <c>expParts[].type</c> strings, beside the automatic pair-form contract checks
/// in <see cref="WellKnownVocabularyTests"/>.
/// </summary>
[TestClass]
public sealed class WellKnownMessagePartTypesTests
{
    /// <summary>Every outer part type matches the vendored suite's schema spelling exactly, including the camelCase of <c>bidiIsolation</c>.</summary>
    [TestMethod]
    public void EveryPartTypeMatchesTheSuiteSchemaSpelling()
    {
        Assert.AreEqual("text", WellKnownMessagePartTypes.Text);
        Assert.AreEqual("bidiIsolation", WellKnownMessagePartTypes.BidiIsolation);
        Assert.AreEqual("fallback", WellKnownMessagePartTypes.Fallback);
        Assert.AreEqual("markup", WellKnownMessagePartTypes.Markup);
        Assert.AreEqual("string", WellKnownMessagePartTypes.StringType);
        Assert.AreEqual("number", WellKnownMessagePartTypes.Number);
    }

    /// <summary>The six outer part types are all distinct.</summary>
    [TestMethod]
    public void ThereAreSixDistinctPartTypes()
    {
        //Named killer: WellKnownMessagePartTypes.cs, one member's *Utf8 literal copy-pasted from a
        //sibling instead of given its own spelling: HashSet.Add below would then see a duplicate and
        //the count would fall short of 6.
        string[] types =
        [
            WellKnownMessagePartTypes.Text,
            WellKnownMessagePartTypes.BidiIsolation,
            WellKnownMessagePartTypes.Fallback,
            WellKnownMessagePartTypes.Markup,
            WellKnownMessagePartTypes.StringType,
            WellKnownMessagePartTypes.Number
        ];

        var distinct = new HashSet<string>(types, StringComparer.Ordinal);

        Assert.HasCount(6, distinct);
    }
}
