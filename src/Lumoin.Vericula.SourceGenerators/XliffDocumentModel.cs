using System.Diagnostics;

namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The parsed shape of one XLIFF file as the generator pipeline sees it: its languages, its units,
/// the parse error when the file could not be read at all, the line and column of the element the
/// error or the document itself is anchored to, and any non-fatal warnings about the document.
/// </summary>
/// <param name="Path">The additional file's path.</param>
/// <param name="SourceLanguage">The document's declared source language, or empty when parsing failed before it was read.</param>
/// <param name="TargetLanguage">The document's declared target language, or <see langword="null"/> when the document declares none.</param>
/// <param name="Units">The parsed translation units, empty when parsing failed.</param>
/// <param name="Error">The parse failure message, or <see langword="null"/> when the document parsed successfully.</param>
/// <param name="Line">The one-based line the error, or the document itself, is anchored to; 0 on a successful parse.</param>
/// <param name="Column">The one-based column the error, or the document itself, is anchored to; 0 on a successful parse.</param>
/// <param name="Warnings">Non-fatal warnings about the document, such as a file name suggesting a different target language.</param>
[DebuggerDisplay("XliffDocumentModel: {Path} ({SourceLanguage} -> {TargetLanguage}), units: {Units.Length}")]
internal sealed record XliffDocumentModel(
    string Path,
    string SourceLanguage,
    string? TargetLanguage,
    EquatableArray<UnitModel> Units,
    string? Error,
    int Line,
    int Column,
    EquatableArray<string> Warnings);
