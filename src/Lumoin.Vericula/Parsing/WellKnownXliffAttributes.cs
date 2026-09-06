using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Parsing;

/// <summary>
/// The well-known XLIFF 2.x attribute NAMES the reader and writer exchange, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>:
/// the core attributes and the Metadata and Validation module attributes.
/// </summary>
/// <remarks>
/// These are attribute names, not their values; values live in <see cref="WellKnownXliffAttributeValues"/>.
/// Each name is spelled once as a UTF-8 source literal and carried alongside as an interned string.
/// </remarks>
public static class WellKnownXliffAttributes
{
    /// <summary>The UTF-8 source literal of <see cref="Version"/>.</summary>
    public static ReadOnlySpan<byte> VersionUtf8 => "version"u8;

    /// <summary>The XLIFF version on the root per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#version">XLIFF 2.1, version</see>.</summary>
    public static readonly string Version = Utf8Constants.ToInternedString(VersionUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SourceLanguage"/>.</summary>
    public static ReadOnlySpan<byte> SourceLanguageUtf8 => "srcLang"u8;

    /// <summary>The source language on the root per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#srcLang">XLIFF 2.1, srcLang</see>.</summary>
    public static readonly string SourceLanguage = Utf8Constants.ToInternedString(SourceLanguageUtf8);

    /// <summary>The UTF-8 source literal of <see cref="TargetLanguage"/>.</summary>
    public static ReadOnlySpan<byte> TargetLanguageUtf8 => "trgLang"u8;

    /// <summary>The target language on the root per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#trgLang">XLIFF 2.1, trgLang</see>; required when any target is present.</summary>
    public static readonly string TargetLanguage = Utf8Constants.ToInternedString(TargetLanguageUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Id"/>.</summary>
    public static ReadOnlySpan<byte> IdUtf8 => "id"u8;

    /// <summary>The identifier of a file, group, unit or segment, an XML name token, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#id">XLIFF 2.1, id</see>.</summary>
    public static readonly string Id = Utf8Constants.ToInternedString(IdUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Space"/>.</summary>
    public static ReadOnlySpan<byte> SpaceUtf8 => "space"u8;

    /// <summary>The whitespace handling declaration in the XML namespace per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#xml_space">XLIFF 2.1, xml:space</see>.</summary>
    public static readonly string Space = Utf8Constants.ToInternedString(SpaceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="State"/>.</summary>
    public static ReadOnlySpan<byte> StateUtf8 => "state"u8;

    /// <summary>A segment's translation state per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string State = Utf8Constants.ToInternedString(StateUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubState"/>.</summary>
    public static ReadOnlySpan<byte> SubStateUtf8 => "subState"u8;

    /// <summary>A segment's tool-specific state refinement, <c>prefix:value</c>, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#substate">XLIFF 2.1, subState</see>.</summary>
    public static readonly string SubState = Utf8Constants.ToInternedString(SubStateUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Category"/>.</summary>
    public static ReadOnlySpan<byte> CategoryUtf8 => "category"u8;

    /// <summary>A metadata group's category per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#meta_category">XLIFF 2.1, category</see>.</summary>
    public static readonly string Category = Utf8Constants.ToInternedString(CategoryUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Type"/>.</summary>
    public static ReadOnlySpan<byte> TypeUtf8 => "type"u8;

    /// <summary>A metadata value's type per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#meta_type">XLIFF 2.1, type</see>.</summary>
    public static readonly string Type = Utf8Constants.ToInternedString(TypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="IsPresent"/>.</summary>
    public static ReadOnlySpan<byte> IsPresentUtf8 => "isPresent"u8;

    /// <summary>The rule attribute requiring its text in the target per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_isPresent">XLIFF 2.1, isPresent</see>.</summary>
    public static readonly string IsPresent = Utf8Constants.ToInternedString(IsPresentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="IsNotPresent"/>.</summary>
    public static ReadOnlySpan<byte> IsNotPresentUtf8 => "isNotPresent"u8;

    /// <summary>The rule attribute forbidding its text in the target per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_isNotPresent">XLIFF 2.1, isNotPresent</see>.</summary>
    public static readonly string IsNotPresent = Utf8Constants.ToInternedString(IsNotPresentUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StartsWith"/>.</summary>
    public static ReadOnlySpan<byte> StartsWithUtf8 => "startsWith"u8;

    /// <summary>The rule attribute pinning the start of the target per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_startsWith">XLIFF 2.1, startsWith</see>.</summary>
    public static readonly string StartsWith = Utf8Constants.ToInternedString(StartsWithUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EndsWith"/>.</summary>
    public static ReadOnlySpan<byte> EndsWithUtf8 => "endsWith"u8;

    /// <summary>The rule attribute pinning the end of the target per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_endsWith">XLIFF 2.1, endsWith</see>.</summary>
    public static readonly string EndsWith = Utf8Constants.ToInternedString(EndsWithUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Occurs"/>.</summary>
    public static ReadOnlySpan<byte> OccursUtf8 => "occurs"u8;

    /// <summary>The rule attribute constraining occurrences per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_occurs">XLIFF 2.1, occurs</see>; the reader refuses it.</summary>
    public static readonly string Occurs = Utf8Constants.ToInternedString(OccursUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ExistsInSource"/>.</summary>
    public static ReadOnlySpan<byte> ExistsInSourceUtf8 => "existsInSource"u8;

    /// <summary>The rule attribute conditioning a rule on the source per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_existsInSource">XLIFF 2.1, existsInSource</see>; the reader refuses it when set.</summary>
    public static readonly string ExistsInSource = Utf8Constants.ToInternedString(ExistsInSourceUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CaseSensitive"/>.</summary>
    public static ReadOnlySpan<byte> CaseSensitiveUtf8 => "caseSensitive"u8;

    /// <summary>The rule attribute selecting case sensitivity per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_caseSensitive">XLIFF 2.1, caseSensitive</see>; the reader refuses the insensitive setting.</summary>
    public static readonly string CaseSensitive = Utf8Constants.ToInternedString(CaseSensitiveUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Normalization"/>.</summary>
    public static ReadOnlySpan<byte> NormalizationUtf8 => "normalization"u8;

    /// <summary>The rule attribute selecting Unicode normalization per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_normalization">XLIFF 2.1, normalization</see>.</summary>
    public static readonly string Normalization = Utf8Constants.ToInternedString(NormalizationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Disabled"/>.</summary>
    public static ReadOnlySpan<byte> DisabledUtf8 => "disabled"u8;

    /// <summary>The rule attribute switching a rule off per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#val_disabled">XLIFF 2.1, disabled</see>.</summary>
    public static readonly string Disabled = Utf8Constants.ToInternedString(DisabledUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CanCopy"/>.</summary>
    public static ReadOnlySpan<byte> CanCopyUtf8 => "canCopy"u8;

    /// <summary>Whether a code may be copied per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#canCopy">XLIFF 2.1, canCopy</see>.</summary>
    public static readonly string CanCopy = Utf8Constants.ToInternedString(CanCopyUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CanDelete"/>.</summary>
    public static ReadOnlySpan<byte> CanDeleteUtf8 => "canDelete"u8;

    /// <summary>Whether a code may be deleted per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#canDelete">XLIFF 2.1, canDelete</see>.</summary>
    public static readonly string CanDelete = Utf8Constants.ToInternedString(CanDeleteUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CanOverlap"/>.</summary>
    public static ReadOnlySpan<byte> CanOverlapUtf8 => "canOverlap"u8;

    /// <summary>Whether a span may overlap another per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#canOverlap">XLIFF 2.1, canOverlap</see>.</summary>
    public static readonly string CanOverlap = Utf8Constants.ToInternedString(CanOverlapUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CanReorder"/>.</summary>
    public static ReadOnlySpan<byte> CanReorderUtf8 => "canReorder"u8;

    /// <summary>How freely a code may be reordered per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#canReorder">XLIFF 2.1, canReorder</see>.</summary>
    public static readonly string CanReorder = Utf8Constants.ToInternedString(CanReorderUtf8);

    /// <summary>The UTF-8 source literal of <see cref="CopyOf"/>.</summary>
    public static ReadOnlySpan<byte> CopyOfUtf8 => "copyOf"u8;

    /// <summary>The identifier of the code a code was copied from per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#copyOf">XLIFF 2.1, copyOf</see>.</summary>
    public static readonly string CopyOf = Utf8Constants.ToInternedString(CopyOfUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DataRef"/>.</summary>
    public static ReadOnlySpan<byte> DataRefUtf8 => "dataRef"u8;

    /// <summary>The identifier of the <c>&lt;data&gt;</c> entry a standalone or paired code refers to per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dataRef">XLIFF 2.1, dataRef</see>.</summary>
    public static readonly string DataRef = Utf8Constants.ToInternedString(DataRefUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DataRefStart"/>.</summary>
    public static ReadOnlySpan<byte> DataRefStartUtf8 => "dataRefStart"u8;

    /// <summary>The identifier of the <c>&lt;data&gt;</c> entry a spanning code's start half refers to per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dataRefStart">XLIFF 2.1, dataRefStart</see>.</summary>
    public static readonly string DataRefStart = Utf8Constants.ToInternedString(DataRefStartUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DataRefEnd"/>.</summary>
    public static ReadOnlySpan<byte> DataRefEndUtf8 => "dataRefEnd"u8;

    /// <summary>The identifier of the <c>&lt;data&gt;</c> entry a spanning code's end half refers to per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dataRefEnd">XLIFF 2.1, dataRefEnd</see>.</summary>
    public static readonly string DataRefEnd = Utf8Constants.ToInternedString(DataRefEndUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Dir"/>.</summary>
    public static ReadOnlySpan<byte> DirUtf8 => "dir"u8;

    /// <summary>The text direction of an inline element or a <c>&lt;data&gt;</c> entry per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dir">XLIFF 2.1, dir</see>.</summary>
    public static readonly string Dir = Utf8Constants.ToInternedString(DirUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Disp"/>.</summary>
    public static ReadOnlySpan<byte> DispUtf8 => "disp"u8;

    /// <summary>Text meant for display to a human translator, on a standalone or paired code, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#disp">XLIFF 2.1, disp</see>.</summary>
    public static readonly string Disp = Utf8Constants.ToInternedString(DispUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DispStart"/>.</summary>
    public static ReadOnlySpan<byte> DispStartUtf8 => "dispStart"u8;

    /// <summary>The <see cref="Disp"/> of a spanning code's start half per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dispStart">XLIFF 2.1, dispStart</see>.</summary>
    public static readonly string DispStart = Utf8Constants.ToInternedString(DispStartUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DispEnd"/>.</summary>
    public static ReadOnlySpan<byte> DispEndUtf8 => "dispEnd"u8;

    /// <summary>The <see cref="Disp"/> of a spanning code's end half per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#dispEnd">XLIFF 2.1, dispEnd</see>.</summary>
    public static readonly string DispEnd = Utf8Constants.ToInternedString(DispEndUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Equiv"/>.</summary>
    public static ReadOnlySpan<byte> EquivUtf8 => "equiv"u8;

    /// <summary>A code's plain-text stand-in per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#equiv">XLIFF 2.1, equiv</see>.</summary>
    public static readonly string Equiv = Utf8Constants.ToInternedString(EquivUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EquivStart"/>.</summary>
    public static ReadOnlySpan<byte> EquivStartUtf8 => "equivStart"u8;

    /// <summary>The <see cref="Equiv"/> of a spanning code's start half per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#equivStart">XLIFF 2.1, equivStart</see>.</summary>
    public static readonly string EquivStart = Utf8Constants.ToInternedString(EquivStartUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EquivEnd"/>.</summary>
    public static ReadOnlySpan<byte> EquivEndUtf8 => "equivEnd"u8;

    /// <summary>The <see cref="Equiv"/> of a spanning code's end half per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#equivEnd">XLIFF 2.1, equivEnd</see>.</summary>
    public static readonly string EquivEnd = Utf8Constants.ToInternedString(EquivEndUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Hex"/>.</summary>
    public static ReadOnlySpan<byte> HexUtf8 => "hex"u8;

    /// <summary>A <c>&lt;cp&gt;</c> element's hexBinary code point per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#hex">XLIFF 2.1, hex</see>.</summary>
    public static readonly string Hex = Utf8Constants.ToInternedString(HexUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Isolated"/>.</summary>
    public static ReadOnlySpan<byte> IsolatedUtf8 => "isolated"u8;

    /// <summary>Whether a spanning code's start or end has no matching half in the unit per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#isolated">XLIFF 2.1, isolated</see>.</summary>
    public static readonly string Isolated = Utf8Constants.ToInternedString(IsolatedUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Ref"/>.</summary>
    public static ReadOnlySpan<byte> RefUtf8 => "ref"u8;

    /// <summary>A URI referencing further information about an annotation per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk_ref">XLIFF 2.1, ref</see>.</summary>
    public static readonly string Ref = Utf8Constants.ToInternedString(RefUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StartRef"/>.</summary>
    public static ReadOnlySpan<byte> StartRefUtf8 => "startRef"u8;

    /// <summary>The identifier of the start an <c>ec</c> or <c>em</c> element closes per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#startRef">XLIFF 2.1, startRef</see>.</summary>
    public static readonly string StartRef = Utf8Constants.ToInternedString(StartRefUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubFlows"/>.</summary>
    public static ReadOnlySpan<byte> SubFlowsUtf8 => "subFlows"u8;

    /// <summary>The space-separated unit ids a code's sub-flow content lives in per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#subFlows">XLIFF 2.1, subFlows</see>.</summary>
    public static readonly string SubFlows = Utf8Constants.ToInternedString(SubFlowsUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubFlowsStart"/>.</summary>
    public static ReadOnlySpan<byte> SubFlowsStartUtf8 => "subFlowsStart"u8;

    /// <summary>The <see cref="SubFlows"/> of a spanning code's start half per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#subFlowsStart">XLIFF 2.1, subFlowsStart</see>.</summary>
    public static readonly string SubFlowsStart = Utf8Constants.ToInternedString(SubFlowsStartUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubFlowsEnd"/>.</summary>
    public static ReadOnlySpan<byte> SubFlowsEndUtf8 => "subFlowsEnd"u8;

    /// <summary>The <see cref="SubFlows"/> of a spanning code's end half per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#subFlowsEnd">XLIFF 2.1, subFlowsEnd</see>.</summary>
    public static readonly string SubFlowsEnd = Utf8Constants.ToInternedString(SubFlowsEndUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubType"/>.</summary>
    public static ReadOnlySpan<byte> SubTypeUtf8 => "subType"u8;

    /// <summary>A code or annotation's full <c>prefix:value</c> sub-type per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#subType">XLIFF 2.1, subType</see>.</summary>
    public static readonly string SubType = Utf8Constants.ToInternedString(SubTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Translate"/>.</summary>
    public static ReadOnlySpan<byte> TranslateUtf8 => "translate"u8;

    /// <summary>Whether an annotation's content is translatable per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#translate">XLIFF 2.1, translate</see>.</summary>
    public static readonly string Translate = Utf8Constants.ToInternedString(TranslateUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Value"/>.</summary>
    public static ReadOnlySpan<byte> ValueUtf8 => "value"u8;

    /// <summary>An annotation's own value text per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#mrk_value">XLIFF 2.1, value</see>.</summary>
    public static readonly string Value = Utf8Constants.ToInternedString(ValueUtf8);
}
