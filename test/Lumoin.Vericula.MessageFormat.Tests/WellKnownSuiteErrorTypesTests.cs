using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Unit tests for <see cref="WellKnownSuiteErrorTypes.ToDiagnosticId(string)"/>: every one of the
/// suite's thirteen known error types maps to its matching <see cref="WellKnownMessageFormatDiagnostics"/>
/// id, and an unrecognized string throws.
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

    /// <summary>Every one of the suite's six runtime error types maps to its corresponding <c>VFX2xx</c> id.</summary>
    [TestMethod]
    public void EveryRuntimeErrorTypeMapsToItsDiagnosticId()
    {
        //Named killer: WellKnownSuiteErrorTypes.cs's ToDiagnosticId, a runtime arm (for example
        //_ when IsUnresolvedVariable(type) => WellKnownMessageFormatDiagnostics.UnresolvedVariable)
        //deleted or pointed at the wrong id: this call would then fall through to the catch-all arm and
        //throw ArgumentOutOfRangeException instead of returning the expected id, or return a mismatched
        //one.
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.UnresolvedVariable, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.UnresolvedVariable));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.UnknownFunction, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.UnknownFunction));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.BadSelector, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.BadSelector));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.BadOperand, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.BadOperand));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.BadOption, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.BadOption));
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.BadVariantKey, WellKnownSuiteErrorTypes.ToDiagnosticId(WellKnownSuiteErrorTypes.BadVariantKey));
    }

    /// <summary>An error type string outside the suite's thirteen known values throws.</summary>
    [TestMethod]
    public void AnUnrecognizedErrorTypeThrows()
    {
        //Named killer: WellKnownSuiteErrorTypes.cs's ToDiagnosticId catch-all arm removed or replaced
        //with a value default: an unrecognized string would then return a bogus id (or null) instead of
        //throwing.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => WellKnownSuiteErrorTypes.ToDiagnosticId("not-a-real-error-type"));
    }
}
