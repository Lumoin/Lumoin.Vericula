using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// The well-known XLIFF 2.x element NAMES the reader and writer exchange, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>:
/// the core structure, the Metadata, Validation and Glossary module elements, and the inline codes
/// and annotation markers the reader refuses.
/// </summary>
/// <remarks>
/// These are element names, not attribute names or values; attributes live in
/// <see cref="WellKnownXliffAttributes"/> and their values in <see cref="WellKnownXliffAttributeValues"/>.
/// Each name is spelled once as a UTF-8 source literal and carried alongside as an interned string.
/// </remarks>
public static class WellKnownXliffElements
{
    /// <summary>The UTF-8 source literal of <see cref="Xliff"/>.</summary>
    public static ReadOnlySpan<byte> XliffUtf8 => "xliff"u8;

    /// <summary>The root element per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#xliff">XLIFF 2.1, xliff</see>.</summary>
    public static readonly string Xliff = Utf8Constants.ToInternedString(XliffUtf8);

    /// <summary>The UTF-8 source literal of <see cref="File"/>.</summary>
    public static ReadOnlySpan<byte> FileUtf8 => "file"u8;

    /// <summary>A file, one original document's worth of units, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#file">XLIFF 2.1, file</see>.</summary>
    public static readonly string File = Utf8Constants.ToInternedString(FileUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Group"/>.</summary>
    public static ReadOnlySpan<byte> GroupUtf8 => "group"u8;

    /// <summary>A named grouping of units and groups per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#group">XLIFF 2.1, group</see>.</summary>
    public static readonly string Group = Utf8Constants.ToInternedString(GroupUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Unit"/>.</summary>
    public static ReadOnlySpan<byte> UnitUtf8 => "unit"u8;

    /// <summary>The smallest translatable unit per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#unit">XLIFF 2.1, unit</see>.</summary>
    public static readonly string Unit = Utf8Constants.ToInternedString(UnitUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Notes"/>.</summary>
    public static ReadOnlySpan<byte> NotesUtf8 => "notes"u8;

    /// <summary>The container of notes per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#notes">XLIFF 2.1, notes</see>.</summary>
    public static readonly string Notes = Utf8Constants.ToInternedString(NotesUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Note"/>.</summary>
    public static ReadOnlySpan<byte> NoteUtf8 => "note"u8;

    /// <summary>One free-text note per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#note">XLIFF 2.1, note</see>.</summary>
    public static readonly string Note = Utf8Constants.ToInternedString(NoteUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Segment"/>.</summary>
    public static ReadOnlySpan<byte> SegmentUtf8 => "segment"u8;

    /// <summary>A translatable segment per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#segment">XLIFF 2.1, segment</see>.</summary>
    public static readonly string Segment = Utf8Constants.ToInternedString(SegmentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Ignorable"/>.</summary>
    public static ReadOnlySpan<byte> IgnorableUtf8 => "ignorable"u8;

    /// <summary>Extracted content that is not translated per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#ignorable">XLIFF 2.1, ignorable</see>.</summary>
    public static readonly string Ignorable = Utf8Constants.ToInternedString(IgnorableUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Source"/>.</summary>
    public static ReadOnlySpan<byte> SourceUtf8 => "source"u8;

    /// <summary>The source content of a segment or ignorable per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#source">XLIFF 2.1, source</see>.</summary>
    public static readonly string Source = Utf8Constants.ToInternedString(SourceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Target"/>.</summary>
    public static ReadOnlySpan<byte> TargetUtf8 => "target"u8;

    /// <summary>The target content of a segment or ignorable per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#target">XLIFF 2.1, target</see>.</summary>
    public static readonly string Target = Utf8Constants.ToInternedString(TargetUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Metadata"/>.</summary>
    public static ReadOnlySpan<byte> MetadataUtf8 => "metadata"u8;

    /// <summary>The Metadata module container per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#metadata">XLIFF 2.1, metadata</see>.</summary>
    public static readonly string Metadata = Utf8Constants.ToInternedString(MetadataUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MetaGroup"/>.</summary>
    public static ReadOnlySpan<byte> MetaGroupUtf8 => "metaGroup"u8;

    /// <summary>A group of metadata elements sharing a category per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#metagroup">XLIFF 2.1, metaGroup</see>.</summary>
    public static readonly string MetaGroup = Utf8Constants.ToInternedString(MetaGroupUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Meta"/>.</summary>
    public static ReadOnlySpan<byte> MetaUtf8 => "meta"u8;

    /// <summary>One typed metadata value per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#meta">XLIFF 2.1, meta</see>.</summary>
    public static readonly string Meta = Utf8Constants.ToInternedString(MetaUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Validation"/>.</summary>
    public static ReadOnlySpan<byte> ValidationUtf8 => "validation"u8;

    /// <summary>The Validation module container per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#validation">XLIFF 2.1, validation</see>.</summary>
    public static readonly string Validation = Utf8Constants.ToInternedString(ValidationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Rule"/>.</summary>
    public static ReadOnlySpan<byte> RuleUtf8 => "rule"u8;

    /// <summary>One validation rule per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#rule">XLIFF 2.1, rule</see>.</summary>
    public static readonly string Rule = Utf8Constants.ToInternedString(RuleUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Glossary"/>.</summary>
    public static ReadOnlySpan<byte> GlossaryUtf8 => "glossary"u8;

    /// <summary>The Glossary module container, allowed on a unit only, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#glossary">XLIFF 2.1, glossary</see>.</summary>
    public static readonly string Glossary = Utf8Constants.ToInternedString(GlossaryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="GlossEntry"/>.</summary>
    public static ReadOnlySpan<byte> GlossEntryUtf8 => "glossEntry"u8;

    /// <summary>One glossary entry per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#glossentry">XLIFF 2.1, glossEntry</see>.</summary>
    public static readonly string GlossEntry = Utf8Constants.ToInternedString(GlossEntryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Term"/>.</summary>
    public static ReadOnlySpan<byte> TermUtf8 => "term"u8;

    /// <summary>A glossary entry's source term per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#term">XLIFF 2.1, term</see>.</summary>
    public static readonly string Term = Utf8Constants.ToInternedString(TermUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Translation"/>.</summary>
    public static ReadOnlySpan<byte> TranslationUtf8 => "translation"u8;

    /// <summary>A glossary entry's translation per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#translation">XLIFF 2.1, translation</see>.</summary>
    public static readonly string Translation = Utf8Constants.ToInternedString(TranslationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Definition"/>.</summary>
    public static ReadOnlySpan<byte> DefinitionUtf8 => "definition"u8;

    /// <summary>A glossary entry's definition per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#definition">XLIFF 2.1, definition</see>.</summary>
    public static readonly string Definition = Utf8Constants.ToInternedString(DefinitionUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CodePoint"/>.</summary>
    public static ReadOnlySpan<byte> CodePointUtf8 => "cp"u8;

    /// <summary>The inline code for a character the text cannot carry literally per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#cp">XLIFF 2.1, cp</see>; the reader refuses it because flattening loses the character.</summary>
    public static readonly string CodePoint = Utf8Constants.ToInternedString(CodePointUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Placeholder"/>.</summary>
    public static ReadOnlySpan<byte> PlaceholderUtf8 => "ph"u8;

    /// <summary>The standalone placeholder code per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#ph">XLIFF 2.1, ph</see>; the reader refuses it because it carries no text.</summary>
    public static readonly string Placeholder = Utf8Constants.ToInternedString(PlaceholderUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StartCode"/>.</summary>
    public static ReadOnlySpan<byte> StartCodeUtf8 => "sc"u8;

    /// <summary>The start marker of a spanning code per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#sc">XLIFF 2.1, sc</see>; the reader refuses it because it carries no text.</summary>
    public static readonly string StartCode = Utf8Constants.ToInternedString(StartCodeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EndCode"/>.</summary>
    public static ReadOnlySpan<byte> EndCodeUtf8 => "ec"u8;

    /// <summary>The end marker of a spanning code per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#ec">XLIFF 2.1, ec</see>; the reader refuses it because it carries no text.</summary>
    public static readonly string EndCode = Utf8Constants.ToInternedString(EndCodeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="PairedCode"/>.</summary>
    public static ReadOnlySpan<byte> PairedCodeUtf8 => "pc"u8;

    /// <summary>A well-formed spanning original code per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#pc">XLIFF 2.1, pc</see>; the reader refuses it because flattening drops its id, dataRef links to &lt;originalData&gt; and can* attributes.</summary>
    public static readonly string PairedCode = Utf8Constants.ToInternedString(PairedCodeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Marker"/>.</summary>
    public static ReadOnlySpan<byte> MarkerUtf8 => "mrk"u8;

    /// <summary>A spanning annotation marker per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk">XLIFF 2.1, mrk</see>; the reader refuses it because flattening drops its id, translate, type and value attributes.</summary>
    public static readonly string Marker = Utf8Constants.ToInternedString(MarkerUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StartMarker"/>.</summary>
    public static ReadOnlySpan<byte> StartMarkerUtf8 => "sm"u8;

    /// <summary>The start marker of an annotation the spanning form cannot express per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#sm">XLIFF 2.1, sm</see>; the reader refuses it because it carries no text.</summary>
    public static readonly string StartMarker = Utf8Constants.ToInternedString(StartMarkerUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EndMarker"/>.</summary>
    public static ReadOnlySpan<byte> EndMarkerUtf8 => "em"u8;

    /// <summary>The end marker of an annotation the spanning form cannot express per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#em">XLIFF 2.1, em</see>; the reader refuses it because it carries no text.</summary>
    public static readonly string EndMarker = Utf8Constants.ToInternedString(EndMarkerUtf8);

    /// <summary>The UTF-8 source literal of <see cref="OriginalData"/>.</summary>
    public static ReadOnlySpan<byte> OriginalDataUtf8 => "originalData"u8;

    /// <summary>The container of a unit's <c>data</c> entries per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#originalData">XLIFF 2.1, originalData</see>.</summary>
    public static readonly string OriginalData = Utf8Constants.ToInternedString(OriginalDataUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Data"/>.</summary>
    public static ReadOnlySpan<byte> DataUtf8 => "data"u8;

    /// <summary>One original-data entry a code refers to by id per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#data">XLIFF 2.1, data</see>.</summary>
    public static readonly string Data = Utf8Constants.ToInternedString(DataUtf8);

    /// <summary>Determines if an element's local name is <see cref="Xliff"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is the root; otherwise, <see langword="false"/>.</returns>
    public static bool IsXliff(string localName) => string.Equals(localName, Xliff, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="File"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is a file; otherwise, <see langword="false"/>.</returns>
    public static bool IsFile(string localName) => string.Equals(localName, File, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="Unit"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is a unit; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnit(string localName) => string.Equals(localName, Unit, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="Segment"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is a segment; otherwise, <see langword="false"/>.</returns>
    public static bool IsSegment(string localName) => string.Equals(localName, Segment, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="Ignorable"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is an ignorable; otherwise, <see langword="false"/>.</returns>
    public static bool IsIgnorable(string localName) => string.Equals(localName, Ignorable, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="Group"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is a group; otherwise, <see langword="false"/>.</returns>
    public static bool IsGroup(string localName) => string.Equals(localName, Group, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="Validation"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is a Validation module container; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidation(string localName) => string.Equals(localName, Validation, StringComparison.Ordinal);

    /// <summary>
    /// Determines if an element's local name is one of the inline codes or annotation markers the
    /// reader refuses because flattening to text would lose information it carries only in attributes
    /// or in a nested structure the model has no slot for: <see cref="CodePoint"/>,
    /// <see cref="Placeholder"/>, <see cref="StartCode"/>, <see cref="EndCode"/>,
    /// <see cref="PairedCode"/>, <see cref="Marker"/>, <see cref="StartMarker"/> or
    /// <see cref="EndMarker"/>.
    /// </summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if flattening the element to text would lose data; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnsupportedInlineMarkup(string localName) => string.Equals(localName, CodePoint, StringComparison.Ordinal)
        || string.Equals(localName, Placeholder, StringComparison.Ordinal)
        || string.Equals(localName, StartCode, StringComparison.Ordinal)
        || string.Equals(localName, EndCode, StringComparison.Ordinal)
        || string.Equals(localName, PairedCode, StringComparison.Ordinal)
        || string.Equals(localName, Marker, StringComparison.Ordinal)
        || string.Equals(localName, StartMarker, StringComparison.Ordinal)
        || string.Equals(localName, EndMarker, StringComparison.Ordinal);
}
