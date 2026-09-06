namespace Lumoin.Vericula.Validation;

/// <summary>
/// The base of the closed validation rule hierarchy. Concrete rules are sealed records.
/// </summary>
public abstract record ValidationRule
{
    /// <summary>
    /// Whether this rule is disabled within the scope of its enclosing element, per XLIFF 2.1
    /// §5.8.5.9 disabled: "determines whether a rule MUST or MUST NOT be applied". A disabled rule
    /// is kept in the model rather than dropped, since §5.8.4.3's Processing Requirements say
    /// "Modifiers MUST NOT remove either &lt;rule&gt; elements or their attributes defined in this
    /// module"; the linter skips it instead.
    /// </summary>
    public bool Disabled { get; init; }

    /// <summary>
    /// The normalization form the linter applies to this rule's text and to the target text before
    /// comparing them, per XLIFF 2.1 §5.8.5.8 normalization. Defaults to
    /// <see cref="TextNormalization.Nfc"/>, the section's documented default value.
    /// </summary>
    public TextNormalization Normalization { get; init; } = TextNormalization.Nfc;
}
