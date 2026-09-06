namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The well-known XLIFF 2.x attribute NAMES this generator reads, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>.
/// </summary>
/// <remarks>
/// This mirrors <c>Lumoin.Vericula.Parsing.WellKnownXliffAttributes</c>; the generator assembly cannot
/// reference the library, so it carries its own copy of the members it needs.
/// </remarks>
internal static class WellKnownXliffAttributes
{
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

    /// <summary>The identifier of a unit or segment, an XML name token, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#id">XLIFF 2.1, id</see>.</summary>
    public static readonly string Id = Utf8Constants.ToInternedString(IdUtf8);

    /// <summary>The UTF-8 source literal of <see cref="State"/>.</summary>
    public static ReadOnlySpan<byte> StateUtf8 => "state"u8;

    /// <summary>A segment's translation state per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string State = Utf8Constants.ToInternedString(StateUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SubState"/>.</summary>
    public static ReadOnlySpan<byte> SubStateUtf8 => "subState"u8;

    /// <summary>A segment's tool-specific state refinement, <c>prefix:value</c>, per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#subState">XLIFF 2.1, subState</see>.</summary>
    public static readonly string SubState = Utf8Constants.ToInternedString(SubStateUtf8);
}
