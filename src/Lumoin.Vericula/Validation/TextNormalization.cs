namespace Lumoin.Vericula.Validation;

/// <summary>
/// The Unicode normalization form the linter applies to a rule's text and to the target text before
/// comparing them, per XLIFF 2.1 §5.8.5.8 normalization: "none: No normalization SHOULD be done."
/// and "nfc: Normalization Form C MUST be used.", with nfc as the section's documented default.
/// </summary>
public enum TextNormalization
{
    /// <summary>No normalization is applied; text is compared exactly as it appears in the document.</summary>
    None = 0,

    /// <summary>Both rule text and target text are normalized to Unicode Normalization Form C before comparing.</summary>
    Nfc = 1
}
