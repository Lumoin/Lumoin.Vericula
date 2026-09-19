using Lumoin.Vericula.MessageFormat.Evaluation;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Pins the exact spelling of every <see cref="WellKnownMessageFunctionOptions"/> name and value,
/// beside the automatic pair-form contract checks in <see cref="WellKnownVocabularyTests"/>.
/// </summary>
[TestClass]
public sealed class WellKnownMessageFunctionOptionsTests
{
    /// <summary>Every <c>u:</c> namespace option name is spelled with its namespace prefix.</summary>
    [TestMethod]
    public void EveryOptionNameIsSpelledWithItsNamespacePrefix()
    {
        Assert.AreEqual("u:dir", WellKnownMessageFunctionOptions.UDir);
        Assert.AreEqual("u:id", WellKnownMessageFunctionOptions.UId);
        Assert.AreEqual("u:locale", WellKnownMessageFunctionOptions.ULocale);
    }

    /// <summary>Every <see cref="WellKnownMessageFunctionOptions.UDir"/> value is spelled in lowercase, exactly as the specification names it.</summary>
    [TestMethod]
    public void EveryUDirValueIsSpelledInLowercase()
    {
        Assert.AreEqual("ltr", WellKnownMessageFunctionOptions.Ltr);
        Assert.AreEqual("rtl", WellKnownMessageFunctionOptions.Rtl);
        Assert.AreEqual("auto", WellKnownMessageFunctionOptions.Auto);
        Assert.AreEqual("inherit", WellKnownMessageFunctionOptions.Inherit);
    }

    /// <summary>The three option names and four <c>u:dir</c> values are seven distinct strings.</summary>
    [TestMethod]
    public void ThereAreSevenDistinctSpellings()
    {
        //Named killer: WellKnownMessageFunctionOptions.cs, one member's *Utf8 literal copy-pasted from
        //a sibling instead of given its own spelling: HashSet.Add below would then see a duplicate and
        //the count would fall short of 7.
        string[] spellings =
        [
            WellKnownMessageFunctionOptions.UDir,
            WellKnownMessageFunctionOptions.UId,
            WellKnownMessageFunctionOptions.ULocale,
            WellKnownMessageFunctionOptions.Ltr,
            WellKnownMessageFunctionOptions.Rtl,
            WellKnownMessageFunctionOptions.Auto,
            WellKnownMessageFunctionOptions.Inherit
        ];

        var distinct = new HashSet<string>(spellings, StringComparer.Ordinal);

        Assert.HasCount(7, distinct);
    }
}
