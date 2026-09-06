namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// The base of a variant key: a <see cref="LiteralKey"/> matched against a selector's resolved value,
/// or the <see cref="CatchallKey"/> that matches any value. Concrete keys are sealed records. See
/// UTS #35 part 9 (MessageFormat), version 48.2, section "Message Model" (data model) and "Key"
/// (syntax).
/// </summary>
public abstract record VariantKey;
