using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Lumoin.Vericula.Content;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// The half of <see cref="XliffReader"/> that parses a <c>&lt;source&gt;</c> or <c>&lt;target&gt;</c>
/// element into <see cref="InlineContent"/>, per the design at XLIFF 2.1 §4.7 Inline Content, §4.2.3
/// Inline Elements and §4.3.1 Attributes.
/// </summary>
public static partial class XliffReader
{
    /// <summary>The unqualified attributes <see cref="AppendCodePoint"/> accepts on a <c>&lt;cp&gt;</c> element.</summary>
    private static readonly string[] CodePointAttributes = [WellKnownXliffAttributes.Hex];

    /// <summary>The unqualified attributes <see cref="AppendPlaceholder"/> accepts on a <c>&lt;ph&gt;</c> element.</summary>
    private static readonly string[] PlaceholderAttributes =
    [
        WellKnownXliffAttributes.Id, WellKnownXliffAttributes.CanCopy, WellKnownXliffAttributes.CanDelete, WellKnownXliffAttributes.CanReorder,
        WellKnownXliffAttributes.CopyOf, WellKnownXliffAttributes.Disp, WellKnownXliffAttributes.Equiv, WellKnownXliffAttributes.DataRef,
        WellKnownXliffAttributes.SubFlows, WellKnownXliffAttributes.SubType, WellKnownXliffAttributes.Type
    ];

    /// <summary>The unqualified attributes <see cref="AppendPairedCode"/> accepts on a <c>&lt;pc&gt;</c> element.</summary>
    private static readonly string[] PairedCodeAttributes =
    [
        WellKnownXliffAttributes.Id, WellKnownXliffAttributes.CanCopy, WellKnownXliffAttributes.CanDelete, WellKnownXliffAttributes.CanOverlap,
        WellKnownXliffAttributes.CanReorder, WellKnownXliffAttributes.CopyOf, WellKnownXliffAttributes.DispStart, WellKnownXliffAttributes.DispEnd,
        WellKnownXliffAttributes.EquivStart, WellKnownXliffAttributes.EquivEnd, WellKnownXliffAttributes.DataRefStart, WellKnownXliffAttributes.DataRefEnd,
        WellKnownXliffAttributes.SubFlowsStart, WellKnownXliffAttributes.SubFlowsEnd, WellKnownXliffAttributes.SubType, WellKnownXliffAttributes.Type,
        WellKnownXliffAttributes.Dir
    ];

    /// <summary>The unqualified attributes <see cref="AppendStartCode"/> accepts on an <c>&lt;sc&gt;</c> element.</summary>
    private static readonly string[] StartCodeAttributes =
    [
        WellKnownXliffAttributes.Id, WellKnownXliffAttributes.CanCopy, WellKnownXliffAttributes.CanDelete, WellKnownXliffAttributes.CanOverlap,
        WellKnownXliffAttributes.CanReorder, WellKnownXliffAttributes.CopyOf, WellKnownXliffAttributes.DataRef, WellKnownXliffAttributes.Dir,
        WellKnownXliffAttributes.Disp, WellKnownXliffAttributes.Equiv, WellKnownXliffAttributes.Isolated, WellKnownXliffAttributes.SubFlows,
        WellKnownXliffAttributes.SubType, WellKnownXliffAttributes.Type
    ];

    /// <summary>The unqualified attributes <see cref="AppendEndCode"/> accepts on an <c>&lt;ec&gt;</c> element.</summary>
    private static readonly string[] EndCodeAttributes =
    [
        WellKnownXliffAttributes.Id, WellKnownXliffAttributes.CanCopy, WellKnownXliffAttributes.CanDelete, WellKnownXliffAttributes.CanOverlap,
        WellKnownXliffAttributes.CanReorder, WellKnownXliffAttributes.CopyOf, WellKnownXliffAttributes.DataRef, WellKnownXliffAttributes.Dir,
        WellKnownXliffAttributes.Disp, WellKnownXliffAttributes.Equiv, WellKnownXliffAttributes.Isolated, WellKnownXliffAttributes.StartRef,
        WellKnownXliffAttributes.SubFlows, WellKnownXliffAttributes.SubType, WellKnownXliffAttributes.Type
    ];

    /// <summary>The unqualified attributes <see cref="AppendMarker"/> and <see cref="AppendStartMarker"/> accept on a <c>&lt;mrk&gt;</c> or <c>&lt;sm&gt;</c> element.</summary>
    private static readonly string[] AnnotationAttributes =
    [
        WellKnownXliffAttributes.Id, WellKnownXliffAttributes.Translate, WellKnownXliffAttributes.Type, WellKnownXliffAttributes.Ref, WellKnownXliffAttributes.Value
    ];

    /// <summary>The unqualified attributes <see cref="AppendEndMarker"/> accepts on an <c>&lt;em&gt;</c> element.</summary>
    private static readonly string[] EndMarkerAttributes = [WellKnownXliffAttributes.StartRef];

    /// <summary>The unqualified attributes <see cref="ParseOriginalData"/> accepts on a <c>&lt;data&gt;</c> element.</summary>
    private static readonly string[] DataAttributes = [WellKnownXliffAttributes.Id, WellKnownXliffAttributes.Dir];

    /// <summary>The largest code point <c>&lt;cp&gt;</c>'s <c>hex</c> attribute may name (5.3.1).</summary>
    private const int MaxCodePoint = 0x10FFFF;

    /// <summary>
    /// Reads the unit's <c>&lt;originalData&gt;</c> element, if any, into a lookup from a
    /// <c>&lt;data&gt;</c> id to its resolved <see cref="OriginalData"/>. XLIFF 2.1 §4.2.2.5 places
    /// <c>&lt;originalData&gt;</c> after <c>&lt;notes&gt;</c> and before the segments, but the reader
    /// accepts it anywhere among the unit's children, and refuses a second one (the spec allows zero or
    /// one). A <c>&lt;data&gt;</c> entry that no code ends up referencing is simply never attached to a
    /// part and so is dropped, with no separate filtering step.
    /// </summary>
    /// <param name="unitElement">The <c>&lt;unit&gt;</c> element.</param>
    /// <param name="unitId">The unit's id, for error messages.</param>
    /// <returns>The id-to-data lookup; empty when the unit carries no <c>&lt;originalData&gt;</c>.</returns>
    /// <exception cref="XliffFormatException">If the unit has more than one <c>&lt;originalData&gt;</c> element, or a <c>&lt;data&gt;</c> id is missing, malformed or duplicated, or an unqualified attribute is unsupported.</exception>
    private static ImmutableDictionary<string, OriginalData> ParseOriginalData(XElement unitElement, string unitId)
    {
        XElement? originalDataElement = null;
        foreach(XElement candidate in unitElement.Elements(Core + WellKnownXliffElements.OriginalData))
        {
            if(originalDataElement is not null)
            {
                throw WithLocation(new XliffFormatException($"Unit '{unitId}' has more than one <originalData> element; XLIFF 2.1 §4.2.2.5 allows at most one."), candidate);
            }

            originalDataElement = candidate;
        }

        if(originalDataElement is null)
        {
            return ImmutableDictionary.Create<string, OriginalData>(StringComparer.Ordinal);
        }

        ImmutableDictionary<string, OriginalData>.Builder data = ImmutableDictionary.CreateBuilder<string, OriginalData>(StringComparer.Ordinal);
        foreach(XElement dataElement in originalDataElement.Elements(Core + WellKnownXliffElements.Data))
        {
            RefuseUnknownAttributes(dataElement, unitId, WellKnownXliffElements.Data, DataAttributes);
            string dataId = RequiredId(dataElement, WellKnownXliffElements.Data);
            if(data.ContainsKey(dataId))
            {
                throw WithLocation(new XliffFormatException($"Duplicate <data> id '{dataId}' in unit '{unitId}'; XLIFF 2.1 §4.3.1.21 requires data ids to be unique within their unit."), dataElement);
            }

            //xml:space is namespace-qualified, so RefuseUnknownAttributes already leaves it alone; its
            //value is not validated because a <data> element's text is always taken verbatim (XLIFF 2.1
            //§4.2.2.11 restricts xml:space to "preserve" on this element, so there is nothing else it
            //could mean).
            TextDirection direction = ParseDataDirection(dataElement.Attribute(WellKnownXliffAttributes.Dir)?.Value, unitId, dataElement);
            data[dataId] = new OriginalData(ReadDataText(dataElement, unitId), direction);
        }

        return data.ToImmutable();
    }

    /// <summary>
    /// Reads a <c>&lt;data&gt;</c> element's text: character data verbatim and <c>&lt;cp&gt;</c>
    /// children decoded into it, in document order (spec 4.2.2.11 allows the two kinds of content in
    /// any order). An XML comment or processing instruction is ignored, matching how they are ignored
    /// inside <c>&lt;source&gt;</c>/<c>&lt;target&gt;</c> (spec §4.5, §4.6); any other element is
    /// refused, since §4.2.2.11 allows only text and <c>&lt;cp&gt;</c> here.
    /// </summary>
    /// <exception cref="XliffFormatException">If a child element other than <c>&lt;cp&gt;</c> appears, or a <c>&lt;cp&gt;</c> carries an unsupported attribute or an invalid <c>hex</c> value.</exception>
    private static string ReadDataText(XElement dataElement, string unitId)
    {
        var text = new StringBuilder();
        foreach(XNode node in dataElement.Nodes())
        {
            switch(node)
            {
                case(XText textNode):
                {
                    text.Append(textNode.Value);

                    break;
                }

                case(XElement element) when WellKnownXliffElements.IsCodePoint(element.Name.LocalName) && WellKnownXliffNamespaces.IsCore(element.Name.NamespaceName):
                {
                    RefuseUnknownAttributes(element, unitId, WellKnownXliffElements.CodePoint, CodePointAttributes);
                    text.Append(DecodeCodePoint(ParseCodePointHex(RequiredHex(element, unitId), unitId, element)));

                    break;
                }

                case(XElement element):
                {
                    throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element appears inside <data> in unit '{unitId}'; XLIFF 2.1 §4.2.2.11 allows only text and <cp> there."), element);
                }
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// Collects the ids of the unit's direct <c>&lt;segment&gt;</c> and <c>&lt;ignorable&gt;</c>
    /// children, which seed both sides' <see cref="InlineParseState.Ids"/>: XLIFF 2.1 §4.3.1.21 puts
    /// them in the same uniqueness scope as the unit's inline element ids.
    /// </summary>
    private static ImmutableHashSet<string> CollectSegmentScopeIds(XElement unitElement)
    {
        ImmutableHashSet<string>.Builder ids = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach(XElement child in unitElement.Elements())
        {
            bool core = WellKnownXliffNamespaces.IsCore(child.Name.NamespaceName);
            bool segmentOrIgnorable = WellKnownXliffElements.IsSegment(child.Name.LocalName) || WellKnownXliffElements.IsIgnorable(child.Name.LocalName);
            if(!core || !segmentOrIgnorable)
            {
                continue;
            }

            string? segmentId = child.Attribute(WellKnownXliffAttributes.Id)?.Value;
            if(segmentId is not null)
            {
                ids.Add(segmentId);
            }
        }

        return ids.ToImmutable();
    }

    /// <summary>
    /// Parses one <c>&lt;source&gt;</c> or <c>&lt;target&gt;</c> element's nodes into
    /// <see cref="InlineContent"/>.
    /// </summary>
    /// <param name="container">The <c>&lt;source&gt;</c> or <c>&lt;target&gt;</c> element.</param>
    /// <param name="state">The side's inline-content state as it stood before this element.</param>
    /// <param name="context">The parse's fixed inputs: which side this is, the other side's ids, this segment's sibling source ids, the unit's data lookup and its id.</param>
    /// <returns>The parsed content, and the state as it stands after it.</returns>
    /// <exception cref="XliffFormatException">If the content does not conform to what the reader accepts (5.3.2).</exception>
    private static (InlineContent Content, InlineParseState State) ParseInlineContentRoot(XElement container, InlineParseState state, InlineParseContext context)
    {
        ImmutableArray<InlinePart>.Builder builder = ImmutableArray.CreateBuilder<InlinePart>();
        state = AppendNodes(container.Nodes(), builder, state, context);

        return (InlineContent.Create(builder), state);
    }

    /// <summary>
    /// Walks a sequence of XML nodes, appending each one's contribution to <paramref name="builder"/>:
    /// text and CDATA verbatim, an inline element parsed by <see cref="AppendElement"/>, and comments
    /// and processing instructions ignored (XLIFF 2.1 §4.5, §4.6).
    /// </summary>
    private static InlineParseState AppendNodes(IEnumerable<XNode> nodes, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        foreach(XNode node in nodes)
        {
            switch(node)
            {
                case(XText text):
                {
                    AppendText(builder, text.Value);

                    break;
                }

                case(XElement element):
                {
                    state = AppendElement(element, builder, state, context);

                    break;
                }
            }
        }

        return state;
    }

    /// <summary>Appends a text run to <paramref name="builder"/>, dropping it when empty (<see cref="InlineTextPart"/> never carries an empty string).</summary>
    private static void AppendText(ImmutableArray<InlinePart>.Builder builder, string text)
    {
        if(text.Length != 0)
        {
            builder.Add(new InlineTextPart(text));
        }
    }

    /// <summary>
    /// Dispatches one child element of inline content to the parser for its kind, refusing a
    /// foreign-namespace element outright and any core element that is not one of the eight inline
    /// elements XLIFF 2.1 §4.2.3 defines.
    /// </summary>
    private static InlineParseState AppendElement(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        if(!WellKnownXliffNamespaces.IsCore(element.Name.NamespaceName))
        {
            throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element from another namespace appears inside inline content in unit '{context.UnitId}'; XLIFF 2.1 §4.7 inline content allows only the core inline elements."), element);
        }

        string name = element.Name.LocalName;

        return name switch
        {
            _ when WellKnownXliffElements.IsCodePoint(name) => AppendCodePoint(element, builder, state, context),
            _ when WellKnownXliffElements.IsPlaceholder(name) => AppendPlaceholder(element, builder, state, context),
            _ when WellKnownXliffElements.IsPairedCode(name) => AppendPairedCode(element, builder, state, context),
            _ when WellKnownXliffElements.IsStartCode(name) => AppendStartCode(element, builder, state, context),
            _ when WellKnownXliffElements.IsEndCode(name) => AppendEndCode(element, builder, state, context),
            _ when WellKnownXliffElements.IsMarker(name) => AppendMarker(element, builder, state, context),
            _ when WellKnownXliffElements.IsStartMarker(name) => AppendStartMarker(element, builder, state, context),
            _ when WellKnownXliffElements.IsEndMarker(name) => AppendEndMarker(element, builder, state, context),
            _ => throw WithLocation(new XliffFormatException($"A <{name}> element inside inline content in unit '{context.UnitId}' is not a recognized XLIFF inline element."), element)
        };
    }

    /// <summary>Parses a <c>&lt;cp&gt;</c> element (5.3.1) and appends its decoded character to the text.</summary>
    private static InlineParseState AppendCodePoint(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        RefuseUnknownAttributes(element, context.UnitId, WellKnownXliffElements.CodePoint, CodePointAttributes);
        int codePoint = ParseCodePointHex(RequiredHex(element, context.UnitId), context.UnitId, element);
        AppendText(builder, DecodeCodePoint(codePoint));

        return state;
    }

    /// <summary>Reads a <c>&lt;cp&gt;</c> element's required <c>hex</c> attribute.</summary>
    private static string RequiredHex(XElement element, string unitId)
    {
        string? hex = element.Attribute(WellKnownXliffAttributes.Hex)?.Value;
        if(string.IsNullOrEmpty(hex))
        {
            throw WithLocation(new XliffFormatException($"A <cp> element in unit '{unitId}' does not declare the required hex attribute."), element);
        }

        return hex;
    }

    /// <summary>
    /// Parses a <c>&lt;cp&gt;</c> element's <c>hex</c> value (5.3.1): hexBinary of 2, 4 or 6 digits,
    /// either letter case, at most <see cref="MaxCodePoint"/>. Odd lengths and lengths over 6 are
    /// refused by the length check; a value with a non-hexadecimal character (white space included) is
    /// refused separately, before parsing, so the two failure modes carry distinct messages and so that
    /// <see cref="NumberStyles.HexNumber"/>'s own <c>AllowLeadingWhite</c>/<c>AllowTrailingWhite</c>
    /// flags never let a space-padded value such as <c>" A0 "</c> or <c>"A "</c> through.
    /// </summary>
    /// <exception cref="XliffFormatException">If <paramref name="hex"/> is not 2, 4 or 6 hexadecimal digits, or names a code point above <see cref="MaxCodePoint"/>.</exception>
    private static int ParseCodePointHex(string hex, string unitId, XElement element)
    {
        if(hex.Length is not (2 or 4 or 6))
        {
            throw WithLocation(new XliffFormatException($"A <cp> element in unit '{unitId}' has the hex value '{hex}', which must be 2, 4 or 6 hexadecimal digits (XLIFF 2.1 §4.2.3.1)."), element);
        }

        foreach(char digit in hex)
        {
            if(!char.IsAsciiHexDigit(digit))
            {
                throw WithLocation(new XliffFormatException($"A <cp> element in unit '{unitId}' has the hex value '{hex}', which is not a valid hexadecimal number."), element);
            }
        }

        if(!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
        {
            throw WithLocation(new XliffFormatException($"A <cp> element in unit '{unitId}' has the hex value '{hex}', which is not a valid hexadecimal number."), element);
        }

        if(value > MaxCodePoint)
        {
            throw WithLocation(new XliffFormatException($"A <cp> element in unit '{unitId}' has the hex value '{hex}', naming code point U+{value.ToString("X", CultureInfo.InvariantCulture)}, which exceeds the maximum U+10FFFF."), element);
        }

        return value;
    }

    /// <summary>
    /// Decodes a code point into the string it contributes to the text: one UTF-16 code unit at or
    /// below U+FFFF, carried as-is even when it is an unpaired surrogate half, or a full surrogate pair
    /// above it. Two <c>&lt;cp&gt;</c> elements that each decode to one half of a valid surrogate pair
    /// merge into one character purely because their decoded halves land next to each other in the
    /// text; no separate merge step is needed.
    /// </summary>
    private static string DecodeCodePoint(int value) => value <= 0xFFFF ? ((char)value).ToString() : char.ConvertFromUtf32(value);

    /// <summary>Parses a <c>&lt;ph&gt;</c> element (a standalone code) into a <see cref="PlaceholderPart"/>.</summary>
    private static InlineParseState AppendPlaceholder(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        RefuseUnknownAttributes(element, context.UnitId, WellKnownXliffElements.Placeholder, PlaceholderAttributes);
        string id = RequiredId(element, WellKnownXliffElements.Placeholder);
        state = RegisterInlineId(state, id, context, element);

        InlineCodeType type = ParseCodeType(element.Attribute(WellKnownXliffAttributes.Type)?.Value, context.UnitId, element);
        string? subType = ParseSubType(element.Attribute(WellKnownXliffAttributes.SubType)?.Value, type, context.UnitId, element);
        string? dataRef = element.Attribute(WellKnownXliffAttributes.DataRef)?.Value;

        builder.Add(new PlaceholderPart(
            id,
            type,
            subType,
            element.Attribute(WellKnownXliffAttributes.Equiv)?.Value ?? string.Empty,
            element.Attribute(WellKnownXliffAttributes.Disp)?.Value,
            dataRef,
            ResolveDataRef(dataRef, context.Data, context.UnitId, element),
            ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanCopy)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanCopy),
            ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanDelete)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanDelete),
            ParseCanReorder(element.Attribute(WellKnownXliffAttributes.CanReorder)?.Value, context.UnitId, element),
            element.Attribute(WellKnownXliffAttributes.CopyOf)?.Value,
            element.Attribute(WellKnownXliffAttributes.SubFlows)?.Value));

        return state;
    }

    /// <summary>
    /// Parses a <c>&lt;pc&gt;</c> element (a well-formed spanning code) into a <see cref="StartCodePart"/>,
    /// its children, and an <see cref="EndCodePart"/>, mapping its shared and half-specific attributes
    /// per table 2 (5.3, XLIFF 2.1 §4.7.2.2): <c>dispStart</c>/<c>equivStart</c>/<c>subFlowsStart</c>/
    /// <c>dataRefStart</c> to the start, the <c>End</c> ones to the end, and <c>id</c>/<c>type</c>/
    /// <c>subType</c>/<c>canCopy</c>/<c>canDelete</c>/<c>canReorder</c>/<c>copyOf</c>/<c>canOverlap</c>/
    /// <c>dir</c> to both; the end's <see cref="EndCodePart.StartRef"/> is the shared <c>id</c>. A
    /// <c>&lt;pc&gt;</c> has no <c>isolated</c> attribute of its own, so both halves are never isolated.
    /// </summary>
    private static InlineParseState AppendPairedCode(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        RefuseUnknownAttributes(element, context.UnitId, WellKnownXliffElements.PairedCode, PairedCodeAttributes);
        string id = RequiredId(element, WellKnownXliffElements.PairedCode);
        state = RegisterInlineId(state, id, context, element);

        InlineCodeType type = ParseCodeType(element.Attribute(WellKnownXliffAttributes.Type)?.Value, context.UnitId, element);
        string? subType = ParseSubType(element.Attribute(WellKnownXliffAttributes.SubType)?.Value, type, context.UnitId, element);
        bool canCopy = ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanCopy)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanCopy);
        bool canDelete = ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanDelete)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanDelete);
        ReorderHint canReorder = ParseCanReorder(element.Attribute(WellKnownXliffAttributes.CanReorder)?.Value, context.UnitId, element);
        string? copyOf = element.Attribute(WellKnownXliffAttributes.CopyOf)?.Value;
        bool canOverlap = ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanOverlap)?.Value, false, context.UnitId, element, WellKnownXliffAttributes.CanOverlap);
        TextDirection direction = ParseDirection(element.Attribute(WellKnownXliffAttributes.Dir)?.Value, context.UnitId, element);
        string? dataRefStart = element.Attribute(WellKnownXliffAttributes.DataRefStart)?.Value;
        string? dataRefEnd = element.Attribute(WellKnownXliffAttributes.DataRefEnd)?.Value;

        builder.Add(new StartCodePart(
            id,
            type,
            subType,
            element.Attribute(WellKnownXliffAttributes.EquivStart)?.Value ?? string.Empty,
            element.Attribute(WellKnownXliffAttributes.DispStart)?.Value,
            dataRefStart,
            ResolveDataRef(dataRefStart, context.Data, context.UnitId, element),
            canCopy,
            canDelete,
            canReorder,
            copyOf,
            element.Attribute(WellKnownXliffAttributes.SubFlowsStart)?.Value,
            canOverlap,
            Isolated: false,
            direction,
            SpanForm.Paired));

        state = AppendNodes(element.Nodes(), builder, state, context);

        builder.Add(new EndCodePart(
            id,
            null,
            type,
            subType,
            element.Attribute(WellKnownXliffAttributes.EquivEnd)?.Value ?? string.Empty,
            element.Attribute(WellKnownXliffAttributes.DispEnd)?.Value,
            dataRefEnd,
            ResolveDataRef(dataRefEnd, context.Data, context.UnitId, element),
            canCopy,
            canDelete,
            canOverlap,
            canReorder,
            copyOf,
            element.Attribute(WellKnownXliffAttributes.SubFlowsEnd)?.Value,
            Isolated: false,
            direction,
            SpanForm.Paired));

        return state;
    }

    /// <summary>Parses an <c>&lt;sc&gt;</c> element (the start half of a split spanning code) into a <see cref="StartCodePart"/> and opens it for a later <c>&lt;ec&gt;</c> on the same side.</summary>
    private static InlineParseState AppendStartCode(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        RefuseUnknownAttributes(element, context.UnitId, WellKnownXliffElements.StartCode, StartCodeAttributes);
        string id = RequiredId(element, WellKnownXliffElements.StartCode);
        state = RegisterInlineId(state, id, context, element);

        InlineCodeType type = ParseCodeType(element.Attribute(WellKnownXliffAttributes.Type)?.Value, context.UnitId, element);
        string? subType = ParseSubType(element.Attribute(WellKnownXliffAttributes.SubType)?.Value, type, context.UnitId, element);
        bool isolated = ParseYesNo(element.Attribute(WellKnownXliffAttributes.Isolated)?.Value, false, context.UnitId, element, WellKnownXliffAttributes.Isolated);
        string? dataRef = element.Attribute(WellKnownXliffAttributes.DataRef)?.Value;

        builder.Add(new StartCodePart(
            id,
            type,
            subType,
            element.Attribute(WellKnownXliffAttributes.Equiv)?.Value ?? string.Empty,
            element.Attribute(WellKnownXliffAttributes.Disp)?.Value,
            dataRef,
            ResolveDataRef(dataRef, context.Data, context.UnitId, element),
            ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanCopy)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanCopy),
            ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanDelete)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanDelete),
            ParseCanReorder(element.Attribute(WellKnownXliffAttributes.CanReorder)?.Value, context.UnitId, element),
            element.Attribute(WellKnownXliffAttributes.CopyOf)?.Value,
            element.Attribute(WellKnownXliffAttributes.SubFlows)?.Value,
            ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanOverlap)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanOverlap),
            isolated,
            ParseDirection(element.Attribute(WellKnownXliffAttributes.Dir)?.Value, context.UnitId, element),
            SpanForm.Split));

        return state with { OpenStarts = state.OpenStarts.SetItem(id, isolated) };
    }

    /// <summary>
    /// Parses an <c>&lt;ec&gt;</c> element (the end half of a split spanning code) into an
    /// <see cref="EndCodePart"/>: when <c>isolated="yes"</c> it must carry its own <c>id</c> and no
    /// <c>startRef</c>; otherwise it must carry <c>startRef</c> naming a start still open on this side,
    /// which it then closes (5.3.2).
    /// </summary>
    private static InlineParseState AppendEndCode(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        RefuseUnknownAttributes(element, context.UnitId, WellKnownXliffElements.EndCode, EndCodeAttributes);
        bool isolated = ParseYesNo(element.Attribute(WellKnownXliffAttributes.Isolated)?.Value, false, context.UnitId, element, WellKnownXliffAttributes.Isolated);
        string? startRefAttribute = element.Attribute(WellKnownXliffAttributes.StartRef)?.Value;
        string? idAttribute = element.Attribute(WellKnownXliffAttributes.Id)?.Value;

        string? startRef;
        string? endId;
        if(isolated)
        {
            if(string.IsNullOrWhiteSpace(idAttribute))
            {
                throw WithLocation(new XliffFormatException($"An isolated <ec> element in unit '{context.UnitId}' does not declare the required id attribute."), element);
            }

            if(startRefAttribute is not null)
            {
                throw WithLocation(new XliffFormatException($"An isolated <ec> element in unit '{context.UnitId}' must not declare a startRef attribute; XLIFF 2.1 §4.2.3.5 uses id instead."), element);
            }

            try
            {
                endId = RequireNameToken(idAttribute, WellKnownXliffElements.EndCode);
            }
            catch(XliffFormatException exception)
            {
                throw WithLocation(exception, element);
            }

            state = RegisterInlineId(state, endId, context, element);
            startRef = null;
        }
        else
        {
            if(string.IsNullOrWhiteSpace(startRefAttribute))
            {
                throw WithLocation(new XliffFormatException($"A <ec> element in unit '{context.UnitId}' does not declare the required startRef attribute."), element);
            }

            state = ResolveAndCloseStart(state, startRefAttribute, context.UnitId, element);
            startRef = startRefAttribute;
            endId = null;
        }

        InlineCodeType type = ParseCodeType(element.Attribute(WellKnownXliffAttributes.Type)?.Value, context.UnitId, element);
        string? subType = ParseSubType(element.Attribute(WellKnownXliffAttributes.SubType)?.Value, type, context.UnitId, element);
        string? dataRef = element.Attribute(WellKnownXliffAttributes.DataRef)?.Value;

        builder.Add(new EndCodePart(
            startRef,
            endId,
            type,
            subType,
            element.Attribute(WellKnownXliffAttributes.Equiv)?.Value ?? string.Empty,
            element.Attribute(WellKnownXliffAttributes.Disp)?.Value,
            dataRef,
            ResolveDataRef(dataRef, context.Data, context.UnitId, element),
            ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanCopy)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanCopy),
            ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanDelete)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanDelete),
            ParseYesNo(element.Attribute(WellKnownXliffAttributes.CanOverlap)?.Value, true, context.UnitId, element, WellKnownXliffAttributes.CanOverlap),
            ParseCanReorder(element.Attribute(WellKnownXliffAttributes.CanReorder)?.Value, context.UnitId, element),
            element.Attribute(WellKnownXliffAttributes.CopyOf)?.Value,
            element.Attribute(WellKnownXliffAttributes.SubFlows)?.Value,
            isolated,
            ParseDirection(element.Attribute(WellKnownXliffAttributes.Dir)?.Value, context.UnitId, element),
            SpanForm.Split));

        return state;
    }

    /// <summary>Parses a <c>&lt;mrk&gt;</c> element (a wrapping annotation) into an <see cref="AnnotationStartPart"/>, its children, and an <see cref="AnnotationEndPart"/>.</summary>
    private static InlineParseState AppendMarker(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        RefuseUnknownAttributes(element, context.UnitId, WellKnownXliffElements.Marker, AnnotationAttributes);
        string id = RequiredId(element, WellKnownXliffElements.Marker);
        state = RegisterInlineId(state, id, context, element);

        (string type, bool? translate, string? refValue, string? value) = ParseAnnotationAttributes(element, id, context.UnitId);

        builder.Add(new AnnotationStartPart(id, type, translate, refValue, value, AnnotationForm.Marker));
        state = AppendNodes(element.Nodes(), builder, state, context);
        builder.Add(new AnnotationEndPart(id, AnnotationForm.Marker));

        return state;
    }

    /// <summary>Parses an <c>&lt;sm&gt;</c> element (the start of a split annotation) into an <see cref="AnnotationStartPart"/> and opens it for a later <c>&lt;em&gt;</c> on the same side.</summary>
    private static InlineParseState AppendStartMarker(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        RefuseUnknownAttributes(element, context.UnitId, WellKnownXliffElements.StartMarker, AnnotationAttributes);
        string id = RequiredId(element, WellKnownXliffElements.StartMarker);
        state = RegisterInlineId(state, id, context, element);

        (string type, bool? translate, string? refValue, string? value) = ParseAnnotationAttributes(element, id, context.UnitId);

        builder.Add(new AnnotationStartPart(id, type, translate, refValue, value, AnnotationForm.Split));

        return state with { OpenAnnotations = state.OpenAnnotations.Add(id) };
    }

    /// <summary>Parses an <c>&lt;em&gt;</c> element (the end of a split annotation) into an <see cref="AnnotationEndPart"/> and closes the <c>&lt;sm&gt;</c> its <c>startRef</c> names on the same side.</summary>
    private static InlineParseState AppendEndMarker(XElement element, ImmutableArray<InlinePart>.Builder builder, InlineParseState state, InlineParseContext context)
    {
        RefuseUnknownAttributes(element, context.UnitId, WellKnownXliffElements.EndMarker, EndMarkerAttributes);
        string startRef = RequiredStartRef(element, WellKnownXliffElements.EndMarker, context.UnitId);
        state = ResolveAndCloseAnnotation(state, startRef, context.UnitId, element);
        builder.Add(new AnnotationEndPart(startRef, AnnotationForm.Split));

        return state;
    }

    /// <summary>
    /// Parses the attributes <c>&lt;mrk&gt;</c> and <c>&lt;sm&gt;</c> share: <c>type</c> (defaulting to
    /// <see cref="WellKnownXliffAttributeValues.Generic"/>; a value outside the three reserved words and
    /// not shaped as <c>prefix:value</c> is accepted as-is, since 5.3 leaves this leniency unenforced),
    /// <c>translate</c>, <c>ref</c> and <c>value</c>, refusing a <c>comment</c> annotation that carries
    /// neither <c>value</c> nor <c>ref</c>, or both of them (XLIFF 2.1 §4.7.3.1.3: "if and only if the
    /// value attribute is not present, the ref attribute MUST be present").
    /// </summary>
    private static (string Type, bool? Translate, string? Ref, string? Value) ParseAnnotationAttributes(XElement element, string id, string unitId)
    {
        string type = element.Attribute(WellKnownXliffAttributes.Type)?.Value ?? WellKnownXliffAttributeValues.Generic;
        bool? translate = ParseNullableYesNo(element.Attribute(WellKnownXliffAttributes.Translate)?.Value, unitId, element, WellKnownXliffAttributes.Translate);
        string? refValue = element.Attribute(WellKnownXliffAttributes.Ref)?.Value;
        string? value = element.Attribute(WellKnownXliffAttributes.Value)?.Value;
        if(WellKnownXliffAttributeValues.IsComment(type) && value is null && refValue is null)
        {
            throw WithLocation(new XliffFormatException($"The comment annotation '{id}' in unit '{unitId}' has neither a value nor a ref attribute; XLIFF 2.1 §4.7.3.1.3 requires one."), element);
        }

        if(WellKnownXliffAttributeValues.IsComment(type) && value is not null && refValue is not null)
        {
            throw WithLocation(new XliffFormatException($"The comment annotation '{id}' in unit '{unitId}' has both a value and a ref attribute; XLIFF 2.1 §4.7.3.1.3 allows only one."), element);
        }

        return (type, translate, refValue, value);
    }

    /// <summary>
    /// Registers an inline element's id against the state for its side, enforcing XLIFF 2.1 §4.3.1.21.
    /// The same-side check is unconditional: an id already used on this side is always refused, even
    /// when it is the sibling source id this same segment's target is otherwise allowed to reuse (that
    /// exemption covers one reuse of the source's id, not a second one). The cross-side check refuses an
    /// id already used on the other side of the unit unless this is the target side reusing exactly the
    /// sibling source id this same segment introduced; a legitimate sibling reuse is not recorded again,
    /// it is already in <see cref="InlineParseState.Ids"/> from when the source side registered it.
    /// </summary>
    private static InlineParseState RegisterInlineId(InlineParseState state, string id, InlineParseContext context, XElement element)
    {
        if(state.Ids.Contains(id))
        {
            throw WithLocation(new XliffFormatException($"The id '{id}' is used more than once in unit '{context.UnitId}'; XLIFF 2.1 §4.3.1.21 requires inline ids to be unique within the unit, except a target element reusing its own sibling source element's id."), element);
        }

        bool exemptSiblingReuse = context.IsTarget && context.SiblingSourceIds.Contains(id);
        if(context.OtherSideIds.Contains(id) && !exemptSiblingReuse)
        {
            throw WithLocation(new XliffFormatException($"The id '{id}' is used more than once in unit '{context.UnitId}'; XLIFF 2.1 §4.3.1.21 requires inline ids to be unique within the unit, except a target element reusing its own sibling source element's id."), element);
        }

        return state with { Ids = state.Ids.Add(id) };
    }

    /// <summary>Closes the open <c>&lt;sc&gt;</c> named by an <c>&lt;ec&gt;</c>'s <c>startRef</c>, refusing a reference to no open start on this side or to one marked <c>isolated="yes"</c> (which must never be closed by an <c>&lt;ec&gt;</c>).</summary>
    private static InlineParseState ResolveAndCloseStart(InlineParseState state, string startRef, string unitId, XElement element)
    {
        if(!state.OpenStarts.TryGetValue(startRef, out bool isolatedStart))
        {
            throw WithLocation(new XliffFormatException($"The <ec> in unit '{unitId}' has startRef '{startRef}', which names no open <sc> on this side."), element);
        }

        if(isolatedStart)
        {
            throw WithLocation(new XliffFormatException($"The <sc> with id '{startRef}' in unit '{unitId}' is isolated and must not be closed by an <ec>."), element);
        }

        return state with { OpenStarts = state.OpenStarts.Remove(startRef) };
    }

    /// <summary>Closes the open <c>&lt;sm&gt;</c> named by an <c>&lt;em&gt;</c>'s <c>startRef</c>, refusing a reference to no open start marker on this side.</summary>
    private static InlineParseState ResolveAndCloseAnnotation(InlineParseState state, string startRef, string unitId, XElement element)
    {
        if(!state.OpenAnnotations.Contains(startRef))
        {
            throw WithLocation(new XliffFormatException($"The <em> in unit '{unitId}' has startRef '{startRef}', which names no open <sm> on this side."), element);
        }

        return state with { OpenAnnotations = state.OpenAnnotations.Remove(startRef) };
    }

    /// <summary>
    /// Refuses a unit whose source or target side still has an open, non-isolated <c>&lt;sc&gt;</c>
    /// once every segment has been parsed (5.3.2); a split annotation left open (an <c>&lt;sm&gt;</c>
    /// with no <c>&lt;em&gt;</c>) is tolerated instead, so no equivalent check runs over
    /// <see cref="InlineParseState.OpenAnnotations"/>.
    /// </summary>
    private static void RequireEveryStartCodeClosedOrIsolated(InlineParseState state, string unitId, string side, XElement unitElement)
    {
        foreach(KeyValuePair<string, bool> openStart in state.OpenStarts)
        {
            if(!openStart.Value)
            {
                throw WithLocation(new XliffFormatException($"Unit '{unitId}' has a <sc> with id '{openStart.Key}' on the {side} side that is never closed by a matching <ec>; mark it isolated=\"yes\" if it truly has none in this unit."), unitElement);
            }
        }
    }

    /// <summary>Resolves a code's <c>dataRef</c>/<c>dataRefStart</c>/<c>dataRefEnd</c> against the unit's data lookup, refusing a reference to an id the unit does not define.</summary>
    private static OriginalData? ResolveDataRef(string? dataRefId, ImmutableDictionary<string, OriginalData> data, string unitId, XElement element)
    {
        if(dataRefId is null)
        {
            return null;
        }

        if(data.TryGetValue(dataRefId, out OriginalData? originalData))
        {
            return originalData;
        }

        throw WithLocation(new XliffFormatException($"A code in unit '{unitId}' references the <data> id '{dataRefId}', which the unit does not define."), element);
    }

    /// <summary>Refuses an unqualified attribute of <paramref name="element"/> that is not in <paramref name="known"/>; a namespace-qualified attribute (a module, an extension, or <c>xml:</c>) is silently dropped instead, matching the rest of the reader.</summary>
    private static void RefuseUnknownAttributes(XElement element, string unitId, string elementName, string[] known)
    {
        foreach(XAttribute attribute in element.Attributes())
        {
            if(attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None)
            {
                continue;
            }

            if(Array.IndexOf(known, attribute.Name.LocalName) < 0)
            {
                throw WithLocation(new XliffFormatException($"A <{elementName}> element in unit '{unitId}' carries the unsupported attribute '{attribute.Name.LocalName}'."), element);
            }
        }
    }

    /// <summary>Reads and validates a required <c>startRef</c> attribute, for <c>&lt;em&gt;</c> (XLIFF 2.1 §4.2.3.8 requires it).</summary>
    private static string RequiredStartRef(XElement element, string elementName, string unitId)
    {
        string? value = element.Attribute(WellKnownXliffAttributes.StartRef)?.Value;
        if(string.IsNullOrWhiteSpace(value))
        {
            throw WithLocation(new XliffFormatException($"A <{elementName}> element in unit '{unitId}' does not declare the required startRef attribute."), element);
        }

        return value;
    }

    /// <summary>Parses a code's <c>type</c> attribute (XLIFF 2.1 §4.3.1.40): absent means <see cref="InlineCodeType.None"/>, and any value other than the six reserved words is refused.</summary>
    private static InlineCodeType ParseCodeType(string? value, string unitId, XElement element)
    {
        return value switch
        {
            null => InlineCodeType.None,
            _ when WellKnownXliffAttributeValues.IsFmt(value) => InlineCodeType.Format,
            _ when WellKnownXliffAttributeValues.IsUi(value) => InlineCodeType.UserInterface,
            _ when WellKnownXliffAttributeValues.IsQuote(value) => InlineCodeType.Quote,
            _ when WellKnownXliffAttributeValues.IsLink(value) => InlineCodeType.Link,
            _ when WellKnownXliffAttributeValues.IsImage(value) => InlineCodeType.Image,
            _ when WellKnownXliffAttributeValues.IsOther(value) => InlineCodeType.Other,
            _ => throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element in unit '{unitId}' has the unsupported type '{value}'; XLIFF 2.1 §4.3.1.40 restricts a code's type to fmt, ui, quote, link, image or other."), element)
        };
    }

    /// <summary>
    /// Parses a code's <c>subType</c> attribute (XLIFF 2.1 §4.3.1.36): it requires <c>type</c> to be
    /// present at all, and the reserved <c>xlf:</c> sub-types each require a specific <c>type</c>
    /// (<c>xlf:b</c>/<c>xlf:i</c>/<c>xlf:u</c>/<c>xlf:lb</c> require <c>fmt</c>, <c>xlf:var</c> requires
    /// <c>ui</c>); any other prefix is free.
    /// </summary>
    private static string? ParseSubType(string? subType, InlineCodeType type, string unitId, XElement element)
    {
        if(subType is null)
        {
            return null;
        }

        if(type == InlineCodeType.None)
        {
            throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element in unit '{unitId}' has subType '{subType}' without a type; XLIFF 2.1 §4.3.1.36 requires subType to only be used together with type."), element);
        }

        bool reservedFmt = WellKnownXliffAttributeValues.IsSubTypeBold(subType) || WellKnownXliffAttributeValues.IsSubTypeItalic(subType)
            || WellKnownXliffAttributeValues.IsSubTypeUnderline(subType) || WellKnownXliffAttributeValues.IsSubTypeLineBreak(subType) || WellKnownXliffAttributeValues.IsSubTypePageBreak(subType);
        if(reservedFmt && type != InlineCodeType.Format)
        {
            throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element in unit '{unitId}' has the reserved subType '{subType}', which XLIFF 2.1 §4.3.1.36 requires type=\"fmt\" for."), element);
        }

        if(WellKnownXliffAttributeValues.IsSubTypeVariable(subType) && type != InlineCodeType.UserInterface)
        {
            throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element in unit '{unitId}' has the reserved subType '{subType}', which XLIFF 2.1 §4.3.1.36 requires type=\"ui\" for."), element);
        }

        return subType;
    }

    /// <summary>Parses a yes/no attribute, defaulting to <paramref name="defaultValue"/> when absent and refusing any other value.</summary>
    private static bool ParseYesNo(string? value, bool defaultValue, string unitId, XElement element, string attributeName)
    {
        return value switch
        {
            null => defaultValue,
            _ when WellKnownXliffAttributeValues.IsYes(value) => true,
            _ when WellKnownXliffAttributeValues.IsNo(value) => false,
            _ => throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element in unit '{unitId}' has the {attributeName} value '{value}'; expected yes or no."), element)
        };
    }

    /// <summary>Parses a nullable yes/no attribute (<c>translate</c>), where absent means null (inherited) rather than a boolean default.</summary>
    private static bool? ParseNullableYesNo(string? value, string unitId, XElement element, string attributeName)
    {
        return value switch
        {
            null => null,
            _ when WellKnownXliffAttributeValues.IsYes(value) => true,
            _ when WellKnownXliffAttributeValues.IsNo(value) => false,
            _ => throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element in unit '{unitId}' has the {attributeName} value '{value}'; expected yes or no."), element)
        };
    }

    /// <summary>Parses a code's <c>canReorder</c> attribute (XLIFF 2.1 §4.3.1.5): absent defaults to <see cref="ReorderHint.Yes"/>.</summary>
    private static ReorderHint ParseCanReorder(string? value, string unitId, XElement element)
    {
        return value switch
        {
            null => ReorderHint.Yes,
            _ when WellKnownXliffAttributeValues.IsYes(value) => ReorderHint.Yes,
            _ when WellKnownXliffAttributeValues.IsFirstNo(value) => ReorderHint.FirstNo,
            _ when WellKnownXliffAttributeValues.IsNo(value) => ReorderHint.No,
            _ => throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element in unit '{unitId}' has the canReorder value '{value}'; expected yes, firstNo or no."), element)
        };
    }

    /// <summary>Parses a code's <c>dir</c> attribute: absent means <see cref="TextDirection.Inherited"/>.</summary>
    private static TextDirection ParseDirection(string? value, string unitId, XElement element)
    {
        return value switch
        {
            null => TextDirection.Inherited,
            _ when WellKnownXliffAttributeValues.IsLtr(value) => TextDirection.LeftToRight,
            _ when WellKnownXliffAttributeValues.IsRtl(value) => TextDirection.RightToLeft,
            _ when WellKnownXliffAttributeValues.IsAuto(value) => TextDirection.Auto,
            _ => throw WithLocation(new XliffFormatException($"A <{element.Name.LocalName}> element in unit '{unitId}' has the dir value '{value}'; expected ltr, rtl or auto."), element)
        };
    }

    /// <summary>Parses a <c>&lt;data&gt;</c> element's <c>dir</c> attribute: absent means <see cref="TextDirection.Auto"/>, its own default rather than the inherited one every other element's <c>dir</c> has (XLIFF 2.1 §4.3.1.12).</summary>
    private static TextDirection ParseDataDirection(string? value, string unitId, XElement element)
    {
        return value switch
        {
            null => TextDirection.Auto,
            _ when WellKnownXliffAttributeValues.IsLtr(value) => TextDirection.LeftToRight,
            _ when WellKnownXliffAttributeValues.IsRtl(value) => TextDirection.RightToLeft,
            _ when WellKnownXliffAttributeValues.IsAuto(value) => TextDirection.Auto,
            _ => throw WithLocation(new XliffFormatException($"A <data> element in unit '{unitId}' has the dir value '{value}'; expected ltr, rtl or auto."), element)
        };
    }

    /// <summary>
    /// The unit-level, per-side state <see cref="ParseSegment"/> threads across a unit's segments in
    /// document order (5.3.2), because a spanning code's halves or a split annotation's halves may sit
    /// in different segments.
    /// </summary>
    /// <param name="Ids">The inline ids already used on this side, seeded with the unit's segment and ignorable ids (XLIFF 2.1 §4.3.1.21 puts them in the same scope).</param>
    /// <param name="OpenStarts">The ids of <c>&lt;sc&gt;</c> elements opened on this side that have not yet been closed by a matching <c>&lt;ec&gt;</c>, mapped to whether they were opened with <c>isolated="yes"</c>.</param>
    /// <param name="OpenAnnotations">The ids of <c>&lt;sm&gt;</c> elements opened on this side that have not yet been closed by a matching <c>&lt;em&gt;</c>.</param>
    private sealed record InlineParseState(ImmutableHashSet<string> Ids, ImmutableDictionary<string, bool> OpenStarts, ImmutableHashSet<string> OpenAnnotations)
    {
        /// <summary>Builds the state a unit's side starts from, before any of its segments are parsed.</summary>
        /// <param name="seededIds">The unit's segment and ignorable ids (<see cref="CollectSegmentScopeIds"/>).</param>
        /// <returns>The seeded state, with no open starts or annotations.</returns>
        public static InlineParseState Seed(ImmutableHashSet<string> seededIds)
        {
            return new InlineParseState(seededIds, ImmutableDictionary.Create<string, bool>(StringComparer.Ordinal), ImmutableHashSet.Create<string>(StringComparer.Ordinal));
        }
    }

    /// <summary>The fixed inputs one <see cref="ParseInlineContentRoot"/> call parses content with, alongside the <see cref="InlineParseState"/> that changes as it walks.</summary>
    /// <param name="IsTarget">Whether this parse is over a <c>&lt;target&gt;</c> element rather than a <c>&lt;source&gt;</c>; only the target side may reuse an id from the other side (5.3.2).</param>
    /// <param name="OtherSideIds">The ids already used on the other side of the unit so far (for the target side, the source ids of every segment up to and including this one; for the source side, the target ids of every earlier segment), consulted on both sides so an id is unique across the unit except for the one sibling-reuse exemption.</param>
    /// <param name="SiblingSourceIds">The ids this same segment's <c>&lt;source&gt;</c> introduced, the only ones a target inline element may reuse; consulted only when <see cref="IsTarget"/> is true.</param>
    /// <param name="Data">The unit's resolved <c>&lt;data&gt;</c> lookup, for a code's <c>dataRef</c> to resolve against.</param>
    /// <param name="UnitId">The enclosing unit's id, for error messages.</param>
    private sealed record InlineParseContext(bool IsTarget, ImmutableHashSet<string> OtherSideIds, ImmutableHashSet<string> SiblingSourceIds, ImmutableDictionary<string, OriginalData> Data, string UnitId);
}
