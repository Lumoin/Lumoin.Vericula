namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// The base of the closed pattern part hierarchy: a <see cref="TextPart"/>, an
/// <see cref="ExpressionPart"/>, or a <see cref="MarkupPart"/>. Concrete parts are sealed records.
/// See UTS #35 part 9 (MessageFormat), version 48.2, section "Pattern Model" (data model) and
/// "Pattern" (syntax).
/// </summary>
public abstract record PatternPart;
