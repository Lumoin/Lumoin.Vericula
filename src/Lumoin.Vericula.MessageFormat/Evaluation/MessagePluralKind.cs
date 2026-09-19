namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Which CLDR plural rule set a number is matched against: cardinal (counting) or ordinal (ranking).
/// See UTS #35 part 9 (MessageFormat), version 48.2 (functions/number.md), where <c>:number</c> and
/// <c>:integer</c> selection is defined; see also the Unicode CLDR plural rules, which define cardinal
/// and ordinal rule sets.
/// </summary>
/// <remarks>
/// Consumed by the number backend seam landing in a later step; declared now so the MessageFormat
/// assembly's enum census is complete for this step.
/// </remarks>
public enum MessagePluralKind
{
    /// <summary>Cardinal plural rules: "1 file", "2 files".</summary>
    Cardinal = 0,

    /// <summary>Ordinal plural rules: "1st", "2nd", "3rd".</summary>
    Ordinal = 1
}
