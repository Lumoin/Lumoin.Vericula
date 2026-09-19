namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Determines whether one matching variant key ranks ahead of another for the same
/// <see cref="MessageResolvedValue"/>, for a selector's use in matcher resolution when more than one
/// key matches. See UTS #35 part 9 (MessageFormat), version 48.2, section "Resolved Values"
/// (formatting.md).
/// </summary>
/// <param name="normalizedKey">The candidate key, NFC-normalized.</param>
/// <param name="normalizedOther">The key it is being ranked against, NFC-normalized.</param>
/// <returns>
/// <see langword="true"/> when <paramref name="normalizedKey"/> ranks ahead of
/// <paramref name="normalizedOther"/>, or one or more <see cref="MessageFunctionError"/>s on failure.
/// A value's own string match never prefers one key over another, so a <c>:string</c> resolved value
/// carries no <see cref="PreferKey"/> delegate.
/// </returns>
public delegate MessageOperation<bool> PreferKey(string normalizedKey, string normalizedOther);
