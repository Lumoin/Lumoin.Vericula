using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>The four matcher-level checks: Variant Key Mismatch, Missing Fallback Variant, Duplicate Variant, and Missing Selector Annotation.</summary>
internal static partial class MessageDataModelValidator
{
    /// <summary>
    /// Checks a select message's matcher against all four matcher-level rules, applying the no-cascade
    /// exclusions documented on the type: a variant is first checked for a key-count mismatch and, if
    /// found wanting, excluded from every later check; the fallback check itself runs only when at
    /// least one variant survived that exclusion.
    /// </summary>
    /// <param name="select">The select message to check.</param>
    /// <param name="source">The source text the message was parsed from.</param>
    /// <param name="offsets">The offset side table to locate each offending construct in.</param>
    /// <returns>Every matcher-level diagnostic found, in the order: mismatches, the fallback check, duplicate variants, then missing selector annotations.</returns>
    private static IEnumerable<MessageFormatDiagnostic> ValidateMatcher(SelectMessage select, string source, MessageFormatOffsets offsets)
    {
        int selectorCount = select.Selectors.Length;
        var remaining = new List<Variant>();

        foreach(Variant variant in select.Variants)
        {
            if(variant.Keys.Length != selectorCount)
            {
                yield return CreateDiagnostic(WellKnownMessageFormatDiagnostics.VariantKeyMismatch,
                    $"This variant has {variant.Keys.Length} key(s) but the matcher has {selectorCount} selector(s).",
                    source, offsets.OffsetOf(variant));

                continue;
            }

            remaining.Add(variant);
        }

        if(remaining.Count > 0 && !remaining.Any(IsAllCatchall))
        {
            yield return CreateDiagnostic(WellKnownMessageFormatDiagnostics.MissingFallbackVariant,
                "The matcher has no variant whose keys are all the catch-all key.", source, offsets.OffsetOf(select));
        }

        var seenKeyLists = new List<ImmutableArray<VariantKey>>();
        foreach(Variant variant in remaining)
        {
            if(seenKeyLists.Any(seenKeys => KeyListsEqual(seenKeys, variant.Keys)))
            {
                yield return CreateDiagnostic(WellKnownMessageFormatDiagnostics.DuplicateVariant,
                    "Another variant already uses this list of keys.", source, offsets.OffsetOf(variant));

                continue;
            }

            seenKeyLists.Add(variant.Keys);
        }

        Dictionary<string, Declaration> declarationsByName = DeclarationsByName(select.Declarations);
        foreach(Variable selector in select.Selectors)
        {
            if(!SelectorReachesFunction(selector.Name, declarationsByName))
            {
                yield return CreateDiagnostic(WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation,
                    $"The selector '${selector.Name}' does not directly or indirectly reference a declaration with a function.",
                    source, offsets.OffsetOf(selector));
            }
        }
    }

    /// <summary>Whether every key of a variant is the catch-all key.</summary>
    /// <param name="variant">The variant to inspect.</param>
    /// <returns><see langword="true"/> if <paramref name="variant"/>'s keys are all <see cref="CatchallKey"/>; otherwise, <see langword="false"/>.</returns>
    private static bool IsAllCatchall(Variant variant) => variant.Keys.All(static key => key is CatchallKey);

    /// <summary>
    /// Compares two variants' key lists for the equality the spec's "Duplicate Variant" rule uses: the
    /// same length, and each pair of keys either both the catch-all or both a literal whose NFC-
    /// normalized string values match ordinally (never a catch-all against a literal, even one whose
    /// value happens to be <c>*</c>: a quoted <c>|*|</c> is a literal, not the catch-all). Both lists are
    /// always the same length in practice, since every caller draws both from variants that already
    /// passed the key-count check against the same selector count.
    /// </summary>
    /// <param name="left">One variant's keys.</param>
    /// <param name="right">Another variant's keys.</param>
    /// <returns><see langword="true"/> if the two key lists are equal by the spec's rule; otherwise, <see langword="false"/>.</returns>
    private static bool KeyListsEqual(ImmutableArray<VariantKey> left, ImmutableArray<VariantKey> right)
    {
        if(left.Length != right.Length)
        {
            return false;
        }

        for(int index = 0; index < left.Length; index++)
        {
            if(!KeysEqual(left[index], right[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Compares two variant keys for the spec's "Duplicate Variant" equality: see <see cref="KeyListsEqual"/>.</summary>
    /// <param name="left">One key.</param>
    /// <param name="right">Another key.</param>
    /// <returns><see langword="true"/> if the two keys are equal; otherwise, <see langword="false"/>.</returns>
    private static bool KeysEqual(VariantKey left, VariantKey right) => (left, right) switch
    {
        (CatchallKey, CatchallKey) => true,
        (LiteralKey leftLiteral, LiteralKey rightLiteral) =>
            string.Equals(MessageFormatCharacterClasses.NormalizeToNfc(leftLiteral.Literal.Value), MessageFormatCharacterClasses.NormalizeToNfc(rightLiteral.Literal.Value), StringComparison.Ordinal),
        _ => false
    };

    /// <summary>Maps every declared name to its declaration; a name declared more than once maps to the last such declaration in source order.</summary>
    /// <remarks>
    /// A message with two declarations sharing a name is already invalid (<see cref="WellKnownMessageFormatDiagnostics.DuplicateDeclaration"/>
    /// covers it), so this last-wins choice only affects <see cref="SelectorReachesFunction"/>'s walk on
    /// an already-invalid message: an earlier declaration that carried a function is not enough by
    /// itself to satisfy a selector once a later declaration for the same name drops the function, so
    /// fixing only the duplicate-declaration diagnostic can also add or remove a
    /// <see cref="WellKnownMessageFormatDiagnostics.MissingSelectorAnnotation"/> alongside it.
    /// </remarks>
    /// <param name="declarations">The declarations to index.</param>
    /// <returns>The name-to-declaration map.</returns>
    private static Dictionary<string, Declaration> DeclarationsByName(ImmutableArray<Declaration> declarations)
    {
        var declarationsByName = new Dictionary<string, Declaration>(StringComparer.Ordinal);

        foreach(Declaration declaration in declarations)
        {
            declarationsByName[declaration.Name] = declaration;
        }

        return declarationsByName;
    }

    /// <summary>
    /// Walks the chain the spec's "Missing Selector Annotation" rule describes: starting from a
    /// selector's own variable name, following each local declaration whose expression is a bare
    /// variable expression with no function to that variable's own declaration, until either a
    /// declaration whose expression carries a function is reached (a hit, whether that declaration is
    /// an input or a local declaration), or the chain runs out (an undeclared name, a declaration whose
    /// expression has neither a function nor is a bare variable, or a cycle back to an already-visited
    /// name: each a miss).
    /// </summary>
    /// <param name="variableName">The selector's own variable name to start the walk from.</param>
    /// <param name="declarationsByName">Every declaration in scope, by name.</param>
    /// <returns><see langword="true"/> if the chain reaches a declaration with a function; otherwise, <see langword="false"/>.</returns>
    private static bool SelectorReachesFunction(string variableName, Dictionary<string, Declaration> declarationsByName)
    {
        var visitedNames = new HashSet<string>(StringComparer.Ordinal);
        string currentName = variableName;

        while(visitedNames.Add(currentName))
        {
            if(!declarationsByName.TryGetValue(currentName, out Declaration? declaration))
            {
                return false;
            }

            if(declaration is InputDeclaration inputDeclaration)
            {
                return inputDeclaration.Value.Function is not null;
            }

            if(declaration is not LocalDeclaration localDeclaration)
            {
                return false;
            }

            if(localDeclaration.Value.Function is not null)
            {
                return true;
            }

            if(localDeclaration.Value is not VariableExpression bareVariable)
            {
                return false;
            }

            currentName = bareVariable.Variable.Name;
        }

        return false;
    }
}
