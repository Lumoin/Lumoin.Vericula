using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// Literal text within a pattern, with escape sequences already processed. Adjacent text and escapes
/// in the source fold into one non-empty <see cref="TextPart"/>. See UTS #35 part 9 (MessageFormat),
/// version 48.2, section "Pattern Model" (data model) and "Text" (syntax).
/// </summary>
/// <param name="Text">The part's cooked, non-empty text.</param>
[DebuggerDisplay("TextPart: {Text}")]
public sealed record TextPart(string Text): PatternPart;
