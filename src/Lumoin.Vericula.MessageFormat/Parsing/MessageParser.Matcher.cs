using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Parsing;

/// <summary>The matcher: selectors, variants and keys.</summary>
internal ref partial struct MessageParser
{
    /// <summary>
    /// Parses <c>matcher = match-statement s variant *(o variant)</c>, where
    /// <c>match-statement = match 1*(s selector)</c> and <c>selector = variable</c> (the <c>.match</c>
    /// keyword already consumed by the caller). The gap between the last selector and the mandatory
    /// first variant satisfies both <c>match-statement</c>'s own trailing requirement and
    /// <c>matcher</c>'s separate <c>s</c> with a single whitespace check, since consuming the run
    /// greedily satisfies either reading of the grammar identically.
    /// </summary>
    /// <param name="declarations">The declarations already parsed, carried into the resulting message.</param>
    /// <param name="matchOffset">The offset the <c>.match</c> keyword itself started at, recorded against the resulting <see cref="SelectMessage"/> for a <see cref="WellKnownMessageFormatDiagnostics.MissingFallbackVariant"/> diagnostic, which blames the matcher as a whole rather than any one variant.</param>
    /// <returns>The parsed message, or <see langword="null"/> on a syntax error.</returns>
    private SelectMessage? ParseMatcher(ImmutableArray<Declaration> declarations, int matchOffset)
    {
        var selectors = ImmutableArray.CreateBuilder<Variable>();

        while(true)
        {
            if(!RequireWhitespace("before a selector"))
            {
                return null;
            }

            int selectorStart = _index;
            Variable? selector = ParseVariable();
            if(Failed || selector is null)
            {
                return null;
            }

            _offsets.Record(selector, selectorStart);

            selectors.Add(selector);

            int afterWhitespace = PeekPastOptionalWhitespaceFrom(_index);
            if(PeekCharAt(afterWhitespace) != MessageFormatSigils.Dollar)
            {
                break;
            }
        }

        if(!RequireWhitespace("before a variant"))
        {
            return null;
        }

        var variants = ImmutableArray.CreateBuilder<Variant>();

        Variant? firstVariant = ParseVariant();
        if(Failed || firstVariant is null)
        {
            return null;
        }

        variants.Add(firstVariant);

        while(true)
        {
            int before = _index;
            SkipOptionalWhitespace();
            if(Failed)
            {
                return null;
            }

            if(!LooksLikeKeyStart())
            {
                _index = before;

                break;
            }

            Variant? variant = ParseVariant();
            if(Failed || variant is null)
            {
                return null;
            }

            variants.Add(variant);
        }

        var selectMessage = new SelectMessage(declarations, selectors.ToImmutable(), variants.ToImmutable());
        _offsets.Record(selectMessage, matchOffset);

        return selectMessage;
    }

    /// <summary>Parses <c>variant = key *(s key) o quoted-pattern</c>.</summary>
    /// <returns>The parsed variant, or <see langword="null"/> on a syntax error.</returns>
    private Variant? ParseVariant()
    {
        int variantStart = _index;
        var keys = ImmutableArray.CreateBuilder<VariantKey>();

        VariantKey? firstKey = ParseVariantKey();
        if(Failed || firstKey is null)
        {
            return null;
        }

        keys.Add(firstKey);

        while(true)
        {
            int before = _index;
            bool sawWhitespace = SkipOptionalWhitespaceTrackingRealWhitespace();
            if(Failed)
            {
                return null;
            }

            if(!LooksLikeKeyStart())
            {
                _index = before;

                break;
            }

            if(!sawWhitespace)
            {
                Fail(_index, ExpectedButFound("whitespace before the next key"));

                return null;
            }

            VariantKey? key = ParseVariantKey();
            if(Failed || key is null)
            {
                return null;
            }

            keys.Add(key);
        }

        SkipOptionalWhitespace();
        if(Failed)
        {
            return null;
        }

        Pattern? pattern = ParseQuotedPattern();
        if(Failed || pattern is null)
        {
            return null;
        }

        var variant = new Variant(keys.ToImmutable(), pattern);
        _offsets.Record(variant, variantStart);

        return variant;
    }

    /// <summary>Parses <c>key = literal / "*"</c>.</summary>
    /// <returns>The parsed key, or <see langword="null"/> on a syntax error.</returns>
    private VariantKey? ParseVariantKey()
    {
        if(PeekCharRaw() == MessageFormatSigils.Asterisk)
        {
            _index++;

            return new CatchallKey();
        }

        Literal? literal = ParseLiteral();
        if(Failed || literal is null)
        {
            return null;
        }

        return new LiteralKey(literal);
    }
}
