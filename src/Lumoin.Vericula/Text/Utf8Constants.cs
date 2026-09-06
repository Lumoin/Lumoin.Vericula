using System.Text;

namespace Lumoin.Vericula.Text;

/// <summary>
/// Derives the string half of a well-known vocabulary pair from its UTF-8 source literal, so every
/// well-known name is spelled exactly once, in the <c>u8</c> literal, and its string form can never
/// drift from it.
/// </summary>
public static class Utf8Constants
{
    /// <summary>
    /// Decodes a UTF-8 source literal into an interned string.
    /// </summary>
    /// <param name="utf8SourceLiteral">The UTF-8 bytes of the literal.</param>
    /// <returns>The interned string with the same text.</returns>
    public static string ToInternedString(ReadOnlySpan<byte> utf8SourceLiteral)
    {
        return string.Intern(Encoding.UTF8.GetString(utf8SourceLiteral));
    }
}
