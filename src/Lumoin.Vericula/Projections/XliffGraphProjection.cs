using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Text;
using Lumoin.Base;
using Lumoin.Veritas.Core;
using Lumoin.Veritas.Turtle;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Scopes;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Projections;

/// <summary>
/// Projects an <see cref="XliffDocument"/> into an RDF graph: one resource per file and per unit, the
/// unit's source and target texts as language-tagged labels, its notes as untagged comments, the union
/// of its own scopes and every enclosing group's scopes as subjects, and the file and unit ids as
/// identifiers. The graph is a derived view of the document, never the other way round; XLIFF stays the
/// source of truth.
/// </summary>
/// <remarks>
/// <para>
/// Resources are named under a caller-supplied absolute base IRI: a file's IRI is the base with the
/// percent-encoded file id appended, and a unit's IRI is its file's IRI with the percent-encoded unit
/// id appended the same way. If the IRI being extended already ends with <c>#</c> or <c>/</c>, the id
/// is appended directly; otherwise a <c>/</c> is inserted first — this keeps both slash- and
/// hash-style vocabulary namespaces (such as this library's own <c>vericula:</c> namespace) intact.
/// Appending is always onto the IRI's string form, never RFC 3986 relative-reference resolution
/// against it (<c>new Uri(baseIri, relative)</c>): resolution drops a base's fragment outright and,
/// whenever the base's path carries no trailing slash, its last path segment too, silently folding
/// every base that shares a path prefix onto the same resource. Because appending is string
/// concatenation rather than parsing, the base IRI cannot carry a query component either; one is
/// refused with an <see cref="ArgumentException"/>. An id of <c>.</c> or <c>..</c> is refused with
/// an <see cref="ArgumentException"/> naming the id, since RFC 3986 dot-segment normalization would
/// otherwise fold such an IRI onto an unrelated resource. A source or target language tag that is empty
/// or not shaped like a BCP 47 tag is refused the same way, before it can be minted into an unparseable
/// Turtle language tag: a document read through <see cref="Lumoin.Vericula.Parsing.XliffReader"/> never
/// carries one, but a document assembled by hand can. Only published vocabularies are used, from
/// <see cref="WellKnownProjectionTerms"/>, except for a file's target language: no published term
/// distinguishes the source and target roles of <c>dcterms:language</c>, so the target tag additionally
/// carries the Vericula projection vocabulary's own <c>vericula:targetLanguage</c> term.
/// </para>
/// <para>
/// Groups themselves are not projected as resources — only their scopes are, inherited onto every unit
/// in their subtree. Group ids, group metadata, unit metadata, segment structure, states, tone
/// profiles, glossaries and validation rules are not projected.
/// </para>
/// <para>
/// The projection is pure and deterministic: quads come out in document order, files first and their
/// units after (a unit's own scopes before its enclosing groups' scopes, outermost group first), so two
/// projections of the same document yield the same sequence and the same Turtle.
/// </para>
/// </remarks>
public static class XliffGraphProjection
{
    /// <summary>
    /// Projects the document into quads in the default graph.
    /// </summary>
    /// <param name="document">The document to project.</param>
    /// <param name="baseIri">The absolute IRI under which files and units are named.</param>
    /// <returns>The quads, in document order.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="document"/> or <paramref name="baseIri"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// If <paramref name="baseIri"/> is not absolute or carries a query component; if a file or unit id
    /// is <c>.</c> or <c>..</c>; if a source or target language tag is empty or not shaped like a BCP
    /// 47 tag; or if a unit carries a target text while its file declares no target language.
    /// </exception>
    public static ImmutableArray<Quad> Project(XliffDocument document, Uri baseIri)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(baseIri);
        if(!baseIri.IsAbsoluteUri)
        {
            throw new ArgumentException("The base IRI must be absolute.", nameof(baseIri));
        }

        return [.. Quads(document, baseIri)];
    }

    /// <summary>
    /// Projects the document and writes the graph as Turtle to a pipe, completing it.
    /// </summary>
    /// <param name="document">The document to project.</param>
    /// <param name="baseIri">The absolute IRI under which files and units are named.</param>
    /// <param name="output">The pipe to write UTF-8 Turtle to.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="baseIri"/> is not absolute.</exception>
    public static void WriteTurtle(XliffDocument document, Uri baseIri, PipeWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        TurtleWriter.Write(Project(document, baseIri), output, TurtleSyntax.Turtle, TurtleOptions());
    }

    /// <summary>
    /// Projects the document and writes the graph as Turtle to a stream, which is left open.
    /// </summary>
    /// <param name="document">The document to project.</param>
    /// <param name="baseIri">The absolute IRI under which files and units are named.</param>
    /// <param name="stream">The stream to write UTF-8 Turtle to.</param>
    /// <exception cref="ArgumentNullException">If any argument is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="baseIri"/> is not absolute.</exception>
    public static void WriteTurtle(XliffDocument document, Uri baseIri, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        ImmutableArray<Quad> quads = Project(document, baseIri);
        PipeWriter output = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        TurtleWriter.Write(quads, output, TurtleSyntax.Turtle, TurtleOptions());
    }

    /// <summary>
    /// Builds the Turtle writer options shared by every serialization: the <c>rdfs:</c>, <c>dcterms:</c>
    /// and <c>vericula:</c> prefixes the projection's terms come from, so the emitted Turtle uses
    /// prefixed names instead of full IRIs.
    /// </summary>
    /// <returns>The writer options for this projection's vocabulary.</returns>
    private static TurtleWriterOptions TurtleOptions()
    {
        return new TurtleWriterOptions
        {
            Prefixes = new Dictionary<Utf8String, Utf8String>
            {
                [Utf8(WellKnownProjectionTerms.RdfsPrefix)] = Utf8(WellKnownProjectionTerms.RdfsNamespace),
                [Utf8(WellKnownProjectionTerms.DcTermsPrefix)] = Utf8(WellKnownProjectionTerms.DcTermsNamespace),
                [Utf8(WellKnownProjectionTerms.VericulaPrefix)] = Utf8(WellKnownProjectionTerms.VericulaNamespace)
            }
        };
    }

    /// <summary>
    /// Projects every file of the document, in document order: the file's own quads, then the quads of
    /// every unit under it, flattened depth-first with its enclosing groups' scopes.
    /// </summary>
    /// <param name="document">The document to project.</param>
    /// <param name="baseIri">The absolute IRI under which files and units are named.</param>
    /// <returns>The document's quads, in document order.</returns>
    private static IEnumerable<Quad> Quads(XliffDocument document, Uri baseIri)
    {
        foreach(XliffFile file in document.Files)
        {
            Uri fileIri = FileIri(baseIri, file);
            NamedNode fileNode = Node(fileIri);
            RequireWellFormedLanguageTag(file.SourceLanguage, file, "source");
            yield return new Quad(fileNode, WellKnownProjectionTerms.IdentifierNode, PlainLiteral(file.Id));
            yield return new Quad(fileNode, WellKnownProjectionTerms.SourceLanguageNode, PlainLiteral(file.SourceLanguage.Value));
            if(file.TargetLanguage is not null)
            {
                RequireWellFormedLanguageTag(file.TargetLanguage, file, "target");
                yield return new Quad(fileNode, WellKnownProjectionTerms.LanguageNode, PlainLiteral(file.TargetLanguage.Value));
                yield return new Quad(fileNode, WellKnownProjectionTerms.TargetLanguageNode, PlainLiteral(file.TargetLanguage.Value));
            }

            foreach((XliffUnit unit, ImmutableArray<Scope> inheritedScopes) in Flatten(file))
            {
                foreach(Quad quad in Quads(unit, inheritedScopes, file, fileNode, fileIri))
                {
                    yield return quad;
                }
            }
        }
    }

    /// <summary>
    /// Projects one unit's quads: its identifier, its membership in the file, its source label, its
    /// target label when it has a complete translation, its notes as comments, and its own scopes
    /// followed by its inherited scopes as subjects.
    /// </summary>
    /// <param name="unit">The unit to project.</param>
    /// <param name="inheritedScopes">The scopes inherited from the unit's enclosing groups, outermost first.</param>
    /// <param name="file">The file the unit belongs to.</param>
    /// <param name="fileNode">The file's already-built resource node.</param>
    /// <param name="fileIri">The file's IRI, extended to build the unit's own IRI.</param>
    /// <returns>The unit's quads.</returns>
    /// <exception cref="ArgumentException">If the unit carries a target text while its file declares no target language.</exception>
    private static IEnumerable<Quad> Quads(XliffUnit unit, ImmutableArray<Scope> inheritedScopes, XliffFile file, NamedNode fileNode, Uri fileIri)
    {
        NamedNode unitNode = Node(UnitIri(fileIri, unit));
        yield return new Quad(unitNode, WellKnownProjectionTerms.IdentifierNode, PlainLiteral(unit.Id));
        yield return new Quad(unitNode, WellKnownProjectionTerms.IsPartOfNode, fileNode);
        yield return new Quad(unitNode, WellKnownProjectionTerms.LabelNode, TaggedLiteral(unit.Source, file.SourceLanguage));
        if(unit.Target is { } target)
        {
            if(file.TargetLanguage is not { } targetLanguage)
            {
                throw new ArgumentException($"Unit '{unit.Id}' in file '{file.Id}' has a target text but the file declares no target language.");
            }

            yield return new Quad(unitNode, WellKnownProjectionTerms.LabelNode, TaggedLiteral(target, targetLanguage));
        }

        foreach(string note in unit.Notes)
        {
            yield return new Quad(unitNode, WellKnownProjectionTerms.CommentNode, PlainLiteral(note));
        }

        foreach(Scope scope in unit.Scopes)
        {
            yield return new Quad(unitNode, WellKnownProjectionTerms.SubjectNode, PlainLiteral(scope.Value));
        }

        foreach(Scope scope in inheritedScopes)
        {
            yield return new Quad(unitNode, WellKnownProjectionTerms.SubjectNode, PlainLiteral(scope.Value));
        }
    }

    /// <summary>
    /// Refuses a source or target language tag that is not shaped like a BCP 47 tag, using the same
    /// check <see cref="Lumoin.Vericula.Parsing.XliffReader"/> and
    /// <see cref="Lumoin.Vericula.Parsing.XliffWriter"/> already apply to srcLang and trgLang, so a
    /// hand-built document is held to the same shape a document read through the reader is guaranteed
    /// to already satisfy.
    /// </summary>
    /// <param name="tag">The language tag to check.</param>
    /// <param name="file">The file the tag belongs to, used in the refusal message.</param>
    /// <param name="role">Whether the tag is the file's "source" or "target" language, used in the refusal message.</param>
    /// <exception cref="ArgumentException">If <paramref name="tag"/> is not a well-formed BCP 47 tag.</exception>
    private static void RequireWellFormedLanguageTag(LanguageTag tag, XliffFile file, string role)
    {
        if(!LanguageTag.IsWellFormed(tag.Value))
        {
            throw new ArgumentException($"File '{file.Id}' has a {role} language tag '{tag.Value}' that is not a well-formed BCP 47 tag.");
        }
    }

    /// <summary>
    /// Builds a file's resource IRI by extending the base IRI with its percent-encoded id.
    /// </summary>
    /// <param name="baseIri">The absolute IRI under which files are named.</param>
    /// <param name="file">The file to build an IRI for.</param>
    /// <returns>The file's resource IRI.</returns>
    private static Uri FileIri(Uri baseIri, XliffFile file)
    {
        return ResolvedNode(baseIri, file.Id, "file id");
    }

    /// <summary>
    /// Builds a unit's resource IRI by extending its file's IRI with its percent-encoded id.
    /// </summary>
    /// <param name="fileIri">The unit's file's resource IRI.</param>
    /// <param name="unit">The unit to build an IRI for.</param>
    /// <returns>The unit's resource IRI.</returns>
    private static Uri UnitIri(Uri fileIri, XliffUnit unit)
    {
        return ResolvedNode(fileIri, unit.Id, "unit id");
    }

    /// <summary>
    /// Appends the percent-encoded id onto <paramref name="baseIri"/>'s string form: directly, when the
    /// base already ends with <c>#</c> or <c>/</c>, otherwise after inserting a <c>/</c>. This is
    /// string concatenation, not RFC 3986 relative-reference resolution, so a hash-namespaced base's
    /// fragment is preserved rather than dropped, and no base path segment is stripped; the price is
    /// that the base cannot carry a query component, since concatenating after one would silently
    /// extend it instead of naming a new resource.
    /// </summary>
    /// <param name="baseIri">The IRI to extend.</param>
    /// <param name="id">The id to append, percent-encoded.</param>
    /// <param name="idKind">What kind of id this is, used in the refusal message.</param>
    /// <returns>The extended resource IRI.</returns>
    /// <exception cref="ArgumentException">If <paramref name="id"/> is <c>.</c> or <c>..</c>, or <paramref name="baseIri"/> carries a query component.</exception>
    private static Uri ResolvedNode(Uri baseIri, string id, string idKind)
    {
        if(id is "." or "..")
        {
            throw new ArgumentException($"The {idKind} '{id}' cannot be used to build a resource IRI: '.' and '..' are dot segments that RFC 3986 normalization would fold onto a different resource.");
        }

        if(!string.IsNullOrEmpty(baseIri.Query))
        {
            throw new ArgumentException($"The base IRI '{baseIri}' carries a query component; resource IRIs are built by appending to its string form, which a query component would silently extend instead of naming a new resource.");
        }

        string absolute = baseIri.AbsoluteUri;
        string separator = absolute.EndsWith('#') || absolute.EndsWith('/') ? string.Empty : "/";

        return new Uri(absolute + separator + Uri.EscapeDataString(id), UriKind.Absolute);
    }

    /// <summary>
    /// Wraps a resource IRI as a named node.
    /// </summary>
    /// <param name="iri">The resource's IRI.</param>
    /// <returns>The named node.</returns>
    private static NamedNode Node(Uri iri)
    {
        return new NamedNode(Utf8(iri.AbsoluteUri));
    }

    /// <summary>
    /// Builds an <c>xsd:string</c>-typed literal, used for a value with no natural-language content of
    /// its own, such as an identifier.
    /// </summary>
    /// <param name="value">The literal's text.</param>
    /// <returns>The plain literal.</returns>
    private static Literal PlainLiteral(string value)
    {
        return new Literal(Utf8(value), Vocabulary.Xsd.Nodes.String);
    }

    /// <summary>
    /// Builds an <c>rdf:langString</c> literal carrying its BCP 47 language tag, used for a unit's
    /// source or target text.
    /// </summary>
    /// <param name="value">The literal's text.</param>
    /// <param name="language">The text's language tag.</param>
    /// <returns>The language-tagged literal.</returns>
    private static Literal TaggedLiteral(string value, LanguageTag language)
    {
        return new Literal(Utf8(value), Vocabulary.Rdf.Nodes.LangString, Utf8(language.Value));
    }

    /// <summary>
    /// Encodes a string as the UTF-8 byte form the graph model's nodes and literals carry.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <returns>The UTF-8 encoded value.</returns>
    private static Utf8String Utf8(string value)
    {
        return new Utf8String(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Flattens a file's own units, each with no inherited scopes, followed by every unit under its
    /// groups, each paired with its enclosing groups' scopes.
    /// </summary>
    /// <param name="file">The file to flatten.</param>
    /// <returns>Every unit under the file, paired with its inherited scopes, in document order.</returns>
    private static IEnumerable<(XliffUnit Unit, ImmutableArray<Scope> InheritedScopes)> Flatten(XliffFile file)
    {
        foreach(XliffUnit unit in file.Units)
        {
            yield return (unit, ImmutableArray<Scope>.Empty);
        }

        foreach(XliffGroup group in file.Groups)
        {
            foreach((XliffUnit unit, ImmutableArray<Scope> inheritedScopes) in Flatten(group, ImmutableArray<Scope>.Empty))
            {
                yield return (unit, inheritedScopes);
            }
        }
    }

    /// <summary>
    /// Recursively flattens a group's own units, paired with the inherited scopes plus this group's own
    /// scopes, followed by every unit of its nested groups.
    /// </summary>
    /// <param name="group">The group to flatten.</param>
    /// <param name="inheritedScopes">The scopes inherited from the group's own enclosing groups, outermost first.</param>
    /// <returns>Every unit under the group, paired with its inherited scopes, in document order.</returns>
    private static IEnumerable<(XliffUnit Unit, ImmutableArray<Scope> InheritedScopes)> Flatten(XliffGroup group, ImmutableArray<Scope> inheritedScopes)
    {
        ImmutableArray<Scope> scopes = inheritedScopes.AddRange(group.Scopes);
        foreach(XliffUnit unit in group.Units)
        {
            yield return (unit, scopes);
        }

        foreach(XliffGroup nested in group.Groups)
        {
            foreach((XliffUnit unit, ImmutableArray<Scope> nestedScopes) in Flatten(nested, scopes))
            {
                yield return (unit, nestedScopes);
            }
        }
    }
}
