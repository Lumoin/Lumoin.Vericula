using Lumoin.Vericula.MessageFormat.Evaluation;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Pins the exact spelling of every <see cref="WellKnownMessageFunctions"/> name, beside the automatic
/// pair-form contract checks in <see cref="WellKnownVocabularyTests"/>.
/// </summary>
[TestClass]
public sealed class WellKnownMessageFunctionsTests
{
    /// <summary>Every Stable and Draft default function name is spelled without the leading <c>:</c> sigil, exactly as the specification names it.</summary>
    [TestMethod]
    public void EveryFunctionNameIsSpelledWithoutTheSigil()
    {
        Assert.AreEqual("string", WellKnownMessageFunctions.StringName);
        Assert.AreEqual("number", WellKnownMessageFunctions.Number);
        Assert.AreEqual("integer", WellKnownMessageFunctions.IntegerName);
        Assert.AreEqual("offset", WellKnownMessageFunctions.Offset);
        Assert.AreEqual("currency", WellKnownMessageFunctions.Currency);
        Assert.AreEqual("percent", WellKnownMessageFunctions.Percent);
        Assert.AreEqual("unit", WellKnownMessageFunctions.Unit);
        Assert.AreEqual("datetime", WellKnownMessageFunctions.Datetime);
        Assert.AreEqual("date", WellKnownMessageFunctions.Date);
        Assert.AreEqual("time", WellKnownMessageFunctions.Time);
    }

    /// <summary>The registry names ten distinct default functions.</summary>
    [TestMethod]
    public void ThereAreTenDistinctFunctionNames()
    {
        //Named killer: WellKnownMessageFunctions.cs, one member's *Utf8 literal copy-pasted from a
        //sibling instead of given its own spelling: HashSet.Add below would then see a duplicate and
        //the count would fall short of 10.
        string[] names =
        [
            WellKnownMessageFunctions.StringName,
            WellKnownMessageFunctions.Number,
            WellKnownMessageFunctions.IntegerName,
            WellKnownMessageFunctions.Offset,
            WellKnownMessageFunctions.Currency,
            WellKnownMessageFunctions.Percent,
            WellKnownMessageFunctions.Unit,
            WellKnownMessageFunctions.Datetime,
            WellKnownMessageFunctions.Date,
            WellKnownMessageFunctions.Time
        ];

        var distinct = new HashSet<string>(names, StringComparer.Ordinal);

        Assert.HasCount(10, distinct);
    }
}
