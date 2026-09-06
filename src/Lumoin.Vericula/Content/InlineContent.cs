using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Lumoin.Vericula.Content;

/// <summary>
/// A segment's source or target content: a flat, ordered sequence of <see cref="InlinePart"/>s. Flat,
/// not a tree, because XLIFF <c>sc</c>/<c>ec</c> pairs may overlap and cross segment boundaries, and
/// MessageFormat markup is flat too; a tree is derived later only where a renderer needs one.
/// </summary>
/// <remarks>
/// Equality is sequence equality over <see cref="Parts"/>, not the compiler-synthesized record
/// equality: <see cref="ImmutableArray{T}"/> implements its own equality by comparing the backing
/// array's reference, so two contents built separately from equal parts would otherwise compare
/// unequal. <see cref="Equals(InlineContent?)"/> and <see cref="GetHashCode"/> are overridden to walk
/// <see cref="Parts"/> element by element instead.
/// </remarks>
[DebuggerDisplay("InlineContent: {Parts.Length} part(s)")]
public sealed record InlineContent
{
    /// <summary>The characters a text part escapes when the content <see cref="HasCodes"/>, so an HTML fragment tokenizes correctly.</summary>
    private static readonly char[] MarkupEscapeCharacters = ['&', '<', '>'];

    /// <summary>Content with no parts.</summary>
    public static readonly InlineContent Empty = new(ImmutableArray<InlinePart>.Empty);

    /// <summary>The content's parts, in document order.</summary>
    public ImmutableArray<InlinePart> Parts { get; }

    /// <summary>The only constructor; see <see cref="Create(IEnumerable{InlinePart})"/> and <see cref="FromText(string)"/> for the public ways in.</summary>
    /// <param name="parts">The already-normalized parts.</param>
    private InlineContent(ImmutableArray<InlinePart> parts)
    {
        Parts = parts;
    }

    /// <summary>
    /// Builds content from a sequence of parts, normalizing it: adjacent <see cref="InlineTextPart"/>s
    /// merge into one, a default-initialized <paramref name="parts"/> (an uninitialized
    /// <see cref="ImmutableArray{T}"/>, which throws if enumerated directly) is treated the same as an
    /// empty sequence, and either case that ends up with no parts returns <see cref="Empty"/>. This is
    /// the only way to build a non-empty <see cref="InlineContent"/> from parts.
    /// </summary>
    /// <param name="parts">The parts to build content from, in order.</param>
    /// <returns>The normalized content.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="parts"/> is null.</exception>
    public static InlineContent Create(IEnumerable<InlinePart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        if(parts is ImmutableArray<InlinePart> { IsDefault: true })
        {
            return Empty;
        }

        ImmutableArray<InlinePart>.Builder builder = ImmutableArray.CreateBuilder<InlinePart>();
        InlineTextPart? pendingText = null;

        foreach(InlinePart part in parts)
        {
            if(part is InlineTextPart text)
            {
                pendingText = pendingText is null ? text : new InlineTextPart(pendingText.Text + text.Text);

                continue;
            }

            if(pendingText is not null)
            {
                builder.Add(pendingText);
                pendingText = null;
            }

            builder.Add(part);
        }

        if(pendingText is not null)
        {
            builder.Add(pendingText);
        }

        return builder.Count == 0 ? Empty : new InlineContent(builder.ToImmutable());
    }

    /// <summary>
    /// Builds content holding one text part with <paramref name="text"/> verbatim; the text is never
    /// parsed for markup.
    /// </summary>
    /// <param name="text">The content's text.</param>
    /// <returns>The built content, or <see cref="Empty"/> when <paramref name="text"/> is empty.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="text"/> is null.</exception>
    public static InlineContent FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text.Length == 0 ? Empty : new InlineContent(ImmutableArray.Create<InlinePart>(new InlineTextPart(text)));
    }

    /// <summary>Whether the content has no parts.</summary>
    public bool IsEmpty => Parts.IsEmpty;

    /// <summary>Whether the content carries at least one code part (<see cref="PlaceholderPart"/>, <see cref="StartCodePart"/> or <see cref="EndCodePart"/>).</summary>
    public bool HasCodes => Parts.Any(static part => part is PlaceholderPart or StartCodePart or EndCodePart);

    /// <summary>
    /// The content's plain text restricted to what is translatable: <see cref="InlineTranslatability.Walk(InlineContent, ImmutableStack{bool})"/>
    /// starting from an empty stack, so every part starts out translatable unless an enclosing
    /// annotation in this same content says otherwise.
    /// </summary>
    public string TranslatableText => InlineTranslatability.Walk(this, ImmutableStack<bool>.Empty).Text;

    /// <summary>
    /// Renders the content to one string. See the type's remarks for the exact rule; in short, a
    /// <see cref="InlineRendering.Markup"/> render is plain text when the content has no codes and an
    /// HTML fragment when it has at least one, while a <see cref="InlineRendering.Plain"/> render is
    /// always plain text (XLIFF 2.1 §4.7.8's equality type B view).
    /// </summary>
    /// <param name="rendering">Which rendering to produce.</param>
    /// <returns>The rendered string.</returns>
    public string Render(InlineRendering rendering)
    {
        return rendering == InlineRendering.Plain ? RenderPlain() : RenderMarkup();
    }

    /// <inheritdoc cref="object.Equals(object?)"/>
    public bool Equals(InlineContent? other)
    {
        if(other is null)
        {
            return false;
        }

        if(ReferenceEquals(this, other))
        {
            return true;
        }

        return Parts.SequenceEqual(other.Parts);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach(InlinePart part in Parts)
        {
            hash.Add(part);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Renders every text part verbatim and every code part's <c>equiv</c> fallback text, with
    /// nothing escaped and annotation parts contributing nothing.
    /// </summary>
    private string RenderPlain()
    {
        var text = new StringBuilder();
        foreach(InlinePart part in Parts)
        {
            switch(part)
            {
                case InlineTextPart textPart:
                {
                    text.Append(textPart.Text);

                    break;
                }

                case PlaceholderPart placeholder:
                {
                    text.Append(placeholder.Equiv);

                    break;
                }

                case StartCodePart startCode:
                {
                    text.Append(startCode.Equiv);

                    break;
                }

                case EndCodePart endCode:
                {
                    text.Append(endCode.Equiv);

                    break;
                }

                //InlinePart also covers AnnotationStartPart and AnnotationEndPart, which render
                //nothing under either rendering: only text and codes carry visible content.
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// Renders plain text when the content has no codes, or an HTML fragment when it has at least
    /// one: text escaped so it tokenizes correctly, and each code rendered from, in order of
    /// preference, its <see cref="OriginalData"/>, a synthesized tag from
    /// <see cref="WellKnownInlineTokens.TryResolve"/>, its <c>disp</c>, or its escaped <c>equiv</c>.
    /// </summary>
    private string RenderMarkup()
    {
        bool escapeText = HasCodes;
        var markup = new StringBuilder();
        foreach(InlinePart part in Parts)
        {
            switch(part)
            {
                case InlineTextPart textPart:
                {
                    markup.Append(escapeText ? EscapeMarkupText(textPart.Text) : textPart.Text);

                    break;
                }

                case PlaceholderPart placeholder:
                {
                    string codeText = RenderCodeText(placeholder.Type, placeholder.SubType, placeholder.OriginalData, placeholder.Disp, placeholder.Equiv, out bool isTag);
                    markup.Append(isTag ? $"<{codeText}/>" : codeText);

                    break;
                }

                case StartCodePart startCode:
                {
                    string codeText = RenderCodeText(startCode.Type, startCode.SubType, startCode.OriginalData, startCode.Disp, startCode.Equiv, out bool isTag);
                    markup.Append(isTag ? $"<{codeText}>" : codeText);

                    break;
                }

                case EndCodePart endCode:
                {
                    string codeText = RenderCodeText(endCode.Type, endCode.SubType, endCode.OriginalData, endCode.Disp, endCode.Equiv, out bool isTag);
                    markup.Append(isTag ? $"</{codeText}>" : codeText);

                    break;
                }

                //Annotation parts render nothing under Markup either.
            }
        }

        return markup.ToString();
    }

    /// <summary>
    /// Resolves what one code part contributes to a <see cref="InlineRendering.Markup"/> render: its
    /// original data's text verbatim, an element name to wrap in a tag, its display text verbatim, or
    /// its escaped <c>equiv</c>, in that order of preference.
    /// </summary>
    /// <param name="type">The code's reserved kind.</param>
    /// <param name="subType">The code's full <c>prefix:value</c> sub-type, or null when absent.</param>
    /// <param name="originalData">The code's resolved original data, or null when it carries none.</param>
    /// <param name="disp">The code's display text, or null when absent.</param>
    /// <param name="equiv">The code's plain-text stand-in.</param>
    /// <param name="isTag"><see langword="true"/> when the returned text is an element name the caller must wrap in a tag of its own shape; <see langword="false"/> when it is text to append as-is.</param>
    /// <returns>The resolved text or element name.</returns>
    private static string RenderCodeText(InlineCodeType type, string? subType, OriginalData? originalData, string? disp, string equiv, out bool isTag)
    {
        if(originalData is not null)
        {
            isTag = false;

            return originalData.Text;
        }

        if(WellKnownInlineTokens.TryResolve(type, subType, originalData, out string name))
        {
            isTag = true;

            return name;
        }

        isTag = false;

        return disp ?? EscapeMarkupText(equiv);
    }

    /// <summary>Escapes <c>&amp;</c>, <c>&lt;</c> and <c>&gt;</c> so a text run cannot be mistaken for markup inside an HTML fragment.</summary>
    /// <param name="text">The text to escape.</param>
    /// <returns><paramref name="text"/> unchanged when it holds none of the escaped characters; otherwise, the escaped text.</returns>
    private static string EscapeMarkupText(string text)
    {
        if(text.IndexOfAny(MarkupEscapeCharacters) < 0)
        {
            return text;
        }

        var escaped = new StringBuilder(text.Length);
        foreach(char character in text)
        {
            switch(character)
            {
                case '&':
                {
                    escaped.Append("&amp;");

                    break;
                }

                case '<':
                {
                    escaped.Append("&lt;");

                    break;
                }

                case '>':
                {
                    escaped.Append("&gt;");

                    break;
                }

                default:
                {
                    escaped.Append(character);

                    break;
                }
            }
        }

        return escaped.ToString();
    }
}
