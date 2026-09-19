namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// A CLDR plural category: the outcome of matching a number against a locale's plural rules. See
/// UTS #35 part 9 (MessageFormat), version 48.2 (functions/number.md), where <c>:number</c> and
/// <c>:integer</c> selection is defined; see also the Unicode CLDR plural rules, which define these
/// categories.
/// </summary>
/// <remarks>
/// Consumed by the number backend seam landing in a later step; declared now so the MessageFormat
/// assembly's enum census is complete for this step.
/// </remarks>
public enum MessagePluralCategory
{
    /// <summary>The CLDR "zero" category.</summary>
    Zero = 0,

    /// <summary>The CLDR "one" category.</summary>
    One = 1,

    /// <summary>The CLDR "two" category.</summary>
    Two = 2,

    /// <summary>The CLDR "few" category.</summary>
    Few = 3,

    /// <summary>The CLDR "many" category.</summary>
    Many = 4,

    /// <summary>The CLDR "other" category: every locale defines this one, as the catch-all.</summary>
    Other = 5
}
