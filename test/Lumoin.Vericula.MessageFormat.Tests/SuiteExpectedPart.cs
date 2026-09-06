using System.Diagnostics;
using System.Text.Json;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// One expected formatted-output part from a suite case's <c>expParts</c> assertion. Loosely typed
/// because the schema's five part shapes (text, bidi isolation, markup, an expression, and fallback)
/// share only a <see cref="Type"/> discriminator, not a common property set; every member beyond
/// <see cref="Type"/> is <see langword="null"/> on a part shape that does not carry it.
/// </summary>
/// <param name="Type">The part's discriminator: <c>text</c>, <c>bidiIsolation</c>, <c>markup</c>, an expression kind (<c>datetime</c>, <c>number</c>, <c>string</c> or <c>test</c>), or <c>fallback</c>.</param>
/// <param name="Value">The part's <c>value</c> member (a string for a text or bidi-isolation part, any JSON value for an expression part), kept as a <see cref="JsonElement"/> because its shape depends on <see cref="Type"/>; <see langword="null"/> when the part carries none.</param>
/// <param name="Kind">The markup part's <c>kind</c> (<c>open</c>, <c>standalone</c> or <c>close</c>); <see langword="null"/> for every other part type.</param>
/// <param name="Name">The markup part's element name; <see langword="null"/> for every other part type.</param>
/// <param name="Id">The <c>id</c> member a markup or expression part may carry; <see langword="null"/> when absent.</param>
/// <param name="Options">The markup part's <c>options</c> object, kept as a <see cref="JsonElement"/>; <see langword="null"/> when absent.</param>
/// <param name="Locale">The expression part's resolved locale; <see langword="null"/> when absent.</param>
/// <param name="Parts">The expression part's nested <c>parts</c> array, kept as a <see cref="JsonElement"/> because each entry's own shape is open-ended; <see langword="null"/> when absent.</param>
/// <param name="Source">The fallback part's source text; <see langword="null"/> for every other part type.</param>
[DebuggerDisplay("SuiteExpectedPart: {Type,nq}")]
public sealed record SuiteExpectedPart(
    string Type,
    JsonElement? Value,
    string? Kind,
    string? Name,
    string? Id,
    JsonElement? Options,
    string? Locale,
    JsonElement? Parts,
    string? Source);
