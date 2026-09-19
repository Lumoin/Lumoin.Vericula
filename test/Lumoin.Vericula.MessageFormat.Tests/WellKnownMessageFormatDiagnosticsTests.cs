using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Pins the exact spelling of the seven evaluation-time diagnostic ids <see cref="WellKnownMessageFormatDiagnostics"/>
/// adds (VFX207 to VFX213), beside the automatic pair-form contract checks in
/// <see cref="WellKnownVocabularyTests"/>.
/// </summary>
[TestClass]
public sealed class WellKnownMessageFormatDiagnosticsTests
{
    /// <summary><see cref="WellKnownMessageFormatDiagnostics.UnresolvedVariable"/> is spelled "VFX207".</summary>
    [TestMethod]
    public void UnresolvedVariableIsVFX207()
    {
        Assert.AreEqual("VFX207", WellKnownMessageFormatDiagnostics.UnresolvedVariable);
    }

    /// <summary><see cref="WellKnownMessageFormatDiagnostics.UnknownFunction"/> is spelled "VFX208".</summary>
    [TestMethod]
    public void UnknownFunctionIsVFX208()
    {
        Assert.AreEqual("VFX208", WellKnownMessageFormatDiagnostics.UnknownFunction);
    }

    /// <summary><see cref="WellKnownMessageFormatDiagnostics.BadSelector"/> is spelled "VFX209".</summary>
    [TestMethod]
    public void BadSelectorIsVFX209()
    {
        Assert.AreEqual("VFX209", WellKnownMessageFormatDiagnostics.BadSelector);
    }

    /// <summary><see cref="WellKnownMessageFormatDiagnostics.BadOperand"/> is spelled "VFX210".</summary>
    [TestMethod]
    public void BadOperandIsVFX210()
    {
        Assert.AreEqual("VFX210", WellKnownMessageFormatDiagnostics.BadOperand);
    }

    /// <summary><see cref="WellKnownMessageFormatDiagnostics.BadOption"/> is spelled "VFX211".</summary>
    [TestMethod]
    public void BadOptionIsVFX211()
    {
        Assert.AreEqual("VFX211", WellKnownMessageFormatDiagnostics.BadOption);
    }

    /// <summary><see cref="WellKnownMessageFormatDiagnostics.BadVariantKey"/> is spelled "VFX212".</summary>
    [TestMethod]
    public void BadVariantKeyIsVFX212()
    {
        Assert.AreEqual("VFX212", WellKnownMessageFormatDiagnostics.BadVariantKey);
    }

    /// <summary><see cref="WellKnownMessageFormatDiagnostics.UnsupportedOperation"/> is spelled "VFX213".</summary>
    [TestMethod]
    public void UnsupportedOperationIsVFX213()
    {
        Assert.AreEqual("VFX213", WellKnownMessageFormatDiagnostics.UnsupportedOperation);
    }

    /// <summary>The seven evaluation-time ids are all distinct from each other and from the seven static ids.</summary>
    [TestMethod]
    public void EveryDiagnosticIdIsDistinct()
    {
        //Named killer: WellKnownMessageFormatDiagnostics.cs, one of VFX207-VFX213's *Utf8 literals
        //copy-pasted from a sibling instead of given its own number: HashSet.Add below would then see a
        //duplicate and the count would fall short of 14.
        string[] ids =
        [
            WellKnownMessageFormatDiagnostics.SyntaxError,
            WellKnownMessageFormatDiagnostics.VariantKeyMismatch,
            WellKnownMessageFormatDiagnostics.MissingFallbackVariant,
            WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation,
            WellKnownMessageFormatDiagnostics.DuplicateDeclaration,
            WellKnownMessageFormatDiagnostics.DuplicateOptionName,
            WellKnownMessageFormatDiagnostics.DuplicateVariant,
            WellKnownMessageFormatDiagnostics.UnresolvedVariable,
            WellKnownMessageFormatDiagnostics.UnknownFunction,
            WellKnownMessageFormatDiagnostics.BadSelector,
            WellKnownMessageFormatDiagnostics.BadOperand,
            WellKnownMessageFormatDiagnostics.BadOption,
            WellKnownMessageFormatDiagnostics.BadVariantKey,
            WellKnownMessageFormatDiagnostics.UnsupportedOperation
        ];

        var distinct = new HashSet<string>(ids, StringComparer.Ordinal);

        Assert.HasCount(14, distinct);
    }
}
