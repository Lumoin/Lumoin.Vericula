using Lumoin.Vericula.MessageFormat.Diagnostics;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Hand-written tests asserting that a syntax error's <see cref="MessageFormatDiagnostic.Offset"/>,
/// <see cref="MessageFormatDiagnostic.Line"/> and <see cref="MessageFormatDiagnostic.Position"/> are
/// computed correctly for a message spanning three lines, once per line-ending style: LF, CR, and
/// CRLF. Every source below is <c>"first" + break + "second" + break + "thi\xrd"</c>: an unknown
/// escape (<c>\x</c>) on the third line, whose offending character ('x', the code point right after
/// the backslash) is at UTF-16 offset 17 for a one-code-unit break and 19 for the two-code-unit CRLF
/// break, line 3, position 5 in every case.
/// </summary>
[TestClass]
public sealed class MessageParserPositionTests
{
    /// <summary>Parses <paramref name="source"/>, asserting it failed with exactly one syntax-error diagnostic, and returns that diagnostic.</summary>
    /// <param name="source">The source text to parse.</param>
    /// <returns>The sole diagnostic.</returns>
    private static MessageFormatDiagnostic ParsesToOneSyntaxError(string source)
    {
        MessageParseResult result = MessageFormatReader.TryParse(source);

        Assert.IsNull(result.Message);
        Assert.HasCount(1, result.Diagnostics);
        Assert.IsTrue(WellKnownMessageFormatDiagnostics.IsSyntaxError(result.Diagnostics[0].Id));

        return result.Diagnostics[0];
    }

    /// <summary>A syntax error on the third line of an LF-broken message reports offset 17, line 3, position 5.</summary>
    [TestMethod]
    public void ReportsTheRightPositionWithLfLineBreaks()
    {
        MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError("first\nsecond\nthi\\xrd");

        Assert.AreEqual(17, diagnostic.Offset);
        Assert.AreEqual(3, diagnostic.Line);
        Assert.AreEqual(5, diagnostic.Position);
    }

    /// <summary>A syntax error on the third line of a CR-broken message reports offset 17, line 3, position 5: a lone CR ends a line exactly like a lone LF.</summary>
    [TestMethod]
    public void ReportsTheRightPositionWithCrLineBreaks()
    {
        MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError("first\rsecond\rthi\\xrd");

        Assert.AreEqual(17, diagnostic.Offset);
        Assert.AreEqual(3, diagnostic.Line);
        Assert.AreEqual(5, diagnostic.Position);
    }

    /// <summary>A syntax error on the third line of a CRLF-broken message reports offset 19 (each break is two code units wider), line 3, position 5.</summary>
    [TestMethod]
    public void ReportsTheRightPositionWithCrLfLineBreaks()
    {
        MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError("first\r\nsecond\r\nthi\\xrd");

        Assert.AreEqual(19, diagnostic.Offset);
        Assert.AreEqual(3, diagnostic.Line);
        Assert.AreEqual(5, diagnostic.Position);
    }

    /// <summary>
    /// A source that is exactly the <c>.match</c> keyword and nothing else still recognizes the
    /// keyword (occupying the whole remaining source is not a reason to reject it) and reports the
    /// error at the end of input, where the required selector is missing, not at offset 0.
    /// </summary>
    [TestMethod]
    public void RecognizesAKeywordThatIsExactlyTheRemainingSource()
    {
        //Named killer: MessageParser.Scanning.cs line 269, TryConsumeKeyword's bounds check.
        //Changing <= to < rejects a keyword that occupies exactly the remaining source; the
        //diagnostic then reports offset 0 (the keyword never recognized, falling through to the
        //complex-body error) instead of offset 6 (recognized, then missing the required selector),
        //and this test's offset assertion sees the difference.
        MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError(".match");

        Assert.AreEqual(6, diagnostic.Offset);
    }

    /// <summary>
    /// Two consecutive lone line breaks of the same kind (CR-CR or LF-LF, with no pairing between
    /// them) each end their own line: a diagnostic two breaks later reports line 3, not line 2.
    /// </summary>
    [TestMethod]
    public void PositionRecoversCorrectlyAfterTwoConsecutiveLoneLineBreaksOfTheSameKind()
    {
        //Named killers: MessageFormatPosition.cs line 35 (M-105, the CR branch's `continue;`) and
        //line 44 (M-108, the LF branch's `continue;`), both mutated to `;`. Falling through past
        //the sibling `if(character=='\n'/'\r')` check (false, the local `character` variable is
        //unchanged) reaches the loop's own trailing `index++`, silently skipping the second break
        //character entirely: it is never examined by the loop, so it never gets its own
        //line++/lineStart update. For source "a<brk><brk>b\\xcd" (an unknown escape's 'x' at
        //offset 5), the correct line/position is (3, 3); the mutant reports (2, 4).
        (char lineBreak, char kind)[] cases = [('\r', 'r'), ('\n', 'n')];

        foreach((char lineBreak, char kind) in cases)
        {
            string source = $"a{lineBreak}{lineBreak}b\\xcd";
            MessageFormatDiagnostic diagnostic = ParsesToOneSyntaxError(source);

            Assert.AreEqual(5, diagnostic.Offset, $"kind '{kind}'");
            Assert.AreEqual(3, diagnostic.Line, $"kind '{kind}'");
            Assert.AreEqual(3, diagnostic.Position, $"kind '{kind}'");
        }
    }
}
