using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Unit tests for <see cref="MessageFormatDiagnostic.Source"/>: null for a parse-time diagnostic built
/// through the five-argument positional constructor, and settable through <c>with</c> or an object
/// initializer for a diagnostic raised against a model with no source text to point into.
/// </summary>
[TestClass]
public sealed class MessageFormatDiagnosticTests
{
    /// <summary>The five-argument positional constructor leaves <see cref="MessageFormatDiagnostic.Source"/> null, so every existing parse-time call site stays untouched.</summary>
    [TestMethod]
    public void ThePositionalConstructorLeavesSourceNull()
    {
        //Named killer: MessageFormatDiagnostic.cs's Source property, an initializer value added (for
        //example "= string.Empty;"): every parse-time diagnostic, built through this same five-argument
        //constructor, would then report a non-null Source instead of null.
        var diagnostic = new MessageFormatDiagnostic(WellKnownMessageFormatDiagnostics.SyntaxError, "Unexpected end of input.", 5, 1, 6);

        Assert.IsNull(diagnostic.Source);
    }

    /// <summary><see cref="MessageFormatDiagnostic.Source"/> is settable through an object initializer, and the placeholder <c>(-1, 0, 0)</c> location is used alongside it for a model-only evaluation finding.</summary>
    [TestMethod]
    public void SourceIsSettableForAModelOnlyEvaluationFinding()
    {
        var diagnostic = new MessageFormatDiagnostic(WellKnownMessageFormatDiagnostics.UnresolvedVariable, "Unresolved variable.", -1, 0, 0)
        {
            Source = "{$missing}"
        };

        Assert.AreEqual(-1, diagnostic.Offset);
        Assert.AreEqual(0, diagnostic.Line);
        Assert.AreEqual(0, diagnostic.Position);
        Assert.AreEqual("{$missing}", diagnostic.Source);
    }

    /// <summary><c>with</c> can add a <see cref="MessageFormatDiagnostic.Source"/> to an existing diagnostic without disturbing its other fields.</summary>
    [TestMethod]
    public void WithAddsASourceWithoutDisturbingOtherFields()
    {
        var parsed = new MessageFormatDiagnostic(WellKnownMessageFormatDiagnostics.BadOperand, "Bad operand.", -1, 0, 0);
        MessageFormatDiagnostic withSource = parsed with { Source = "{:number $name}" };

        Assert.AreEqual(parsed.Id, withSource.Id);
        Assert.AreEqual(parsed.Message, withSource.Message);
        Assert.AreEqual(parsed.Offset, withSource.Offset);
        Assert.AreEqual(parsed.Line, withSource.Line);
        Assert.AreEqual(parsed.Position, withSource.Position);
        Assert.AreEqual("{:number $name}", withSource.Source);
        Assert.IsNull(parsed.Source);
    }
}
