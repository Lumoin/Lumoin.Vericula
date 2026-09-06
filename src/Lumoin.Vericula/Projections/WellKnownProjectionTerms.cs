using Lumoin.Base;
using Lumoin.Veritas.Core;
using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Projections;

/// <summary>
/// The well-known RDF terms the graph projection uses to describe a document: RDF Schema for labels
/// and comments, and Dublin Core Terms for identifiers, part-of and subject relations. Only published
/// vocabularies are used; Vericula mints no term of its own here.
/// </summary>
/// <remarks>
/// Each IRI is spelled once as a UTF-8 source literal and carried alongside as an interned string and
/// as a <see cref="NamedNode"/> ready for a quad. RDF Schema is defined in
/// <see href="https://www.w3.org/TR/rdf12-schema/">RDF 1.2 Schema</see>; Dublin Core Terms in
/// <see href="https://www.dublincore.org/specifications/dublin-core/dcmi-terms/">DCMI Metadata Terms</see>.
/// </remarks>
public static class WellKnownProjectionTerms
{
    /// <summary>The UTF-8 source literal of <see cref="RdfsNamespace"/>.</summary>
    public static ReadOnlySpan<byte> RdfsNamespaceUtf8 => "http://www.w3.org/2000/01/rdf-schema#"u8;

    /// <summary>The RDF Schema namespace.</summary>
    public static readonly string RdfsNamespace = Utf8Constants.ToInternedString(RdfsNamespaceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DcTermsNamespace"/>.</summary>
    public static ReadOnlySpan<byte> DcTermsNamespaceUtf8 => "http://purl.org/dc/terms/"u8;

    /// <summary>The Dublin Core Terms namespace.</summary>
    public static readonly string DcTermsNamespace = Utf8Constants.ToInternedString(DcTermsNamespaceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="RdfsPrefix"/>.</summary>
    public static ReadOnlySpan<byte> RdfsPrefixUtf8 => "rdfs"u8;

    /// <summary>The conventional prefix of <see cref="RdfsNamespace"/>.</summary>
    public static readonly string RdfsPrefix = Utf8Constants.ToInternedString(RdfsPrefixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DcTermsPrefix"/>.</summary>
    public static ReadOnlySpan<byte> DcTermsPrefixUtf8 => "dcterms"u8;

    /// <summary>The conventional prefix of <see cref="DcTermsNamespace"/>.</summary>
    public static readonly string DcTermsPrefix = Utf8Constants.ToInternedString(DcTermsPrefixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Label"/>.</summary>
    public static ReadOnlySpan<byte> LabelUtf8 => "http://www.w3.org/2000/01/rdf-schema#label"u8;

    /// <summary>
    /// <c>rdfs:label</c>, a human-readable name of a resource per
    /// <see href="https://www.w3.org/TR/rdf12-schema/#ch_label">RDF 1.2 Schema, rdfs:label</see>; the
    /// projection carries a unit's source and target texts as language-tagged labels.
    /// </summary>
    public static readonly string Label = Utf8Constants.ToInternedString(LabelUtf8);

    /// <summary>The <see cref="Label"/> predicate as a node.</summary>
    public static NamedNode LabelNode { get; } = new(new Utf8String(LabelUtf8.ToArray()));

    /// <summary>The UTF-8 source literal of <see cref="Comment"/>.</summary>
    public static ReadOnlySpan<byte> CommentUtf8 => "http://www.w3.org/2000/01/rdf-schema#comment"u8;

    /// <summary>
    /// <c>rdfs:comment</c>, a human-readable description of a resource per
    /// <see href="https://www.w3.org/TR/rdf12-schema/#ch_comment">RDF 1.2 Schema, rdfs:comment</see>;
    /// the projection carries a unit's notes as untagged, plain-string comments.
    /// </summary>
    public static readonly string Comment = Utf8Constants.ToInternedString(CommentUtf8);

    /// <summary>The <see cref="Comment"/> predicate as a node.</summary>
    public static NamedNode CommentNode { get; } = new(new Utf8String(CommentUtf8.ToArray()));

    /// <summary>The UTF-8 source literal of <see cref="Identifier"/>.</summary>
    public static ReadOnlySpan<byte> IdentifierUtf8 => "http://purl.org/dc/terms/identifier"u8;

    /// <summary>
    /// <c>dcterms:identifier</c>, an unambiguous reference to the resource within a given context per
    /// <see href="https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#http://purl.org/dc/terms/identifier">DCMI Terms, identifier</see>;
    /// the projection carries the XLIFF file and unit ids.
    /// </summary>
    public static readonly string Identifier = Utf8Constants.ToInternedString(IdentifierUtf8);

    /// <summary>The <see cref="Identifier"/> predicate as a node.</summary>
    public static NamedNode IdentifierNode { get; } = new(new Utf8String(IdentifierUtf8.ToArray()));

    /// <summary>The UTF-8 source literal of <see cref="IsPartOf"/>.</summary>
    public static ReadOnlySpan<byte> IsPartOfUtf8 => "http://purl.org/dc/terms/isPartOf"u8;

    /// <summary>
    /// <c>dcterms:isPartOf</c>, a related resource in which the described resource is physically or
    /// logically included, per
    /// <see href="https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#http://purl.org/dc/terms/isPartOf">DCMI Terms, isPartOf</see>;
    /// the projection relates a unit to its file.
    /// </summary>
    public static readonly string IsPartOf = Utf8Constants.ToInternedString(IsPartOfUtf8);

    /// <summary>The <see cref="IsPartOf"/> predicate as a node.</summary>
    public static NamedNode IsPartOfNode { get; } = new(new Utf8String(IsPartOfUtf8.ToArray()));

    /// <summary>The UTF-8 source literal of <see cref="Subject"/>.</summary>
    public static ReadOnlySpan<byte> SubjectUtf8 => "http://purl.org/dc/terms/subject"u8;

    /// <summary>
    /// <c>dcterms:subject</c>, a topic of the resource, per
    /// <see href="https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#http://purl.org/dc/terms/subject">DCMI Terms, subject</see>;
    /// the projection carries a unit's scopes as subjects.
    /// </summary>
    public static readonly string Subject = Utf8Constants.ToInternedString(SubjectUtf8);

    /// <summary>The <see cref="Subject"/> predicate as a node.</summary>
    public static NamedNode SubjectNode { get; } = new(new Utf8String(SubjectUtf8.ToArray()));

    /// <summary>The UTF-8 source literal of <see cref="Language"/>.</summary>
    public static ReadOnlySpan<byte> LanguageUtf8 => "http://purl.org/dc/terms/language"u8;

    /// <summary>
    /// <c>dcterms:language</c>, a language of the resource, per
    /// <see href="https://www.dublincore.org/specifications/dublin-core/dcmi-terms/#http://purl.org/dc/terms/language">DCMI Terms, language</see>;
    /// the projection carries a file's source and target language tags with this predicate, so that a
    /// consumer that only understands Dublin Core still sees both.
    /// </summary>
    public static readonly string Language = Utf8Constants.ToInternedString(LanguageUtf8);

    /// <summary>
    /// The <see cref="Language"/> predicate as a node, asserted once for a file's source language.
    /// Renders identically to <see cref="LanguageNode"/>; the two names exist so the emitting code
    /// states which role of <c>dcterms:language</c> a given triple carries.
    /// </summary>
    public static NamedNode SourceLanguageNode { get; } = new(new Utf8String(LanguageUtf8.ToArray()));

    /// <summary>
    /// The <see cref="Language"/> predicate as a node, asserted for a file's target language alongside
    /// <see cref="TargetLanguageNode"/>. See <see cref="SourceLanguageNode"/>.
    /// </summary>
    public static NamedNode LanguageNode { get; } = new(new Utf8String(LanguageUtf8.ToArray()));

    /// <summary>The UTF-8 source literal of <see cref="TargetLanguage"/>.</summary>
    public static ReadOnlySpan<byte> TargetLanguageUtf8 => "https://vericula.lumoin.com/vocab#targetLanguage"u8;

    /// <summary>
    /// <c>vericula:targetLanguage</c>, the Vericula projection vocabulary's own term for a file's target
    /// language. No published vocabulary distinguishes the source role of <c>dcterms:language</c> from
    /// its target role, so the projection asserts both: <c>dcterms:language</c> on each of the two tags,
    /// for a consumer that only understands Dublin Core, and this term additionally on the target tag,
    /// for a consumer that needs to tell the two apart.
    /// </summary>
    public static readonly string TargetLanguage = Utf8Constants.ToInternedString(TargetLanguageUtf8);

    /// <summary>The <see cref="TargetLanguage"/> predicate as a node.</summary>
    public static NamedNode TargetLanguageNode { get; } = new(new Utf8String(TargetLanguageUtf8.ToArray()));

    /// <summary>The UTF-8 source literal of <see cref="VericulaPrefix"/>.</summary>
    public static ReadOnlySpan<byte> VericulaPrefixUtf8 => "vericula"u8;

    /// <summary>The conventional prefix of <see cref="VericulaNamespace"/>.</summary>
    public static readonly string VericulaPrefix = Utf8Constants.ToInternedString(VericulaPrefixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="VericulaNamespace"/>.</summary>
    public static ReadOnlySpan<byte> VericulaNamespaceUtf8 => "https://vericula.lumoin.com/vocab#"u8;

    /// <summary>The Vericula projection vocabulary's own namespace, used only for <see cref="TargetLanguage"/>.</summary>
    public static readonly string VericulaNamespace = Utf8Constants.ToInternedString(VericulaNamespaceUtf8);
}
