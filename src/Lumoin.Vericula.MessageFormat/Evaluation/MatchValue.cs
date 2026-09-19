namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// Determines whether a <see cref="MessageResolvedValue"/> matches a variant key, for a selector's use
/// in matcher resolution. See UTS #35 part 9 (MessageFormat), version 48.2, section "Resolved Values"
/// (formatting.md).
/// </summary>
/// <param name="normalizedKey">The variant key, NFC-normalized.</param>
/// <returns><see langword="true"/> when the value matches the key, or one or more <see cref="MessageFunctionError"/>s on failure.</returns>
public delegate MessageOperation<bool> MatchValue(string normalizedKey);
