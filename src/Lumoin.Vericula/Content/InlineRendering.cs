namespace Lumoin.Vericula.Content;

/// <summary>
/// The two ways <see cref="InlineContent.Render(InlineRendering)"/> can turn a content's parts into
/// one string.
/// </summary>
public enum InlineRendering
{
    /// <summary>
    /// An HTML fragment when the content carries at least one code part (with text escaped so the
    /// fragment tokenizes correctly), or plain text verbatim otherwise. See
    /// <see cref="InlineContent.Render(InlineRendering)"/> for the exact rule per part kind.
    /// </summary>
    Markup = 0,

    /// <summary>
    /// Plain text: every text part verbatim and every code part's <c>equiv</c> fallback text, with
    /// nothing escaped and annotation parts contributing nothing. This is XLIFF 2.1 §4.7.8's
    /// equality type B view.
    /// </summary>
    Plain = 1
}
