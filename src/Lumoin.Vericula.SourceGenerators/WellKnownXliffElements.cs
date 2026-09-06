namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The well-known XLIFF 2.x element NAMES this generator reads, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>:
/// the core structure and the standalone or spanning inline codes and annotation markers that carry
/// no text of their own, or carry attributes <see cref="System.Xml.Linq.XElement.Value"/> would drop.
/// </summary>
/// <remarks>
/// This mirrors <c>Lumoin.Vericula.Parsing.WellKnownXliffElements</c>; the generator assembly cannot
/// reference the library, so it carries its own copy of the members it needs.
/// </remarks>
internal static class WellKnownXliffElements
{
    /// <summary>The UTF-8 source literal of <see cref="Xliff"/>.</summary>
    public static ReadOnlySpan<byte> XliffUtf8 => "xliff"u8;

    /// <summary>The root element per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#xliff">XLIFF 2.1, xliff</see>.</summary>
    public static readonly string Xliff = Utf8Constants.ToInternedString(XliffUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Unit"/>.</summary>
    public static ReadOnlySpan<byte> UnitUtf8 => "unit"u8;

    /// <summary>The smallest translatable unit per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#unit">XLIFF 2.1, unit</see>.</summary>
    public static readonly string Unit = Utf8Constants.ToInternedString(UnitUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Segment"/>.</summary>
    public static ReadOnlySpan<byte> SegmentUtf8 => "segment"u8;

    /// <summary>A translatable segment per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#segment">XLIFF 2.1, segment</see>.</summary>
    public static readonly string Segment = Utf8Constants.ToInternedString(SegmentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Ignorable"/>.</summary>
    public static ReadOnlySpan<byte> IgnorableUtf8 => "ignorable"u8;

    /// <summary>Extracted content that is not translated per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#ignorable">XLIFF 2.1, ignorable</see>; its text still folds into the unit's whole text.</summary>
    public static readonly string Ignorable = Utf8Constants.ToInternedString(IgnorableUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Source"/>.</summary>
    public static ReadOnlySpan<byte> SourceUtf8 => "source"u8;

    /// <summary>The source content of a segment or ignorable per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#source">XLIFF 2.1, source</see>.</summary>
    public static readonly string Source = Utf8Constants.ToInternedString(SourceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Target"/>.</summary>
    public static ReadOnlySpan<byte> TargetUtf8 => "target"u8;

    /// <summary>The target content of a segment or ignorable per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#target">XLIFF 2.1, target</see>.</summary>
    public static readonly string Target = Utf8Constants.ToInternedString(TargetUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CodePoint"/>.</summary>
    public static ReadOnlySpan<byte> CodePointUtf8 => "cp"u8;

    /// <summary>The inline code for a character the text cannot carry literally per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#cp">XLIFF 2.1, cp</see>; folding it to text would lose the character.</summary>
    public static readonly string CodePoint = Utf8Constants.ToInternedString(CodePointUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Placeholder"/>.</summary>
    public static ReadOnlySpan<byte> PlaceholderUtf8 => "ph"u8;

    /// <summary>The standalone placeholder code per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#ph">XLIFF 2.1, ph</see>; it carries no text.</summary>
    public static readonly string Placeholder = Utf8Constants.ToInternedString(PlaceholderUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StartCode"/>.</summary>
    public static ReadOnlySpan<byte> StartCodeUtf8 => "sc"u8;

    /// <summary>The start marker of a spanning code per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#sc">XLIFF 2.1, sc</see>; it carries no text.</summary>
    public static readonly string StartCode = Utf8Constants.ToInternedString(StartCodeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EndCode"/>.</summary>
    public static ReadOnlySpan<byte> EndCodeUtf8 => "ec"u8;

    /// <summary>The end marker of a spanning code per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#ec">XLIFF 2.1, ec</see>; it carries no text.</summary>
    public static readonly string EndCode = Utf8Constants.ToInternedString(EndCodeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="PairedCode"/>.</summary>
    public static ReadOnlySpan<byte> PairedCodeUtf8 => "pc"u8;

    /// <summary>The spanning original code per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#pc">XLIFF 2.1, pc</see>; folding it to text would lose the code itself, its id and its dataRef link.</summary>
    public static readonly string PairedCode = Utf8Constants.ToInternedString(PairedCodeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Marker"/>.</summary>
    public static ReadOnlySpan<byte> MarkerUtf8 => "mrk"u8;

    /// <summary>The spanning annotation marker per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk">XLIFF 2.1, mrk</see>; folding it to text would lose its translate, type and value attributes.</summary>
    public static readonly string Marker = Utf8Constants.ToInternedString(MarkerUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StartMarker"/>.</summary>
    public static ReadOnlySpan<byte> StartMarkerUtf8 => "sm"u8;

    /// <summary>The standalone start of an annotation per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#sm">XLIFF 2.1, sm</see>; it carries no text.</summary>
    public static readonly string StartMarker = Utf8Constants.ToInternedString(StartMarkerUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EndMarker"/>.</summary>
    public static ReadOnlySpan<byte> EndMarkerUtf8 => "em"u8;

    /// <summary>The standalone end of an annotation per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#em">XLIFF 2.1, em</see>; it carries no text.</summary>
    public static readonly string EndMarker = Utf8Constants.ToInternedString(EndMarkerUtf8);

    /// <summary>Determines if an element's local name is <see cref="Xliff"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is the root; otherwise, <see langword="false"/>.</returns>
    public static bool IsXliff(string localName) => string.Equals(localName, Xliff, StringComparison.Ordinal);

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

    /// <summary>
    /// Determines if an element's local name is one of the inline codes or annotation markers this
    /// generator does not fold into text: <see cref="CodePoint"/>, <see cref="Placeholder"/>,
    /// <see cref="StartCode"/>, <see cref="EndCode"/>, <see cref="PairedCode"/>, <see cref="Marker"/>,
    /// <see cref="StartMarker"/> or <see cref="EndMarker"/>.
    /// </summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if folding the element to text would lose data; otherwise, <see langword="false"/>.</returns>
    public static bool IsUnsupportedInlineMarkup(string localName) => string.Equals(localName, CodePoint, StringComparison.Ordinal)
        || string.Equals(localName, Placeholder, StringComparison.Ordinal)
        || string.Equals(localName, StartCode, StringComparison.Ordinal)
        || string.Equals(localName, EndCode, StringComparison.Ordinal)
        || string.Equals(localName, PairedCode, StringComparison.Ordinal)
        || string.Equals(localName, Marker, StringComparison.Ordinal)
        || string.Equals(localName, StartMarker, StringComparison.Ordinal)
        || string.Equals(localName, EndMarker, StringComparison.Ordinal);
}
