namespace Lumoin.Vericula.MessageFormat.Evaluation;

/// <summary>
/// The base of the closed formatted-parts hierarchy: a <see cref="MessageTextPart"/>, a
/// <see cref="MessageBidiPart"/>, a <see cref="MessageFallbackPart"/>, a <see cref="MessageMarkupPart"/>,
/// or a <see cref="MessageValuePart"/>. Concrete parts are sealed records. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Formatting" (formatting.md).
/// </summary>
public abstract record MessagePart;
