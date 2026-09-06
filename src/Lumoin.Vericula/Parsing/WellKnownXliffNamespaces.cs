using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// The well-known XML namespace names of XLIFF 2.x and of Vericula's own extension, and the prefixes
/// the writer declares for them, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>.
/// </summary>
/// <remarks>
/// Each name is spelled once as a UTF-8 source literal and carried alongside as an interned string.
/// The Metadata, Validation and Glossary modules keep their 2.0 namespace names in XLIFF 2.1.
/// Vericula's namespace holds the attributes the standard modules have no slot for: the custom
/// validation rules, and a glossary translation's status and an entry's rationale and scopes.
/// </remarks>
public static class WellKnownXliffNamespaces
{
    /// <summary>The UTF-8 source literal of <see cref="Core"/>.</summary>
    public static ReadOnlySpan<byte> CoreUtf8 => "urn:oasis:names:tc:xliff:document:2.0"u8;

    /// <summary>
    /// The XLIFF 2.0 and 2.1 core namespace per
    /// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#keyconcepts">XLIFF 2.1, Key concepts</see>,
    /// which states it verbatim as <c>urn:oasis:names:tc:xliff:document:2.0</c>.
    /// </summary>
    public static readonly string Core = Utf8Constants.ToInternedString(CoreUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Metadata"/>.</summary>
    public static ReadOnlySpan<byte> MetadataUtf8 => "urn:oasis:names:tc:xliff:metadata:2.0"u8;

    /// <summary>
    /// The Metadata module namespace per
    /// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#metadata_module">XLIFF 2.1, Metadata Module</see>.
    /// </summary>
    public static readonly string Metadata = Utf8Constants.ToInternedString(MetadataUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Validation"/>.</summary>
    public static ReadOnlySpan<byte> ValidationUtf8 => "urn:oasis:names:tc:xliff:validation:2.0"u8;

    /// <summary>
    /// The Validation module namespace per
    /// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#validation_module">XLIFF 2.1, Validation Module</see>.
    /// </summary>
    public static readonly string Validation = Utf8Constants.ToInternedString(ValidationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Glossary"/>.</summary>
    public static ReadOnlySpan<byte> GlossaryUtf8 => "urn:oasis:names:tc:xliff:glossary:2.0"u8;

    /// <summary>
    /// The Glossary module namespace per
    /// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#glossary-module">XLIFF 2.1, Glossary Module</see>.
    /// </summary>
    public static readonly string Glossary = Utf8Constants.ToInternedString(GlossaryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Vericula"/>.</summary>
    public static ReadOnlySpan<byte> VericulaUtf8 => "urn:lumoin:vericula:xliff:1.0"u8;

    /// <summary>
    /// The namespace of Vericula's own attributes, used where XLIFF permits attributes from other
    /// namespaces: on a validation rule for the custom length and pattern rules, and on glossary
    /// entries and translations for status, rationale and scopes.
    /// </summary>
    public static readonly string Vericula = Utf8Constants.ToInternedString(VericulaUtf8);

    /// <summary>The UTF-8 source literal of <see cref="XmlPrefix"/>.</summary>
    public static ReadOnlySpan<byte> XmlPrefixUtf8 => "xml"u8;

    /// <summary>The reserved prefix of the XML namespace itself, which carries <c>xml:space</c>.</summary>
    public static readonly string XmlPrefix = Utf8Constants.ToInternedString(XmlPrefixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MetadataPrefix"/>.</summary>
    public static ReadOnlySpan<byte> MetadataPrefixUtf8 => "mda"u8;

    /// <summary>The conventional prefix of the Metadata module, <c>mda</c>.</summary>
    public static readonly string MetadataPrefix = Utf8Constants.ToInternedString(MetadataPrefixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ValidationPrefix"/>.</summary>
    public static ReadOnlySpan<byte> ValidationPrefixUtf8 => "val"u8;

    /// <summary>The conventional prefix of the Validation module, <c>val</c>.</summary>
    public static readonly string ValidationPrefix = Utf8Constants.ToInternedString(ValidationPrefixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="GlossaryPrefix"/>.</summary>
    public static ReadOnlySpan<byte> GlossaryPrefixUtf8 => "gls"u8;

    /// <summary>The conventional prefix of the Glossary module, <c>gls</c>.</summary>
    public static readonly string GlossaryPrefix = Utf8Constants.ToInternedString(GlossaryPrefixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="VericulaPrefix"/>.</summary>
    public static ReadOnlySpan<byte> VericulaPrefixUtf8 => "vericula"u8;

    /// <summary>The prefix the writer declares for <see cref="Vericula"/>.</summary>
    public static readonly string VericulaPrefix = Utf8Constants.ToInternedString(VericulaPrefixUtf8);

    /// <summary>Determines if a namespace name is the XLIFF core namespace, <see cref="Core"/>.</summary>
    /// <param name="namespaceName">The namespace name of an element or attribute.</param>
    /// <returns><see langword="true"/> if the name is the core namespace; otherwise, <see langword="false"/>.</returns>
    public static bool IsCore(string? namespaceName) => string.Equals(namespaceName, Core, StringComparison.Ordinal);
}
