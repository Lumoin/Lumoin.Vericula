using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// A variant key that matches a selector's resolved value against a literal, compared by NFC-
/// normalized string value rather than syntactic form. See UTS #35 part 9 (MessageFormat), version
/// 48.2, section "Message Model" (data model) and "Key" (syntax).
/// </summary>
/// <param name="Literal">The literal to match against.</param>
[DebuggerDisplay("LiteralKey: {Literal}")]
public sealed record LiteralKey(Literal Literal): VariantKey;
