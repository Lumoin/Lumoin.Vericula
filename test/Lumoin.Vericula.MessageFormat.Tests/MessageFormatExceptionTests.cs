using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Unit tests for <see cref="MessageFormatException"/>: the diagnostics constructor stores and
/// exposes exactly what it was given, while the three plain constructors it shares with
/// <c>Lumoin.Vericula.Parsing.XliffFormatException</c>'s shape all yield an empty
/// <see cref="MessageFormatException.Diagnostics"/>.
/// </summary>
[TestClass]
public sealed class MessageFormatExceptionTests
{
    /// <summary>The diagnostics constructor exposes both the message and the diagnostics it was given.</summary>
    [TestMethod]
    public void TheDiagnosticsConstructorStoresAndExposesItsDiagnostics()
    {
        var diagnostic = new MessageFormatDiagnostic(WellKnownMessageFormatDiagnostics.SyntaxError, "Unexpected end of input.", 5, 1, 6);
        ImmutableArray<MessageFormatDiagnostic> diagnostics = [diagnostic];

        var exception = new MessageFormatException("VFX200 at line 1, position 6: Unexpected end of input.", diagnostics);

        Assert.AreEqual("VFX200 at line 1, position 6: Unexpected end of input.", exception.Message);
        Assert.HasCount(1, exception.Diagnostics);
        Assert.AreEqual(diagnostic, exception.Diagnostics[0]);
    }

    /// <summary>The diagnostics constructor stores a default (never-assigned) diagnostics argument as an empty array, not the default array itself.</summary>
    [TestMethod]
    public void TheDiagnosticsConstructorStoresADefaultArgumentAsEmpty()
    {
        //Named killer: MessageFormatException.cs, the diagnostics constructor's
        //`diagnostics.IsDefault ? [] : diagnostics`. Changing it back to storing `diagnostics`
        //unconditionally would store the default array as-is; this test's read of Diagnostics.IsEmpty
        //would then throw NullReferenceException instead of returning true.
        var exception = new MessageFormatException("boom", default(ImmutableArray<MessageFormatDiagnostic>));

        Assert.IsTrue(exception.Diagnostics.IsEmpty);
    }

    /// <summary>The parameterless constructor yields an empty <see cref="MessageFormatException.Diagnostics"/>.</summary>
    [TestMethod]
    public void TheParameterlessConstructorYieldsNoDiagnostics()
    {
        var exception = new MessageFormatException();

        Assert.HasCount(0, exception.Diagnostics);
    }

    /// <summary>The message-only constructor yields an empty <see cref="MessageFormatException.Diagnostics"/>.</summary>
    [TestMethod]
    public void TheMessageConstructorYieldsNoDiagnostics()
    {
        var exception = new MessageFormatException("boom");

        Assert.AreEqual("boom", exception.Message);
        Assert.HasCount(0, exception.Diagnostics);
    }

    /// <summary>The message-and-inner-exception constructor yields an empty <see cref="MessageFormatException.Diagnostics"/>.</summary>
    [TestMethod]
    public void TheMessageAndInnerExceptionConstructorYieldsNoDiagnostics()
    {
        var innerException = new InvalidOperationException("cause");
        var exception = new MessageFormatException("boom", innerException);

        Assert.AreEqual("boom", exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
        Assert.HasCount(0, exception.Diagnostics);
    }
}
