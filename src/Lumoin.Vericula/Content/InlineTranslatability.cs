using System.Collections.Immutable;
using System.Text;

namespace Lumoin.Vericula.Content;

/// <summary>
/// Walks an <see cref="InlineContent"/>'s parts to find the text that is translatable given nested
/// <c>translate</c> overrides, carrying its stack state across calls so a unit-level check can walk a
/// unit's segments in document order without losing whether an annotation opened in an earlier
/// segment is still open. <see cref="InlineContent.TranslatableText"/> is this walk from an empty
/// stack, restricted to one content.
/// </summary>
public static class InlineTranslatability
{
    /// <summary>
    /// Walks <paramref name="content"/>'s parts, accumulating the <see cref="InlineRendering.Plain"/>
    /// text of every part that is translatable given <paramref name="stack"/>, and returns the stack
    /// as it stands once the walk ends (an annotation left open, such as a split <c>sm</c> with no
    /// <c>em</c> in this content, stays on it, so a caller walking a unit's later segments sees it as
    /// still in effect).
    /// </summary>
    /// <param name="content">The content to walk.</param>
    /// <param name="stack">
    /// The incoming stack of nested <c>translate</c> overrides, innermost on top; each entry is the
    /// effective value in force at that nesting level, seeded by "translatable" (true) when the
    /// stack is empty.
    /// </param>
    /// <returns>The translatable text found, and the stack once the walk ends.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="content"/> or <paramref name="stack"/> is null.</exception>
    public static (string Text, ImmutableStack<bool> Stack) Walk(InlineContent content, ImmutableStack<bool> stack)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(stack);

        var text = new StringBuilder();
        foreach(InlinePart part in content.Parts)
        {
            switch(part)
            {
                case InlineTextPart textPart:
                {
                    if(CurrentlyTranslatable(stack))
                    {
                        text.Append(textPart.Text);
                    }

                    break;
                }

                case PlaceholderPart placeholder:
                {
                    if(CurrentlyTranslatable(stack))
                    {
                        text.Append(placeholder.Equiv);
                    }

                    break;
                }

                case StartCodePart startCode:
                {
                    if(CurrentlyTranslatable(stack))
                    {
                        text.Append(startCode.Equiv);
                    }

                    break;
                }

                case EndCodePart endCode:
                {
                    if(CurrentlyTranslatable(stack))
                    {
                        text.Append(endCode.Equiv);
                    }

                    break;
                }

                case AnnotationStartPart annotationStart:
                {
                    bool effective = annotationStart.Translate ?? CurrentlyTranslatable(stack);
                    stack = stack.Push(effective);

                    break;
                }

                case AnnotationEndPart:
                {
                    //A close with nothing open (a malformed or cross-boundary split annotation) is
                    //tolerated as a no-op rather than thrown from a text-only walk; the reader and
                    //writer are where such mismatches are refused.
                    if(!stack.IsEmpty)
                    {
                        stack = stack.Pop();
                    }

                    break;
                }
            }
        }

        return (text.ToString(), stack);
    }

    /// <summary>Whether text at the current nesting level is translatable: the top of <paramref name="stack"/>, or true (the seed) when it is empty.</summary>
    /// <param name="stack">The stack of nested <c>translate</c> overrides, innermost on top.</param>
    /// <returns><see langword="true"/> if text at this nesting level is translatable; otherwise, <see langword="false"/>.</returns>
    private static bool CurrentlyTranslatable(ImmutableStack<bool> stack) => stack.IsEmpty || stack.Peek();
}
