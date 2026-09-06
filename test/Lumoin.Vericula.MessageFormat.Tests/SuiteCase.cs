using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// One fully merged case from the Unicode MessageFormat 2.0 conformance suite: one entry of a
/// vendored file's <c>tests</c> array, with the file's <c>defaultTestProperties</c> folded in for
/// every property the entry itself did not set, plus the file and position the entry came from.
/// </summary>
/// <param name="File">The vendored file's embedded-resource logical name, for example <c>tests/functions/number.json</c>.</param>
/// <param name="Index">The case's one-based position within <see cref="File"/>'s <c>tests</c> array.</param>
/// <param name="Description">The case's own description; <see langword="null"/> when the suite gives none.</param>
/// <param name="Locale">The locale to format under, after merging with the file's defaults.</param>
/// <param name="Src">The MessageFormat 2.0 source text, after merging with the file's defaults.</param>
/// <param name="BidiIsolation">The bidi isolation strategy, <c>"default"</c> or <c>"none"</c>; <c>"default"</c> when neither the case nor the file's defaults set it.</param>
/// <param name="Params">The named values the formatter resolves external variables against, after merging; empty when neither the case nor the file's defaults set any.</param>
/// <param name="Tags">The feature tags the case relies on, after merging; empty when neither sets any.</param>
/// <param name="Exp">The expected formatted-to-string result, after merging; <see langword="null"/> when the case makes no such assertion.</param>
/// <param name="ExpParts">The expected formatted-to-parts result, after merging; empty when the case makes no such assertion.</param>
/// <param name="ExpErrors">The type strings of the errors the case expects, after merging; empty when the case expects none.</param>
/// <param name="Only">Whether the case set the suite's development-time <c>only</c> flag; the vendored suite sets this on no case.</param>
[DebuggerDisplay("SuiteCase: {File,nq}#{Index} {Src,nq}")]
public sealed record SuiteCase(
    string File,
    int Index,
    string? Description,
    string Locale,
    string Src,
    string BidiIsolation,
    ImmutableArray<SuiteParam> Params,
    ImmutableArray<string> Tags,
    string? Exp,
    ImmutableArray<SuiteExpectedPart> ExpParts,
    ImmutableArray<string> ExpErrors,
    bool Only);
