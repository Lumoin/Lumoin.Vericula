using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>Expressions, markup, functions, options and attributes: the placeholder-level productions.</summary>
internal ref partial struct MessageParser
{
    /// <summary>
    /// Parses one of the three expression forms, each of which owns its own <c>{ o ... o }</c>
    /// delimiters: <c>literal-expression</c>, <c>variable-expression</c>, or <c>function-expression</c>,
    /// chosen by whether the operand position starts with <c>:</c>, <c>$</c>, or a literal.
    /// </summary>
    /// <returns>The parsed expression, or <see langword="null"/> on a syntax error.</returns>
    private Expression? ParseExpression()
    {
        ConsumeChar(MessageFormatSigils.OpenBrace, "'{' to start an expression");
        if(Failed)
        {
            return null;
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        char c = PeekCharRaw();
        if(c == MessageFormatSigils.Colon)
        {
            FunctionRef? function = ParseFunction();
            if(Failed || function is null)
            {
                return null;
            }

            if(!TryParseFunctionAndAttributesTail(function, out FunctionRef? finalFunction, out ImmutableArray<MessageAttribute> attributes))
            {
                return null;
            }

            ConsumeChar(MessageFormatSigils.CloseBrace, "'}' to close the expression");
            if(Failed)
            {
                return null;
            }

            return new FunctionExpression(finalFunction!, attributes);
        }

        if(c == MessageFormatSigils.Dollar)
        {
            Variable? variable = ParseVariable();
            if(Failed || variable is null)
            {
                return null;
            }

            if(!TryParseFunctionAndAttributesTail(null, out FunctionRef? function, out ImmutableArray<MessageAttribute> attributes))
            {
                return null;
            }

            ConsumeChar(MessageFormatSigils.CloseBrace, "'}' to close the expression");
            if(Failed)
            {
                return null;
            }

            return new VariableExpression(variable, function, attributes);
        }

        Literal? literal = ParseLiteral();
        if(Failed || literal is null)
        {
            return null;
        }

        if(!TryParseFunctionAndAttributesTail(null, out FunctionRef? literalFunction, out ImmutableArray<MessageAttribute> literalAttributes))
        {
            return null;
        }

        ConsumeChar(MessageFormatSigils.CloseBrace, "'}' to close the expression");
        if(Failed)
        {
            return null;
        }

        return new LiteralExpression(literal, literalFunction, literalAttributes);
    }

    /// <summary>
    /// Parses the tail shared by every expression form after its operand (or, for a function
    /// expression, after its function): an optional <c>[s function]</c> (only when
    /// <paramref name="initialFunction"/> is <see langword="null"/>, meaning none has been parsed yet)
    /// followed by <c>*(s attribute)</c>, up to but not including the closing <c>}</c>. Each candidate
    /// whitespace run is checked for a genuine <c>ws</c> character, since an <c>s</c> is required
    /// before both a function and an attribute here.
    /// </summary>
    /// <param name="initialFunction">The function already parsed (for a function expression), or <see langword="null"/> when one may still follow.</param>
    /// <param name="function">The expression's function: <paramref name="initialFunction"/>, or the one found in the tail, or <see langword="null"/> when none is present.</param>
    /// <param name="attributes">The expression's attributes, in source order.</param>
    /// <returns><see langword="true"/> on success; <see langword="false"/> on a syntax error.</returns>
    private bool TryParseFunctionAndAttributesTail(FunctionRef? initialFunction, out FunctionRef? function, out ImmutableArray<MessageAttribute> attributes)
    {
        function = initialFunction;
        attributes = ImmutableArray<MessageAttribute>.Empty;

        var builder = ImmutableArray.CreateBuilder<MessageAttribute>();

        while(true)
        {
            bool sawWhitespace = SkipOptionalWhitespaceTrackingRealWhitespace();
            if(Failed)
            {
                return false;
            }

            char next = PeekCharRaw();
            if(next == MessageFormatSigils.CloseBrace)
            {
                break;
            }

            if(next == MessageFormatSigils.Colon)
            {
                if(builder.Count > 0)
                {
                    Fail(_index, ExpectedButFound("'}' or another attribute; a function must come before every attribute"));

                    return false;
                }

                if(function is not null)
                {
                    Fail(_index, ExpectedButFound("'}' or an attribute; an expression has at most one function"));

                    return false;
                }

                if(!sawWhitespace)
                {
                    Fail(_index, ExpectedButFound("whitespace before the function"));

                    return false;
                }

                function = ParseFunction();
                if(Failed || function is null)
                {
                    return false;
                }

                continue;
            }

            if(next == MessageFormatSigils.At)
            {
                if(!sawWhitespace)
                {
                    Fail(_index, ExpectedButFound("whitespace before the attribute"));

                    return false;
                }

                MessageAttribute? attribute = ParseAttribute();
                if(Failed || attribute is null)
                {
                    return false;
                }

                builder.Add(attribute);

                continue;
            }

            Fail(_index, ExpectedButFound("'}', a function, or an attribute"));

            return false;
        }

        attributes = builder.ToImmutable();

        return true;
    }

    /// <summary>
    /// Parses one of the two <c>markup</c> alternatives, whose own opening <c>{ o</c> has not yet been
    /// consumed: <c>"#" identifier *(s option) *(s attribute) o ["/"]</c> for an open or standalone
    /// element, or <c>"/" identifier *(s option) *(s attribute) o</c> for a close element, either way
    /// followed by <c>"}"</c>.
    /// </summary>
    /// <returns>The parsed markup part, or <see langword="null"/> on a syntax error.</returns>
    private MarkupPart? ParseMarkup()
    {
        ConsumeChar(MessageFormatSigils.OpenBrace, "'{' to start markup");
        if(Failed)
        {
            return null;
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        char opener = PeekCharRaw();
        if(opener != MessageFormatSigils.Hash && opener != MessageFormatSigils.Slash)
        {
            Fail(_index, ExpectedButFound("'#' or '/' to start markup"));

            return null;
        }

        _index++;

        string? name = ParseIdentifier();
        if(Failed || name is null)
        {
            return null;
        }

        var options = ImmutableArray.CreateBuilder<Option>();
        while(true)
        {
            int before = _index;
            bool sawWhitespace = SkipOptionalWhitespaceTrackingRealWhitespace();
            if(Failed)
            {
                return null;
            }

            if(!NextLooksLikeIdentifierStart())
            {
                _index = before;

                break;
            }

            if(!sawWhitespace)
            {
                Fail(_index, ExpectedButFound("whitespace before the option"));

                return null;
            }

            Option? option = ParseOption();
            if(Failed || option is null)
            {
                return null;
            }

            options.Add(option);
        }

        var attributes = ImmutableArray.CreateBuilder<MessageAttribute>();
        while(true)
        {
            int before = _index;
            bool sawWhitespace = SkipOptionalWhitespaceTrackingRealWhitespace();
            if(Failed)
            {
                return null;
            }

            if(PeekCharRaw() != MessageFormatSigils.At)
            {
                _index = before;

                break;
            }

            if(!sawWhitespace)
            {
                Fail(_index, ExpectedButFound("whitespace before the attribute"));

                return null;
            }

            MessageAttribute? attribute = ParseAttribute();
            if(Failed || attribute is null)
            {
                return null;
            }

            attributes.Add(attribute);
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        MarkupKind kind = opener == MessageFormatSigils.Hash ? MarkupKind.Open : MarkupKind.Close;
        if(kind == MarkupKind.Open && PeekCharRaw() == MessageFormatSigils.Slash)
        {
            _index++;
            kind = MarkupKind.Standalone;
        }

        ConsumeChar(MessageFormatSigils.CloseBrace, "'}' to close the markup");
        if(Failed)
        {
            return null;
        }

        return new MarkupPart(kind, name, options.ToImmutable(), attributes.ToImmutable());
    }

    /// <summary>Parses <c>function = ":" identifier *(s option)</c>.</summary>
    /// <returns>The parsed function reference, or <see langword="null"/> on a syntax error.</returns>
    private FunctionRef? ParseFunction()
    {
        ConsumeChar(MessageFormatSigils.Colon, "':' to start a function");
        if(Failed)
        {
            return null;
        }

        string? name = ParseIdentifier();
        if(Failed || name is null)
        {
            return null;
        }

        var options = ImmutableArray.CreateBuilder<Option>();
        while(true)
        {
            int before = _index;
            bool sawWhitespace = SkipOptionalWhitespaceTrackingRealWhitespace();
            if(Failed)
            {
                return null;
            }

            if(!NextLooksLikeIdentifierStart())
            {
                _index = before;

                break;
            }

            if(!sawWhitespace)
            {
                Fail(_index, ExpectedButFound("whitespace before the option"));

                return null;
            }

            Option? option = ParseOption();
            if(Failed || option is null)
            {
                return null;
            }

            options.Add(option);
        }

        return new FunctionRef(name, options.ToImmutable());
    }

    /// <summary>Parses <c>option = identifier o "=" o (literal / variable)</c>.</summary>
    /// <returns>The parsed option, or <see langword="null"/> on a syntax error.</returns>
    private Option? ParseOption()
    {
        int optionStart = _index;

        string? name = ParseIdentifier();
        if(Failed || name is null)
        {
            return null;
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        ConsumeChar(MessageFormatSigils.EqualsSign, "'=' after the option name");
        if(Failed)
        {
            return null;
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        Operand? value = ParseOperand();
        if(Failed || value is null)
        {
            return null;
        }

        var option = new Option(name, value);
        _offsets.Record(option, optionStart);

        return option;
    }

    /// <summary>Parses <c>attribute = "@" identifier [o "=" o literal]</c>.</summary>
    /// <returns>The parsed attribute, or <see langword="null"/> on a syntax error.</returns>
    private MessageAttribute? ParseAttribute()
    {
        ConsumeChar(MessageFormatSigils.At, "'@' to start an attribute");
        if(Failed)
        {
            return null;
        }

        string? name = ParseIdentifier();
        if(Failed || name is null)
        {
            return null;
        }

        int before = _index;
        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        if(PeekCharRaw() != MessageFormatSigils.EqualsSign)
        {
            _index = before;

            return new MessageAttribute(name, null);
        }

        _index++;

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        Literal? value = ParseLiteral();
        if(Failed || value is null)
        {
            return null;
        }

        return new MessageAttribute(name, value);
    }

    /// <summary>Parses an operand: a <c>variable</c> when the cursor starts with <c>$</c>, otherwise a <c>literal</c>.</summary>
    /// <returns>The parsed operand, or <see langword="null"/> on a syntax error.</returns>
    private Operand? ParseOperand()
    {
        if(PeekCharRaw() == MessageFormatSigils.Dollar)
        {
            return ParseVariable();
        }

        return ParseLiteral();
    }
}
