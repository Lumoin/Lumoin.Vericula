using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// The JSON shape shared by a suite file's <c>defaultTestProperties</c> object and each entry of its
/// <c>tests</c> array. Every member is optional: a case may omit any property its file's defaults
/// supply, and <c>defaultTestProperties</c> never carries <see cref="Description"/> or
/// <see cref="Only"/>. Deserialized only inside the loader, which folds a case's own values over its
/// file's defaults into a merged, public <see cref="SuiteCase"/>.
/// </summary>
/// <param name="Description">The case's own description; absent on <c>defaultTestProperties</c> and on a case that gives none.</param>
/// <param name="Locale">The locale to format under.</param>
/// <param name="Src">The MessageFormat 2.0 source text.</param>
/// <param name="BidiIsolation">The bidi isolation strategy, <c>"default"</c> or <c>"none"</c>.</param>
/// <param name="Params">The named values the formatter resolves external variables against.</param>
/// <param name="Tags">The feature tags the case relies on.</param>
/// <param name="Exp">The expected formatted-to-string result.</param>
/// <param name="ExpParts">The expected formatted-to-parts result.</param>
/// <param name="ExpErrors">The runtime or data-model errors the case expects.</param>
/// <param name="Only">A development-time flag asking a runner to run only the cases that set it; absent when unset.</param>
[DebuggerDisplay("SuiteRawCase: {Src,nq}")]
internal sealed record SuiteRawCase(
    string? Description,
    string? Locale,
    string? Src,
    string? BidiIsolation,
    SuiteParam[]? Params,
    string[]? Tags,
    string? Exp,
    SuiteExpectedPart[]? ExpParts,
    SuiteRawExpectedError[]? ExpErrors,
    bool? Only);
