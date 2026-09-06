using Lumoin.Vericula.Content;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class WellKnownInlineTokensTests
{
    [TestMethod]
    public void TryResolveMatchesEveryDocumentedResolutionCase()
    {
        //Table-tests the three-tier resolution order (design 5.1): a reserved sub-type decides
        //outright, then a tag name recovered from original data, then the code's type alone.
        Case[] cases =
        [
            //Reserved sub-types map first, and win even when the type or original data would
            //otherwise resolve differently.
            new("xlf:b maps to b", InlineCodeType.Other, "xlf:b", null, true, WellKnownInlineTokens.B),
            new("xlf:i maps to i", InlineCodeType.None, "xlf:i", null, true, WellKnownInlineTokens.I),
            new("xlf:u maps to u", InlineCodeType.None, "xlf:u", null, true, WellKnownInlineTokens.U),
            new("xlf:lb maps to br", InlineCodeType.None, "xlf:lb", null, true, WellKnownInlineTokens.Br),
            new("xlf:pb resolves false outright, not falling through to type", InlineCodeType.Link, "xlf:pb", null, false, null),
            new("xlf:var resolves false outright, not falling through to type", InlineCodeType.Image, "xlf:var", null, false, null),
            new("xlf:b wins over original data that would resolve differently", InlineCodeType.None, "xlf:b", "<i>", true, WellKnownInlineTokens.B),
            //Named killer: WellKnownInlineTokens.cs:169, TryResolve's reserved-sub-type tier moved to
            //run after the type-alone switch: a code whose type alone would resolve (Link to a) must
            //still prefer its reserved sub-type (xlf:b to b) over that type-alone fallback.
            new("xlf:b wins over a type that would otherwise resolve on its own", InlineCodeType.Link, "xlf:b", null, true, WellKnownInlineTokens.B),

            //Original data is tried next, matching the whole tag-name token, case-insensitively.
            new("<B> resolves to b, case-insensitively", InlineCodeType.None, null, "<B>", true, WellKnownInlineTokens.B),
            new("</b> resolves to b from a closing tag", InlineCodeType.None, null, "</b>", true, WellKnownInlineTokens.B),
            new("<bdi dir=\"rtl\"> resolves to bdi, stopping at the first space", InlineCodeType.None, null, "<bdi dir=\"rtl\">", true, WellKnownInlineTokens.Bdi),
            new("<bdi> resolves to bdi itself, never to the shorter b", InlineCodeType.None, null, "<bdi>", true, WellKnownInlineTokens.Bdi),
            new("<img/> stops at the slash and resolves to img", InlineCodeType.None, null, "<img/>", true, WellKnownInlineTokens.Img),
            new("original data naming an unknown element resolves false", InlineCodeType.None, null, "<foo>", false, null),
            new("original data not shaped like a tag resolves false", InlineCodeType.None, null, "plain text", false, null),
            new("original data wins over type alone", InlineCodeType.Link, null, "<span>", true, WellKnownInlineTokens.Span),

            //Type alone is the last resort.
            new("Link resolves to a", InlineCodeType.Link, null, null, true, WellKnownInlineTokens.A),
            new("Image resolves to img", InlineCodeType.Image, null, null, true, WellKnownInlineTokens.Img),
            new("Quote resolves to q", InlineCodeType.Quote, null, null, true, WellKnownInlineTokens.Q),
            new("Format resolves false", InlineCodeType.Format, null, null, false, null),
            new("UserInterface resolves false", InlineCodeType.UserInterface, null, null, false, null),
            new("Other resolves false", InlineCodeType.Other, null, null, false, null),
            new("None resolves false", InlineCodeType.None, null, null, false, null),

            //A non-reserved sub-type falls through to the later clues instead of resolving false.
            new("an unreserved sub-type falls through to type alone", InlineCodeType.Link, "acme:custom", null, true, WellKnownInlineTokens.A)
        ];

        var failures = new List<string>();
        foreach(Case @case in cases)
        {
            OriginalData? originalData = @case.OriginalDataText is null ? null : new OriginalData(@case.OriginalDataText);
            bool resolved = WellKnownInlineTokens.TryResolve(@case.Type, @case.SubType, originalData, out string name);

            if(resolved != @case.ExpectedResolved || (resolved && !string.Equals(name, @case.ExpectedName, StringComparison.Ordinal)))
            {
                failures.Add($"{@case.Description}: expected ({@case.ExpectedResolved}, {@case.ExpectedName}) but got ({resolved}, {name}).");
            }
        }

        Assert.HasCount(0, failures, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void IsVoidIsTrueOnlyForBrWbrAndImg()
    {
        Assert.IsTrue(WellKnownInlineTokens.IsVoid(WellKnownInlineTokens.Br));
        Assert.IsTrue(WellKnownInlineTokens.IsVoid(WellKnownInlineTokens.Wbr));
        Assert.IsTrue(WellKnownInlineTokens.IsVoid(WellKnownInlineTokens.Img));
        Assert.IsFalse(WellKnownInlineTokens.IsVoid(WellKnownInlineTokens.Span));
    }

    /// <summary>One case of the <see cref="WellKnownInlineTokens.TryResolve"/> resolution table.</summary>
    /// <param name="Description">A short description of the case, used in a failure message.</param>
    /// <param name="Type">The code's reserved kind.</param>
    /// <param name="SubType">The code's sub-type, or null when absent.</param>
    /// <param name="OriginalDataText">The text of the code's original data, or null when it carries none.</param>
    /// <param name="ExpectedResolved">Whether resolution is expected to succeed.</param>
    /// <param name="ExpectedName">The expected resolved name, when <paramref name="ExpectedResolved"/> is true.</param>
    private sealed record Case(string Description, InlineCodeType Type, string? SubType, string? OriginalDataText, bool ExpectedResolved, string? ExpectedName);
}
