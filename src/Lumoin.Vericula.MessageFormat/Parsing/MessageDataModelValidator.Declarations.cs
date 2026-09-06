using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>The Duplicate Declaration check.</summary>
internal static partial class MessageDataModelValidator
{
    /// <summary>
    /// Checks every declaration against the three rules of the spec's "Declarations" section, folded
    /// into one pass: a declaration is a <see cref="WellKnownMessageFormatDiagnostics.DuplicateDeclaration"/>
    /// when the name it binds already occurred anywhere in an earlier declaration (as that earlier
    /// declaration's own bound name, an operand, or an option value: exactly what
    /// <see cref="UsedNames"/> collects, which subsumes plain "same name twice"), or, failing that, when
    /// its own right-hand side refers to the very name it is binding (an input declaration's own
    /// function options, or a local declaration's own operand or option values: what
    /// <see cref="SelfReferencedNames"/> collects). At most one diagnostic is raised per declaration,
    /// the first rule that applies; a declaration that violates neither still contributes its own names
    /// to what later declarations are checked against.
    /// </summary>
    /// <param name="declarations">The message's declarations, in source order.</param>
    /// <param name="source">The source text the declarations were parsed from.</param>
    /// <param name="offsets">The offset side table to locate each offending declaration in.</param>
    /// <returns>One diagnostic per offending declaration, in source order.</returns>
    private static IEnumerable<MessageFormatDiagnostic> ValidateDeclarations(ImmutableArray<Declaration> declarations, string source, MessageFormatOffsets offsets)
    {
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach(Declaration declaration in declarations)
        {
            if(seenNames.Contains(declaration.Name))
            {
                yield return CreateDiagnostic(WellKnownMessageFormatDiagnostics.DuplicateDeclaration,
                    $"The variable '${declaration.Name}' is already declared.", source, offsets.OffsetOf(declaration));
            }
            else if(SelfReferencedNames(declaration).Contains(declaration.Name))
            {
                yield return CreateDiagnostic(WellKnownMessageFormatDiagnostics.DuplicateDeclaration,
                    $"The declaration of '${declaration.Name}' refers to itself.", source, offsets.OffsetOf(declaration));
            }

            foreach(string name in UsedNames(declaration))
            {
                seenNames.Add(name);
            }
        }
    }

    /// <summary>Enumerates every variable name a function call's options reference by value, in option order.</summary>
    /// <param name="function">The function to inspect, or <see langword="null"/> for none.</param>
    /// <returns>The variable-valued options' variable names.</returns>
    private static IEnumerable<string> OptionVariableNames(FunctionRef? function)
    {
        if(function is null)
        {
            yield break;
        }

        foreach(Option option in function.Options)
        {
            if(option.Value is Variable variable)
            {
                yield return variable.Name;
            }
        }
    }

    /// <summary>
    /// Every variable name a declaration refers to on its own right-hand side: any variable named as an
    /// option value of its function call, and, for a local declaration whose expression is a bare
    /// variable, that variable's own name. Deliberately excludes, for an input declaration, the
    /// mandatory operand that always equals its own bound name (the spec requires
    /// <c>InputDeclaration.Value.Variable.Name == InputDeclaration.Name</c>; that equality is not itself
    /// a self-reference error, only a further use of the same name is).
    /// </summary>
    /// <param name="declaration">The declaration to inspect.</param>
    /// <returns>The names <paramref name="declaration"/>'s own right-hand side refers to.</returns>
    private static IEnumerable<string> SelfReferencedNames(Declaration declaration)
    {
        foreach(string name in OptionVariableNames(FunctionOf(declaration)))
        {
            yield return name;
        }

        if(declaration is LocalDeclaration { Value: VariableExpression variableExpression })
        {
            yield return variableExpression.Variable.Name;
        }
    }

    /// <summary>Every variable name that occurs anywhere within a declaration, including its own bound name.</summary>
    /// <param name="declaration">The declaration to inspect.</param>
    /// <returns><paramref name="declaration"/>'s own bound name, followed by <see cref="SelfReferencedNames"/>.</returns>
    private static IEnumerable<string> UsedNames(Declaration declaration)
    {
        yield return declaration.Name;

        foreach(string name in SelfReferencedNames(declaration))
        {
            yield return name;
        }
    }
}
