using System.Text;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// The character classes the MessageFormat 2.0 grammar defines by Unicode scalar value, tested
/// against a decoded <see cref="Rune"/> rather than a UTF-16 <see cref="char"/> so a supplementary-
/// plane scalar (outside the Basic Multilingual Plane) is classified correctly. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "ABNF Grammar" (<c>message.abnf</c>), productions
/// <c>name-start</c>, <c>name-char</c>, <c>text-char</c>, <c>quoted-char</c>, <c>ws</c> and <c>bidi</c>.
/// There is no member for <c>simple-start-char</c>: the parser decides simple versus complex by the
/// first non-<c>ws</c> code point after the leading <c>o</c> (see <see cref="MessageParser.ParseMessage"/>),
/// never by classifying pattern content against it, except that a bidi mark immediately before a
/// <c>.</c> is ambiguous and retried as a simple message when the complex alternative fails.
/// </summary>
internal static class MessageFormatCharacterClasses
{
    /// <summary>
    /// Determines whether a scalar value is <c>ws</c>: SPACE, HTAB, CR, LF, or IDEOGRAPHIC SPACE
    /// (U+3000).
    /// </summary>
    /// <param name="scalarValue">The Unicode scalar value to classify.</param>
    /// <returns><see langword="true"/> if <paramref name="scalarValue"/> is a <c>ws</c> character; otherwise, <see langword="false"/>.</returns>
    internal static bool IsWhitespace(int scalarValue) => scalarValue is 0x20 or 0x09 or 0x0D or 0x0A or 0x3000;

    /// <summary>
    /// Determines whether a scalar value is <c>bidi</c>: ARABIC LETTER MARK, LEFT-TO-RIGHT MARK,
    /// RIGHT-TO-LEFT MARK, or one of the four directional isolate controls U+2066 to U+2069.
    /// </summary>
    /// <param name="scalarValue">The Unicode scalar value to classify.</param>
    /// <returns><see langword="true"/> if <paramref name="scalarValue"/> is a <c>bidi</c> mark; otherwise, <see langword="false"/>.</returns>
    internal static bool IsBidi(int scalarValue) => scalarValue is 0x061C or 0x200E or 0x200F or (>= 0x2066 and <= 0x2069);

    /// <summary>
    /// Determines whether a scalar value is <c>name-start</c>: an ASCII letter, <c>+</c>, <c>_</c>, or
    /// one of a series of ranges spanning the rest of the Unicode scalar space with control,
    /// whitespace, bidi-control, surrogate and noncharacter code points omitted.
    /// </summary>
    /// <param name="scalarValue">The Unicode scalar value to classify.</param>
    /// <returns><see langword="true"/> if <paramref name="scalarValue"/> may start a name; otherwise, <see langword="false"/>.</returns>
    internal static bool IsNameStart(int scalarValue) => scalarValue switch
    {
        (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') => true,
        0x2B or 0x5F => true,
        >= 0xA1 and <= 0x61B => true,
        >= 0x61D and <= 0x167F => true,
        >= 0x1681 and <= 0x1FFF => true,
        >= 0x200B and <= 0x200D => true,
        >= 0x2010 and <= 0x2027 => true,
        >= 0x2030 and <= 0x205E => true,
        >= 0x2060 and <= 0x2065 => true,
        >= 0x206A and <= 0x2FFF => true,
        >= 0x3001 and <= 0xD7FF => true,
        >= 0xE000 and <= 0xFDCF => true,
        >= 0xFDF0 and <= 0xFFFD => true,
        >= 0x10000 and <= 0x1FFFD => true,
        >= 0x20000 and <= 0x2FFFD => true,
        >= 0x30000 and <= 0x3FFFD => true,
        >= 0x40000 and <= 0x4FFFD => true,
        >= 0x50000 and <= 0x5FFFD => true,
        >= 0x60000 and <= 0x6FFFD => true,
        >= 0x70000 and <= 0x7FFFD => true,
        >= 0x80000 and <= 0x8FFFD => true,
        >= 0x90000 and <= 0x9FFFD => true,
        >= 0xA0000 and <= 0xAFFFD => true,
        >= 0xB0000 and <= 0xBFFFD => true,
        >= 0xC0000 and <= 0xCFFFD => true,
        >= 0xD0000 and <= 0xDFFFD => true,
        >= 0xE0000 and <= 0xEFFFD => true,
        >= 0xF0000 and <= 0xFFFFD => true,
        >= 0x100000 and <= 0x10FFFD => true,
        _ => false
    };

    /// <summary>Determines whether a scalar value is <c>name-char</c>: <see cref="IsNameStart"/>, an ASCII digit, <c>-</c>, or <c>.</c>.</summary>
    /// <param name="scalarValue">The Unicode scalar value to classify.</param>
    /// <returns><see langword="true"/> if <paramref name="scalarValue"/> may continue a name; otherwise, <see langword="false"/>.</returns>
    internal static bool IsNameChar(int scalarValue) => IsNameStart(scalarValue) || scalarValue is (>= '0' and <= '9') or '-' or '.';

    /// <summary>
    /// Determines whether a scalar value is <c>text-char</c>: any scalar value except NULL (U+0000),
    /// REVERSE SOLIDUS (<c>\</c>), LEFT CURLY BRACKET (<c>{</c>) and RIGHT CURLY BRACKET (<c>}</c>).
    /// </summary>
    /// <param name="scalarValue">The Unicode scalar value to classify.</param>
    /// <returns><see langword="true"/> if <paramref name="scalarValue"/> may appear as plain pattern text; otherwise, <see langword="false"/>.</returns>
    internal static bool IsTextChar(int scalarValue) => scalarValue switch
    {
        >= 0x01 and <= 0x5B => true,
        >= 0x5D and <= 0x7A => true,
        0x7C => true,
        >= 0x7E and <= 0x10FFFF => true,
        _ => false
    };

    /// <summary>
    /// Determines whether a scalar value is <c>quoted-char</c>: any scalar value except NULL
    /// (U+0000), REVERSE SOLIDUS (<c>\</c>) and VERTICAL LINE (<c>|</c>).
    /// </summary>
    /// <param name="scalarValue">The Unicode scalar value to classify.</param>
    /// <returns><see langword="true"/> if <paramref name="scalarValue"/> may appear inside a quoted literal; otherwise, <see langword="false"/>.</returns>
    internal static bool IsQuotedChar(int scalarValue) => scalarValue switch
    {
        >= 0x01 and <= 0x5B => true,
        >= 0x5D and <= 0x7B => true,
        >= 0x7D and <= 0x10FFFF => true,
        _ => false
    };

    /// <summary>
    /// NFC-normalizes text for the two places the spec requires it: a name's stored value (see
    /// <see cref="MessageParser.ParseName"/>) and a literal key's value when compared for the
    /// "Duplicate Variant" rule (see <see cref="MessageDataModelValidator"/>). A literal's own stored
    /// value is never normalized; only these two uses are.
    /// </summary>
    /// <param name="value">The raw text to normalize.</param>
    /// <returns><paramref name="value"/> unchanged if it is already NFC-normalized; otherwise, its NFC-normalized form.</returns>
    internal static string NormalizeToNfc(string value) => value.IsNormalized() ? value : value.Normalize();
}
