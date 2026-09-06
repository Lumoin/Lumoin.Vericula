using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Hand-written tests for <see cref="MessageFormatReader.Parse(string)"/>: the throwing overload
/// built on top of <see cref="MessageFormatReader.TryParse(string)"/>.
/// </summary>
[TestClass]
public sealed class MessageFormatReaderParseTests
{
    /// <summary>A syntactically valid message returns its model.</summary>
    [TestMethod]
    public void ReturnsTheModelForAValidMessage()
    {
        Message message = MessageFormatReader.Parse("hello world");

        var patternMessage = message as PatternMessage;
        Assert.IsNotNull(patternMessage);
        Assert.HasCount(1, patternMessage.Pattern.Parts);
    }

    /// <summary>
    /// An invalid message throws <see cref="MessageFormatException"/> whose message names the
    /// diagnostic's id and position and whose <see cref="MessageFormatException.Diagnostics"/> carries
    /// exactly that one diagnostic.
    /// </summary>
    [TestMethod]
    public void ThrowsWithTheIdAndPositionAndOneDiagnosticForAnInvalidMessage()
    {
        MessageFormatException exception = Assert.ThrowsExactly<MessageFormatException>(
            () => MessageFormatReader.Parse("abc}def"));

        Assert.HasCount(1, exception.Diagnostics);
        MessageFormatDiagnostic diagnostic = exception.Diagnostics[0];
        Assert.IsTrue(WellKnownMessageFormatDiagnostics.IsSyntaxError(diagnostic.Id));
        Assert.Contains(
            $"{diagnostic.Id} at line {diagnostic.Line}, position {diagnostic.Position}: ",
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// <see cref="MessageFormatReader.TryParse(string)"/> throws <see cref="ArgumentNullException"/>
    /// on a null source rather than reaching the parser with a null field.
    /// </summary>
    [TestMethod]
    public void TryParseThrowsForANullSource()
    {
        //Named killer: MessageFormatReader.cs line 20, removing TryParse's
        //ArgumentNullException.ThrowIfNull guard. A null source would then reach
        //MessageParser.Parse, which throws NullReferenceException the first time it reads the
        //source's length instead; this test's ThrowsExactly<ArgumentNullException> sees the
        //difference.
        Assert.ThrowsExactly<ArgumentNullException>(() => MessageFormatReader.TryParse(null!));
    }

    /// <summary>
    /// <see cref="MessageFormatReader.Parse(string)"/> throws <see cref="ArgumentNullException"/> on a
    /// null source rather than the substantially different <see cref="MessageFormatException"/> a
    /// failed parse throws.
    /// </summary>
    [TestMethod]
    public void ParseThrowsForANullSource()
    {
        //Confirms the documented contract; not a named killer of its own. Removing Parse's own
        //ArgumentNullException.ThrowIfNull guard at MessageFormatReader.cs line 38 is an EQUIVALENT
        //mutant while TryParse's guard at line 20 (see TryParseThrowsForANullSource) stays in place
        //one call down: both guards throw the same exception type with the same parameter name
        //("source", from CallerArgumentExpression at each call site), so this test cannot tell them
        //apart, and neither could any other type-based assertion.
        Assert.ThrowsExactly<ArgumentNullException>(() => MessageFormatReader.Parse(null!));
    }
}
