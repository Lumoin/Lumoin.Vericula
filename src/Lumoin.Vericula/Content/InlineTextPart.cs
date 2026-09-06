using System.Diagnostics;

namespace Lumoin.Vericula.Content;

/// <summary>
/// A run of text within an <see cref="InlineContent"/>. Named <see cref="InlineTextPart"/>, not
/// <c>TextPart</c>, because <c>Lumoin.Vericula.MessageFormat.DataModel.TextPart</c> already exists in
/// the assembly the MessageFormat library references this one from.
/// </summary>
/// <remarks>
/// Characters invalid in XML, which XLIFF writes as <c>&lt;cp hex="..."/&gt;</c>, are carried inside
/// <see cref="Text"/> as themselves; <c>cp</c> is a serialization detail the writer and reader handle,
/// not something the model represents separately. <see cref="InlineContent.Create(System.Collections.Generic.IEnumerable{InlinePart})"/>
/// merges adjacent text parts, so a run built by hand never needs to anticipate that; whitespace-only
/// text between codes is still text and is kept, never dropped.
/// </remarks>
[DebuggerDisplay("InlineTextPart: {Text}")]
public sealed record InlineTextPart : InlinePart
{
    /// <summary>The part's text; never empty.</summary>
    public string Text { get; }

    /// <summary>Creates a text part.</summary>
    /// <param name="text">The part's text; must not be empty.</param>
    /// <exception cref="ArgumentException">If <paramref name="text"/> is empty.</exception>
    public InlineTextPart(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        Text = text;
    }
}
