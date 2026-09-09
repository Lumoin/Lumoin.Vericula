using Lumoin.Vericula.SourceGenerators;

namespace Lumoin.Vericula.SourceGenerators.Tests;

/// <summary>
/// Mirrors <c>Lumoin.Vericula.Tests.WellKnownInlineTokensTests</c> for the generator's own copy of
/// <see cref="WellKnownInlineTokens"/>. Unlike the core library, this assembly's only caller
/// (<c>XliffSourceGenerator.InlineContent.ResolveCodeText</c>) never passes a non-null original-data
/// text into <see cref="WellKnownInlineTokens.TryResolve"/>, because a non-null original data is
/// rendered directly instead of consulting this table; so the original-data clue, and
/// <see cref="WellKnownInlineTokens.IsVoid(string)"/>, reach no production code path in this assembly
/// and need their own coverage here to catch drift such as a slicing mistake.
/// </summary>
[TestClass]
public sealed class WellKnownInlineTokensTests
{
    [TestMethod]
    public void TryResolveMatchesEveryDocumentedResolutionCase()
    {
        //Table-tests the three-tier resolution order (5.1, 5.8): a reserved sub-type decides outright,
        //then a tag name recovered from original data, then the code's type alone.
        Case[] cases =
        [
            new("xlf:b maps to b", null, WellKnownXliffAttributeValues.SubTypeBold, null, true, WellKnownInlineTokens.B),
            new("xlf:pb resolves false outright, not falling through to type", WellKnownXliffAttributeValues.Link, WellKnownXliffAttributeValues.SubTypePageBreak, null, false, null),
            new("xlf:var resolves false outright, not falling through to type", WellKnownXliffAttributeValues.Image, WellKnownXliffAttributeValues.SubTypeVariable, null, false, null),

            //Named killer: WellKnownInlineTokens.cs:220, TryResolveFromOriginalData's closing-tag
            //branch "span.Slice(2)" changed to "span.Slice(1)" would leave the leading '/' in the
            //candidate ("/b" instead of "b"), so "</b>" below would resolve false instead of matching
            //b; nothing else in this assembly calls this clue (see the type XML doc), so only this
            //test can catch it.
            new("<B> resolves to b, case-insensitively", null, null, "<B>", true, WellKnownInlineTokens.B),
            new("</b> resolves to b from a closing tag", null, null, "</b>", true, WellKnownInlineTokens.B),
            new("<bdi dir=\"rtl\"> resolves to bdi, stopping at the first space", null, null, "<bdi dir=\"rtl\">", true, WellKnownInlineTokens.Bdi),
            new("<bdi> resolves to bdi itself, never to the shorter b", null, null, "<bdi>", true, WellKnownInlineTokens.Bdi),
            new("original data naming an unknown element resolves false", null, null, "<foo>", false, null),
            new("original data not shaped like a tag resolves false", null, null, "plain text", false, null),

            new("Link resolves to a", WellKnownXliffAttributeValues.Link, null, null, true, WellKnownInlineTokens.A),
            new("Image resolves to img", WellKnownXliffAttributeValues.Image, null, null, true, WellKnownInlineTokens.Img),
            new("Quote resolves to q", WellKnownXliffAttributeValues.Quote, null, null, true, WellKnownInlineTokens.Q)
        ];

        var failures = new List<string>();
        foreach(Case @case in cases)
        {
            bool resolved = WellKnownInlineTokens.TryResolve(@case.Type, @case.SubType, @case.OriginalDataText, out string name);

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
        //Named killer: WellKnownInlineTokens.cs:138-140, IsVoid's Br/Wbr/Img comparisons - dropping
        //any one arm (or comparing against the wrong field) would flip one of these four assertions;
        //this predicate has no production caller in this assembly, so only this test exercises it.
        Assert.IsTrue(WellKnownInlineTokens.IsVoid(WellKnownInlineTokens.Br));
        Assert.IsTrue(WellKnownInlineTokens.IsVoid(WellKnownInlineTokens.Wbr));
        Assert.IsTrue(WellKnownInlineTokens.IsVoid(WellKnownInlineTokens.Img));
        Assert.IsFalse(WellKnownInlineTokens.IsVoid(WellKnownInlineTokens.Span));
    }

    /// <summary>One case of the <see cref="WellKnownInlineTokens.TryResolve"/> resolution table.</summary>
    /// <param name="Description">A short description of the case, used in a failure message.</param>
    /// <param name="Type">The code's raw <c>type</c> attribute value, or null when absent.</param>
    /// <param name="SubType">The code's full <c>prefix:value</c> sub-type, or null when absent.</param>
    /// <param name="OriginalDataText">The text of the code's original data, or null when it carries none.</param>
    /// <param name="ExpectedResolved">Whether resolution is expected to succeed.</param>
    /// <param name="ExpectedName">The expected resolved name, when <paramref name="ExpectedResolved"/> is true.</param>
    private sealed record Case(string Description, string? Type, string? SubType, string? OriginalDataText, bool ExpectedResolved, string? ExpectedName);
}
