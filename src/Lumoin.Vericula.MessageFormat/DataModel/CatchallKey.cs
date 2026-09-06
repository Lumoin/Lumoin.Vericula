using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// The catch-all variant key, written <c>*</c>, that matches any value for its selector. Carries no
/// state: the source form is always the single asterisk. See UTS #35 part 9 (MessageFormat), version
/// 48.2, section "Message Model" (data model) and "Key" (syntax).
/// </summary>
[DebuggerDisplay("*")]
public sealed record CatchallKey: VariantKey;
