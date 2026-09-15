using System.Collections.Immutable;
using System.Globalization;
using System.Xml;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Units;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// The half of <see cref="XliffWriter"/> that writes a <c>&lt;source&gt;</c>/<c>&lt;target&gt;</c>
/// element's <see cref="InlineContent"/> and a unit's <c>&lt;originalData&gt;</c>, and the unit- and
/// side-level inline content problems, per the design at XLIFF 2.1 §4.7 Inline Content, §4.2.3 Inline
/// Elements and §4.3.1 Attributes.
/// </summary>
public static partial class XliffWriter
{
    /// <summary>
    /// Limits combined Paired code and Marker annotation nesting to 64 levels, matching the reader's
    /// MaxGroupDepth choice so accepted models keep recursive serialization safely bounded.
    /// </summary>
    private const int MaxInlineNestingDepth = 64;

    /// <summary>
    /// Writes a <c>&lt;source&gt;</c> or <c>&lt;target&gt;</c> element in mixed-content mode: an empty
    /// string is written immediately after the start tag (<see cref="XmlWriter.WriteString(string)"/>
    /// with <see cref="string.Empty"/> reaches the raw writer and switches it to mixed content) so the
    /// indenting writer never treats an element-first or element-only child as reason to inject
    /// whitespace before it (5.4). Doing this even for text-first content, such as a plain string with
    /// no codes, costs nothing: an empty string emits no bytes, so the existing byte-exact golden
    /// output is unchanged.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="elementName">The local name of the element to write (<c>source</c> or <c>target</c>).</param>
    /// <param name="parts">The content's parts, in document order.</param>
    private static void WriteInlineContent(XmlWriter writer, string elementName, ImmutableArray<InlinePart> parts)
    {
        writer.WriteStartElement(elementName);
        writer.WriteString(string.Empty);
        WriteContentParts(writer, parts, 0, parts.Length, 0);
        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes the parts of one content run between <paramref name="index"/> (inclusive) and
    /// <paramref name="endExclusive"/> (exclusive): text (<c>cp</c>-encoded per
    /// <see cref="WriteInlineText"/>), a standalone code, a <c>Split</c> code or annotation half as its
    /// own self-closed element, and a <c>Paired</c> start or a <c>Marker</c> annotation start as a real
    /// nesting element whose matching end is found with <see cref="FindMatchingPairedEnd"/>/
    /// <see cref="FindMatchingMarkerEnd"/> and recursed into. This assumes the content already passed
    /// the unit's problems: every <c>Paired</c> start and <c>Marker</c> start has its matching end
    /// properly nested within <paramref name="endExclusive"/>, so the two lookups always succeed and a
    /// bare <c>Paired</c>/<c>Marker</c> end is never reached by the loop itself. The recursion
    /// <paramref name="depth"/> cannot exceed <see cref="MaxInlineNestingDepth"/> after validation.
    /// </summary>
    private static void WriteContentParts(XmlWriter writer, ImmutableArray<InlinePart> parts, int index, int endExclusive, int depth)
    {
        if(depth > MaxInlineNestingDepth)
        {
            throw new InvalidOperationException("Inline content exceeds the maximum nesting depth; the document must be validated before writing.");
        }

        while(index < endExclusive)
        {
            switch(parts[index])
            {
                case(InlineTextPart text):
                {
                    WriteInlineText(writer, text.Text);
                    index++;

                    break;
                }

                case(PlaceholderPart placeholder):
                {
                    WritePlaceholder(writer, placeholder);
                    index++;

                    break;
                }

                case(StartCodePart startCode) when startCode.Form == SpanForm.Split:
                {
                    WriteStartCode(writer, startCode);
                    index++;

                    break;
                }

                case(StartCodePart pairedStart):
                {
                    int endIndex = FindMatchingPairedEnd(parts, index);
                    var pairedEnd = (EndCodePart)parts[endIndex];
                    WritePairedCodeStart(writer, pairedStart, pairedEnd);
                    writer.WriteString(string.Empty);
                    WriteContentParts(writer, parts, index + 1, endIndex, depth + 1);
                    writer.WriteEndElement();
                    index = endIndex + 1;

                    break;
                }

                case(EndCodePart endCode) when endCode.Form == SpanForm.Split:
                {
                    WriteEndCode(writer, endCode);
                    index++;

                    break;
                }

                case(AnnotationStartPart startMarker) when startMarker.Form == AnnotationForm.Split:
                {
                    WriteStartMarker(writer, startMarker);
                    index++;

                    break;
                }

                case(AnnotationStartPart markerStart):
                {
                    int endIndex = FindMatchingMarkerEnd(parts, index);
                    WriteMarkerStart(writer, markerStart);
                    writer.WriteString(string.Empty);
                    WriteContentParts(writer, parts, index + 1, endIndex, depth + 1);
                    writer.WriteEndElement();
                    index = endIndex + 1;

                    break;
                }

                case(AnnotationEndPart endMarker) when endMarker.Form == AnnotationForm.Split:
                {
                    WriteEndMarker(writer, endMarker);
                    index++;

                    break;
                }

                //A bare Paired end or Marker end (not consumed above as the match of an earlier
                //start) means the content was never validated: the unit's problems refuse that
                //model before the writer ever serializes it (5.4's nesting rule).
                default:
                {
                    throw new InvalidOperationException($"An inline part of type '{parts[index].GetType().Name}' at position {index.ToString(CultureInfo.InvariantCulture)} has no matching Paired or Marker start; the document must be validated before writing.");
                }
            }
        }
    }

    /// <summary>
    /// Finds the index of the <see cref="EndCodePart"/> that closes the <see cref="SpanForm.Paired"/>
    /// <see cref="StartCodePart"/> at <paramref name="startIndex"/>, by depth-counting only the
    /// <c>Paired</c> starts and ends met along the way (a <c>Marker</c> annotation nested inside, or
    /// wrapping, the span does not affect the count, since proper nesting keeps it entirely on one
    /// side).
    /// </summary>
    private static int FindMatchingPairedEnd(ImmutableArray<InlinePart> parts, int startIndex)
    {
        int depth = 1;
        for(int index = startIndex + 1; index < parts.Length; index++)
        {
            if(parts[index] is StartCodePart { Form: SpanForm.Paired })
            {
                depth++;
            }
            else if(parts[index] is EndCodePart { Form: SpanForm.Paired })
            {
                depth--;
                if(depth == 0)
                {
                    return index;
                }
            }
        }

        throw new InvalidOperationException("A Paired start has no matching end in its content; the document must be validated before writing.");
    }

    /// <summary>Finds the index of the <see cref="AnnotationEndPart"/> that closes the <see cref="AnnotationForm.Marker"/> <see cref="AnnotationStartPart"/> at <paramref name="startIndex"/>, the same way <see cref="FindMatchingPairedEnd"/> does for a <c>Paired</c> code.</summary>
    private static int FindMatchingMarkerEnd(ImmutableArray<InlinePart> parts, int startIndex)
    {
        int depth = 1;
        for(int index = startIndex + 1; index < parts.Length; index++)
        {
            if(parts[index] is AnnotationStartPart { Form: AnnotationForm.Marker })
            {
                depth++;
            }
            else if(parts[index] is AnnotationEndPart { Form: AnnotationForm.Marker })
            {
                depth--;
                if(depth == 0)
                {
                    return index;
                }
            }
        }

        throw new InvalidOperationException("A Marker annotation start has no matching end in its content; the document must be validated before writing.");
    }

    /// <summary>Writes a standalone code, a <c>&lt;ph&gt;</c> element, self-closed.</summary>
    private static void WritePlaceholder(XmlWriter writer, PlaceholderPart part)
    {
        writer.WriteStartElement(WellKnownXliffElements.Placeholder);
        writer.WriteAttributeString(WellKnownXliffAttributes.Id, part.Id);
        WriteCodeTypeIfPresent(writer, part.Type);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubType, part.SubType);
        WriteIfNotNull(writer, WellKnownXliffAttributes.DataRef, part.DataRef);
        WriteIfNotEmpty(writer, WellKnownXliffAttributes.Equiv, part.Equiv);
        WriteIfNotNull(writer, WellKnownXliffAttributes.Disp, part.Disp);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanCopy, part.CanCopy, defaultValue: true);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanDelete, part.CanDelete, defaultValue: true);
        WriteReorderIfNotDefault(writer, part.CanReorder);
        WriteIfNotNull(writer, WellKnownXliffAttributes.CopyOf, part.CopyOf);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubFlows, part.SubFlows);
        writer.WriteEndElement();
    }

    /// <summary>Writes the start half of a split spanning code, an <c>&lt;sc&gt;</c> element, self-closed.</summary>
    private static void WriteStartCode(XmlWriter writer, StartCodePart part)
    {
        writer.WriteStartElement(WellKnownXliffElements.StartCode);
        writer.WriteAttributeString(WellKnownXliffAttributes.Id, part.Id);
        WriteCodeTypeIfPresent(writer, part.Type);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubType, part.SubType);
        WriteDirectionIfNotInherited(writer, part.Direction);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.Isolated, part.Isolated, defaultValue: false);
        WriteIfNotNull(writer, WellKnownXliffAttributes.DataRef, part.DataRef);
        WriteIfNotEmpty(writer, WellKnownXliffAttributes.Equiv, part.Equiv);
        WriteIfNotNull(writer, WellKnownXliffAttributes.Disp, part.Disp);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanCopy, part.CanCopy, defaultValue: true);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanDelete, part.CanDelete, defaultValue: true);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanOverlap, part.CanOverlap, defaultValue: true);
        WriteReorderIfNotDefault(writer, part.CanReorder);
        WriteIfNotNull(writer, WellKnownXliffAttributes.CopyOf, part.CopyOf);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubFlows, part.SubFlows);
        writer.WriteEndElement();
    }

    /// <summary>Writes the end half of a split spanning code, an <c>&lt;ec&gt;</c> element, self-closed: <c>id</c>/<c>isolated="yes"</c> when <see cref="EndCodePart.Isolated"/>, otherwise <c>startRef</c>. <c>dir</c> is written only when <see cref="EndCodePart.Isolated"/>, per XLIFF 2.1 §4.2.3.5: <c>dir</c> MAY be used if and only if <c>isolated="yes"</c>.</summary>
    private static void WriteEndCode(XmlWriter writer, EndCodePart part)
    {
        writer.WriteStartElement(WellKnownXliffElements.EndCode);
        if(part.Isolated)
        {
            writer.WriteAttributeString(WellKnownXliffAttributes.Id, part.Id ?? string.Empty);
            writer.WriteAttributeString(WellKnownXliffAttributes.Isolated, WellKnownXliffAttributeValues.Yes);
            WriteDirectionIfNotInherited(writer, part.Direction);
        }
        else
        {
            writer.WriteAttributeString(WellKnownXliffAttributes.StartRef, part.StartRef ?? string.Empty);
        }

        WriteCodeTypeIfPresent(writer, part.Type);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubType, part.SubType);
        WriteIfNotNull(writer, WellKnownXliffAttributes.DataRef, part.DataRef);
        WriteIfNotEmpty(writer, WellKnownXliffAttributes.Equiv, part.Equiv);
        WriteIfNotNull(writer, WellKnownXliffAttributes.Disp, part.Disp);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanCopy, part.CanCopy, defaultValue: true);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanDelete, part.CanDelete, defaultValue: true);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanOverlap, part.CanOverlap, defaultValue: true);
        WriteReorderIfNotDefault(writer, part.CanReorder);
        WriteIfNotNull(writer, WellKnownXliffAttributes.CopyOf, part.CopyOf);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubFlows, part.SubFlows);
        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes the open <c>&lt;pc&gt;</c> tag for a well-formed spanning code, mapping the two halves'
    /// attributes per table 2 (XLIFF 2.1 §4.7.2.2): the shared attributes (<c>id</c>, <c>type</c>,
    /// <c>subType</c>, <c>dir</c>, <c>canCopy</c>, <c>canDelete</c>, <c>canReorder</c>, <c>copyOf</c>,
    /// <c>canOverlap</c>) come from <paramref name="start"/>; the half-specific ones become
    /// <c>...Start</c>/<c>...End</c> pairs from <paramref name="start"/> and <paramref name="end"/>
    /// respectively. The caller writes the wrapped content and the closing <c>&lt;/pc&gt;</c>.
    /// </summary>
    private static void WritePairedCodeStart(XmlWriter writer, StartCodePart start, EndCodePart end)
    {
        writer.WriteStartElement(WellKnownXliffElements.PairedCode);
        writer.WriteAttributeString(WellKnownXliffAttributes.Id, start.Id);
        WriteCodeTypeIfPresent(writer, start.Type);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubType, start.SubType);
        WriteDirectionIfNotInherited(writer, start.Direction);
        WriteIfNotNull(writer, WellKnownXliffAttributes.DataRefStart, start.DataRef);
        WriteIfNotNull(writer, WellKnownXliffAttributes.DataRefEnd, end.DataRef);
        WriteIfNotEmpty(writer, WellKnownXliffAttributes.EquivStart, start.Equiv);
        WriteIfNotEmpty(writer, WellKnownXliffAttributes.EquivEnd, end.Equiv);
        WriteIfNotNull(writer, WellKnownXliffAttributes.DispStart, start.Disp);
        WriteIfNotNull(writer, WellKnownXliffAttributes.DispEnd, end.Disp);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanCopy, start.CanCopy, defaultValue: true);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanDelete, start.CanDelete, defaultValue: true);
        WriteYesNoIfNotDefault(writer, WellKnownXliffAttributes.CanOverlap, start.CanOverlap, defaultValue: false);
        WriteReorderIfNotDefault(writer, start.CanReorder);
        WriteIfNotNull(writer, WellKnownXliffAttributes.CopyOf, start.CopyOf);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubFlowsStart, start.SubFlows);
        WriteIfNotNull(writer, WellKnownXliffAttributes.SubFlowsEnd, end.SubFlows);
    }

    /// <summary>Writes the open <c>&lt;mrk&gt;</c> tag for a wrapping annotation. The caller writes the wrapped content and the closing <c>&lt;/mrk&gt;</c>.</summary>
    private static void WriteMarkerStart(XmlWriter writer, AnnotationStartPart part)
    {
        writer.WriteStartElement(WellKnownXliffElements.Marker);
        WriteAnnotationAttributes(writer, part);
    }

    /// <summary>Writes an <c>&lt;sm&gt;</c> element, self-closed: the start of a split annotation.</summary>
    private static void WriteStartMarker(XmlWriter writer, AnnotationStartPart part)
    {
        writer.WriteStartElement(WellKnownXliffElements.StartMarker);
        WriteAnnotationAttributes(writer, part);
        writer.WriteEndElement();
    }

    /// <summary>Writes the <c>id</c>, <c>type</c> (omitted when <c>generic</c>, the default), <c>translate</c>, <c>ref</c> and <c>value</c> attributes <c>&lt;mrk&gt;</c> and <c>&lt;sm&gt;</c> share.</summary>
    private static void WriteAnnotationAttributes(XmlWriter writer, AnnotationStartPart part)
    {
        writer.WriteAttributeString(WellKnownXliffAttributes.Id, part.Id);
        if(!WellKnownXliffAttributeValues.IsGeneric(part.Type))
        {
            writer.WriteAttributeString(WellKnownXliffAttributes.Type, part.Type);
        }

        if(part.Translate is { } translate)
        {
            writer.WriteAttributeString(WellKnownXliffAttributes.Translate, translate ? WellKnownXliffAttributeValues.Yes : WellKnownXliffAttributeValues.No);
        }

        WriteIfNotNull(writer, WellKnownXliffAttributes.Ref, part.Ref);
        WriteIfNotNull(writer, WellKnownXliffAttributes.Value, part.Value);
    }

    /// <summary>Writes an <c>&lt;em&gt;</c> element, self-closed: the end of a split annotation, naming the <c>&lt;sm&gt;</c> it closes by <c>startRef</c>.</summary>
    private static void WriteEndMarker(XmlWriter writer, AnnotationEndPart part)
    {
        writer.WriteStartElement(WellKnownXliffElements.EndMarker);
        writer.WriteAttributeString(WellKnownXliffAttributes.StartRef, part.StartRef);
        writer.WriteEndElement();
    }

    /// <summary>
    /// Writes a text run, encoding every UTF-16 code unit for which <see cref="XmlConvert.IsXmlChar(char)"/>
    /// is false and that is not the first half of a valid surrogate pair (a high surrogate immediately
    /// followed by a low surrogate) as <c>&lt;cp hex="XXXX"/&gt;</c>, four uppercase hexadecimal digits;
    /// a valid surrogate pair is written as the two code units it is, verbatim, since together they are
    /// one valid XML character above U+FFFF. Everything else is written as text, batched between
    /// encoded code points into as few <see cref="XmlWriter.WriteString(string)"/> calls as possible.
    /// </summary>
    private static void WriteInlineText(XmlWriter writer, string text)
    {
        int start = 0;
        int index = 0;
        while(index < text.Length)
        {
            char current = text[index];
            bool validPair = char.IsHighSurrogate(current) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]);
            if(validPair)
            {
                index += 2;

                continue;
            }

            if(XmlConvert.IsXmlChar(current))
            {
                index += 1;

                continue;
            }

            if(index > start)
            {
                writer.WriteString(text.Substring(start, index - start));
            }

            writer.WriteStartElement(WellKnownXliffElements.CodePoint);
            writer.WriteAttributeString(WellKnownXliffAttributes.Hex, ((int)current).ToString("X4", CultureInfo.InvariantCulture));
            writer.WriteEndElement();
            index += 1;
            start = index;
        }

        if(index > start)
        {
            writer.WriteString(text.Substring(start, index - start));
        }
    }

    /// <summary>
    /// Writes the unit's &lt;originalData&gt; element: one &lt;data&gt; per entry, in the
    /// first-appearance order <see cref="CollectOriginalData(XliffUnit)"/> already produced, its text
    /// <c>cp</c>-encoded (<see cref="WriteInlineText"/>) and its <c>dir</c> written only when not
    /// <see cref="TextDirection.Auto"/>, <c>data</c>'s own default (XLIFF 2.1 §4.3.1.12).
    /// </summary>
    private static void WriteOriginalDataElement(XmlWriter writer, ImmutableArray<(string Id, OriginalData Data)> entries)
    {
        writer.WriteStartElement(WellKnownXliffElements.OriginalData);
        foreach((string id, OriginalData data) in entries)
        {
            writer.WriteStartElement(WellKnownXliffElements.Data);
            writer.WriteAttributeString(WellKnownXliffAttributes.Id, id);
            if(data.Direction != TextDirection.Auto)
            {
                writer.WriteAttributeString(WellKnownXliffAttributes.Dir, DirectionAttributeValue(data.Direction));
            }

            writer.WriteString(string.Empty);
            WriteInlineText(writer, data.Text);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Collects the unit's distinct <c>dataRef</c> ids and their resolved <see cref="OriginalData"/>,
    /// in first-appearance order across the unit's segments, source before target within a segment
    /// (5.4). Assumes the unit's problems already refused a <c>dataRef</c> with no resolved data
    /// anywhere in the unit and conflicting data for one <c>dataRef</c>
    /// (<see cref="OriginalDataConsistencyProblems(XliffUnit)"/>), so the first non-null
    /// <see cref="OriginalData"/> found for an id is the one every part sharing it agrees on.
    /// </summary>
    private static ImmutableArray<(string Id, OriginalData Data)> CollectOriginalData(XliffUnit unit)
    {
        var order = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var resolved = new Dictionary<string, OriginalData>(StringComparer.Ordinal);
        foreach(XliffSegment segment in unit.Segments)
        {
            CollectOriginalData(segment.SourceContent.Parts, order, seen, resolved);
            if(segment.TargetContent is not null)
            {
                CollectOriginalData(segment.TargetContent.Parts, order, seen, resolved);
            }
        }

        ImmutableArray<(string, OriginalData)>.Builder entries = ImmutableArray.CreateBuilder<(string, OriginalData)>(order.Count);
        foreach(string id in order)
        {
            entries.Add((id, resolved[id]));
        }

        return entries.ToImmutable();
    }

    /// <summary>The half of <see cref="CollectOriginalData(XliffUnit)"/> that walks one content's parts, recording each <c>dataRef</c>'s first-appearance position and its resolved data.</summary>
    private static void CollectOriginalData(ImmutableArray<InlinePart> parts, List<string> order, HashSet<string> seen, Dictionary<string, OriginalData> resolved)
    {
        foreach(InlinePart part in parts)
        {
            (string? dataRef, OriginalData? data) = DataReferenceOf(part);
            if(dataRef is null)
            {
                continue;
            }

            if(seen.Add(dataRef))
            {
                order.Add(dataRef);
            }

            if(data is not null && !resolved.ContainsKey(dataRef))
            {
                resolved[dataRef] = data;
            }
        }
    }

    /// <summary>Extracts a part's <c>dataRef</c> id and resolved <see cref="OriginalData"/>, or <c>(null, null)</c> for a part kind that carries neither (a text part or an annotation part).</summary>
    private static (string? DataRef, OriginalData? Data) DataReferenceOf(InlinePart part)
    {
        return part switch
        {
            PlaceholderPart placeholder => (placeholder.DataRef, placeholder.OriginalData),
            StartCodePart start => (start.DataRef, start.OriginalData),
            EndCodePart end => (end.DataRef, end.OriginalData),
            _ => (null, null)
        };
    }

    /// <summary>Writes <paramref name="name"/>="<paramref name="value"/>" when <paramref name="value"/> is not null; the spec default for every attribute this is used for is "absent".</summary>
    private static void WriteIfNotNull(XmlWriter writer, string name, string? value)
    {
        if(value is not null)
        {
            writer.WriteAttributeString(name, value);
        }
    }

    /// <summary>Writes <paramref name="name"/>="<paramref name="value"/>" when <paramref name="value"/> is not empty; the spec default for <c>equiv</c>/<c>equivStart</c>/<c>equivEnd</c> is empty.</summary>
    private static void WriteIfNotEmpty(XmlWriter writer, string name, string value)
    {
        if(value.Length != 0)
        {
            writer.WriteAttributeString(name, value);
        }
    }

    /// <summary>Writes <paramref name="name"/> as <c>yes</c>/<c>no</c> only when <paramref name="value"/> differs from <paramref name="defaultValue"/> (which varies by attribute and, for <c>canOverlap</c>, by <see cref="SpanForm"/>).</summary>
    private static void WriteYesNoIfNotDefault(XmlWriter writer, string name, bool value, bool defaultValue)
    {
        if(value != defaultValue)
        {
            writer.WriteAttributeString(name, value ? WellKnownXliffAttributeValues.Yes : WellKnownXliffAttributeValues.No);
        }
    }

    /// <summary>Writes <c>canReorder</c> only when it is not <see cref="ReorderHint.Yes"/>, the spec default.</summary>
    private static void WriteReorderIfNotDefault(XmlWriter writer, ReorderHint hint)
    {
        string? value = hint switch
        {
            ReorderHint.Yes => null,
            ReorderHint.FirstNo => WellKnownXliffAttributeValues.FirstNo,
            ReorderHint.No => WellKnownXliffAttributeValues.No,
            _ => throw new ArgumentOutOfRangeException(nameof(hint), hint, "Unknown reorder hint.")
        };

        WriteIfNotNull(writer, WellKnownXliffAttributes.CanReorder, value);
    }

    /// <summary>Writes a code's <c>dir</c> only when it is not <see cref="TextDirection.Inherited"/>, the spec default for a code (as opposed to <see cref="OriginalData"/>, whose own default is <see cref="TextDirection.Auto"/>; see <see cref="WriteOriginalDataElement"/>).</summary>
    private static void WriteDirectionIfNotInherited(XmlWriter writer, TextDirection direction)
    {
        if(direction != TextDirection.Inherited)
        {
            writer.WriteAttributeString(WellKnownXliffAttributes.Dir, DirectionAttributeValue(direction));
        }
    }

    /// <summary>Maps <see cref="TextDirection.LeftToRight"/>/<see cref="TextDirection.RightToLeft"/>/<see cref="TextDirection.Auto"/> to the <c>ltr</c>/<c>rtl</c>/<c>auto</c> XML value; <see cref="TextDirection.Inherited"/> has no XML value of its own (it means the attribute is omitted) and is never passed here.</summary>
    private static string DirectionAttributeValue(TextDirection direction)
    {
        return direction switch
        {
            TextDirection.LeftToRight => WellKnownXliffAttributeValues.Ltr,
            TextDirection.RightToLeft => WellKnownXliffAttributeValues.Rtl,
            TextDirection.Auto => WellKnownXliffAttributeValues.Auto,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "A direction of Inherited, or an undefined value, has no dir attribute value.")
        };
    }

    /// <summary>Writes a code's <c>type</c> only when it is not <see cref="InlineCodeType.None"/>, the spec default (the attribute absent).</summary>
    private static void WriteCodeTypeIfPresent(XmlWriter writer, InlineCodeType type)
    {
        string? value = type switch
        {
            InlineCodeType.None => null,
            InlineCodeType.Format => WellKnownXliffAttributeValues.Fmt,
            InlineCodeType.UserInterface => WellKnownXliffAttributeValues.Ui,
            InlineCodeType.Quote => WellKnownXliffAttributeValues.Quote,
            InlineCodeType.Link => WellKnownXliffAttributeValues.Link,
            InlineCodeType.Image => WellKnownXliffAttributeValues.Image,
            InlineCodeType.Other => WellKnownXliffAttributeValues.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown inline code type.")
        };

        WriteIfNotNull(writer, WellKnownXliffAttributes.Type, value);
    }

    /// <summary>
    /// Yields every unit- and side-level inline content problem (5.4): duplicate inline ids, improper
    /// <c>Paired</c>/<c>Marker</c> nesting, <c>Split</c> code and annotation pairing across the unit's
    /// segments, and <c>OriginalData</c> consistency. Per-part attribute problems (name-token shape and
    /// XML-text validity) are reported alongside each segment instead, by
    /// <see cref="InlinePartFieldProblems"/>, since they need no cross-segment state.
    /// </summary>
    private static IEnumerable<string> InlineUnitProblems(XliffUnit unit)
    {
        foreach(string problem in DuplicateInlineIdProblems(unit))
        {
            yield return problem;
        }

        foreach(string problem in PairedAndMarkerNestingProblems(unit))
        {
            yield return problem;
        }

        foreach(string problem in SplitPairingProblems(unit, isTarget: false))
        {
            yield return problem;
        }

        foreach(string problem in SplitPairingProblems(unit, isTarget: true))
        {
            yield return problem;
        }

        foreach(string problem in OriginalDataConsistencyProblems(unit))
        {
            yield return problem;
        }
    }

    /// <summary>
    /// Yields a problem for every inline id used more than once on the same side of the unit, or
    /// shared with a segment/ignorable id, mirroring the reader's own rule (XLIFF 2.1 §4.3.1.21): a
    /// target element may reuse only its own sibling source element's id from this same segment; every
    /// other collision, in either direction and including with an earlier or later segment's ids, is a
    /// problem.
    /// </summary>
    private static IEnumerable<string> DuplicateInlineIdProblems(XliffUnit unit)
    {
        var segmentScopeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach(XliffSegment segment in unit.Segments)
        {
            if(segment.Id is not null)
            {
                segmentScopeIds.Add(segment.Id);
            }
        }

        var sourceSeen = new HashSet<string>(StringComparer.Ordinal);
        var targetSeen = new HashSet<string>(StringComparer.Ordinal);
        foreach(XliffSegment segment in unit.Segments)
        {
            ImmutableArray<string> sourceIds = InlineIdsOf(segment.SourceContent.Parts);
            foreach(string id in sourceIds)
            {
                bool freshOnSource = sourceSeen.Add(id);
                if(segmentScopeIds.Contains(id) || !freshOnSource || targetSeen.Contains(id))
                {
                    yield return DuplicateInlineIdMessage(id, unit.Id);
                }
            }

            if(segment.TargetContent is null)
            {
                continue;
            }

            foreach(string id in InlineIdsOf(segment.TargetContent.Parts))
            {
                bool freshOnTarget = targetSeen.Add(id);
                bool exempt = sourceIds.Contains(id);
                if(segmentScopeIds.Contains(id) || !freshOnTarget || (!exempt && sourceSeen.Contains(id)))
                {
                    yield return DuplicateInlineIdMessage(id, unit.Id);
                }
            }
        }
    }

    /// <summary>The message a duplicate inline id problem carries, shared by every place that finds one.</summary>
    private static string DuplicateInlineIdMessage(string id, string unitId) =>
        $"The inline id '{id}' is used more than once in unit '{unitId}'; inline ids, and segment/ignorable ids, must be unique within the unit, except a target element reusing its own sibling source element's id.";

    /// <summary>Extracts the ids an inline content's parts introduce (the same set the reader registers): a placeholder's, a start code's (whether <c>Paired</c> or <c>Split</c>), an isolated end code's own id, and an annotation start's (whether <c>Marker</c> or <c>Split</c>).</summary>
    private static ImmutableArray<string> InlineIdsOf(ImmutableArray<InlinePart> parts)
    {
        ImmutableArray<string>.Builder ids = ImmutableArray.CreateBuilder<string>();
        foreach(InlinePart part in parts)
        {
            string? id = part switch
            {
                PlaceholderPart placeholder => placeholder.Id,
                StartCodePart start => start.Id,
                EndCodePart { Isolated: true } end => end.Id,
                AnnotationStartPart annotationStart => annotationStart.Id,
                _ => null
            };

            if(id is not null)
            {
                ids.Add(id);
            }
        }

        return ids.ToImmutable();
    }

    /// <summary>
    /// Yields a problem for every <c>Paired</c> code or <c>Marker</c> annotation that has no matching
    /// end in the same content, or that crosses another such span instead of nesting inside or around
    /// it (XLIFF 2.1 §4.7.2.2, §4.7.3.2: these two forms must nest properly; <c>Split</c> forms may
    /// overlap and are not checked here), and, for a matched <c>Paired</c> pair, the problems of
    /// <see cref="PairedHalvesProblems"/>. Scoped per segment's content, since a <c>pc</c> or <c>mrk</c>
    /// element cannot itself cross a segment boundary.
    /// </summary>
    private static IEnumerable<string> PairedAndMarkerNestingProblems(XliffUnit unit)
    {
        foreach(XliffSegment segment in unit.Segments)
        {
            foreach(string problem in ContentNestingProblems(segment.SourceContent.Parts, $"the source of unit '{unit.Id}'"))
            {
                yield return problem;
            }

            if(segment.TargetContent is not null)
            {
                foreach(string problem in ContentNestingProblems(segment.TargetContent.Parts, $"the target of unit '{unit.Id}'"))
                {
                    yield return problem;
                }
            }
        }
    }

    /// <summary>The per-content half of <see cref="PairedAndMarkerNestingProblems"/>: a stack-based well-formedness walk over one content's parts.</summary>
    private static IEnumerable<string> ContentNestingProblems(ImmutableArray<InlinePart> parts, string what)
    {
        var stack = new Stack<InlinePart>();
        for(int index = 0; index < parts.Length; index++)
        {
            InlinePart part = parts[index];
            if(stack.Count == MaxInlineNestingDepth
                && part is StartCodePart { Form: SpanForm.Paired } or AnnotationStartPart { Form: AnnotationForm.Marker })
            {
                yield return $"The inline part at position {index.ToString(CultureInfo.InvariantCulture)} in {what} exceeds the maximum inline nesting depth of {MaxInlineNestingDepth.ToString(CultureInfo.InvariantCulture)}.";

                yield break;
            }

            switch(part)
            {
                case(StartCodePart start) when start.Form == SpanForm.Paired:
                {
                    stack.Push(start);

                    break;
                }

                case(AnnotationStartPart start) when start.Form == AnnotationForm.Marker:
                {
                    stack.Push(start);

                    break;
                }

                case(EndCodePart end) when end.Form == SpanForm.Paired:
                {
                    if(stack.Count == 0 || stack.Peek() is not StartCodePart openStart || !string.Equals(openStart.Id, end.StartRef, StringComparison.Ordinal))
                    {
                        yield return $"A </pc> in {what} does not close the innermost open span; Paired pairs and Marker annotations must nest properly (XLIFF 2.1 §4.7.2.2).";
                    }
                    else
                    {
                        stack.Pop();
                        foreach(string problem in PairedHalvesProblems(openStart, end, what))
                        {
                            yield return problem;
                        }
                    }

                    break;
                }

                case(AnnotationEndPart end) when end.Form == AnnotationForm.Marker:
                {
                    if(stack.Count == 0 || stack.Peek() is not AnnotationStartPart openMarker || !string.Equals(openMarker.Id, end.StartRef, StringComparison.Ordinal))
                    {
                        yield return $"A </mrk> in {what} does not close the innermost open span; Paired pairs and Marker annotations must nest properly (XLIFF 2.1 §4.7.3.2).";
                    }
                    else
                    {
                        stack.Pop();
                    }

                    break;
                }
            }
        }

        foreach(InlinePart open in stack)
        {
            string id = open is StartCodePart start ? start.Id : ((AnnotationStartPart)open).Id;

            yield return $"The Paired code or Marker annotation with id '{id}' in {what} has no closing end in the same content.";
        }
    }

    /// <summary>Yields a problem when a matched <c>Paired</c> start is <see cref="StartCodePart.Isolated"/> (a <c>pc</c> span is never isolated) or when its shared attributes disagree with its end's.</summary>
    private static IEnumerable<string> PairedHalvesProblems(StartCodePart start, EndCodePart end, string what)
    {
        if(start.Isolated)
        {
            yield return $"The Paired code with id '{start.Id}' in {what} is isolated; a span from <pc> must never be isolated.";
        }

        bool disagree = start.Type != end.Type
            || !string.Equals(start.SubType, end.SubType, StringComparison.Ordinal)
            || start.CanCopy != end.CanCopy
            || start.CanDelete != end.CanDelete
            || start.CanReorder != end.CanReorder
            || !string.Equals(start.CopyOf, end.CopyOf, StringComparison.Ordinal)
            || start.CanOverlap != end.CanOverlap
            || start.Direction != end.Direction;
        if(disagree)
        {
            yield return $"The Paired code with id '{start.Id}' in {what} has start and end halves that disagree on a shared attribute; a <pc> writes one set of shared attributes, taken from its start half.";
        }
    }

    /// <summary>
    /// Yields a problem for every <c>Split</c> code or annotation end whose <c>startRef</c> names no
    /// open start on the same side of the unit (across the unit's segments in document order,
    /// mirroring the reader), an end that tries to close a <c>Split</c> code start marked
    /// <see cref="StartCodePart.Isolated"/>, and, once every segment is walked, a non-isolated
    /// <c>Split</c> code start still open (an <c>sm</c> left open is tolerated, matching the reader).
    /// </summary>
    private static IEnumerable<string> SplitPairingProblems(XliffUnit unit, bool isTarget)
    {
        var openCodeStarts = new Dictionary<string, bool>(StringComparer.Ordinal);
        var openAnnotationStarts = new HashSet<string>(StringComparer.Ordinal);
        string side = isTarget ? "target" : "source";
        foreach(XliffSegment segment in unit.Segments)
        {
            InlineContent? content = isTarget ? segment.TargetContent : segment.SourceContent;
            if(content is null)
            {
                continue;
            }

            foreach(InlinePart part in content.Parts)
            {
                switch(part)
                {
                    case(StartCodePart start) when start.Form == SpanForm.Split:
                    {
                        openCodeStarts[start.Id] = start.Isolated;

                        break;
                    }

                    case(EndCodePart end) when end.Form == SpanForm.Split && !end.Isolated:
                    {
                        if(end.StartRef is null || !openCodeStarts.TryGetValue(end.StartRef, out bool isolatedStart))
                        {
                            yield return $"An <ec> in the {side} of unit '{unit.Id}' has startRef '{end.StartRef}', which names no open <sc> on that side.";
                        }
                        else if(isolatedStart)
                        {
                            yield return $"An <ec> in the {side} of unit '{unit.Id}' has startRef '{end.StartRef}', which is isolated and must not be closed by an <ec>.";
                        }
                        else
                        {
                            openCodeStarts.Remove(end.StartRef);
                        }

                        break;
                    }

                    case(AnnotationStartPart startMarker) when startMarker.Form == AnnotationForm.Split:
                    {
                        openAnnotationStarts.Add(startMarker.Id);

                        break;
                    }

                    case(AnnotationEndPart endMarker) when endMarker.Form == AnnotationForm.Split:
                    {
                        if(!openAnnotationStarts.Remove(endMarker.StartRef))
                        {
                            yield return $"An <em> in the {side} of unit '{unit.Id}' has startRef '{endMarker.StartRef}', which names no open <sm> on that side.";
                        }

                        break;
                    }
                }
            }
        }

        foreach(KeyValuePair<string, bool> openStart in openCodeStarts)
        {
            if(!openStart.Value)
            {
                yield return $"Unit '{unit.Id}' has a <sc> with id '{openStart.Key}' on the {side} side that is never closed by a matching <ec>; mark it isolated to leave it open.";
            }
        }
    }

    /// <summary>
    /// Yields a problem for a part that carries <see cref="OriginalData"/> without a <c>DataRef</c> id
    /// to name it (nothing to write it under), a <c>dataRef</c> no part in the unit resolves (no
    /// <c>&lt;data&gt;</c> text to write), and two parts sharing one <c>dataRef</c> whose resolved
    /// <see cref="OriginalData"/> differ (5.1: the reader guarantees they agree; here that guarantee is
    /// checked instead of assumed).
    /// </summary>
    private static IEnumerable<string> OriginalDataConsistencyProblems(XliffUnit unit)
    {
        var byDataRef = new Dictionary<string, List<OriginalData>>(StringComparer.Ordinal);
        foreach(XliffSegment segment in unit.Segments)
        {
            foreach(string problem in OriginalDataConsistencyProblems(segment.SourceContent.Parts, unit.Id, byDataRef))
            {
                yield return problem;
            }

            if(segment.TargetContent is not null)
            {
                foreach(string problem in OriginalDataConsistencyProblems(segment.TargetContent.Parts, unit.Id, byDataRef))
                {
                    yield return problem;
                }
            }
        }

        foreach(KeyValuePair<string, List<OriginalData>> group in byDataRef)
        {
            var distinct = new HashSet<OriginalData>(group.Value);
            if(distinct.Count > 1)
            {
                yield return $"Unit '{unit.Id}' has more than one distinct original data value for the dataRef '{group.Key}'; every code sharing a dataRef must carry equal original data.";
            }
            else if(distinct.Count == 0)
            {
                yield return $"Unit '{unit.Id}' has the dataRef '{group.Key}' on a code, but no part in the unit carries the resolved original data for it.";
            }
        }
    }

    /// <summary>The per-content half of <see cref="OriginalDataConsistencyProblems(XliffUnit)"/>, also yielding the "original data without a dataRef" problem for the parts it walks.</summary>
    private static IEnumerable<string> OriginalDataConsistencyProblems(ImmutableArray<InlinePart> parts, string unitId, Dictionary<string, List<OriginalData>> byDataRef)
    {
        foreach(InlinePart part in parts)
        {
            (string? dataRef, OriginalData? data) = DataReferenceOf(part);
            if(data is not null && dataRef is null)
            {
                yield return $"A code in unit '{unitId}' carries original data without a dataRef to name it; original data can only be written when a code names it by dataRef.";
            }

            if(dataRef is null)
            {
                continue;
            }

            if(!byDataRef.TryGetValue(dataRef, out List<OriginalData>? values))
            {
                values = [];
                byDataRef[dataRef] = values;
            }

            if(data is not null)
            {
                values.Add(data);
            }
        }
    }

    /// <summary>
    /// Yields the per-field problems of one content's parts: an id, startRef, dataRef or copyOf that is
    /// not an XML name token (<see cref="NameTokenProblems"/>), a disp, equiv, subType, copyOf, value,
    /// ref or annotation type that contains a character XML cannot carry
    /// (<see cref="XmlTextProblems"/>) — these attribute values cannot be <c>cp</c>-encoded the way
    /// text parts and original data are, so the writer refuses them instead (5.4) — a non-isolated
    /// end code that carries an <see cref="EndCodePart.Id"/> (XLIFF 2.1 §4.2.3.5: <c>id</c> is used if
    /// and only if <c>isolated="yes"</c>; the writer would otherwise silently drop it, matching what the
    /// reader used to do before this refusal existed there too), an <see cref="AnnotationStartPart.Type"/>
    /// that is neither <c>generic</c>, <c>term</c>, <c>comment</c> nor shaped <c>prefix:value</c> (XLIFF
    /// 2.1 §4.3.1.40, §4.7.3.1.4, the same shape <see cref="XliffReader.IsPrefixedAnnotationType"/>
    /// checks on read), and a <c>comment</c> annotation whose <see cref="AnnotationStartPart.Value"/> and
    /// <see cref="AnnotationStartPart.Ref"/> are both null or both non-null (XLIFF 2.1 §4.7.3.1.3) — both
    /// mirror what the reader already refuses, so the writer cannot produce a document the reader would
    /// then refuse to read back.
    /// </summary>
    private static IEnumerable<string> InlinePartFieldProblems(ImmutableArray<InlinePart> parts, string what)
    {
        for(int index = 0; index < parts.Length; index++)
        {
            InlinePart part = parts[index];
            int? undefinedForm = part switch
            {
                StartCodePart start when !Enum.IsDefined(start.Form) => (int)start.Form,
                EndCodePart end when !Enum.IsDefined(end.Form) => (int)end.Form,
                AnnotationStartPart start when !Enum.IsDefined(start.Form) => (int)start.Form,
                AnnotationEndPart end when !Enum.IsDefined(end.Form) => (int)end.Form,
                _ => null
            };
            if(undefinedForm is { } value)
            {
                yield return $"The inline part of type '{part.GetType().Name}' at position {index.ToString(CultureInfo.InvariantCulture)} in {what} has undefined Form value {value.ToString(CultureInfo.InvariantCulture)}.";
            }

            if(part is not (InlineTextPart or PlaceholderPart or StartCodePart or EndCodePart or AnnotationStartPart or AnnotationEndPart))
            {
                yield return $"The inline part of type '{part.GetType().FullName}' at position {index.ToString(CultureInfo.InvariantCulture)} in {what} is of a kind the writer cannot serialize.";
            }

            switch(part)
            {
                case(PlaceholderPart placeholder):
                {
                    foreach(string problem in NameTokenProblems(placeholder.Id, $"inline id in {what}"))
                    {
                        yield return problem;
                    }

                    foreach(string problem in CodeReferenceProblems(placeholder.DataRef, placeholder.CopyOf, what))
                    {
                        yield return problem;
                    }

                    foreach(string problem in CodeTextFieldProblems(placeholder.Disp, placeholder.Equiv, placeholder.SubType, placeholder.CopyOf, placeholder.SubFlows, what))
                    {
                        yield return problem;
                    }

                    break;
                }

                case(StartCodePart start):
                {
                    foreach(string problem in NameTokenProblems(start.Id, $"inline id in {what}"))
                    {
                        yield return problem;
                    }

                    foreach(string problem in CodeReferenceProblems(start.DataRef, start.CopyOf, what))
                    {
                        yield return problem;
                    }

                    foreach(string problem in CodeTextFieldProblems(start.Disp, start.Equiv, start.SubType, start.CopyOf, start.SubFlows, what))
                    {
                        yield return problem;
                    }

                    break;
                }

                case(EndCodePart end):
                {
                    IEnumerable<string> idProblems = end.Isolated
                        ? NameTokenProblems(end.Id, $"inline id in {what}")
                        : NameTokenProblems(end.StartRef, $"inline startRef in {what}");
                    foreach(string problem in idProblems)
                    {
                        yield return problem;
                    }

                    if(!end.Isolated && end.Id is not null)
                    {
                        yield return $"An end code in {what} carries an id but is not isolated; XLIFF 2.1 §4.2.3.5 uses id if and only if isolated=\"yes\".";
                    }

                    foreach(string problem in CodeReferenceProblems(end.DataRef, end.CopyOf, what))
                    {
                        yield return problem;
                    }

                    foreach(string problem in CodeTextFieldProblems(end.Disp, end.Equiv, end.SubType, end.CopyOf, end.SubFlows, what))
                    {
                        yield return problem;
                    }

                    break;
                }

                case(AnnotationStartPart annotationStart):
                {
                    foreach(string problem in NameTokenProblems(annotationStart.Id, $"inline id in {what}"))
                    {
                        yield return problem;
                    }

                    foreach(string problem in XmlTextProblems(annotationStart.Type, $"an annotation type in {what}"))
                    {
                        yield return problem;
                    }

                    foreach(string problem in XmlTextProblems(annotationStart.Ref, $"an annotation ref in {what}"))
                    {
                        yield return problem;
                    }

                    foreach(string problem in XmlTextProblems(annotationStart.Value, $"an annotation value in {what}"))
                    {
                        yield return problem;
                    }

                    if(!WellKnownXliffAttributeValues.IsGeneric(annotationStart.Type) && !WellKnownXliffAttributeValues.IsTerm(annotationStart.Type)
                        && !WellKnownXliffAttributeValues.IsComment(annotationStart.Type) && !XliffReader.IsPrefixedAnnotationType(annotationStart.Type))
                    {
                        yield return $"An annotation type in {what} is '{annotationStart.Type}', which is neither generic, term, comment nor shaped prefix:value; XLIFF 2.1 §4.3.1.40 and §4.7.3.1.4 require one of those.";
                    }

                    if(WellKnownXliffAttributeValues.IsComment(annotationStart.Type))
                    {
                        if(annotationStart.Value is null && annotationStart.Ref is null)
                        {
                            yield return $"A comment annotation in {what} has neither a value nor a ref attribute; XLIFF 2.1 §4.7.3.1.3 requires one.";
                        }
                        else if(annotationStart.Value is not null && annotationStart.Ref is not null)
                        {
                            yield return $"A comment annotation in {what} has both a value and a ref attribute; XLIFF 2.1 §4.7.3.1.3 allows only one.";
                        }
                    }

                    break;
                }

                case(AnnotationEndPart annotationEnd):
                {
                    foreach(string problem in NameTokenProblems(annotationEnd.StartRef, $"inline startRef in {what}"))
                    {
                        yield return problem;
                    }

                    break;
                }
            }
        }
    }

    /// <summary>The <see cref="NameTokenProblems"/> shared by every code's optional <c>dataRef</c> and <c>copyOf</c>, each checked only when present.</summary>
    private static IEnumerable<string> CodeReferenceProblems(string? dataRef, string? copyOf, string what)
    {
        if(dataRef is not null)
        {
            foreach(string problem in NameTokenProblems(dataRef, $"dataRef in {what}"))
            {
                yield return problem;
            }
        }

        if(copyOf is not null)
        {
            foreach(string problem in NameTokenProblems(copyOf, $"copyOf in {what}"))
            {
                yield return problem;
            }
        }
    }

    /// <summary>The <see cref="XmlTextProblems"/> shared by every code's <c>disp</c>, <c>equiv</c>, <c>subType</c>, <c>copyOf</c> and <c>subFlows</c> (<c>copyOf</c> is checked for both name-token shape, in <see cref="CodeReferenceProblems"/>, and general XML-text validity, here).</summary>
    private static IEnumerable<string> CodeTextFieldProblems(string? disp, string equiv, string? subType, string? copyOf, string? subFlows, string what)
    {
        foreach(string problem in XmlTextProblems(disp, $"a disp in {what}"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(equiv, $"an equiv in {what}"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(subType, $"a subType in {what}"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(copyOf, $"a copyOf in {what}"))
        {
            yield return problem;
        }

        foreach(string problem in XmlTextProblems(subFlows, $"a subFlows in {what}"))
        {
            yield return problem;
        }
    }
}
