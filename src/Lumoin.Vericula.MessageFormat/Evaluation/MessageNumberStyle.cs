namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Which default numeric function formatted a resolved number. See UTS #35 part 9 (MessageFormat),
/// version 48.2 (functions/number.md), where <c>:number</c>, <c>:integer</c>, <c>:currency</c>,
/// <c>:percent</c> and <c>:unit</c> are defined.
/// </summary>
/// <remarks>
/// Consumed by the number backend seam landing in a later step; declared now so the MessageFormat
/// assembly's enum census is complete for this step. The design names this member <c>Integer</c>;
/// CA1720 (identifiers should not contain type names, "Integer" being Visual Basic's <c>Int32</c>
/// alias) forbids that exact identifier, and no suppression exists anywhere in this repository, so
/// this member is <see cref="IntegerStyle"/> instead, the closest available name applied consistently
/// everywhere the design's "Integer" would otherwise have been a bare identifier.
/// </remarks>
public enum MessageNumberStyle
{
    /// <summary>Formatted by <c>:number</c>.</summary>
    Number = 0,

    /// <summary>Formatted by <c>:integer</c>.</summary>
    IntegerStyle = 1,

    /// <summary>Formatted by <c>:currency</c>.</summary>
    Currency = 2,

    /// <summary>Formatted by <c>:percent</c>.</summary>
    Percent = 3,

    /// <summary>Formatted by <c>:unit</c>.</summary>
    Unit = 4
}
