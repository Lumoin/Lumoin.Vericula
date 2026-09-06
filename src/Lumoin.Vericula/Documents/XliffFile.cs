using System.Collections.Immutable;
using System.Diagnostics;
using Lumoin.Base;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Tone;
using Lumoin.Vericula.Units;
using Lumoin.Vericula.Validation;

namespace Lumoin.Vericula.Documents;

/// <summary>
/// One translation file within an XLIFF document, pairing a source language with a target language.
/// </summary>
[DebuggerDisplay("XliffFile: {Id} ({SourceLanguage} -> {TargetLanguage})")]
public sealed record XliffFile(
    string Id,
    LanguageTag SourceLanguage,
    LanguageTag? TargetLanguage,
    ToneProfile? ToneProfile,
    Glossary? Glossary,
    ValidationRuleSet? ValidationRules,
    ImmutableArray<XliffGroup> Groups,
    ImmutableArray<XliffUnit> Units)
{
    /// <summary>
    /// Out-of-band metadata about this file's origin, provenance and context — for example the
    /// <see cref="SourceLocation"/> it was read from. Never serialized into the XLIFF bytes.
    /// </summary>
    public Tag Tag { get; init; } = Tag.Empty;
}
