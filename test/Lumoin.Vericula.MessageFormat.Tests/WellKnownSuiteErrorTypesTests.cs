using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Unit tests for <see cref="WellKnownSuiteErrorTypes.ToDiagnosticId(string)"/>: every one of the
/// suite's seven static error types maps to its matching <see cref="WellKnownMessageFormatDiagnostics"/>
/// id, and one of the suite's runtime error types has no mapping yet.
/// </summary>
[TestClass]
public sealed class WellKnownSuiteErrorTypesTests
{
    /// <summary>Every one of the suite's seven static error types maps to its corresponding <c>VFX2xx</c> id.</summary>
    [TestMethod]
    public void EveryStaticErrorTypeMapsToItsDiagnosticId()
    {
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.SyntaxError, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.SyntaxError));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.VariantKeyMismatch, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.VariantKeyMismatch));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.MissingFallbackVariant, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.MissingFallbackVariant));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.MissingSelectorAnnotation));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.DuplicateDeclaration, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.DuplicateDeclaration));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.DuplicateOptionName, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.DuplicateOptionName));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.DuplicateVariant, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.DuplicateVariant));
    }

    /// <summary>One of the suite's six runtime error types throws, because it has no <c>VFX2xx</c> id yet.</summary>
    [TestMethod]
    public void ARuntimeErrorTypeThrows()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.UnresolvedVariable));
    }
}
