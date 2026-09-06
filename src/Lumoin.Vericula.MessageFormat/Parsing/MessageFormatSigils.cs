namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// The single-character sigils and delimiters of the MessageFormat 2.0 grammar. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "ABNF Grammar" (<c>message.abnf</c>).
/// </summary>
/// <remarks>
/// Deliberately exempt from the pair-form (UTF-8 literal, interned string, <c>IsX</c> predicate)
/// contract that <see cref="Diagnostics.WellKnownMessageFormatDiagnostics"/> and
/// <see cref="WellKnownMessageFormatKeywords"/> follow: every member here is a single UTF-16 code
/// unit the scanner matches with <see langword="char"/> equality against the cursor, never assembled
/// into or compared as a multi-character string, so an interned string and an ordinal
/// <c>Is*(string)</c> predicate would add a second, unused representation of the same one code unit
/// rather than removing a duplicated literal. The class also stays <see langword="internal"/>, unlike
/// the two <see langword="public"/> pair-form classes: these constants describe the parser's own
/// implementation, not vocabulary a consumer of the MessageFormat API needs to recognize.
/// </remarks>
internal static class MessageFormatSigils
{
    /// <summary>Opens a placeholder or a quoted-pattern delimiter (as a pair): <c>{</c>.</summary>
    internal const char OpenBrace = '{';

    /// <summary>Closes a placeholder or a quoted-pattern delimiter (as a pair): <c>}</c>.</summary>
    internal const char CloseBrace = '}';

    /// <summary>Marks a declaration or the matcher keyword: <c>.</c>.</summary>
    internal const char Dot = '.';

    /// <summary>Introduces a variable reference: <c>$</c>.</summary>
    internal const char Dollar = '$';

    /// <summary>Introduces a function name within an expression: <c>:</c>. Also separates an identifier's namespace from its name.</summary>
    internal const char Colon = ':';

    /// <summary>Opens or stands alone for a markup element: <c>#</c>.</summary>
    internal const char Hash = '#';

    /// <summary>Closes a markup element, or marks a standalone one when trailing: <c>/</c>.</summary>
    internal const char Slash = '/';

    /// <summary>Introduces an attribute: <c>@</c>.</summary>
    internal const char At = '@';

    /// <summary>Separates an option's or a local declaration's name from its value: <c>=</c>.</summary>
    internal const char EqualsSign = '=';

    /// <summary>Delimits a quoted literal, on both sides: <c>|</c>.</summary>
    internal const char Pipe = '|';

    /// <summary>Escapes the character that follows it in text or in a quoted literal: <c>\</c>.</summary>
    internal const char Backslash = '\\';

    /// <summary>The catch-all variant key: <c>*</c>.</summary>
    internal const char Asterisk = '*';
}
