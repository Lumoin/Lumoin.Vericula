using Microsoft.CodeAnalysis;

namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The diagnostics this generator can report, in the VFX300 range reserved for
/// source generator errors and warnings.
/// </summary>
internal static class XliffDiagnostics
{
    /// <summary>
    /// Reported when an XLIFF additional file cannot be parsed at all.
    /// </summary>
    public static DiagnosticDescriptor ParseFailure { get; } = new(
        id: WellKnownGeneratorDiagnostics.ParseFailure,
        title: "XLIFF document could not be parsed",
        messageFormat: "The XLIFF file '{0}' could not be parsed: {1}",
        category: "Lumoin.Vericula",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Reported when a unit id cannot be given its own generated accessor: the id is not a valid C#
    /// identifier, it collides with a member the generated class already declares, or it repeats
    /// across files with different text.
    /// </summary>
    public static DiagnosticDescriptor InvalidUnitId { get; } = new(
        id: WellKnownGeneratorDiagnostics.InvalidUnitId,
        title: "XLIFF unit id could not be given a generated accessor",
        messageFormat: "The unit id '{0}' in '{1}' {2}",
        category: "Lumoin.Vericula",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Reported when a namespace segment derived from the project's root namespace or assembly name
    /// needed sanitizing, or could not become a valid C# identifier at all and fell back to a
    /// generated placeholder.
    /// </summary>
    public static DiagnosticDescriptor InvalidNamespaceSegment { get; } = new(
        id: WellKnownGeneratorDiagnostics.InvalidNamespaceSegment,
        title: "Generated namespace segment was sanitized",
        messageFormat: "{0}",
        category: "Lumoin.Vericula",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Reported when an additional file's name suggests a different target language than the one its
    /// <c>trgLang</c> attribute declares; the declared <c>trgLang</c> is always the one used.
    /// </summary>
    public static DiagnosticDescriptor LanguageTagFileNameMismatch { get; } = new(
        id: WellKnownGeneratorDiagnostics.LanguageTagFileNameMismatch,
        title: "File name suggests a different target language than trgLang",
        messageFormat: "{0}",
        category: "Lumoin.Vericula",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Reported when a document's <c>srcLang</c> differs from the <c>srcLang</c> of the first document
    /// in path order: only documents that share that source language contribute generated accessors,
    /// so a mismatched document's units are excluded rather than silently dropped with no diagnostic.
    /// </summary>
    public static DiagnosticDescriptor SourceLanguageMismatch { get; } = new(
        id: WellKnownGeneratorDiagnostics.SourceLanguageMismatch,
        title: "XLIFF document's source language differs from the other documents'",
        messageFormat: "The document '{0}' declares srcLang '{1}', which differs from the srcLang '{3}' declared by '{2}'; only documents sharing that source language contribute generated accessors.",
        category: "Lumoin.Vericula",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
