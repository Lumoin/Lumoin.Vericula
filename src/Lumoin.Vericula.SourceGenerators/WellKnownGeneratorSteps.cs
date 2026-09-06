namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The pipeline stage names this generator passes to <c>WithTrackingName</c>, so a driver run with
/// <c>trackIncrementalGeneratorSteps: true</c> can be inspected stage by stage through
/// <see cref="Microsoft.CodeAnalysis.GeneratorRunResult.TrackedSteps"/>.
/// </summary>
internal static class WellKnownGeneratorSteps
{
    /// <summary>The UTF-8 source literal of <see cref="XliffAdditionalFiles"/>.</summary>
    public static ReadOnlySpan<byte> XliffAdditionalFilesUtf8 => "XliffAdditionalFiles"u8;

    /// <summary>The stage that selects the additional files with an <c>.xliff</c> extension.</summary>
    public static readonly string XliffAdditionalFiles = Utf8Constants.ToInternedString(XliffAdditionalFilesUtf8);

    /// <summary>The UTF-8 source literal of <see cref="XliffSource"/>.</summary>
    public static ReadOnlySpan<byte> XliffSourceUtf8 => "XliffSource"u8;

    /// <summary>The stage that reads each selected additional file into a value-equatable source record.</summary>
    public static readonly string XliffSource = Utf8Constants.ToInternedString(XliffSourceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ParsedDocument"/>.</summary>
    public static ReadOnlySpan<byte> ParsedDocumentUtf8 => "ParsedDocument"u8;

    /// <summary>The stage that parses each source record into an equatable document model.</summary>
    public static readonly string ParsedDocument = Utf8Constants.ToInternedString(ParsedDocumentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="AssemblyName"/>.</summary>
    public static ReadOnlySpan<byte> AssemblyNameUtf8 => "AssemblyName"u8;

    /// <summary>The stage that reads the compilation's assembly name.</summary>
    public static readonly string AssemblyName = Utf8Constants.ToInternedString(AssemblyNameUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CollectedModel"/>.</summary>
    public static ReadOnlySpan<byte> CollectedModelUtf8 => "CollectedModel"u8;

    /// <summary>The stage that folds every parsed document and the namespace into the emission input.</summary>
    public static readonly string CollectedModel = Utf8Constants.ToInternedString(CollectedModelUtf8);

    /// <summary>The UTF-8 source literal of <see cref="RootNamespace"/>.</summary>
    public static ReadOnlySpan<byte> RootNamespaceUtf8 => "RootNamespace"u8;

    /// <summary>The stage that reads the consuming project's <c>build_property.RootNamespace</c>, when set.</summary>
    public static readonly string RootNamespace = Utf8Constants.ToInternedString(RootNamespaceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Namespace"/>.</summary>
    public static ReadOnlySpan<byte> NamespaceUtf8 => "Namespace"u8;

    /// <summary>The stage that turns the root namespace or assembly name into a valid, dotted C# namespace.</summary>
    public static readonly string Namespace = Utf8Constants.ToInternedString(NamespaceUtf8);
}
