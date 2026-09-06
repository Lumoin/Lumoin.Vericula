namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// Derives a diagnostic's one-based line and position from its zero-based UTF-16 offset into the
/// source text, counting line breaks the way <see cref="Diagnostics.MessageFormatDiagnostic"/>
/// documents: a line feed, a carriage return, and a carriage return immediately followed by a line
/// feed each end exactly one line.
/// </summary>
internal static class MessageFormatPosition
{
    /// <summary>Locates an offset within a source text.</summary>
    /// <param name="source">The source text <paramref name="offset"/> was found in.</param>
    /// <param name="offset">The zero-based UTF-16 offset to locate; may equal <paramref name="source"/>'s length for an end-of-input offset.</param>
    /// <returns>The one-based line and, within that line, the one-based position, both counted in UTF-16 code units.</returns>
    internal static (int Line, int Position) Locate(string source, int offset)
    {
        int line = 1;
        int lineStart = 0;

        //Only a "\r\n" pair that lies entirely before offset counts as the single line ending CRLF
        //describes; a "\r" whose paired "\n" would land at or after offset is counted as ending a
        //line on its own, leaving that "\n" to start the next line (and so be found by the next
        //iteration, or not at all when it lands exactly on offset).
        int index = 0;
        while(index < offset)
        {
            char character = source[index];
            if(character == '\r')
            {
                bool crlf = index + 1 < offset && index + 1 < source.Length && source[index + 1] == '\n';
                index += crlf ? 2 : 1;
                line++;
                lineStart = index;

                continue;
            }

            if(character == '\n')
            {
                index++;
                line++;
                lineStart = index;

                continue;
            }

            index++;
        }

        return (line, offset - lineStart + 1);
    }
}
