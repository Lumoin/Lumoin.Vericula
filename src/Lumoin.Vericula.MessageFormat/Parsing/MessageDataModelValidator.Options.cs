using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>The Duplicate Option Name check.</summary>
internal static partial class MessageDataModelValidator
{
    /// <summary>
    /// Checks every function call's and every markup's options, wherever one occurs in the message
    /// (a declaration's expression, or a placeholder or markup element in any pattern the message
    /// carries), for a repeated identifier, each list checked independently of every other.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <param name="source">The source text the message was parsed from.</param>
    /// <param name="offsets">The offset side table to locate each offending option in.</param>
    /// <returns>One diagnostic per repeated option identifier, in message then option order.</returns>
    private static IEnumerable<MessageFormatDiagnostic> ValidateDuplicateOptionNames(Message message, string source, MessageFormatOffsets offsets)
    {
        foreach(ImmutableArray<Option> options in AllOptionLists(message))
        {
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

            foreach(Option option in options)
            {
                if(!seenNames.Add(option.Name))
                {
                    yield return CreateDiagnostic(WellKnownMessageFormatDiagnostics.DuplicateOptionName,
                        $"The option '{option.Name}' is repeated.", source, offsets.OffsetOf(option));
                }
            }
        }
    }

    /// <summary>Every options list one function call or one markup element in the message owns.</summary>
    /// <param name="message">The message to walk.</param>
    /// <returns>One <see cref="ImmutableArray{T}"/> of <see cref="Option"/> per function call or markup element found.</returns>
    private static IEnumerable<ImmutableArray<Option>> AllOptionLists(Message message)
    {
        foreach(Declaration declaration in DeclarationsOf(message))
        {
            if(FunctionOf(declaration) is FunctionRef function)
            {
                yield return function.Options;
            }
        }

        if(message is PatternMessage patternMessage)
        {
            foreach(ImmutableArray<Option> options in PatternOptionLists(patternMessage.Pattern))
            {
                yield return options;
            }
        }

        if(message is SelectMessage selectMessage)
        {
            foreach(Variant variant in selectMessage.Variants)
            {
                foreach(ImmutableArray<Option> options in PatternOptionLists(variant.Pattern))
                {
                    yield return options;
                }
            }
        }
    }

    /// <summary>Every options list a pattern's own placeholders and markup own, in pattern order.</summary>
    /// <param name="pattern">The pattern to walk.</param>
    /// <returns>One <see cref="ImmutableArray{T}"/> of <see cref="Option"/> per expression's function call or markup element in <paramref name="pattern"/>.</returns>
    private static IEnumerable<ImmutableArray<Option>> PatternOptionLists(Pattern pattern)
    {
        foreach(PatternPart part in pattern.Parts)
        {
            if(part is ExpressionPart { Expression.Function: FunctionRef function })
            {
                yield return function.Options;

                continue;
            }

            if(part is MarkupPart markupPart)
            {
                yield return markupPart.Options;
            }
        }
    }
}
