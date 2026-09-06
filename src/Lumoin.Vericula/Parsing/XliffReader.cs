using System.Collections.Immutable;
using System.Globalization;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Scopes;
using Lumoin.Vericula.Tone;
using Lumoin.Vericula.Units;
using Lumoin.Vericula.Validation;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// Reads XLIFF 2.0 and 2.1 documents into the <see cref="XliffDocument"/> model, either whole or as
/// a stream of units.
/// </summary>
/// <remarks>
/// <para>
/// The reader is namespace-aware (the XLIFF 2.x core namespace
/// <c>urn:oasis:names:tc:xliff:document:2.0</c>) and rejects DTDs so a hostile document cannot pull
/// in external entities. It reads the core structure (files, groups, units, notes, segments and
/// ignorables with their ids and states) and three XLIFF 2.1 modules: the Validation module on a
/// file becomes its <see cref="XliffFile.ValidationRules"/>; the Glossary module on a unit becomes
/// its <see cref="XliffUnit.Glossary"/>; and the Metadata module carries the parts of the model the
/// standard has no slot for, in Vericula-named metadata groups: a file's tone profile and file-wide
/// glossary, and the scopes and named metadata of groups and units.
/// </para>
/// <para>
/// <see cref="Read(PipeReader)"/> and its overloads materialize the whole document. The unit stream,
/// <see cref="ReadUnitsAsync(PipeReader, CancellationToken)"/> and its overloads, walks the input
/// forward and yields each unit as soon as its element has been read, holding only the current unit
/// in memory; it flattens groups, so a streamed unit carries its own scopes and metadata but not
/// those of the groups enclosing it.
/// </para>
/// <para>
/// What the reader does not accept, it refuses loudly rather than silently corrupting: inline codes
/// that lose data when flattened, targets in a document that declares no target language, duplicate
/// unit ids, unknown segment states, validation rules whose comparison semantics the linter cannot
/// honour, and validation on groups or units. Metadata groups in categories the reader does not know
/// are ignored; this is the one place where information from another tool is dropped.
/// </para>
/// </remarks>
public static class XliffReader
{
    /// <summary>The XLIFF 2.x core namespace, used to match root, file, group, unit and segment elements.</summary>
    private static readonly XNamespace Core = WellKnownXliffNamespaces.Core;

    /// <summary>The Metadata module namespace, used to match a container's <c>metadata</c> element and its groups.</summary>
    private static readonly XNamespace Metadata = WellKnownXliffNamespaces.Metadata;

    /// <summary>The Validation module namespace, used to match a file's <c>validation</c> element and its rules.</summary>
    private static readonly XNamespace Validation = WellKnownXliffNamespaces.Validation;

    /// <summary>The Glossary module namespace, used to match a unit's <c>glossary</c> element and its entries.</summary>
    private static readonly XNamespace Glossary = WellKnownXliffNamespaces.Glossary;

    /// <summary>The Vericula-specific namespace, used for the reader's own attributes and metadata categories.</summary>
    private static readonly XNamespace Vericula = WellKnownXliffNamespaces.Vericula;

    /// <summary>
    /// Reads a whole XLIFF document from a pipe.
    /// </summary>
    /// <param name="input">The pipe to read from; it is left open.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="input"/> is null.</exception>
    /// <exception cref="XliffFormatException">If the input is not well-formed XLIFF the reader accepts.</exception>
    public static XliffDocument Read(PipeReader input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return Read(input.AsStream(leaveOpen: true));
    }

    /// <summary>
    /// Reads a whole XLIFF document from a pipe asynchronously. Cancelling <paramref name="cancellationToken"/>
    /// interrupts a read that is stalled waiting for more bytes, because it cancels the pipe's own
    /// pending read; a plain <see cref="Stream"/> overload can only observe cancellation between XML
    /// nodes, since <see cref="XmlReader"/> has no token-aware async read of its own.
    /// </summary>
    /// <param name="input">The pipe to read from; it is left open.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="input"/> is null.</exception>
    /// <exception cref="XliffFormatException">If the input is not well-formed XLIFF the reader accepts.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled.</exception>
    public static async Task<XliffDocument> ReadAsync(PipeReader input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        await using CancellationTokenRegistration registration = cancellationToken.Register(input.CancelPendingRead);

        return await ReadAsync(input.AsStream(leaveOpen: true), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a whole XLIFF document from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is null.</exception>
    /// <exception cref="XliffFormatException">If the stream is not well-formed XLIFF the reader accepts.</exception>
    public static XliffDocument Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return Parse(Load(stream));
    }

    /// <summary>
    /// Reads a whole XLIFF document from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is null.</exception>
    /// <exception cref="XliffFormatException">If the stream is not well-formed XLIFF the reader accepts.</exception>
    public static async Task<XliffDocument> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return Parse(await LoadAsync(stream, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Streams the units of an XLIFF document from a pipe, in document order, without materializing
    /// the document.
    /// </summary>
    /// <param name="input">The pipe to read from; it is left open.</param>
    /// <returns>The units, one at a time, groups flattened.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="input"/> is null.</exception>
    /// <exception cref="XliffFormatException">If the input is not well-formed XLIFF the reader accepts; thrown from the enumeration.</exception>
    public static IEnumerable<XliffUnit> ReadUnits(PipeReader input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return ReadUnits(input.AsStream(leaveOpen: true));
    }

    /// <summary>
    /// Streams the units of an XLIFF document from a stream, in document order, without materializing
    /// the document.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The units, one at a time, groups flattened.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is null.</exception>
    /// <exception cref="XliffFormatException">If the stream is not well-formed XLIFF the reader accepts; thrown from the enumeration.</exception>
    public static IEnumerable<XliffUnit> ReadUnits(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return IterateUnits(stream);
    }

    /// <summary>
    /// Streams the units of an XLIFF document from a pipe asynchronously, in document order, without
    /// materializing the document. Cancelling <paramref name="cancellationToken"/> interrupts an
    /// enumeration that is stalled waiting for more bytes; see
    /// <see cref="ReadAsync(PipeReader, CancellationToken)"/>.
    /// </summary>
    /// <param name="input">The pipe to read from; it is left open.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The units, one at a time, groups flattened.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="input"/> is null.</exception>
    /// <exception cref="XliffFormatException">If the input is not well-formed XLIFF the reader accepts; thrown from the enumeration.</exception>
    /// <exception cref="OperationCanceledException">If <paramref name="cancellationToken"/> is cancelled; thrown from the enumeration.</exception>
    public static IAsyncEnumerable<XliffUnit> ReadUnitsAsync(PipeReader input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        return IterateUnitsFromPipeAsync(input, cancellationToken);
    }

    /// <summary>
    /// Adapts <see cref="ReadUnitsAsync(Stream, CancellationToken)"/> to a pipe, registering
    /// <paramref name="cancellationToken"/> against the pipe's own pending read so cancellation
    /// interrupts a stalled read rather than only being observed between XML nodes.
    /// </summary>
    /// <param name="input">The pipe to read from; it is left open.</param>
    /// <param name="cancellationToken">A token to cancel the enumeration.</param>
    /// <returns>The units, one at a time, groups flattened.</returns>
    private static async IAsyncEnumerable<XliffUnit> IterateUnitsFromPipeAsync(PipeReader input, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using CancellationTokenRegistration registration = cancellationToken.Register(input.CancelPendingRead);

        await foreach(XliffUnit unit in ReadUnitsAsync(input.AsStream(leaveOpen: true), cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return unit;
        }
    }

    /// <summary>
    /// Streams the units of an XLIFF document from a stream asynchronously, in document order,
    /// without materializing the document.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The units, one at a time, groups flattened.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is null.</exception>
    /// <exception cref="XliffFormatException">If the stream is not well-formed XLIFF the reader accepts; thrown from the enumeration.</exception>
    public static IAsyncEnumerable<XliffUnit> ReadUnitsAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return IterateUnitsAsync(stream, cancellationToken);
    }

    /// <summary>
    /// Walks <paramref name="stream"/> forward node by node, deciding at each node whether to take a
    /// unit or advance past it (<see cref="Inspect(XmlReader, StreamState)"/>), and checks after the
    /// walk ends that an <c>&lt;xliff&gt;</c> root was actually seen.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The units, one at a time, groups flattened.</returns>
    private static IEnumerable<XliffUnit> IterateUnits(Stream stream)
    {
        using XmlReader reader = XmlReader.Create(stream, CreateReaderSettings());
        StreamState state = StreamState.Start;
        while(!reader.EOF)
        {
            StreamStep step;
            (step, state) = Inspect(reader, state);
            if(step == StreamStep.TakeUnit)
            {
                XliffUnit unit;
                (unit, state) = AcceptUnit(ReadElement(reader), state);
                yield return unit;

                continue;
            }

            Advance(reader);
        }

        RequireRoot(state);
    }

    /// <summary>
    /// The asynchronous counterpart of <see cref="IterateUnits(Stream)"/>: walks <paramref name="stream"/>
    /// forward node by node, checking cancellation between nodes since <see cref="XmlReader"/> has no
    /// token-aware async read of its own.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">A token to cancel the enumeration.</param>
    /// <returns>The units, one at a time, groups flattened.</returns>
    private static async IAsyncEnumerable<XliffUnit> IterateUnitsAsync(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using XmlReader reader = XmlReader.Create(stream, CreateReaderSettings());
        StreamState state = StreamState.Start;
        while(!reader.EOF)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StreamStep step;
            (step, state) = Inspect(reader, state);
            if(step == StreamStep.TakeUnit)
            {
                XliffUnit unit;
                (unit, state) = AcceptUnit(await ReadElementAsync(reader, cancellationToken).ConfigureAwait(false), state);
                yield return unit;

                continue;
            }

            await AdvanceAsync(reader).ConfigureAwait(false);
        }

        RequireRoot(state);
    }

    /// <summary>
    /// Decides what the streaming walk does with the reader's current node: take the unit it starts,
    /// or advance past it. The root, file and group elements update the walk's state on the way, and
    /// the checks the whole-document read applies to them are applied here too: required, unique,
    /// well-formed ids on &lt;file&gt; and &lt;group&gt;; a &lt;unit&gt; or &lt;group&gt; only inside a
    /// &lt;file&gt;, refusing either directly under &lt;xliff&gt; with the same message the
    /// whole-document read uses; any other core element directly under &lt;xliff&gt;, refused with that
    /// same message; a &lt;file&gt; with no &lt;unit&gt; or &lt;group&gt; direct child, refused whether
    /// it closes with a paired end tag or is written self-closing; and no Validation module directly or
    /// indirectly under a &lt;group&gt;.
    /// </summary>
    private static (StreamStep Step, StreamState State) Inspect(XmlReader reader, StreamState state)
    {
        bool core = WellKnownXliffNamespaces.IsCore(reader.NamespaceURI);
        string name = reader.LocalName;
        if(reader.NodeType == XmlNodeType.EndElement)
        {
            if(core && WellKnownXliffElements.IsFile(name))
            {
                if(state.FileMembers == 0)
                {
                    throw new XliffFormatException($"File '{state.CurrentFileId}' has no <unit> or <group> element; XLIFF 2.1 §4.2.2.2 requires at least one.");
                }

                return (StreamStep.Advance, state with { InFile = false, CurrentFileId = null, FileMembers = 0 });
            }

            if(core && WellKnownXliffElements.IsGroup(name))
            {
                return (StreamStep.Advance, state with { OpenGroups = state.OpenGroups - 1 });
            }

            return (StreamStep.Advance, state);
        }

        if(reader.NodeType != XmlNodeType.Element)
        {
            return (StreamStep.Advance, state);
        }

        if(!state.RootSeen)
        {
            if(!core || !WellKnownXliffElements.IsXliff(name))
            {
                throw new XliffFormatException("The document root is not an <xliff> element in the XLIFF 2.x core namespace.");
            }

            ParseVersion(reader.GetAttribute(WellKnownXliffAttributes.Version));
            RequireSourceLanguage(reader.GetAttribute(WellKnownXliffAttributes.SourceLanguage));
            string? targetLanguage = RequireWellFormedTargetLanguage(reader.GetAttribute(WellKnownXliffAttributes.TargetLanguage));

            return (StreamStep.Advance, state with { RootSeen = true, TargetLanguage = targetLanguage });
        }

        if(state.OpenGroups > 0 && string.Equals(reader.NamespaceURI, WellKnownXliffNamespaces.Validation, StringComparison.Ordinal) && WellKnownXliffElements.IsValidation(name))
        {
            throw new XliffFormatException("Validation rules on a <group> are not supported; attach them to the <file>.");
        }

        if(!core)
        {
            return (StreamStep.Advance, state);
        }

        if(WellKnownXliffElements.IsFile(name))
        {
            string fileId = RequireId(reader.GetAttribute(WellKnownXliffAttributes.Id), WellKnownXliffElements.File);
            if(state.FileIds.Contains(fileId))
            {
                throw new XliffFormatException($"Duplicate file id '{fileId}'. File ids must be unique within the document.");
            }

            //a self-closing <file/> never raises a separate EndElement node, so the empty check that
            //</file> otherwise applies has to run here too.
            if(reader.IsEmptyElement)
            {
                throw new XliffFormatException($"File '{fileId}' has no <unit> or <group> element; XLIFF 2.1 §4.2.2.2 requires at least one.");
            }

            return (StreamStep.Advance, state with
            {
                InFile = true,
                FileIds = state.FileIds.Add(fileId),
                Seen = ImmutableHashSet.Create<string>(StringComparer.Ordinal),
                CurrentFileId = fileId,
                FileMembers = 0
            });
        }

        if(WellKnownXliffElements.IsGroup(name))
        {
            if(!state.InFile)
            {
                throw new XliffFormatException($"The <xliff> element contains a <{WellKnownXliffElements.Group}> element; XLIFF 2.1 §4.2.2.1 allows one or more <file> elements there and nothing else.");
            }

            RequireId(reader.GetAttribute(WellKnownXliffAttributes.Id), WellKnownXliffElements.Group);

            return (StreamStep.Advance, state with
            {
                OpenGroups = state.OpenGroups + 1,
                FileMembers = state.OpenGroups == 0 ? state.FileMembers + 1 : state.FileMembers
            });
        }

        if(WellKnownXliffElements.IsUnit(name))
        {
            if(!state.InFile)
            {
                throw new XliffFormatException("A <unit> element is not inside a <file>; XLIFF 2.1 §4.2.2.1 allows only <file> children directly under <xliff>.");
            }

            return (StreamStep.TakeUnit, state.OpenGroups == 0 ? state with { FileMembers = state.FileMembers + 1 } : state);
        }

        if(!state.InFile)
        {
            throw new XliffFormatException($"The <xliff> element contains a <{name}> element; XLIFF 2.1 §4.2.2.1 allows one or more <file> elements there and nothing else.");
        }

        return (StreamStep.Advance, state);
    }

    /// <summary>
    /// Parses a streamed unit and applies the per-file invariants the whole-document read enforces:
    /// unique unit ids, and a declared target language whenever a unit carries a translation.
    /// </summary>
    private static (XliffUnit Unit, StreamState State) AcceptUnit(XElement element, StreamState state)
    {
        XliffUnit unit = ParseUnit(element);
        if(state.Seen.Contains(unit.Id))
        {
            throw new XliffFormatException($"Duplicate unit id '{unit.Id}'. Unit ids must be unique within a file.");
        }

        if(HasAnyTarget(unit) && state.TargetLanguage is null)
        {
            throw new XliffFormatException("The document carries <target> content but declares no trgLang; trgLang is required when targets are present.");
        }

        return (unit, state with { Seen = state.Seen.Add(unit.Id) });
    }

    /// <summary>
    /// Throws when the streaming walk reached the end of the input without ever seeing an
    /// <c>&lt;xliff&gt;</c> root element.
    /// </summary>
    /// <param name="state">The streaming walk's final state.</param>
    /// <exception cref="XliffFormatException">If <paramref name="state"/> never saw the root.</exception>
    private static void RequireRoot(StreamState state)
    {
        if(!state.RootSeen)
        {
            throw new XliffFormatException("The document has no <xliff> root element.");
        }
    }

    /// <summary>
    /// Moves <paramref name="reader"/> to its next node, wrapping any well-formedness error the move
    /// raises in an <see cref="XliffFormatException"/>.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <exception cref="XliffFormatException">If the underlying XML is not well-formed.</exception>
    private static void Advance(XmlReader reader)
    {
        try
        {
            reader.Read();
        }
        catch(XmlException exception)
        {
            throw NotWellFormed(exception);
        }
    }

    /// <summary>
    /// The asynchronous counterpart of <see cref="Advance(XmlReader)"/>.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <returns>A task that completes once the reader has moved.</returns>
    /// <exception cref="XliffFormatException">If the underlying XML is not well-formed.</exception>
    private static async Task AdvanceAsync(XmlReader reader)
    {
        try
        {
            await reader.ReadAsync().ConfigureAwait(false);
        }
        catch(XmlException exception)
        {
            throw NotWellFormed(exception);
        }
    }

    /// <summary>
    /// Materializes the element the reader is positioned on and leaves the reader on the node after it.
    /// </summary>
    private static XElement ReadElement(XmlReader reader)
    {
        try
        {
            return (XElement)XNode.ReadFrom(reader);
        }
        catch(XmlException exception)
        {
            throw NotWellFormed(exception);
        }
    }

    /// <summary>
    /// The asynchronous counterpart of <see cref="ReadElement(XmlReader)"/>.
    /// </summary>
    /// <param name="reader">The reader positioned on the element to materialize.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The materialized element; the reader is left on the node after it.</returns>
    /// <exception cref="XliffFormatException">If the underlying XML is not well-formed.</exception>
    private static async Task<XElement> ReadElementAsync(XmlReader reader, CancellationToken cancellationToken)
    {
        try
        {
            return (XElement)await XNode.ReadFromAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        catch(XmlException exception)
        {
            throw NotWellFormed(exception);
        }
    }

    /// <summary>
    /// Wraps an <see cref="XmlException"/> from parsing the raw XML in an <see cref="XliffFormatException"/>
    /// whose message carries the inner exception's own message (which names the well-formedness
    /// problem) and, when the reader could determine one, the line and position it occurred at.
    /// </summary>
    private static XliffFormatException NotWellFormed(XmlException exception)
    {
        return new XliffFormatException($"The XLIFF document is not well-formed XML: {exception.Message}", exception)
        {
            Line = exception.LineNumber > 0 ? exception.LineNumber : null,
            Position = exception.LinePosition > 0 ? exception.LinePosition : null
        };
    }

    /// <summary>
    /// Attaches the line and position <paramref name="element"/> was found at to
    /// <paramref name="exception"/>, when <paramref name="element"/> carries one; loading with
    /// <see cref="LoadOptions.SetLineInfo"/> is what makes <see cref="IXmlLineInfo"/> available on it.
    /// </summary>
    private static XliffFormatException WithLocation(XliffFormatException exception, XElement element)
    {
        if(element is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            exception.Line = lineInfo.LineNumber;
            exception.Position = lineInfo.LinePosition;
        }

        return exception;
    }

    /// <summary>
    /// Loads the whole document from <paramref name="stream"/> with a secure, DTD-prohibiting reader,
    /// keeping line info so a later refusal can name where in the document it occurred.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="XliffFormatException">If the stream is not well-formed XML.</exception>
    private static XDocument Load(Stream stream)
    {
        try
        {
            using var reader = XmlReader.Create(stream, CreateReaderSettings());

            return XDocument.Load(reader, LoadOptions.SetLineInfo);
        }
        catch(XmlException exception)
        {
            throw NotWellFormed(exception);
        }
    }

    /// <summary>
    /// The asynchronous counterpart of <see cref="Load(Stream)"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">A token to cancel the load.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="XliffFormatException">If the stream is not well-formed XML.</exception>
    private static async Task<XDocument> LoadAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = XmlReader.Create(stream, CreateReaderSettings());

            return await XDocument.LoadAsync(reader, LoadOptions.SetLineInfo, cancellationToken).ConfigureAwait(false);
        }
        catch(XmlException exception)
        {
            throw NotWellFormed(exception);
        }
    }

    /// <summary>
    /// Creates reader settings with DTDs prohibited and the resolver removed, so an XLIFF document can
    /// never pull in an external entity or a billion-laughs expansion.
    /// </summary>
    private static XmlReaderSettings CreateReaderSettings()
    {
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            Async = true
        };
    }

    /// <summary>
    /// Builds the whole <see cref="XliffDocument"/> model from a loaded <see cref="XDocument"/>,
    /// enforcing XLIFF 2.1 §4.2.2.1's rule that only <c>&lt;file&gt;</c> elements sit directly under
    /// <c>&lt;xliff&gt;</c> and that file ids are unique in the document.
    /// </summary>
    /// <param name="document">The loaded document.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="XliffFormatException">If the document does not conform to what the reader accepts.</exception>
    private static XliffDocument Parse(XDocument document)
    {
        XElement? root = document.Root;
        if(root is null || root.Name != Core + WellKnownXliffElements.Xliff)
        {
            throw new XliffFormatException("The document root is not an <xliff> element in the XLIFF 2.x core namespace.");
        }

        XliffVersion version = ParseVersion(root.Attribute(WellKnownXliffAttributes.Version)?.Value);
        string sourceLanguage = RequireSourceLanguage(root.Attribute(WellKnownXliffAttributes.SourceLanguage)?.Value);
        string? targetLanguage = RequireWellFormedTargetLanguage(root.Attribute(WellKnownXliffAttributes.TargetLanguage)?.Value);

        var files = ImmutableArray.CreateBuilder<XliffFile>();
        var fileIds = new HashSet<string>(StringComparer.Ordinal);
        foreach(XElement child in root.Elements())
        {
            if(!WellKnownXliffNamespaces.IsCore(child.Name.NamespaceName))
            {
                continue;
            }

            if(!WellKnownXliffElements.IsFile(child.Name.LocalName))
            {
                throw new XliffFormatException(
                    $"The <xliff> element contains a <{child.Name.LocalName}> element; XLIFF 2.1 §4.2.2.1 allows one or more <file> elements there and nothing else.");
            }

            XliffFile file = ParseFile(child, sourceLanguage, targetLanguage);
            if(!fileIds.Add(file.Id))
            {
                throw new XliffFormatException($"Duplicate file id '{file.Id}'. File ids must be unique within the document.");
            }

            files.Add(file);
        }

        return new XliffDocument(version, files.ToImmutable());
    }

    /// <summary>
    /// Validates the required srcLang attribute against the shape BCP 47 requires. XLIFF 2.1
    /// §4.3.1.29 srcLang requires "A language code as described in [BCP 47]" and marks the attribute
    /// required on <c>&lt;xliff&gt;</c>.
    /// </summary>
    /// <param name="value">The srcLang attribute's raw value.</param>
    /// <returns>The validated source language.</returns>
    /// <exception cref="XliffFormatException">If <paramref name="value"/> is missing or not a well-formed BCP 47 tag.</exception>
    private static string RequireSourceLanguage(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            throw new XliffFormatException("The <xliff> element does not declare the required srcLang attribute.");
        }

        if(!LanguageTag.IsWellFormed(value))
        {
            throw new XliffFormatException($"The <xliff> element's srcLang attribute '{value}' is not a well-formed BCP 47 language tag.");
        }

        return value;
    }

    /// <summary>
    /// Validates an optional trgLang attribute against the shape BCP 47 requires; XLIFF 2.1 §4.3.1.37
    /// trgLang describes an empty or malformed value as unacceptable by requiring "A language code as
    /// described in [BCP 47]" whenever the attribute is present at all.
    /// </summary>
    private static string? RequireWellFormedTargetLanguage(string? value)
    {
        if(value is null)
        {
            return null;
        }

        if(!LanguageTag.IsWellFormed(value))
        {
            throw new XliffFormatException($"The <xliff> element's trgLang attribute '{value}' is not a well-formed BCP 47 language tag.");
        }

        return value;
    }

    /// <summary>
    /// Parses the required version attribute, accepting only the "2.0" and "2.1" values the reader
    /// understands.
    /// </summary>
    /// <param name="value">The version attribute's raw value.</param>
    /// <returns>The parsed version.</returns>
    /// <exception cref="XliffFormatException">If <paramref name="value"/> is missing or not "2.0" or "2.1".</exception>
    private static XliffVersion ParseVersion(string? value)
    {
        return value switch
        {
            null => throw new XliffFormatException("The <xliff> element does not declare the required version attribute."),
            _ when WellKnownXliffAttributeValues.IsVersion20(value) => XliffVersion.V20,
            _ when WellKnownXliffAttributeValues.IsVersion21(value) => XliffVersion.V21,
            _ => throw new XliffFormatException($"Unsupported XLIFF version '{value}'. The reader understands 2.0 and 2.1.")
        };
    }

    /// <summary>
    /// The deepest a chain of nested &lt;group&gt; elements may go. XLIFF 2.1 §4.2.2.4 group places no
    /// limit on nesting, but the reader's recursive descent shares the call stack with the rest of the
    /// process, so an unbounded document could exhaust it; this is an implementation safety bound, not
    /// a spec requirement.
    /// </summary>
    private const int MaxGroupDepth = 64;

    /// <summary>
    /// Parses one <c>&lt;file&gt;</c> element into its <see cref="XliffFile"/> model: id, metadata,
    /// validation rules, groups and units. XLIFF 2.1 §4.2.2.2 file requires at least one
    /// <c>&lt;unit&gt;</c> or <c>&lt;group&gt;</c> child, which the reader enforces after collecting both.
    /// </summary>
    /// <param name="fileElement">The <c>&lt;file&gt;</c> element.</param>
    /// <param name="sourceLanguage">The document's source language.</param>
    /// <param name="targetLanguage">The document's target language, or null when it declares none.</param>
    /// <returns>The parsed file.</returns>
    /// <exception cref="XliffFormatException">If the file element does not conform to what the reader accepts.</exception>
    private static XliffFile ParseFile(XElement fileElement, string sourceLanguage, string? targetLanguage)
    {
        string id = RequiredId(fileElement, WellKnownXliffElements.File);
        ParsedMetadata metadata = ParseMetadata(fileElement, WellKnownXliffElements.File, allowTone: true, allowGlossary: true, allowScopesAndMetadata: false);
        ValidationRuleSet? validationRules = ParseValidation(fileElement, WellKnownXliffElements.File);

        var groups = ImmutableArray.CreateBuilder<XliffGroup>();
        foreach(XElement groupElement in fileElement.Elements(Core + WellKnownXliffElements.Group))
        {
            groups.Add(ParseGroup(groupElement, depth: 1));
        }

        var units = ImmutableArray.CreateBuilder<XliffUnit>();
        foreach(XElement unitElement in fileElement.Elements(Core + WellKnownXliffElements.Unit))
        {
            units.Add(ParseUnit(unitElement));
        }

        if(groups.Count == 0 && units.Count == 0)
        {
            throw new XliffFormatException($"File '{id}' has no <unit> or <group> element; XLIFF 2.1 §4.2.2.2 requires at least one.");
        }

        var file = new XliffFile(
            id,
            new LanguageTag(sourceLanguage),
            targetLanguage is null ? null : new LanguageTag(targetLanguage),
            metadata.ToneProfile,
            metadata.Glossary,
            validationRules,
            groups.ToImmutable(),
            units.ToImmutable());

        ValidateFile(file);

        return file;
    }

    /// <summary>
    /// Parses one <c>&lt;group&gt;</c> element and its nested groups and units recursively, refusing
    /// a Validation module on the group (validation only attaches to a <c>&lt;file&gt;</c>) and depth
    /// beyond <see cref="MaxGroupDepth"/>.
    /// </summary>
    /// <param name="groupElement">The <c>&lt;group&gt;</c> element.</param>
    /// <param name="depth">The nesting depth of <paramref name="groupElement"/>, starting at 1 for a group directly under a file.</param>
    /// <returns>The parsed group.</returns>
    /// <exception cref="XliffFormatException">If the group element does not conform to what the reader accepts, or nests too deep.</exception>
    private static XliffGroup ParseGroup(XElement groupElement, int depth)
    {
        if(depth > MaxGroupDepth)
        {
            throw new XliffFormatException($"A <group> nests more than {MaxGroupDepth} levels deep, which the reader does not support.");
        }

        string id = RequiredId(groupElement, WellKnownXliffElements.Group);
        ParsedMetadata metadata = ParseMetadata(groupElement, WellKnownXliffElements.Group, allowTone: false, allowGlossary: false, allowScopesAndMetadata: true);
        RejectValidation(groupElement, WellKnownXliffElements.Group);

        var groups = ImmutableArray.CreateBuilder<XliffGroup>();
        foreach(XElement nested in groupElement.Elements(Core + WellKnownXliffElements.Group))
        {
            groups.Add(ParseGroup(nested, depth + 1));
        }

        var units = ImmutableArray.CreateBuilder<XliffUnit>();
        foreach(XElement unitElement in groupElement.Elements(Core + WellKnownXliffElements.Unit))
        {
            units.Add(ParseUnit(unitElement));
        }

        return new XliffGroup(id, metadata.Scopes, metadata.Metadata, units.ToImmutable(), groups.ToImmutable());
    }

    /// <summary>
    /// Parses one <c>&lt;unit&gt;</c> element into its <see cref="XliffUnit"/> model: id, metadata,
    /// glossary, segments, ignorables and notes. XLIFF 2.1 §4.2.2.5 unit requires at least one
    /// <c>&lt;segment&gt;</c> child, which the reader enforces after collecting the unit's children.
    /// </summary>
    /// <param name="unitElement">The <c>&lt;unit&gt;</c> element.</param>
    /// <returns>The parsed unit.</returns>
    /// <exception cref="XliffFormatException">If the unit element does not conform to what the reader accepts.</exception>
    private static XliffUnit ParseUnit(XElement unitElement)
    {
        string id = RequiredId(unitElement, WellKnownXliffElements.Unit);
        ParsedMetadata metadata = ParseMetadata(unitElement, WellKnownXliffElements.Unit, allowTone: false, allowGlossary: false, allowScopesAndMetadata: true);
        RejectValidation(unitElement, WellKnownXliffElements.Unit);
        Glossary? glossary = ParseGlossaryModule(unitElement, id);

        var segments = ImmutableArray.CreateBuilder<XliffSegment>();
        var segmentIds = new HashSet<string>(StringComparer.Ordinal);
        bool anyTranslatable = false;
        foreach(XElement child in unitElement.Elements())
        {
            bool coreChild = WellKnownXliffNamespaces.IsCore(child.Name.NamespaceName);
            if(coreChild && WellKnownXliffElements.IsSegment(child.Name.LocalName))
            {
                XliffSegment segment = ParseSegment(child, id, SegmentKind.Translatable);
                RegisterSegmentId(segment.Id, id, segmentIds);
                segments.Add(segment);
                anyTranslatable = true;

                continue;
            }

            if(coreChild && WellKnownXliffElements.IsIgnorable(child.Name.LocalName))
            {
                XliffSegment segment = ParseSegment(child, id, SegmentKind.Ignorable);
                RegisterSegmentId(segment.Id, id, segmentIds);
                segments.Add(segment);
            }
        }

        if(!anyTranslatable)
        {
            throw new XliffFormatException($"Unit '{id}' has no <segment> element; XLIFF 2.1 §4.2.2.5 requires a <unit> to contain at least one.");
        }

        var notes = ImmutableArray.CreateBuilder<string>();
        XElement? notesElement = unitElement.Element(Core + WellKnownXliffElements.Notes);
        if(notesElement is not null)
        {
            foreach(XElement note in notesElement.Elements(Core + WellKnownXliffElements.Note))
            {
                notes.Add(note.Value);
            }
        }

        return new XliffUnit(id, segments.ToImmutable(), notes.ToImmutable(), metadata.Scopes, metadata.Metadata, glossary);
    }

    /// <summary>
    /// Records a segment or ignorable id against the unit's set of them, throwing when the same id
    /// appears twice. XLIFF 2.1 §4.3.1.21 id: a value used on &lt;segment&gt; or &lt;ignorable&gt;
    /// "MUST be unique among all of the above ... within the enclosing &lt;unit&gt; element".
    /// </summary>
    private static void RegisterSegmentId(string? segmentId, string unitId, HashSet<string> seen)
    {
        if(segmentId is null)
        {
            return;
        }

        if(!seen.Add(segmentId))
        {
            throw new XliffFormatException($"Duplicate segment id '{segmentId}' in unit '{unitId}'. Segment and ignorable ids must be unique within their unit.");
        }
    }

    /// <summary>
    /// Parses one <c>&lt;segment&gt;</c> or <c>&lt;ignorable&gt;</c> element: its required <c>&lt;source&gt;</c>,
    /// optional <c>&lt;target&gt;</c>, optional id and, for a translatable segment, its state. An ignorable
    /// carries no state of its own, per XLIFF 2.1's ignorable model, so it is always parsed as
    /// <see cref="SegmentState.Initial"/>.
    /// </summary>
    /// <param name="element">The <c>&lt;segment&gt;</c> or <c>&lt;ignorable&gt;</c> element.</param>
    /// <param name="unitId">The enclosing unit's id, for error messages.</param>
    /// <param name="kind">Whether <paramref name="element"/> is a translatable segment or an ignorable one.</param>
    /// <returns>The parsed segment.</returns>
    /// <exception cref="XliffFormatException">If the element does not conform to what the reader accepts.</exception>
    private static XliffSegment ParseSegment(XElement element, string unitId, SegmentKind kind)
    {
        XElement? sourceElement = element.Element(Core + WellKnownXliffElements.Source);
        if(sourceElement is null)
        {
            throw new XliffFormatException($"A <{element.Name.LocalName}> in unit '{unitId}' has no <source> element.");
        }

        InlineContent sourceContent = InlineContent.FromText(ReadContent(sourceElement));
        XElement? targetElement = element.Element(Core + WellKnownXliffElements.Target);
        InlineContent? targetContent = targetElement is null ? null : InlineContent.FromText(ReadContent(targetElement));
        string? id = OptionalId(element, element.Name.LocalName);

        if(kind == SegmentKind.Ignorable)
        {
            return new XliffSegment(id, kind, sourceContent, targetContent, SegmentState.Initial, null);
        }

        SegmentState state = ParseState(element.Attribute(WellKnownXliffAttributes.State)?.Value, unitId);
        string? subState = element.Attribute(WellKnownXliffAttributes.SubState)?.Value;
        if(state == SegmentState.Initial && WellKnownVericulaMetadata.IsNeedsTranslationSubState(subState))
        {
            return new XliffSegment(id, kind, sourceContent, targetContent, SegmentState.NeedsTranslation, null);
        }

        return new XliffSegment(id, kind, sourceContent, targetContent, state, subState);
    }

    /// <summary>
    /// Parses a segment's <c>state</c> attribute, defaulting to <see cref="SegmentState.Initial"/> when
    /// absent per XLIFF 2.1 §4.3.1.31 state's default value, and refusing any value the four standard
    /// states do not name.
    /// </summary>
    /// <param name="value">The state attribute's raw value, or null when absent.</param>
    /// <param name="unitId">The enclosing unit's id, for the error message.</param>
    /// <returns>The parsed state.</returns>
    /// <exception cref="XliffFormatException">If <paramref name="value"/> is not a recognized state.</exception>
    private static SegmentState ParseState(string? value, string unitId)
    {
        return value switch
        {
            null => SegmentState.Initial,
            _ when WellKnownXliffAttributeValues.IsStateInitial(value) => SegmentState.Initial,
            _ when WellKnownXliffAttributeValues.IsStateTranslated(value) => SegmentState.Translated,
            _ when WellKnownXliffAttributeValues.IsStateReviewed(value) => SegmentState.Reviewed,
            _ when WellKnownXliffAttributeValues.IsStateFinal(value) => SegmentState.Final,
            _ => throw new XliffFormatException($"Unit '{unitId}' has a segment with the unknown state '{value}'.")
        };
    }

    /// <summary>
    /// Reads the text of a source or target element, refusing every inline code and annotation marker:
    /// <c>cp</c>, <c>ph</c>, <c>sc</c> and <c>ec</c> carry no text at all, and <c>pc</c>, <c>mrk</c>,
    /// <c>sm</c> and <c>em</c> would flatten to their wrapped text while silently dropping the id,
    /// dataRef links, translate flag and other attributes the model has no slot for (XLIFF 2.1 §4.7
    /// Inline Elements and §4.7.2 Annotations).
    /// </summary>
    private static string ReadContent(XElement element)
    {
        foreach(XElement descendant in element.Descendants())
        {
            if(WellKnownXliffElements.IsUnsupportedInlineMarkup(descendant.Name.LocalName))
            {
                throw new XliffFormatException(
                    $"Inline markup <{descendant.Name.LocalName}> in <{element.Name.LocalName}> is not yet supported; the reader reads plain text only.");
            }
        }

        return element.Value;
    }

    /// <summary>
    /// Reads the Metadata module on a file, group or unit into the model parts it carries. Each
    /// Vericula category belongs on one kind of element; a category on the wrong element is refused,
    /// and categories the reader does not know are skipped.
    /// </summary>
    private static ParsedMetadata ParseMetadata(XElement container, string elementName, bool allowTone, bool allowGlossary, bool allowScopesAndMetadata)
    {
        XElement? metadataElement = container.Element(Metadata + WellKnownXliffElements.Metadata);
        if(metadataElement is null)
        {
            return ParsedMetadata.Empty;
        }

        ToneProfile? tone = null;
        Glossary? glossary = null;
        var scopes = ImmutableArray.CreateBuilder<Scope>();
        ImmutableDictionary<string, string>.Builder metadata = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

        foreach(XElement group in metadataElement.Elements(Metadata + WellKnownXliffElements.MetaGroup))
        {
            string? category = group.Attribute(WellKnownXliffAttributes.Category)?.Value;
            MetadataKind kind = category switch
            {
                null => MetadataKind.Foreign,
                _ when WellKnownVericulaMetadata.IsToneCategory(category) => MetadataKind.Tone,
                _ when WellKnownVericulaMetadata.IsGlossaryCategory(category) => MetadataKind.Glossary,
                _ when WellKnownVericulaMetadata.IsScopesCategory(category) => MetadataKind.Scopes,
                _ when WellKnownVericulaMetadata.IsMetadataCategory(category) => MetadataKind.Metadata,
                _ => MetadataKind.Foreign
            };

            bool allowed = kind switch
            {
                MetadataKind.Tone => allowTone,
                MetadataKind.Glossary => allowGlossary,
                MetadataKind.Scopes or MetadataKind.Metadata => allowScopesAndMetadata,
                _ => false
            };

            if(kind == MetadataKind.Foreign)
            {
                continue;
            }

            if(!allowed)
            {
                throw new XliffFormatException($"A <{elementName}> element carries a metadata group in the category '{category}', which does not belong on a <{elementName}>.");
            }

            if(kind == MetadataKind.Tone)
            {
                tone = ParseTone(group);

                continue;
            }

            if(kind == MetadataKind.Glossary)
            {
                glossary = ParseGlossaryMetadata(group);

                continue;
            }

            if(kind == MetadataKind.Scopes)
            {
                foreach(XElement meta in Metas(group, WellKnownVericulaMetadata.ScopeType))
                {
                    scopes.Add(new Scope(meta.Value));
                }

                continue;
            }

            foreach(XElement meta in group.Elements(Metadata + WellKnownXliffElements.Meta))
            {
                metadata[RequiredType(meta)] = meta.Value;
            }
        }

        return new ParsedMetadata(tone, glossary, scopes.ToImmutable(), metadata.ToImmutable());
    }

    /// <summary>
    /// Parses a Vericula tone metadata group into a <see cref="ToneProfile"/>: its single-valued
    /// fields, and any nested tone metadata group's entries folded into its free-form metadata.
    /// </summary>
    /// <param name="group">The tone metadata group.</param>
    /// <returns>The parsed tone profile.</returns>
    /// <exception cref="XliffFormatException">If the group has no version, or a single-valued field has an invalid enum value.</exception>
    private static ToneProfile ParseTone(XElement group)
    {
        string? version = SingleMeta(group, WellKnownVericulaMetadata.VersionType);
        if(version is null)
        {
            throw new XliffFormatException("A tone profile metadata group has no version.");
        }

        ImmutableDictionary<string, string>.Builder metadata = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach(XElement nested in group.Elements(Metadata + WellKnownXliffElements.MetaGroup))
        {
            if(!WellKnownVericulaMetadata.IsToneMetadataCategory(nested.Attribute(WellKnownXliffAttributes.Category)?.Value))
            {
                continue;
            }

            foreach(XElement meta in nested.Elements(Metadata + WellKnownXliffElements.Meta))
            {
                metadata[RequiredType(meta)] = meta.Value;
            }
        }

        return new ToneProfile(
            version,
            SingleMeta(group, WellKnownVericulaMetadata.AuthorityType),
            ParseEnum<ToneRegister>(SingleMeta(group, WellKnownVericulaMetadata.RegisterType), WellKnownVericulaMetadata.RegisterType),
            SingleMeta(group, WellKnownVericulaMetadata.VoiceType),
            ParseEnum<TextOrientation>(SingleMeta(group, WellKnownVericulaMetadata.OrientationType), WellKnownVericulaMetadata.OrientationType),
            SingleMeta(group, WellKnownVericulaMetadata.CalendarType),
            SingleMeta(group, WellKnownVericulaMetadata.DateFormatType),
            ParseEnum<DigitStyle>(SingleMeta(group, WellKnownVericulaMetadata.DigitStyleType), WellKnownVericulaMetadata.DigitStyleType),
            metadata.ToImmutable());
    }

    /// <summary>
    /// Parses a Vericula file-wide glossary metadata group into a <see cref="Glossary"/>, reading each
    /// nested glossary-entry metadata group's term, translation, definition, status, scopes and
    /// rationale.
    /// </summary>
    /// <param name="group">The glossary metadata group.</param>
    /// <returns>The parsed glossary.</returns>
    /// <exception cref="XliffFormatException">If an entry lacks its term or translation, or a field has an invalid enum value.</exception>
    private static Glossary ParseGlossaryMetadata(XElement group)
    {
        var entries = ImmutableArray.CreateBuilder<GlossaryEntry>();
        foreach(XElement entry in group.Elements(Metadata + WellKnownXliffElements.MetaGroup))
        {
            if(!WellKnownVericulaMetadata.IsGlossaryEntryCategory(entry.Attribute(WellKnownXliffAttributes.Category)?.Value))
            {
                continue;
            }

            string? term = SingleMeta(entry, WellKnownVericulaMetadata.TermType);
            string? translation = SingleMeta(entry, WellKnownVericulaMetadata.TranslationType);
            if(term is null || translation is null)
            {
                throw new XliffFormatException("A file-wide glossary entry lacks its term or translation.");
            }

            var scopes = ImmutableArray.CreateBuilder<Scope>();
            foreach(XElement meta in Metas(entry, WellKnownVericulaMetadata.ScopeType))
            {
                scopes.Add(new Scope(meta.Value));
            }

            entries.Add(new GlossaryEntry(
                term,
                translation,
                SingleMeta(entry, WellKnownVericulaMetadata.DefinitionType),
                ParseEnum<GlossaryEntryStatus>(SingleMeta(entry, WellKnownVericulaMetadata.StatusType), WellKnownVericulaMetadata.StatusType),
                scopes.ToImmutable(),
                SingleMeta(entry, WellKnownVericulaMetadata.RationaleType)));
        }

        return new Glossary(entries.ToImmutable());
    }

    /// <summary>
    /// Reads the Glossary module on a unit. Each translation of an entry becomes one
    /// <see cref="GlossaryEntry"/>; a translation without a Vericula status is preferred when it is
    /// the entry's first and allowed otherwise.
    /// </summary>
    private static Glossary? ParseGlossaryModule(XElement unitElement, string unitId)
    {
        XElement? glossaryElement = unitElement.Element(Glossary + WellKnownXliffElements.Glossary);
        if(glossaryElement is null)
        {
            return null;
        }

        var entries = ImmutableArray.CreateBuilder<GlossaryEntry>();
        foreach(XElement entry in glossaryElement.Elements(Glossary + WellKnownXliffElements.GlossEntry))
        {
            string? term = entry.Element(Glossary + WellKnownXliffElements.Term)?.Value;
            if(term is null)
            {
                throw new XliffFormatException($"A glossary entry in unit '{unitId}' has no <term>.");
            }

            string? definition = entry.Element(Glossary + WellKnownXliffElements.Definition)?.Value;
            string? rationale = entry.Attribute(Vericula + WellKnownVericulaMetadata.RationaleAttribute)?.Value;
            ImmutableArray<Scope> scopes = ParseScopesAttribute(entry.Attribute(Vericula + WellKnownVericulaMetadata.ScopesAttribute)?.Value);

            bool first = true;
            foreach(XElement translation in entry.Elements(Glossary + WellKnownXliffElements.Translation))
            {
                string? statusText = translation.Attribute(Vericula + WellKnownVericulaMetadata.StatusAttribute)?.Value;
                GlossaryEntryStatus status = statusText switch
                {
                    null when first => GlossaryEntryStatus.Preferred,
                    null => GlossaryEntryStatus.Allowed,
                    _ => ParseEnum<GlossaryEntryStatus>(statusText, WellKnownVericulaMetadata.StatusAttribute)
                };
                entries.Add(new GlossaryEntry(term, translation.Value, definition, status, scopes, rationale));
                first = false;
            }

            if(first)
            {
                throw new XliffFormatException($"The glossary entry for '{term}' in unit '{unitId}' has no <translation>; the model needs one.");
            }
        }

        return new Glossary(entries.ToImmutable());
    }

    /// <summary>
    /// Parses a Vericula glossary entry's space-separated scopes attribute into individual scopes.
    /// </summary>
    /// <param name="value">The scopes attribute's raw value, or null or blank when absent.</param>
    /// <returns>The parsed scopes, empty when <paramref name="value"/> is null, blank or has no tokens.</returns>
    private static ImmutableArray<Scope> ParseScopesAttribute(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            return ImmutableArray<Scope>.Empty;
        }

        var scopes = ImmutableArray.CreateBuilder<Scope>();
        foreach(string scope in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            scopes.Add(new Scope(scope));
        }

        return scopes.ToImmutable();
    }

    /// <summary>
    /// Reads the Validation module on a file into a <see cref="ValidationRuleSet"/>, refusing any rule
    /// whose semantics the linter cannot honour before parsing it.
    /// </summary>
    /// <param name="fileElement">The <c>&lt;file&gt;</c> element.</param>
    /// <param name="elementName">The element's local name, for error messages.</param>
    /// <returns>The parsed rule set, or null when the file carries no Validation module.</returns>
    /// <exception cref="XliffFormatException">If a rule does not conform to what the reader accepts.</exception>
    private static ValidationRuleSet? ParseValidation(XElement fileElement, string elementName)
    {
        XElement? validationElement = fileElement.Element(Validation + WellKnownXliffElements.Validation);
        if(validationElement is null)
        {
            return null;
        }

        var rules = ImmutableArray.CreateBuilder<ValidationRule>();
        foreach(XElement ruleElement in validationElement.Elements(Validation + WellKnownXliffElements.Rule))
        {
            RejectUnsupportedRuleSemantics(ruleElement, elementName);
            ValidationRule rule = ParseRule(ruleElement, elementName) with
            {
                Disabled = WellKnownXliffAttributeValues.IsYes(ruleElement.Attribute(WellKnownXliffAttributes.Disabled)?.Value),
                Normalization = ParseNormalization(ruleElement, elementName)
            };
            rules.Add(rule);
        }

        return new ValidationRuleSet(rules.ToImmutable());
    }

    /// <summary>
    /// Refuses rule semantics the linter cannot honour: case-insensitive matching, a rule conditioned
    /// on the source text, and an explicit occurrence count. The <c>disabled</c> and
    /// <c>normalization</c> attributes are read rather than refused; see <see cref="ParseValidation"/>
    /// and <see cref="ParseNormalization"/>.
    /// </summary>
    private static void RejectUnsupportedRuleSemantics(XElement ruleElement, string elementName)
    {
        if(WellKnownXliffAttributeValues.IsNo(ruleElement.Attribute(WellKnownXliffAttributes.CaseSensitive)?.Value))
        {
            throw new XliffFormatException($"A validation rule on a <{elementName}> asks for case-insensitive matching, which is not supported.");
        }

        if(WellKnownXliffAttributeValues.IsYes(ruleElement.Attribute(WellKnownXliffAttributes.ExistsInSource)?.Value))
        {
            throw new XliffFormatException($"A validation rule on a <{elementName}> is conditioned on the source text, which is not supported.");
        }

        if(ruleElement.Attribute(WellKnownXliffAttributes.Occurs) is not null)
        {
            throw new XliffFormatException($"A validation rule on a <{elementName}> constrains the number of occurrences, which is not supported.");
        }
    }

    /// <summary>
    /// Parses a rule's <c>normalization</c> attribute per XLIFF 2.1 §5.8.5.8 normalization. An absent
    /// attribute and the explicit values <c>none</c> and <c>nfc</c> are the forms the linter applies;
    /// <c>nfd</c>, though a legal spec value, is refused by <see cref="WellKnownXliffAttributeValues.IsNormalizationNfd"/>,
    /// as is any other value.
    /// </summary>
    private static TextNormalization ParseNormalization(XElement ruleElement, string elementName)
    {
        string? normalization = ruleElement.Attribute(WellKnownXliffAttributes.Normalization)?.Value;

        return normalization switch
        {
            null => TextNormalization.Nfc,
            _ when WellKnownXliffAttributeValues.IsNormalizationNfc(normalization) => TextNormalization.Nfc,
            _ when WellKnownXliffAttributeValues.IsNormalizationNone(normalization) => TextNormalization.None,
            _ when WellKnownXliffAttributeValues.IsNormalizationNfd(normalization) => throw new XliffFormatException($"A validation rule on a <{elementName}> asks for '{normalization}' normalization, which is not supported."),
            _ => throw new XliffFormatException($"A validation rule on a <{elementName}> asks for '{normalization}' normalization, which is not supported.")
        };
    }

    /// <summary>
    /// Parses one rule element, which carries exactly one rule attribute: one of the standard four, or
    /// one of Vericula's custom length and pattern rules in their own namespace.
    /// </summary>
    private static ValidationRule ParseRule(XElement ruleElement, string elementName)
    {
        List<ValidationRule> candidates = [];
        if(ruleElement.Attribute(WellKnownXliffAttributes.IsPresent)?.Value is { } present)
        {
            RequireNonEmptyRuleText(present, elementName);
            candidates.Add(new PresenceRule(present));
        }

        if(ruleElement.Attribute(WellKnownXliffAttributes.IsNotPresent)?.Value is { } absent)
        {
            RequireNonEmptyRuleText(absent, elementName);
            candidates.Add(new AbsenceRule(absent));
        }

        if(ruleElement.Attribute(WellKnownXliffAttributes.StartsWith)?.Value is { } prefix)
        {
            RequireNonEmptyRuleText(prefix, elementName);
            candidates.Add(new StartsWithRule(prefix));
        }

        if(ruleElement.Attribute(WellKnownXliffAttributes.EndsWith)?.Value is { } suffix)
        {
            RequireNonEmptyRuleText(suffix, elementName);
            candidates.Add(new EndsWithRule(suffix));
        }

        if(ruleElement.Attribute(Vericula + WellKnownVericulaMetadata.MaxLengthAttribute)?.Value is { } maximumLength)
        {
            if(!int.TryParse(maximumLength, NumberStyles.None, CultureInfo.InvariantCulture, out int maximum))
            {
                throw new XliffFormatException($"A validation rule on a <{elementName}> has the non-numeric length budget '{maximumLength}'.");
            }

            candidates.Add(new LengthBudgetRule(maximum));
        }

        if(ruleElement.Attribute(Vericula + WellKnownVericulaMetadata.RegexAttribute)?.Value is { } pattern)
        {
            candidates.Add(new RegexRule(pattern));
        }

        if(candidates.Count == 0 && ForeignRuleAttribute(ruleElement) is { } foreignAttribute)
        {
            throw new XliffFormatException($"A validation rule on a <{elementName}> carries a custom rule attribute '{foreignAttribute.Name.LocalName}' from the namespace '{foreignAttribute.Name.NamespaceName}', which is not supported.");
        }

        if(candidates.Count != 1)
        {
            throw new XliffFormatException($"A validation rule on a <{elementName}> must carry exactly one rule attribute; this one carries {candidates.Count}.");
        }

        return candidates[0];
    }

    /// <summary>
    /// Finds an attribute on a rule element from a namespace that is neither empty (XLIFF's own
    /// unprefixed rule attributes), nor <see cref="Vericula"/>, nor the reserved <c>xml</c> or
    /// <c>xmlns</c> namespaces. XLIFF 2.1 §5.8.4.3 <c>&lt;rule&gt;</c>'s Constraints allow "a custom
    /// rule defined by attributes from any namespace" as the fifth alternative to the four standard
    /// rule attributes; the reader does not know how to evaluate such a rule, so
    /// <see cref="ParseRule"/> refuses it by name instead of reporting it as carrying zero attributes.
    /// </summary>
    private static XAttribute? ForeignRuleAttribute(XElement ruleElement)
    {
        foreach(XAttribute attribute in ruleElement.Attributes())
        {
            XNamespace ns = attribute.Name.Namespace;
            if(ns != XNamespace.None && ns != Vericula && ns != XNamespace.Xmlns && ns != XNamespace.Xml)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>
    /// Refuses an empty value for a rule attribute whose Value description in XLIFF 2.1 §5.8.5.1
    /// isPresent, §5.8.5.3 isNotPresent, §5.8.5.4 startsWith and §5.8.5.5 endsWith is simply "Text":
    /// an empty isNotPresent value, for instance, would flag every target, since the empty string is
    /// present in every string.
    /// </summary>
    private static void RequireNonEmptyRuleText(string value, string elementName)
    {
        if(value.Length == 0)
        {
            throw new XliffFormatException($"A validation rule on a <{elementName}> has an empty value; a rule text must not be empty.");
        }
    }

    /// <summary>
    /// Refuses a Validation module on a group or unit element; the reader only reads validation rules
    /// off a <c>&lt;file&gt;</c>, matching <see cref="Inspect(XmlReader, StreamState)"/>'s check in the
    /// streaming walk.
    /// </summary>
    /// <param name="element">The group or unit element to check.</param>
    /// <param name="elementName">The element's local name, for the error message.</param>
    /// <exception cref="XliffFormatException">If <paramref name="element"/> carries a <c>&lt;validation&gt;</c> child.</exception>
    private static void RejectValidation(XElement element, string elementName)
    {
        if(element.Element(Validation + WellKnownXliffElements.Validation) is not null)
        {
            throw new XliffFormatException($"Validation rules on a <{elementName}> are not supported; attach them to the <file>.");
        }
    }

    /// <summary>
    /// Reads and validates a required id attribute. XLIFF 2.1 §4.3.1.21 id gives the value description
    /// as NMTOKEN, so the reader refuses one that is missing, blank, or shaped otherwise, rather than
    /// accepting an id the writer would then refuse to serialize. The thrown exception carries
    /// <paramref name="element"/>'s line and position when the document was loaded with line info.
    /// </summary>
    private static string RequiredId(XElement element, string elementName)
    {
        try
        {
            return RequireId(element.Attribute(WellKnownXliffAttributes.Id)?.Value, elementName);
        }
        catch(XliffFormatException exception)
        {
            throw WithLocation(exception, element);
        }
    }

    /// <summary>
    /// Reads and validates an optional id attribute, returning null when the attribute is absent, or
    /// the NMTOKEN-checked value when it is present. See <see cref="RequiredId(XElement, string)"/>.
    /// </summary>
    private static string? OptionalId(XElement element, string elementName)
    {
        string? id = element.Attribute(WellKnownXliffAttributes.Id)?.Value;

        return id is null ? null : RequireNameToken(id, elementName);
    }

    /// <summary>
    /// The streaming walk's equivalent of <see cref="RequiredId(XElement, string)"/>: reads and
    /// validates a required id attribute without needing the enclosing element for line info.
    /// </summary>
    /// <param name="id">The id attribute's raw value, or null when absent.</param>
    /// <param name="elementName">The element's local name, for the error message.</param>
    /// <returns>The NMTOKEN-checked id.</returns>
    /// <exception cref="XliffFormatException">If <paramref name="id"/> is missing, blank or not an XML name token.</exception>
    private static string RequireId(string? id, string elementName)
    {
        if(string.IsNullOrWhiteSpace(id))
        {
            throw new XliffFormatException($"A <{elementName}> element does not declare the required id attribute.");
        }

        return RequireNameToken(id, elementName);
    }

    /// <summary>
    /// Checks that <paramref name="id"/> is a well-formed XML NMTOKEN, per XLIFF 2.1 §4.3.1.21 id's
    /// value description, so the reader refuses an id the writer would later refuse to serialize.
    /// </summary>
    /// <param name="id">The id value to check.</param>
    /// <param name="elementName">The element's local name, for the error message.</param>
    /// <returns><paramref name="id"/>, unchanged.</returns>
    /// <exception cref="XliffFormatException">If <paramref name="id"/> is not a valid XML name token.</exception>
    private static string RequireNameToken(string id, string elementName)
    {
        try
        {
            XmlConvert.VerifyNMTOKEN(id);
        }
        catch(XmlException exception)
        {
            throw new XliffFormatException($"The <{elementName}> id '{id}' is not an XML name token.", exception);
        }

        return id;
    }

    /// <summary>
    /// Reads and validates a <c>&lt;meta&gt;</c> element's required type attribute, which the reader
    /// uses as the key when folding metadata into <see cref="ParsedMetadata.Metadata"/>.
    /// </summary>
    /// <param name="meta">The <c>&lt;meta&gt;</c> element.</param>
    /// <returns>The type attribute's value.</returns>
    /// <exception cref="XliffFormatException">If <paramref name="meta"/> does not declare a type attribute.</exception>
    private static string RequiredType(XElement meta)
    {
        string? type = meta.Attribute(WellKnownXliffAttributes.Type)?.Value;
        if(string.IsNullOrWhiteSpace(type))
        {
            throw new XliffFormatException("A metadata element does not declare the required type attribute.");
        }

        return type;
    }

    /// <summary>
    /// Yields the direct <c>&lt;meta&gt;</c> children of <paramref name="group"/> whose type attribute
    /// equals <paramref name="type"/>, in document order.
    /// </summary>
    /// <param name="group">The Metadata module group to search.</param>
    /// <param name="type">The type value to match.</param>
    /// <returns>The matching <c>&lt;meta&gt;</c> elements.</returns>
    private static IEnumerable<XElement> Metas(XElement group, string type)
    {
        foreach(XElement meta in group.Elements(Metadata + WellKnownXliffElements.Meta))
        {
            if(meta.Attribute(WellKnownXliffAttributes.Type)?.Value == type)
            {
                yield return meta;
            }
        }
    }

    /// <summary>
    /// Reads the value of the first <c>&lt;meta&gt;</c> child of <paramref name="group"/> whose type
    /// matches, for the single-valued fields <see cref="ParseTone"/> and <see cref="ParseGlossaryMetadata"/>
    /// read off a Vericula metadata group.
    /// </summary>
    /// <param name="group">The Metadata module group to search.</param>
    /// <param name="type">The type value to match.</param>
    /// <returns>The first matching meta's value, or null when there is none.</returns>
    private static string? SingleMeta(XElement group, string type)
    {
        foreach(XElement meta in Metas(group, type))
        {
            return meta.Value;
        }

        return null;
    }

    /// <summary>
    /// Parses a Vericula metadata value against a <see langword="enum"/> by exact-case member name,
    /// defaulting to the enum's zero value when the source value is absent and refusing any value that
    /// is not one of the enum's defined members.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to parse into.</typeparam>
    /// <param name="value">The raw value, or null when absent.</param>
    /// <param name="name">The field's name, for the error message.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref="XliffFormatException">If <paramref name="value"/> does not name a defined member of <typeparamref name="TEnum"/>.</exception>
    private static TEnum ParseEnum<TEnum>(string? value, string name) where TEnum : struct, Enum
    {
        if(value is null)
        {
            return default;
        }

        if(Enum.TryParse(value, ignoreCase: false, out TEnum parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new XliffFormatException($"The value '{value}' is not a valid {name}.");
    }

    /// <summary>
    /// Reports whether any segment of the unit carries a target, regardless of whether the unit's
    /// translation is complete. A partially translated unit still needs a declared trgLang, even
    /// though <see cref="XliffUnit.Target"/> folds to null until every translatable segment is done.
    /// </summary>
    private static bool HasAnyTarget(XliffUnit unit)
    {
        foreach(XliffSegment segment in unit.Segments)
        {
            if(segment.Target is not null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Enforces the per-file invariants the cook depends on: unique unit ids, and a declared target
    /// language whenever the file actually carries translations.
    /// </summary>
    private static void ValidateFile(XliffFile file)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        bool anyTarget = false;
        foreach(XliffUnit unit in Flatten(file))
        {
            if(!seen.Add(unit.Id))
            {
                throw new XliffFormatException(
                    $"Duplicate unit id '{unit.Id}' in file '{file.Id}'. Unit ids must be unique within a file.");
            }

            if(HasAnyTarget(unit))
            {
                anyTarget = true;
            }
        }

        if(anyTarget && file.TargetLanguage is null)
        {
            throw new XliffFormatException(
                "The document carries <target> content but declares no trgLang; trgLang is required when targets are present.");
        }
    }

    /// <summary>
    /// Yields every unit in <paramref name="file"/>, its own units first, then each group's units
    /// recursively, for <see cref="ValidateFile"/>'s whole-file invariant checks.
    /// </summary>
    /// <param name="file">The file to walk.</param>
    /// <returns>Every unit in the file, groups flattened.</returns>
    private static IEnumerable<XliffUnit> Flatten(XliffFile file)
    {
        foreach(XliffUnit unit in file.Units)
        {
            yield return unit;
        }

        foreach(XliffGroup group in file.Groups)
        {
            foreach(XliffUnit unit in Flatten(group))
            {
                yield return unit;
            }
        }
    }

    /// <summary>
    /// The recursive step of <see cref="Flatten(XliffFile)"/>: yields <paramref name="group"/>'s own
    /// units, then each nested group's units.
    /// </summary>
    /// <param name="group">The group to walk.</param>
    /// <returns>Every unit in the group and its nested groups.</returns>
    private static IEnumerable<XliffUnit> Flatten(XliffGroup group)
    {
        foreach(XliffUnit unit in group.Units)
        {
            yield return unit;
        }

        foreach(XliffGroup nested in group.Groups)
        {
            foreach(XliffUnit unit in Flatten(nested))
            {
                yield return unit;
            }
        }
    }

    /// <summary>
    /// What the streaming walk does with the reader's current node.
    /// </summary>
    private enum StreamStep
    {
        /// <summary>Move to the next node.</summary>
        Advance = 0,

        /// <summary>Materialize the unit element the reader is on and yield it.</summary>
        TakeUnit = 1
    }

    /// <summary>
    /// The kind of Vericula metadata group a category names.
    /// </summary>
    private enum MetadataKind
    {
        /// <summary>A category the reader does not know; skipped.</summary>
        Foreign = 0,

        /// <summary>A file's tone profile.</summary>
        Tone = 1,

        /// <summary>A file-wide glossary.</summary>
        Glossary = 2,

        /// <summary>A group's or unit's scopes.</summary>
        Scopes = 3,

        /// <summary>A group's or unit's named metadata.</summary>
        Metadata = 4
    }

    /// <summary>
    /// The immutable state the streaming walk carries.
    /// </summary>
    /// <param name="RootSeen">Whether the &lt;xliff&gt; root has been seen yet.</param>
    /// <param name="TargetLanguage">The document's target language, or null when it declares none.</param>
    /// <param name="InFile">Whether the walk is currently positioned inside a &lt;file&gt;.</param>
    /// <param name="OpenGroups">How many &lt;group&gt; elements are currently open.</param>
    /// <param name="FileIds">The file ids seen so far in the document.</param>
    /// <param name="Seen">The unit ids seen so far in the current file.</param>
    /// <param name="CurrentFileId">The id of the &lt;file&gt; currently open, or null when none is.</param>
    /// <param name="FileMembers">How many &lt;unit&gt; or &lt;group&gt; elements have been seen as direct children of the current file.</param>
    private sealed record StreamState(
        bool RootSeen,
        string? TargetLanguage,
        bool InFile,
        int OpenGroups,
        ImmutableHashSet<string> FileIds,
        ImmutableHashSet<string> Seen,
        string? CurrentFileId,
        int FileMembers)
    {
        /// <summary>The state before any node has been inspected.</summary>
        public static StreamState Start { get; } = new(
            false,
            null,
            false,
            0,
            ImmutableHashSet.Create<string>(StringComparer.Ordinal),
            ImmutableHashSet.Create<string>(StringComparer.Ordinal),
            null,
            0);
    }

    /// <summary>
    /// The model parts one element's Metadata module carried.
    /// </summary>
    /// <param name="ToneProfile">The file's tone profile, or null when the element carries none.</param>
    /// <param name="Glossary">The file-wide glossary, or null when the element carries none.</param>
    /// <param name="Scopes">The scopes read from the element's Metadata module.</param>
    /// <param name="Metadata">The named metadata read from the element's Metadata module.</param>
    private sealed record ParsedMetadata(
        ToneProfile? ToneProfile,
        Glossary? Glossary,
        ImmutableArray<Scope> Scopes,
        ImmutableDictionary<string, string> Metadata)
    {
        /// <summary>The value for an element without a Metadata module.</summary>
        public static ParsedMetadata Empty { get; } = new(null, null, ImmutableArray<Scope>.Empty, ImmutableDictionary<string, string>.Empty);
    }
}
