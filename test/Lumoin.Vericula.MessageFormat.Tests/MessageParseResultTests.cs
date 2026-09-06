using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Unit tests for <see cref="MessageParseResult.IsValid"/>: a null <see cref="MessageParseResult.Message"/>
/// means invalid regardless of diagnostics, a non-null message with any diagnostic means invalid, and a
/// non-null message with no diagnostics means valid.
/// </summary>
[TestClass]
public sealed class MessageParseResultTests
{
    /// <summary>A minimal message: no declarations, an empty pattern. Enough to stand in for "a message was built" without depending on the parser.</summary>
    private static readonly PatternMessage TinyMessage = new(ImmutableArray<Declaration>.Empty, new Pattern(ImmutableArray<PatternPart>.Empty));

    /// <summary>A null <see cref="MessageParseResult.Message"/> is invalid even when <see cref="MessageParseResult.Diagnostics"/> is empty.</summary>
    [TestMethod]
    public void ANullMessageIsInvalid()
    {
        var result = new MessageParseResult(null, []);

        Assert.IsFalse(result.IsValid);
    }

    /// <summary>A non-null message with at least one diagnostic is invalid.</summary>
    [TestMethod]
    public void AMessageWithDiagnosticsIsInvalid()
    {
        ImmutableArray<MessageFormatDiagnostic> diagnostics =
            [new MessageFormatDiagnostic(WellKnownMessageFormatDiagnostics.DuplicateVariant, "Duplicate variant.", 3, 1, 4)];

        var result = new MessageParseResult(TinyMessage, diagnostics);

        Assert.IsFalse(result.IsValid);
    }

    /// <summary>A non-null message with no diagnostics is valid.</summary>
    [TestMethod]
    public void AMessageWithNoDiagnosticsIsValid()
    {
        var result = new MessageParseResult(TinyMessage, []);

        Assert.IsTrue(result.IsValid);
    }

    /// <summary>A non-null message paired with a default (never-assigned) diagnostics array is valid, not a thrown <see cref="NullReferenceException"/>.</summary>
    [TestMethod]
    public void AMessageWithADefaultDiagnosticsArrayIsValid()
    {
        //Named killer: MessageParseResult.cs, IsValid's Diagnostics.IsDefaultOrEmpty. Changing it back
        //to Diagnostics.IsEmpty throws NullReferenceException for a default ImmutableArray (never
        //produced by MessageFormatReader itself, but not prevented by the record's shape either) instead
        //of returning true, which this test's IsValid read would see, either as a thrown exception or as
        //a failed assertion.
        var result = new MessageParseResult(TinyMessage, default);

        Assert.IsTrue(result.IsValid);
    }
}
