using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The half of <see cref="XliffSourceGenerator"/> that renders a <c>&lt;source&gt;</c> or
/// <c>&lt;target&gt;</c> element's inline content directly to the same strings
/// <c>Lumoin.Vericula.Content.InlineContent.Render</c> would produce for the same document, per XLIFF
/// 2.1 §4.7 Inline Content, §4.2.3 Inline Elements and §4.3.1 Attributes (design 5.7).
/// </summary>
/// <remarks>
/// The generator cannot reference the core library, so this is a small, purpose-built renderer rather
/// than a full model: it keeps only what rendering and the structural checks below need (a code's raw
/// <c>type</c>/<c>subType</c> strings, its resolved original-data text, its <c>disp</c> and
/// <c>equiv</c>), not the full attribute surface the core reader carries (<c>canCopy</c>,
/// <c>canReorder</c>, <c>copyOf</c>, <c>subFlows</c>, <c>dir</c> and the like), none of which affects a
/// rendered string.
/// </remarks>
public sealed partial class XliffSourceGenerator
{
    /// <summary>The largest code point <c>&lt;cp&gt;</c>'s <c>hex</c> attribute may name (XLIFF 2.1 §4.2.3.1).</summary>
    private const int MaxCodePoint = 0x10FFFF;

    /// <summary>The characters a text run escapes when the content carries at least one code part, so an HTML fragment tokenizes correctly.</summary>
    private static readonly char[] MarkupEscapeCharacters = ['&', '<', '>'];

    /// <summary>
    /// Renders one <c>&lt;source&gt;</c> or <c>&lt;target&gt;</c> element's content, or reports the
    /// structural fault (5.3.2) that keeps the whole document from being turned into accessors.
    /// </summary>
    /// <param name="element">The element to render, or <see langword="null"/> when the side is absent.</param>
    /// <param name="unitId">The enclosing unit's id, for failure messages.</param>
    /// <param name="data">The unit's resolved <c>&lt;data&gt;</c> lookup, id to text, for a code's <c>dataRef</c> to resolve against.</param>
    /// <param name="state">This side's state as it stood before this element (5.3.2: ids seen, open <c>sc</c> starts, open <c>sm</c> annotations), carried across the unit's segments in document order.</param>
    /// <param name="parts">The parsed parts, ready for <see cref="RenderMarkup"/>, <see cref="RenderPlain"/> or <see cref="WalkTranslatable"/>; empty when <paramref name="element"/> is <see langword="null"/> or parsing failed.</param>
    /// <param name="newState">The state as it stands after this element.</param>
    /// <param name="failure">The structural-fault message, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> if the element parsed without a structural fault; otherwise, <see langword="false"/>.</returns>
    private static bool TryRenderContent(
        XElement? element,
        string unitId,
        ImmutableDictionary<string, string> data,
        InlineSideState state,
        out ImmutableArray<RenderPart> parts,
        out InlineSideState newState,
        out string? failure)
    {
        if(element is null)
        {
            parts = ImmutableArray<RenderPart>.Empty;
            newState = state;
            failure = null;

            return true;
        }

        ImmutableArray<RenderPart>.Builder builder = ImmutableArray.CreateBuilder<RenderPart>();
        (bool success, InlineSideState resultState, string? resultFailure) = TryAppendNodes(element.Nodes(), builder, unitId, data, state);
        parts = success ? builder.ToImmutable() : ImmutableArray<RenderPart>.Empty;
        newState = resultState;
        failure = resultFailure;

        return success;
    }

    /// <summary>Walks a sequence of XML nodes, appending each one's contribution to <paramref name="builder"/>: text and CDATA verbatim, an inline element parsed by <see cref="TryAppendElement"/>, and comments and processing instructions ignored (XLIFF 2.1 §4.5, §4.6).</summary>
    /// <param name="nodes">The nodes to walk, in document order.</param>
    /// <param name="builder">The builder parts are appended to.</param>
    /// <param name="unitId">The enclosing unit's id, for failure messages.</param>
    /// <param name="data">The unit's resolved <c>&lt;data&gt;</c> lookup.</param>
    /// <param name="state">This side's state as it stood before these nodes.</param>
    /// <returns>Whether the walk succeeded, the state as it stands afterward, and the failure message on the first structural fault.</returns>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendNodes(
        IEnumerable<XNode> nodes,
        ImmutableArray<RenderPart>.Builder builder,
        string unitId,
        ImmutableDictionary<string, string> data,
        InlineSideState state)
    {
        foreach(XNode node in nodes)
        {
            if(node is XText text)
            {
                AppendText(builder, text.Value);

                continue;
            }

            if(node is XElement element)
            {
                (bool success, InlineSideState newState, string? failure) = TryAppendElement(element, builder, unitId, data, state);
                state = newState;
                if(!success)
                {
                    return (false, state, failure);
                }
            }
        }

        return (true, state, null);
    }

    /// <summary>Appends a text run to <paramref name="builder"/>, dropping it when empty (an empty run carries nothing to render).</summary>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="text">The text run, verbatim.</param>
    private static void AppendText(ImmutableArray<RenderPart>.Builder builder, string text)
    {
        if(text.Length != 0)
        {
            builder.Add(new RenderTextPart(text));
        }
    }

    /// <summary>Dispatches one child element of inline content to the parser for its kind, refusing a foreign-namespace element and any core element that is not one of the eight inline elements XLIFF 2.1 §4.2.3 defines.</summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendElement(
        XElement element,
        ImmutableArray<RenderPart>.Builder builder,
        string unitId,
        ImmutableDictionary<string, string> data,
        InlineSideState state)
    {
        if(!WellKnownXliffNamespaces.IsCore(element.Name.NamespaceName))
        {
            return (false, state, $"A <{element.Name.LocalName}> element from another namespace appears inside inline content in unit '{unitId}'; XLIFF 2.1 §4.7 inline content allows only the core inline elements.");
        }

        string name = element.Name.LocalName;

        return name switch
        {
            _ when WellKnownXliffElements.IsCodePoint(name) => TryAppendCodePoint(element, builder, unitId, state),
            _ when WellKnownXliffElements.IsPlaceholder(name) => TryAppendPlaceholder(element, builder, unitId, data, state),
            _ when WellKnownXliffElements.IsPairedCode(name) => TryAppendPairedCode(element, builder, unitId, data, state),
            _ when WellKnownXliffElements.IsStartCode(name) => TryAppendStartCode(element, builder, unitId, data, state),
            _ when WellKnownXliffElements.IsEndCode(name) => TryAppendEndCode(element, builder, unitId, data, state),
            _ when WellKnownXliffElements.IsMarker(name) => TryAppendMarker(element, builder, unitId, data, state),
            _ when WellKnownXliffElements.IsStartMarker(name) => TryAppendStartMarker(element, builder, unitId, state),
            _ when WellKnownXliffElements.IsEndMarker(name) => TryAppendEndMarker(element, builder, unitId, state),
            _ => (false, state, $"A <{name}> element inside inline content in unit '{unitId}' is not a recognized XLIFF inline element.")
        };
    }

    /// <summary>Parses a <c>&lt;cp&gt;</c> element (5.3.1) and appends its decoded character to the text.</summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendCodePoint(XElement element, ImmutableArray<RenderPart>.Builder builder, string unitId, InlineSideState state)
    {
        if(!TryDecodeCodePoint(element.Attribute(WellKnownXliffAttributes.Hex)?.Value, unitId, WellKnownXliffElements.CodePoint, out string decoded, out string? failure))
        {
            return (false, state, failure);
        }

        AppendText(builder, decoded);

        return (true, state, null);
    }

    /// <summary>
    /// Decodes a <c>&lt;cp&gt;</c> or <c>&lt;data&gt;</c>-child <c>hex</c> value (5.3.1): hexBinary of 2,
    /// 4 or 6 digits, either letter case, at most <see cref="MaxCodePoint"/>, a value above U+FFFF
    /// becoming a surrogate pair; two <c>cp</c> elements forming a valid pair merge into one character
    /// purely because their decoded halves land next to each other in the text.
    /// </summary>
    /// <param name="hex">The <c>hex</c> attribute's value, or <see langword="null"/> when absent.</param>
    /// <param name="unitId">The enclosing unit's id, for the failure message.</param>
    /// <param name="elementName">The element's local name, for the failure message.</param>
    /// <param name="decoded">The decoded character or surrogate pair; empty on failure.</param>
    /// <param name="failure">The failure message, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> if <paramref name="hex"/> decoded; otherwise, <see langword="false"/>.</returns>
    private static bool TryDecodeCodePoint(string? hex, string unitId, string elementName, out string decoded, out string? failure)
    {
        if(hex is null || hex.Length == 0)
        {
            decoded = string.Empty;
            failure = $"A <{elementName}> element in unit '{unitId}' does not declare the required hex attribute.";

            return false;
        }

        if(hex.Length is not (2 or 4 or 6))
        {
            decoded = string.Empty;
            failure = $"A <{elementName}> element in unit '{unitId}' has the hex value '{hex}', which must be 2, 4 or 6 hexadecimal digits (XLIFF 2.1 §4.2.3.1).";

            return false;
        }

        foreach(char digit in hex)
        {
            if(!IsHexDigit(digit))
            {
                decoded = string.Empty;
                failure = $"A <{elementName}> element in unit '{unitId}' has the hex value '{hex}', which is not a valid hexadecimal number.";

                return false;
            }
        }

        if(!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
        {
            decoded = string.Empty;
            failure = $"A <{elementName}> element in unit '{unitId}' has the hex value '{hex}', which is not a valid hexadecimal number.";

            return false;
        }

        if(value > MaxCodePoint)
        {
            decoded = string.Empty;
            failure = $"A <{elementName}> element in unit '{unitId}' has the hex value '{hex}', naming code point U+{value.ToString("X", CultureInfo.InvariantCulture)}, which exceeds the maximum U+10FFFF.";

            return false;
        }

        decoded = value <= 0xFFFF ? ((char)value).ToString() : char.ConvertFromUtf32(value);
        failure = null;

        return true;
    }

    /// <summary>Determines whether a character is an ASCII hexadecimal digit (netstandard2.0 has no <c>char.IsAsciiHexDigit</c>).</summary>
    /// <param name="character">The character to test.</param>
    /// <returns><see langword="true"/> if the character is 0-9, a-f or A-F; otherwise, <see langword="false"/>.</returns>
    private static bool IsHexDigit(char character) => character is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

    /// <summary>Parses a <c>&lt;ph&gt;</c> element (a standalone code) into a <see cref="RenderPlaceholderPart"/>.</summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendPlaceholder(XElement element, ImmutableArray<RenderPart>.Builder builder, string unitId, ImmutableDictionary<string, string> data, InlineSideState state)
    {
        (bool hasId, string _, InlineSideState idState, string? idFailure) = TryRequireAndRegisterId(element, WellKnownXliffElements.Placeholder, unitId, state);
        if(!hasId)
        {
            return (false, state, idFailure);
        }

        state = idState;

        (bool typeOk, string? type, string? typeFailure) = TryParseCodeType(element.Attribute(WellKnownXliffAttributes.Type)?.Value, unitId, WellKnownXliffElements.Placeholder);
        if(!typeOk)
        {
            return (false, state, typeFailure);
        }

        string? subType = element.Attribute(WellKnownXliffAttributes.SubType)?.Value;
        if(!TryValidateSubType(subType, type, unitId, WellKnownXliffElements.Placeholder, out string? subTypeFailure))
        {
            return (false, state, subTypeFailure);
        }

        if(!TryResolveDataRef(element.Attribute(WellKnownXliffAttributes.DataRef)?.Value, data, unitId, out string? originalDataText, out string? dataFailure))
        {
            return (false, state, dataFailure);
        }

        string equiv = element.Attribute(WellKnownXliffAttributes.Equiv)?.Value ?? string.Empty;
        string? disp = element.Attribute(WellKnownXliffAttributes.Disp)?.Value;
        (string markupText, bool isTag) = ResolveCodeText(type, subType, originalDataText, disp, equiv);
        builder.Add(new RenderPlaceholderPart(markupText, isTag, equiv));

        return (true, state, null);
    }

    /// <summary>
    /// Parses a <c>&lt;pc&gt;</c> element (a well-formed spanning code) into a
    /// <see cref="RenderStartCodePart"/>, its children, and a <see cref="RenderEndCodePart"/>, mapping
    /// its shared and half-specific attributes per table 2 (5.3, XLIFF 2.1 §4.7.2.2):
    /// <c>dispStart</c>/<c>equivStart</c>/<c>dataRefStart</c> to the start, the <c>End</c> ones to the
    /// end, and <c>id</c>/<c>type</c>/<c>subType</c> to both.
    /// </summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendPairedCode(XElement element, ImmutableArray<RenderPart>.Builder builder, string unitId, ImmutableDictionary<string, string> data, InlineSideState state)
    {
        (bool hasId, string _, InlineSideState idState, string? idFailure) = TryRequireAndRegisterId(element, WellKnownXliffElements.PairedCode, unitId, state);
        if(!hasId)
        {
            return (false, state, idFailure);
        }

        state = idState;

        (bool typeOk, string? type, string? typeFailure) = TryParseCodeType(element.Attribute(WellKnownXliffAttributes.Type)?.Value, unitId, WellKnownXliffElements.PairedCode);
        if(!typeOk)
        {
            return (false, state, typeFailure);
        }

        string? subType = element.Attribute(WellKnownXliffAttributes.SubType)?.Value;
        if(!TryValidateSubType(subType, type, unitId, WellKnownXliffElements.PairedCode, out string? subTypeFailure))
        {
            return (false, state, subTypeFailure);
        }

        if(!TryResolveDataRef(element.Attribute(WellKnownXliffAttributes.DataRefStart)?.Value, data, unitId, out string? startData, out string? startDataFailure))
        {
            return (false, state, startDataFailure);
        }

        if(!TryResolveDataRef(element.Attribute(WellKnownXliffAttributes.DataRefEnd)?.Value, data, unitId, out string? endData, out string? endDataFailure))
        {
            return (false, state, endDataFailure);
        }

        string equivStart = element.Attribute(WellKnownXliffAttributes.EquivStart)?.Value ?? string.Empty;
        string? dispStart = element.Attribute(WellKnownXliffAttributes.DispStart)?.Value;
        string equivEnd = element.Attribute(WellKnownXliffAttributes.EquivEnd)?.Value ?? string.Empty;
        string? dispEnd = element.Attribute(WellKnownXliffAttributes.DispEnd)?.Value;

        (string startMarkup, bool startIsTag) = ResolveCodeText(type, subType, startData, dispStart, equivStart);
        builder.Add(new RenderStartCodePart(startMarkup, startIsTag, equivStart));

        (bool childrenOk, InlineSideState childState, string? childFailure) = TryAppendNodes(element.Nodes(), builder, unitId, data, state);
        if(!childrenOk)
        {
            return (false, childState, childFailure);
        }

        state = childState;

        (string endMarkup, bool endIsTag) = ResolveCodeText(type, subType, endData, dispEnd, equivEnd);
        builder.Add(new RenderEndCodePart(endMarkup, endIsTag, equivEnd));

        return (true, state, null);
    }

    /// <summary>Parses an <c>&lt;sc&gt;</c> element (the start half of a split spanning code) into a <see cref="RenderStartCodePart"/> and opens it for a later <c>&lt;ec&gt;</c> on the same side.</summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendStartCode(XElement element, ImmutableArray<RenderPart>.Builder builder, string unitId, ImmutableDictionary<string, string> data, InlineSideState state)
    {
        (bool hasId, string id, InlineSideState idState, string? idFailure) = TryRequireAndRegisterId(element, WellKnownXliffElements.StartCode, unitId, state);
        if(!hasId)
        {
            return (false, state, idFailure);
        }

        state = idState;

        (bool typeOk, string? type, string? typeFailure) = TryParseCodeType(element.Attribute(WellKnownXliffAttributes.Type)?.Value, unitId, WellKnownXliffElements.StartCode);
        if(!typeOk)
        {
            return (false, state, typeFailure);
        }

        string? subType = element.Attribute(WellKnownXliffAttributes.SubType)?.Value;
        if(!TryValidateSubType(subType, type, unitId, WellKnownXliffElements.StartCode, out string? subTypeFailure))
        {
            return (false, state, subTypeFailure);
        }

        bool isolated = WellKnownXliffAttributeValues.IsYes(element.Attribute(WellKnownXliffAttributes.Isolated)?.Value);

        if(!TryResolveDataRef(element.Attribute(WellKnownXliffAttributes.DataRef)?.Value, data, unitId, out string? originalDataText, out string? dataFailure))
        {
            return (false, state, dataFailure);
        }

        string equiv = element.Attribute(WellKnownXliffAttributes.Equiv)?.Value ?? string.Empty;
        string? disp = element.Attribute(WellKnownXliffAttributes.Disp)?.Value;
        (string markupText, bool isTag) = ResolveCodeText(type, subType, originalDataText, disp, equiv);
        builder.Add(new RenderStartCodePart(markupText, isTag, equiv));

        return (true, state with { OpenStarts = state.OpenStarts.SetItem(id, isolated) }, null);
    }

    /// <summary>
    /// Parses an <c>&lt;ec&gt;</c> element (the end half of a split spanning code) into a
    /// <see cref="RenderEndCodePart"/>: when <c>isolated="yes"</c> it must carry its own <c>id</c> and
    /// no <c>startRef</c>; otherwise it must carry <c>startRef</c> naming a non-isolated start still
    /// open on this side, which it then closes (5.3.2).
    /// </summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendEndCode(XElement element, ImmutableArray<RenderPart>.Builder builder, string unitId, ImmutableDictionary<string, string> data, InlineSideState state)
    {
        bool isolated = WellKnownXliffAttributeValues.IsYes(element.Attribute(WellKnownXliffAttributes.Isolated)?.Value);
        string? startRefAttribute = element.Attribute(WellKnownXliffAttributes.StartRef)?.Value;
        string? idAttribute = element.Attribute(WellKnownXliffAttributes.Id)?.Value;

        if(isolated)
        {
            if(idAttribute is null || string.IsNullOrWhiteSpace(idAttribute))
            {
                return (false, state, $"An isolated <ec> element in unit '{unitId}' does not declare the required id attribute.");
            }

            if(startRefAttribute is not null)
            {
                return (false, state, $"An isolated <ec> element in unit '{unitId}' must not declare a startRef attribute; XLIFF 2.1 §4.2.3.5 uses id instead.");
            }

            if(state.Ids.Contains(idAttribute))
            {
                return (false, state, $"The id '{idAttribute}' is used more than once in unit '{unitId}'; XLIFF 2.1 §4.3.1.21 requires inline ids to be unique within the unit.");
            }

            state = state with { Ids = state.Ids.Add(idAttribute) };
        }
        else
        {
            if(startRefAttribute is null || string.IsNullOrWhiteSpace(startRefAttribute))
            {
                return (false, state, $"A <ec> element in unit '{unitId}' does not declare the required startRef attribute.");
            }

            if(!state.OpenStarts.TryGetValue(startRefAttribute, out bool isolatedStart))
            {
                return (false, state, $"The <ec> in unit '{unitId}' has startRef '{startRefAttribute}', which names no open <sc> on this side.");
            }

            if(isolatedStart)
            {
                return (false, state, $"The <sc> with id '{startRefAttribute}' in unit '{unitId}' is isolated and must not be closed by an <ec>.");
            }

            state = state with { OpenStarts = state.OpenStarts.Remove(startRefAttribute) };
        }

        (bool typeOk, string? type, string? typeFailure) = TryParseCodeType(element.Attribute(WellKnownXliffAttributes.Type)?.Value, unitId, WellKnownXliffElements.EndCode);
        if(!typeOk)
        {
            return (false, state, typeFailure);
        }

        string? subType = element.Attribute(WellKnownXliffAttributes.SubType)?.Value;
        if(!TryValidateSubType(subType, type, unitId, WellKnownXliffElements.EndCode, out string? subTypeFailure))
        {
            return (false, state, subTypeFailure);
        }

        if(!TryResolveDataRef(element.Attribute(WellKnownXliffAttributes.DataRef)?.Value, data, unitId, out string? originalDataText, out string? dataFailure))
        {
            return (false, state, dataFailure);
        }

        string equiv = element.Attribute(WellKnownXliffAttributes.Equiv)?.Value ?? string.Empty;
        string? disp = element.Attribute(WellKnownXliffAttributes.Disp)?.Value;
        (string markupText, bool isTag) = ResolveCodeText(type, subType, originalDataText, disp, equiv);
        builder.Add(new RenderEndCodePart(markupText, isTag, equiv));

        return (true, state, null);
    }

    /// <summary>Parses a <c>&lt;mrk&gt;</c> element (a wrapping annotation) into a <see cref="RenderAnnotationStartPart"/>, its children, and a <see cref="RenderAnnotationEndPart"/>.</summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendMarker(XElement element, ImmutableArray<RenderPart>.Builder builder, string unitId, ImmutableDictionary<string, string> data, InlineSideState state)
    {
        (bool hasId, string id, InlineSideState idState, string? idFailure) = TryRequireAndRegisterId(element, WellKnownXliffElements.Marker, unitId, state);
        if(!hasId)
        {
            return (false, state, idFailure);
        }

        state = idState;

        (bool annotationOk, bool? translate, string? annotationFailure) = TryParseAnnotationAttributes(element, id, unitId);
        if(!annotationOk)
        {
            return (false, state, annotationFailure);
        }

        builder.Add(new RenderAnnotationStartPart(translate));

        (bool childrenOk, InlineSideState childState, string? childFailure) = TryAppendNodes(element.Nodes(), builder, unitId, data, state);
        if(!childrenOk)
        {
            return (false, childState, childFailure);
        }

        builder.Add(new RenderAnnotationEndPart());

        return (true, childState, null);
    }

    /// <summary>Parses an <c>&lt;sm&gt;</c> element (the start of a split annotation) into a <see cref="RenderAnnotationStartPart"/> and opens it for a later <c>&lt;em&gt;</c> on the same side.</summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendStartMarker(XElement element, ImmutableArray<RenderPart>.Builder builder, string unitId, InlineSideState state)
    {
        (bool hasId, string id, InlineSideState idState, string? idFailure) = TryRequireAndRegisterId(element, WellKnownXliffElements.StartMarker, unitId, state);
        if(!hasId)
        {
            return (false, state, idFailure);
        }

        state = idState;

        (bool annotationOk, bool? translate, string? annotationFailure) = TryParseAnnotationAttributes(element, id, unitId);
        if(!annotationOk)
        {
            return (false, state, annotationFailure);
        }

        builder.Add(new RenderAnnotationStartPart(translate));

        return (true, state with { OpenAnnotations = state.OpenAnnotations.Add(id) }, null);
    }

    /// <summary>Parses an <c>&lt;em&gt;</c> element (the end of a split annotation) into a <see cref="RenderAnnotationEndPart"/> and closes the <c>&lt;sm&gt;</c> its <c>startRef</c> names on the same side.</summary>
    private static (bool Success, InlineSideState State, string? Failure) TryAppendEndMarker(XElement element, ImmutableArray<RenderPart>.Builder builder, string unitId, InlineSideState state)
    {
        string? startRef = element.Attribute(WellKnownXliffAttributes.StartRef)?.Value;
        if(startRef is null || string.IsNullOrWhiteSpace(startRef))
        {
            return (false, state, $"A <{WellKnownXliffElements.EndMarker}> element in unit '{unitId}' does not declare the required startRef attribute.");
        }

        if(!state.OpenAnnotations.Contains(startRef))
        {
            return (false, state, $"The <em> in unit '{unitId}' has startRef '{startRef}', which names no open <sm> on this side.");
        }

        builder.Add(new RenderAnnotationEndPart());

        return (true, state with { OpenAnnotations = state.OpenAnnotations.Remove(startRef) }, null);
    }

    /// <summary>
    /// Parses the attributes <c>&lt;mrk&gt;</c> and <c>&lt;sm&gt;</c> share that the generator needs:
    /// <c>translate</c>, and enough of <c>type</c>/<c>value</c>/<c>ref</c> to refuse a <c>comment</c>
    /// annotation carrying neither <c>value</c> nor <c>ref</c> (XLIFF 2.1 §4.7.3.1.3). The generator
    /// does not otherwise model an annotation's <c>type</c>, so no fallback to <c>generic</c> is needed.
    /// </summary>
    private static (bool Success, bool? Translate, string? Failure) TryParseAnnotationAttributes(XElement element, string id, string unitId)
    {
        string? type = element.Attribute(WellKnownXliffAttributes.Type)?.Value;
        string? translateValue = element.Attribute(WellKnownXliffAttributes.Translate)?.Value;
        bool? translate = translateValue is null ? null : WellKnownXliffAttributeValues.IsYes(translateValue);
        string? refValue = element.Attribute(WellKnownXliffAttributes.Ref)?.Value;
        string? value = element.Attribute(WellKnownXliffAttributes.Value)?.Value;

        if(type is not null && WellKnownXliffAttributeValues.IsComment(type) && value is null && refValue is null)
        {
            return (false, null, $"The comment annotation '{id}' in unit '{unitId}' has neither a value nor a ref attribute; XLIFF 2.1 §4.7.3.1.3 requires one.");
        }

        return (true, translate, null);
    }

    /// <summary>Reads and registers an element's required <c>id</c> attribute against <paramref name="state"/>'s side, refusing a missing id or one already used on this side (5.3.2).</summary>
    /// <param name="element">The element to read <c>id</c> from.</param>
    /// <param name="elementName">The element's local name, for failure messages.</param>
    /// <param name="unitId">The enclosing unit's id, for failure messages.</param>
    /// <param name="state">The side's state before this id.</param>
    /// <returns>Whether an id was found and registered, the id itself, the state with it added, and the failure message on either fault.</returns>
    private static (bool Success, string Id, InlineSideState State, string? Failure) TryRequireAndRegisterId(XElement element, string elementName, string unitId, InlineSideState state)
    {
        string? id = element.Attribute(WellKnownXliffAttributes.Id)?.Value;
        if(id is null || string.IsNullOrWhiteSpace(id))
        {
            return (false, string.Empty, state, $"A <{elementName}> element in unit '{unitId}' does not declare the required id attribute.");
        }

        if(state.Ids.Contains(id))
        {
            return (false, string.Empty, state, $"The id '{id}' is used more than once in unit '{unitId}'; XLIFF 2.1 §4.3.1.21 requires inline ids to be unique within the unit.");
        }

        return (true, id, state with { Ids = state.Ids.Add(id) }, null);
    }

    /// <summary>Parses a code's <c>type</c> attribute (XLIFF 2.1 §4.3.1.40): absent means null, and any value other than the six reserved words is refused.</summary>
    private static (bool Success, string? Type, string? Failure) TryParseCodeType(string? value, string unitId, string elementName)
    {
        if(value is null)
        {
            return (true, null, null);
        }

        bool reserved = WellKnownXliffAttributeValues.IsFmt(value) || WellKnownXliffAttributeValues.IsUi(value) || WellKnownXliffAttributeValues.IsQuote(value)
            || WellKnownXliffAttributeValues.IsLink(value) || WellKnownXliffAttributeValues.IsImage(value) || WellKnownXliffAttributeValues.IsOther(value);

        return reserved
            ? (true, value, null)
            : (false, null, $"A <{elementName}> element in unit '{unitId}' has the unsupported type '{value}'; XLIFF 2.1 §4.3.1.40 restricts a code's type to fmt, ui, quote, link, image or other.");
    }

    /// <summary>
    /// Validates a code's <c>subType</c> attribute (XLIFF 2.1 §4.3.1.36): it requires <c>type</c> to be
    /// present at all, and the reserved <c>xlf:</c> sub-types each require a specific <c>type</c>
    /// (<c>xlf:b</c>/<c>xlf:i</c>/<c>xlf:u</c>/<c>xlf:lb</c> require <c>fmt</c>, <c>xlf:var</c> requires
    /// <c>ui</c>); any other prefix is free.
    /// </summary>
    private static bool TryValidateSubType(string? subType, string? type, string unitId, string elementName, out string? failure)
    {
        if(subType is null)
        {
            failure = null;

            return true;
        }

        if(type is null)
        {
            failure = $"A <{elementName}> element in unit '{unitId}' has subType '{subType}' without a type; XLIFF 2.1 §4.3.1.36 requires subType to only be used together with type.";

            return false;
        }

        bool reservedFmt = WellKnownXliffAttributeValues.IsSubTypeBold(subType) || WellKnownXliffAttributeValues.IsSubTypeItalic(subType)
            || WellKnownXliffAttributeValues.IsSubTypeUnderline(subType) || WellKnownXliffAttributeValues.IsSubTypeLineBreak(subType) || WellKnownXliffAttributeValues.IsSubTypePageBreak(subType);
        if(reservedFmt && !WellKnownXliffAttributeValues.IsFmt(type))
        {
            failure = $"A <{elementName}> element in unit '{unitId}' has the reserved subType '{subType}', which XLIFF 2.1 §4.3.1.36 requires type=\"fmt\" for.";

            return false;
        }

        if(WellKnownXliffAttributeValues.IsSubTypeVariable(subType) && !WellKnownXliffAttributeValues.IsUi(type))
        {
            failure = $"A <{elementName}> element in unit '{unitId}' has the reserved subType '{subType}', which XLIFF 2.1 §4.3.1.36 requires type=\"ui\" for.";

            return false;
        }

        failure = null;

        return true;
    }

    /// <summary>Resolves a code's <c>dataRef</c>/<c>dataRefStart</c>/<c>dataRefEnd</c> against the unit's data lookup, refusing a reference to an id the unit does not define.</summary>
    private static bool TryResolveDataRef(string? dataRefId, ImmutableDictionary<string, string> data, string unitId, out string? originalDataText, out string? failure)
    {
        if(dataRefId is null)
        {
            originalDataText = null;
            failure = null;

            return true;
        }

        if(data.TryGetValue(dataRefId, out string? text))
        {
            originalDataText = text;
            failure = null;

            return true;
        }

        originalDataText = null;
        failure = $"A code in unit '{unitId}' references the <data> id '{dataRefId}', which the unit does not define.";

        return false;
    }

    /// <summary>
    /// Resolves what one code contributes to a <see cref="RenderMarkup"/> render: its original data
    /// verbatim, an element name to wrap in a tag, its display text verbatim, or its escaped
    /// <c>equiv</c>, in that order of preference - the same order <c>InlineContent.RenderCodeText</c>
    /// uses (design 5.2). Computed eagerly here, at parse time, rather than at render time: unlike text
    /// escaping (a whole-content decision), a code's resolved text depends only on its own attributes.
    /// </summary>
    private static (string Text, bool IsTag) ResolveCodeText(string? type, string? subType, string? originalDataText, string? disp, string equiv)
    {
        if(originalDataText is not null)
        {
            return (originalDataText, false);
        }

        //Mirrors InlineContent.RenderCodeText's own call: the third clue of TryResolve (matching a tag
        //name recovered from original data) can never fire here, because a non-null originalDataText
        //already returned above; WellKnownInlineTokensTests exercises that clue directly instead.
        if(WellKnownInlineTokens.TryResolve(type, subType, null, out string name))
        {
            return (name, true);
        }

        return (disp ?? EscapeMarkupText(equiv), false);
    }

    /// <summary>Escapes <c>&amp;</c>, <c>&lt;</c> and <c>&gt;</c> so a text run cannot be mistaken for markup inside an HTML fragment.</summary>
    private static string EscapeMarkupText(string text)
    {
        if(text.IndexOfAny(MarkupEscapeCharacters) < 0)
        {
            return text;
        }

        var escaped = new StringBuilder(text.Length);
        foreach(char character in text)
        {
            string? replacement = character switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                _ => null
            };

            if(replacement is null)
            {
                escaped.Append(character);
            }
            else
            {
                escaped.Append(replacement);
            }
        }

        return escaped.ToString();
    }

    /// <summary>Determines whether any part is a code (a placeholder, or a start or end half), the same rule <c>InlineContent.HasCodes</c> uses to decide whether a content's text runs escape.</summary>
    private static bool HasCodeParts(ImmutableArray<RenderPart> parts)
    {
        foreach(RenderPart part in parts)
        {
            if(part is RenderPlaceholderPart or RenderStartCodePart or RenderEndCodePart)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Renders plain text when <paramref name="parts"/> has no codes, or an HTML fragment when it has
    /// at least one: text escaped so it tokenizes correctly, and each code rendered from its
    /// pre-resolved <see cref="RenderPlaceholderPart.MarkupText"/> (or its start/end twin's), wrapped
    /// in a tag when it is one. The same string <c>InlineContent.Render(Markup)</c> produces for the
    /// same document (5.7).
    /// </summary>
    private static string RenderMarkup(ImmutableArray<RenderPart> parts)
    {
        bool escapeText = HasCodeParts(parts);
        var markup = new StringBuilder();
        foreach(RenderPart part in parts)
        {
            markup.Append(part switch
            {
                RenderTextPart text => escapeText ? EscapeMarkupText(text.Text) : text.Text,
                RenderPlaceholderPart placeholder => placeholder.MarkupIsTag ? "<" + placeholder.MarkupText + "/>" : placeholder.MarkupText,
                RenderStartCodePart startCode => startCode.MarkupIsTag ? "<" + startCode.MarkupText + ">" : startCode.MarkupText,
                RenderEndCodePart endCode => endCode.MarkupIsTag ? "</" + endCode.MarkupText + ">" : endCode.MarkupText,
                _ => string.Empty
            });
        }

        return markup.ToString();
    }

    /// <summary>
    /// Renders every text part verbatim and every code part's <c>equiv</c>, with nothing escaped and
    /// annotation parts contributing nothing - XLIFF 2.1's equality type B view (§4.7.8), the same
    /// string <c>InlineContent.Render(Plain)</c> produces for the same document (5.7). Used for the
    /// generated accessor's XML-doc summary so IntelliSense shows readable text rather than escaped
    /// markup.
    /// </summary>
    private static string RenderPlain(ImmutableArray<RenderPart> parts)
    {
        var plain = new StringBuilder();
        foreach(RenderPart part in parts)
        {
            plain.Append(part switch
            {
                RenderTextPart text => text.Text,
                RenderPlaceholderPart placeholder => placeholder.Equiv,
                RenderStartCodePart startCode => startCode.Equiv,
                RenderEndCodePart endCode => endCode.Equiv,
                _ => string.Empty
            });
        }

        return plain.ToString();
    }

    /// <summary>
    /// Walks <paramref name="parts"/>, accumulating the <see cref="RenderPlain"/> text of every part
    /// that is translatable given <paramref name="stack"/>, and returns the stack as it stands once the
    /// walk ends (an annotation left open stays on it, so a caller walking a unit's later segments sees
    /// it as still in effect). Mirrors <c>InlineTranslatability.Walk</c> (5.2); the completeness rule
    /// (5.1) calls this once per segment's source content, carrying the stack across the unit.
    /// </summary>
    /// <param name="parts">The parts to walk.</param>
    /// <param name="stack">The incoming stack of nested <c>translate</c> overrides, innermost on top; empty means translatable.</param>
    /// <returns>The translatable text found, and the stack once the walk ends.</returns>
    private static (string Text, ImmutableStack<bool> Stack) WalkTranslatable(ImmutableArray<RenderPart> parts, ImmutableStack<bool> stack)
    {
        var text = new StringBuilder();
        foreach(RenderPart part in parts)
        {
            string? contribution = part switch
            {
                RenderTextPart textPart => textPart.Text,
                RenderPlaceholderPart placeholder => placeholder.Equiv,
                RenderStartCodePart startCode => startCode.Equiv,
                RenderEndCodePart endCode => endCode.Equiv,
                _ => null
            };

            if(contribution is not null && CurrentlyTranslatable(stack))
            {
                text.Append(contribution);
            }

            stack = part switch
            {
                RenderAnnotationStartPart annotationStart => stack.Push(annotationStart.Translate ?? CurrentlyTranslatable(stack)),
                RenderAnnotationEndPart when !stack.IsEmpty => stack.Pop(),
                _ => stack
            };
        }

        return (text.ToString(), stack);
    }

    /// <summary>Whether text at the current nesting level is translatable: the top of <paramref name="stack"/>, or true (the seed) when it is empty.</summary>
    private static bool CurrentlyTranslatable(ImmutableStack<bool> stack) => stack.IsEmpty || stack.Peek();

    /// <summary>
    /// Reads the unit's <c>&lt;originalData&gt;</c> element, if any, into a lookup from a
    /// <c>&lt;data&gt;</c> id to its decoded text. XLIFF 2.1 §4.2.2.5 allows at most one
    /// <c>&lt;originalData&gt;</c> per unit; the generator honors only the first found and does not
    /// refuse a second (not in the required structural-fault list, 5.7). A <c>&lt;data&gt;</c> entry
    /// with no id can never be referenced by a <c>dataRef</c>, so, like an entry no code references, it
    /// is simply dropped rather than refused.
    /// </summary>
    /// <param name="unitElement">The <c>&lt;unit&gt;</c> element.</param>
    /// <param name="unitId">The unit's id, for failure messages.</param>
    /// <param name="data">The id-to-text lookup; empty when the unit carries no usable <c>&lt;originalData&gt;</c>.</param>
    /// <param name="failure">The failure message naming an invalid <c>hex</c> value inside a <c>&lt;data&gt;</c> entry, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> if every <c>&lt;data&gt;</c> entry's text decoded; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseOriginalData(XElement unitElement, string unitId, out ImmutableDictionary<string, string> data, out string? failure)
    {
        //WellKnownXliffNamespaces.Core is a bare string; it must go through the XNamespace conversion
        //before combining with a local name, or "+" would just concatenate the two strings (and the
        //result, passed to Elements(XName), would fail to parse as a name at all).
        XNamespace core = WellKnownXliffNamespaces.Core;

        XElement? originalDataElement = null;
        foreach(XElement candidate in unitElement.Elements(core + WellKnownXliffElements.OriginalData))
        {
            originalDataElement = candidate;

            break;
        }

        if(originalDataElement is null)
        {
            data = ImmutableDictionary.Create<string, string>(StringComparer.Ordinal);
            failure = null;

            return true;
        }

        ImmutableDictionary<string, string>.Builder builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach(XElement dataElement in originalDataElement.Elements(core + WellKnownXliffElements.Data))
        {
            string? dataId = dataElement.Attribute(WellKnownXliffAttributes.Id)?.Value;
            if(dataId is null)
            {
                continue;
            }

            if(!TryReadDataText(dataElement, unitId, out string text, out failure))
            {
                data = ImmutableDictionary<string, string>.Empty;

                return false;
            }

            builder[dataId] = text;
        }

        data = builder.ToImmutable();
        failure = null;

        return true;
    }

    /// <summary>
    /// Reads a <c>&lt;data&gt;</c> element's text: character data verbatim and <c>&lt;cp&gt;</c>
    /// children decoded into it, in document order. Any other child (a comment, a processing
    /// instruction, or another element) is ignored rather than refused (not in the required
    /// structural-fault list, 5.7).
    /// </summary>
    private static bool TryReadDataText(XElement dataElement, string unitId, out string text, out string? failure)
    {
        var builder = new StringBuilder();
        foreach(XNode node in dataElement.Nodes())
        {
            if(node is XText textNode)
            {
                builder.Append(textNode.Value);

                continue;
            }

            if(node is XElement element && WellKnownXliffNamespaces.IsCore(element.Name.NamespaceName) && WellKnownXliffElements.IsCodePoint(element.Name.LocalName))
            {
                if(!TryDecodeCodePoint(element.Attribute(WellKnownXliffAttributes.Hex)?.Value, unitId, WellKnownXliffElements.CodePoint, out string decoded, out failure))
                {
                    text = string.Empty;

                    return false;
                }

                builder.Append(decoded);
            }
        }

        text = builder.ToString();
        failure = null;

        return true;
    }

    /// <summary>
    /// Refuses a side whose state still has an open, non-isolated <c>&lt;sc&gt;</c> once every segment
    /// of the unit has been parsed (5.3.2); a split annotation left open (an <c>&lt;sm&gt;</c> with no
    /// <c>&lt;em&gt;</c>) is tolerated instead, so no equivalent check runs over
    /// <see cref="InlineSideState.OpenAnnotations"/>.
    /// </summary>
    private static bool TryRequireStartCodesClosedOrIsolated(InlineSideState state, string unitId, string side, out string? failure)
    {
        foreach(KeyValuePair<string, bool> openStart in state.OpenStarts)
        {
            if(!openStart.Value)
            {
                failure = $"Unit '{unitId}' has a <sc> with id '{openStart.Key}' on the {side} side that is never closed by a matching <ec>; mark it isolated=\"yes\" if it truly has none in this unit.";

                return false;
            }
        }

        failure = null;

        return true;
    }

    /// <summary>The base of the flat parts sequence <see cref="RenderMarkup"/>, <see cref="RenderPlain"/> and <see cref="WalkTranslatable"/> consume; a purely local, transient render of one <c>&lt;source&gt;</c> or <c>&lt;target&gt;</c> element, never captured by a cached pipeline model.</summary>
    private abstract record RenderPart;

    /// <summary>A run of text, never empty.</summary>
    /// <param name="Text">The text, verbatim.</param>
    private sealed record RenderTextPart(string Text) : RenderPart;

    /// <summary>A standalone code (<c>ph</c>), pre-resolved to what it contributes to a <see cref="RenderMarkup"/> render.</summary>
    /// <param name="MarkupText">The code's resolved <c>Markup</c>-rendering text: original data, a synthesized tag name, <c>disp</c>, or the escaped <c>equiv</c>.</param>
    /// <param name="MarkupIsTag">Whether <paramref name="MarkupText"/> is an element name to wrap in <c>&lt;name/&gt;</c> rather than text to append as-is.</param>
    /// <param name="Equiv">The code's plain-text stand-in, used by <see cref="RenderPlain"/> and <see cref="WalkTranslatable"/>.</param>
    private sealed record RenderPlaceholderPart(string MarkupText, bool MarkupIsTag, string Equiv) : RenderPart;

    /// <summary>The start of a spanning code (<c>sc</c>, or the opening half of a <c>pc</c>), pre-resolved to what it contributes to a <see cref="RenderMarkup"/> render.</summary>
    /// <param name="MarkupText">The code's resolved markup text; wrapped in <c>&lt;name&gt;</c> when <paramref name="MarkupIsTag"/>.</param>
    /// <param name="MarkupIsTag">Whether <paramref name="MarkupText"/> is an element name to wrap.</param>
    /// <param name="Equiv">The code's plain-text stand-in.</param>
    private sealed record RenderStartCodePart(string MarkupText, bool MarkupIsTag, string Equiv) : RenderPart;

    /// <summary>The end of a spanning code (<c>ec</c>, or the closing half of a <c>pc</c>), pre-resolved to what it contributes to a <see cref="RenderMarkup"/> render.</summary>
    /// <param name="MarkupText">The code's resolved markup text; wrapped in <c>&lt;/name&gt;</c> when <paramref name="MarkupIsTag"/>.</param>
    /// <param name="MarkupIsTag">Whether <paramref name="MarkupText"/> is an element name to wrap.</param>
    /// <param name="Equiv">The code's plain-text stand-in.</param>
    private sealed record RenderEndCodePart(string MarkupText, bool MarkupIsTag, string Equiv) : RenderPart;

    /// <summary>The start of an annotation (<c>mrk</c> opening, or <c>sm</c>); renders nothing itself, but mutates the <see cref="WalkTranslatable"/> stack.</summary>
    /// <param name="Translate">Whether the annotation's content is translatable, or null when the attribute is absent (inherited).</param>
    private sealed record RenderAnnotationStartPart(bool? Translate) : RenderPart;

    /// <summary>The end of an annotation (<c>mrk</c> closing, or <c>em</c>); renders nothing itself, but pops the <see cref="WalkTranslatable"/> stack.</summary>
    private sealed record RenderAnnotationEndPart : RenderPart;

    /// <summary>
    /// Per-side inline-content parse state, carried across a unit's segments in document order (5.3.2).
    /// Mirrors the core reader's <c>InlineParseState</c>, pruned to the cross-side reuse exemption the
    /// generator does not implement (not in the required structural-fault list, 5.7): this class refuses
    /// an id reused anywhere on the same side, full stop.
    /// </summary>
    /// <param name="Ids">The inline ids already used on this side.</param>
    /// <param name="OpenStarts">The ids of <c>&lt;sc&gt;</c> elements opened on this side not yet closed by a matching <c>&lt;ec&gt;</c>, mapped to whether they were opened with <c>isolated="yes"</c>.</param>
    /// <param name="OpenAnnotations">The ids of <c>&lt;sm&gt;</c> elements opened on this side not yet closed by a matching <c>&lt;em&gt;</c>.</param>
    private sealed record InlineSideState(ImmutableHashSet<string> Ids, ImmutableDictionary<string, bool> OpenStarts, ImmutableHashSet<string> OpenAnnotations)
    {
        /// <summary>The state a unit's side starts from, before any of its segments are parsed.</summary>
        public static InlineSideState Empty { get; } = new(
            ImmutableHashSet.Create<string>(StringComparer.Ordinal),
            ImmutableDictionary.Create<string, bool>(StringComparer.Ordinal),
            ImmutableHashSet.Create<string>(StringComparer.Ordinal));
    }
}
