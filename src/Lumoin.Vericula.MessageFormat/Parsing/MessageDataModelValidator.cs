using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>
/// Checks a syntactically complete <see cref="Message"/> against the data model requirements UTS #35
/// part 9 (MessageFormat), version 48.2, sections "Declarations" and "Matcher" (syntax) and "Data
/// Model Errors" (errors) impose, and reports every violation found rather than stopping at the first
/// one. Unlike a syntax error, a data model error never prevents a model from being built: the caller
/// (<see cref="MessageParser.Parse(string)"/>) always has a complete model in hand by the time this
/// runs, and hands it back together with whatever this method finds. Split, like <see cref="MessageParser"/>
/// itself, into one file per group of checks: this file holds the entry point and the helpers every
/// group shares, <c>.Declarations.cs</c> the Duplicate Declaration check, <c>.Options.cs</c> the
/// Duplicate Option Name check, and <c>.Matcher.cs</c> the four matcher-level checks.
/// </summary>
/// <remarks>
/// Every check below applies a no-cascade rule: a construct already reported under one diagnostic id
/// is not also blamed for a second, different id that its own problem would trivially cause.
/// Concretely, a variant whose key count mismatches the selector count
/// (<see cref="WellKnownMessageFormatDiagnostics.VariantKeyMismatch"/>) is dropped from both the
/// fallback and the duplicate-variant checks, and when every variant is dropped that way, the fallback
/// check itself is skipped rather than reporting a "no fallback" that would just restate the mismatches
/// already reported (proven by the suite's own <c>data-model-errors.json</c> cases 1 and 2, each a
/// single mismatched variant with no other variant to fall back to, whose only expected error is the
/// mismatch).
/// </remarks>
internal static partial class MessageDataModelValidator
{
    /// <summary>
    /// Validates a syntactically complete message, grouped by check (declarations, then option names,
    /// then, for a select message, the matcher's four rules) with each group in source order; the
    /// groups themselves are not interleaved by offset, so within the matcher group a mismatch earlier
    /// in the source can still be reported after a missing-selector-annotation diagnostic that comes
    /// later (see <see cref="ValidateMatcher"/>'s own return order).
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="source">The source text <paramref name="message"/> was parsed from, for turning an offset into a line and position.</param>
    /// <param name="offsets">The offset side table <see cref="MessageParser"/> built while parsing <paramref name="message"/>.</param>
    /// <returns>Every data model diagnostic found; empty when <paramref name="message"/> is valid.</returns>
    internal static ImmutableArray<MessageFormatDiagnostic> Validate(Message message, string source, MessageFormatOffsets offsets)
    {
        ImmutableArray<Declaration> declarations = DeclarationsOf(message);

        IEnumerable<MessageFormatDiagnostic> diagnostics = ValidateDeclarations(declarations, source, offsets)
            .Concat(ValidateDuplicateOptionNames(message, source, offsets));

        if(message is SelectMessage selectMessage)
        {
            diagnostics = diagnostics.Concat(ValidateMatcher(selectMessage, source, offsets));
        }

        return [.. diagnostics];
    }

    /// <summary>The declarations a message carries, regardless of which concrete message it is.</summary>
    /// <param name="message">The message to read.</param>
    /// <returns><paramref name="message"/>'s declarations.</returns>
    private static ImmutableArray<Declaration> DeclarationsOf(Message message) => message switch
    {
        PatternMessage patternMessage => patternMessage.Declarations,
        SelectMessage selectMessage => selectMessage.Declarations,
        _ => ImmutableArray<Declaration>.Empty
    };

    /// <summary>The function a declaration's right-hand side applies, if any, regardless of which concrete declaration it is.</summary>
    /// <param name="declaration">The declaration to read.</param>
    /// <returns>The function <paramref name="declaration"/>'s expression carries, or <see langword="null"/> when it has none.</returns>
    private static FunctionRef? FunctionOf(Declaration declaration) => declaration switch
    {
        InputDeclaration input => input.Value.Function,
        LocalDeclaration local => local.Value.Function,
        _ => null
    };

    /// <summary>Builds one diagnostic, deriving its line and position from its offset.</summary>
    /// <param name="id">The diagnostic id, from <see cref="WellKnownMessageFormatDiagnostics"/>.</param>
    /// <param name="message">A human-readable description of the finding.</param>
    /// <param name="source">The source text <paramref name="offset"/> is an index into.</param>
    /// <param name="offset">The zero-based UTF-16 offset of the offending construct.</param>
    /// <returns>The composed diagnostic.</returns>
    private static MessageFormatDiagnostic CreateDiagnostic(string id, string message, string source, int offset)
    {
        (int line, int position) = MessageFormatPosition.Locate(source, offset);

        return new MessageFormatDiagnostic(id, message, offset, line, position);
    }
}
