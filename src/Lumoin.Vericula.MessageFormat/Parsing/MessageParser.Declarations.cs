using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>Input and local declarations.</summary>
internal ref partial struct MessageParser
{
    /// <summary>
    /// Parses <c>input-declaration = input o variable-expression</c> (the <c>.input</c> keyword
    /// already consumed by the caller; note the optional <c>o</c>, unlike <c>local-declaration</c>'s
    /// required <c>s</c>). The declared name is not parsed separately: it is the variable named
    /// inside the variable-expression itself, which is exactly what the spec's rule that the two must
    /// match requires.
    /// </summary>
    /// <returns>The parsed declaration, or <see langword="null"/> on a syntax error.</returns>
    private InputDeclaration? ParseInputDeclaration()
    {
        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        VariableExpression? expression = ParseVariableExpressionOnly();
        if(Failed || expression is null)
        {
            return null;
        }

        return new InputDeclaration(expression.Variable.Name, expression);
    }

    /// <summary>
    /// Parses a <c>variable-expression</c> for <see cref="ParseInputDeclaration"/>, which the grammar
    /// restricts to exactly this expression form (never a literal, function-only, or markup
    /// placeholder).
    /// </summary>
    /// <returns>The parsed variable expression, or <see langword="null"/> on a syntax error.</returns>
    private VariableExpression? ParseVariableExpressionOnly()
    {
        ConsumeChar(MessageFormatSigils.OpenBrace, "'{' to start a variable expression");
        if(Failed)
        {
            return null;
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        Variable? variable = ParseVariable();
        if(Failed || variable is null)
        {
            return null;
        }

        if(!TryParseFunctionAndAttributesTail(null, out FunctionRef? function, out ImmutableArray<MessageAttribute> attributes))
        {
            return null;
        }

        ConsumeChar(MessageFormatSigils.CloseBrace, "'}' to close the variable expression");
        if(Failed)
        {
            return null;
        }

        return new VariableExpression(variable, function, attributes);
    }

    /// <summary>
    /// Parses <c>local-declaration = local s variable o "=" o expression</c> (the <c>.local</c>
    /// keyword already consumed by the caller).
    /// </summary>
    /// <returns>The parsed declaration, or <see langword="null"/> on a syntax error.</returns>
    private LocalDeclaration? ParseLocalDeclaration()
    {
        if(!RequireWhitespace("after '.local'"))
        {
            return null;
        }

        Variable? variable = ParseVariable();
        if(Failed || variable is null)
        {
            return null;
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        ConsumeChar(MessageFormatSigils.EqualsSign, "'=' after the local variable");
        if(Failed)
        {
            return null;
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        Expression? expression = ParseExpression();
        if(Failed || expression is null)
        {
            return null;
        }

        return new LocalDeclaration(variable.Name, expression);
    }
}
