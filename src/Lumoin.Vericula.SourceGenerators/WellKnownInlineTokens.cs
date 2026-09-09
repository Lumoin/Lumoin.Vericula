namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The HTML element names the generator's inline renderer can synthesize a tag for when a code
/// carries no original data of its own, and the resolution that decides which name, if any, applies
/// to a given code.
/// </summary>
/// <remarks>
/// This mirrors <c>Lumoin.Vericula.Content.WellKnownInlineTokens</c>; the generator assembly cannot
/// reference the library, so it carries its own copy, with the core's <c>InlineCodeType</c> enum and
/// <c>OriginalData</c> record replaced by the raw attribute strings the generator already has on hand. Like
/// its core twin, each name here has no per-name <c>Is*</c> predicate of its own: <see cref="TryResolve"/>'s
/// original-data clue matches ordinal-case-insensitively against the whole of <see cref="ElementNames"/>
/// at once, so a predicate per name would have no caller.
/// </remarks>
internal static class WellKnownInlineTokens
{
    /// <summary>The UTF-8 source literal of <see cref="Em"/>.</summary>
    public static ReadOnlySpan<byte> EmUtf8 => "em"u8;

    /// <summary>The HTML emphasis element.</summary>
    public static readonly string Em = Utf8Constants.ToInternedString(EmUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Strong"/>.</summary>
    public static ReadOnlySpan<byte> StrongUtf8 => "strong"u8;

    /// <summary>The HTML strong-importance element.</summary>
    public static readonly string Strong = Utf8Constants.ToInternedString(StrongUtf8);

    /// <summary>The UTF-8 source literal of <see cref="B"/>.</summary>
    public static ReadOnlySpan<byte> BUtf8 => "b"u8;

    /// <summary>The HTML bring-attention-to element.</summary>
    public static readonly string B = Utf8Constants.ToInternedString(BUtf8);

    /// <summary>The UTF-8 source literal of <see cref="I"/>.</summary>
    public static ReadOnlySpan<byte> IUtf8 => "i"u8;

    /// <summary>The HTML alternate-voice element.</summary>
    public static readonly string I = Utf8Constants.ToInternedString(IUtf8);

    /// <summary>The UTF-8 source literal of <see cref="U"/>.</summary>
    public static ReadOnlySpan<byte> UUtf8 => "u"u8;

    /// <summary>The HTML unarticulated-annotation element.</summary>
    public static readonly string U = Utf8Constants.ToInternedString(UUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Br"/>.</summary>
    public static ReadOnlySpan<byte> BrUtf8 => "br"u8;

    /// <summary>The HTML line-break element; void (<see cref="IsVoid(string)"/>).</summary>
    public static readonly string Br = Utf8Constants.ToInternedString(BrUtf8);

    /// <summary>The UTF-8 source literal of <see cref="A"/>.</summary>
    public static ReadOnlySpan<byte> AUtf8 => "a"u8;

    /// <summary>The HTML anchor element.</summary>
    public static readonly string A = Utf8Constants.ToInternedString(AUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Code"/>.</summary>
    public static ReadOnlySpan<byte> CodeUtf8 => "code"u8;

    /// <summary>The HTML code element.</summary>
    public static readonly string Code = Utf8Constants.ToInternedString(CodeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Span"/>.</summary>
    public static ReadOnlySpan<byte> SpanUtf8 => "span"u8;

    /// <summary>The HTML generic inline element.</summary>
    public static readonly string Span = Utf8Constants.ToInternedString(SpanUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Ruby"/>.</summary>
    public static ReadOnlySpan<byte> RubyUtf8 => "ruby"u8;

    /// <summary>The HTML ruby annotation element.</summary>
    public static readonly string Ruby = Utf8Constants.ToInternedString(RubyUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Rt"/>.</summary>
    public static ReadOnlySpan<byte> RtUtf8 => "rt"u8;

    /// <summary>The HTML ruby-text element.</summary>
    public static readonly string Rt = Utf8Constants.ToInternedString(RtUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Rp"/>.</summary>
    public static ReadOnlySpan<byte> RpUtf8 => "rp"u8;

    /// <summary>The HTML ruby-parenthesis element.</summary>
    public static readonly string Rp = Utf8Constants.ToInternedString(RpUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Bdi"/>.</summary>
    public static ReadOnlySpan<byte> BdiUtf8 => "bdi"u8;

    /// <summary>The HTML bidirectional-isolate element.</summary>
    public static readonly string Bdi = Utf8Constants.ToInternedString(BdiUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Bdo"/>.</summary>
    public static ReadOnlySpan<byte> BdoUtf8 => "bdo"u8;

    /// <summary>The HTML bidirectional-override element.</summary>
    public static readonly string Bdo = Utf8Constants.ToInternedString(BdoUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Wbr"/>.</summary>
    public static ReadOnlySpan<byte> WbrUtf8 => "wbr"u8;

    /// <summary>The HTML line-break-opportunity element; void (<see cref="IsVoid(string)"/>).</summary>
    public static readonly string Wbr = Utf8Constants.ToInternedString(WbrUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Abbr"/>.</summary>
    public static ReadOnlySpan<byte> AbbrUtf8 => "abbr"u8;

    /// <summary>The HTML abbreviation element.</summary>
    public static readonly string Abbr = Utf8Constants.ToInternedString(AbbrUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Time"/>.</summary>
    public static ReadOnlySpan<byte> TimeUtf8 => "time"u8;

    /// <summary>The HTML time element.</summary>
    public static readonly string Time = Utf8Constants.ToInternedString(TimeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Q"/>.</summary>
    public static ReadOnlySpan<byte> QUtf8 => "q"u8;

    /// <summary>The HTML inline-quotation element.</summary>
    public static readonly string Q = Utf8Constants.ToInternedString(QUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Img"/>.</summary>
    public static ReadOnlySpan<byte> ImgUtf8 => "img"u8;

    /// <summary>The HTML image element; void (<see cref="IsVoid(string)"/>).</summary>
    public static readonly string Img = Utf8Constants.ToInternedString(ImgUtf8);

    /// <summary>
    /// Determines whether <paramref name="name"/> is one of the HTML elements that never wraps
    /// content: <see cref="Br"/>, <see cref="Wbr"/> or <see cref="Img"/>.
    /// </summary>
    /// <param name="name">A resolved element name, such as one <see cref="TryResolve"/> returned.</param>
    /// <returns><see langword="true"/> if the element never wraps content; otherwise, <see langword="false"/>.</returns>
    public static bool IsVoid(string name) => string.Equals(name, Br, StringComparison.Ordinal)
        || string.Equals(name, Wbr, StringComparison.Ordinal)
        || string.Equals(name, Img, StringComparison.Ordinal);

    /// <summary>
    /// Resolves the HTML element name a code should render as, trying three clues in order: a
    /// reserved <c>subType</c>, then a tag name recovered from <paramref name="originalDataText"/>,
    /// then <paramref name="type"/> alone.
    /// </summary>
    /// <param name="type">The code's raw <c>type</c> attribute value (<c>fmt</c>, <c>ui</c>, <c>quote</c>, <c>link</c>, <c>image</c> or <c>other</c>), or null when absent.</param>
    /// <param name="subType">The code's full <c>prefix:value</c> sub-type, or null when absent.</param>
    /// <param name="originalDataText">The text of the code's resolved original data, or null when it carries none.</param>
    /// <param name="name">The resolved element name; <see cref="string.Empty"/> when resolution fails.</param>
    /// <returns><see langword="true"/> if a name was resolved; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// A reserved sub-type is decided outright, without falling through to the later clues: <c>xlf:b</c>
    /// resolves to <see cref="B"/>, <c>xlf:i</c> to <see cref="I"/>, <c>xlf:u</c> to <see cref="U"/>,
    /// <c>xlf:lb</c> to <see cref="Br"/>; <c>xlf:pb</c> and <c>xlf:var</c> have no HTML element and
    /// resolve false immediately. Any other <paramref name="subType"/>, including null, falls through
    /// to the original-data clue: the text after a leading <c>&lt;</c> or <c>&lt;/</c>, up to the first
    /// <c>&gt;</c>, <c>/</c> or white space, compared ordinal-case-insensitively against this class's
    /// element names. Failing that, <paramref name="type"/> alone resolves <c>link</c> to <see cref="A"/>,
    /// <c>image</c> to <see cref="Img"/> and <c>quote</c> to <see cref="Q"/>; every other type resolves
    /// false.
    /// </remarks>
    public static bool TryResolve(string? type, string? subType, string? originalDataText, out string name)
    {
        if(subType is not null)
        {
            (bool decided, bool resolved, string resolvedName) = subType switch
            {
                _ when WellKnownXliffAttributeValues.IsSubTypeBold(subType) => (true, true, B),
                _ when WellKnownXliffAttributeValues.IsSubTypeItalic(subType) => (true, true, I),
                _ when WellKnownXliffAttributeValues.IsSubTypeUnderline(subType) => (true, true, U),
                _ when WellKnownXliffAttributeValues.IsSubTypeLineBreak(subType) => (true, true, Br),
                _ when WellKnownXliffAttributeValues.IsSubTypePageBreak(subType) || WellKnownXliffAttributeValues.IsSubTypeVariable(subType) => (true, false, string.Empty),
                _ => (false, false, string.Empty)
            };

            if(decided)
            {
                name = resolvedName;

                return resolved;
            }
        }

        if(originalDataText is not null && TryResolveFromOriginalData(originalDataText, out name))
        {
            return true;
        }

        name = type switch
        {
            _ when WellKnownXliffAttributeValues.IsLink(type) => A,
            _ when WellKnownXliffAttributeValues.IsImage(type) => Img,
            _ when WellKnownXliffAttributeValues.IsQuote(type) => Q,
            _ => string.Empty
        };

        return name.Length != 0;
    }

    /// <summary>The element names this class knows, for the original-data resolution clue in <see cref="TryResolve"/>.</summary>
    private static readonly string[] ElementNames =
    [
        Em, Strong, B, I, U, Br, A, Code, Span, Ruby, Rt, Rp, Bdi, Bdo, Wbr, Abbr, Time, Q, Img
    ];

    /// <summary>
    /// Recovers a tag name from an original-data text and matches it against <see cref="ElementNames"/>.
    /// </summary>
    /// <param name="text">The original data's text.</param>
    /// <param name="name">The matched, canonically-cased element name; <see cref="string.Empty"/> when nothing matches.</param>
    /// <returns><see langword="true"/> if a name was recovered and matched; otherwise, <see langword="false"/>.</returns>
    private static bool TryResolveFromOriginalData(string text, out string name)
    {
        ReadOnlySpan<char> span = text.AsSpan();
        if(span.StartsWith("</", StringComparison.Ordinal))
        {
            //netstandard2.0's Span polyfill has no System.Range/System.Index, so slicing goes through
            //Slice(int) rather than the [2..] range syntax core's own copy of this method uses.
            span = span.Slice(2);
        }
        else if(span.Length > 0 && span[0] == '<')
        {
            span = span.Slice(1);
        }
        else
        {
            name = string.Empty;

            return false;
        }

        int end = 0;
        while(end < span.Length && span[end] != '>' && span[end] != '/' && !char.IsWhiteSpace(span[end]))
        {
            end++;
        }

        ReadOnlySpan<char> candidate = span.Slice(0, end);
        if(candidate.IsEmpty)
        {
            name = string.Empty;

            return false;
        }

        foreach(string elementName in ElementNames)
        {
            if(candidate.Equals(elementName, StringComparison.OrdinalIgnoreCase))
            {
                name = elementName;

                return true;
            }
        }

        name = string.Empty;

        return false;
    }
}
