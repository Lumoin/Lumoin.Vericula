namespace Lumoin.Vericula.Content;

/// <summary>
/// The two ways XLIFF can serialize a spanning code: as one well-formed <c>pc</c> element, or as a
/// separate <c>sc</c> and <c>ec</c> pair that may cross segment boundaries. See
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#codesInline">XLIFF 2.1 §4.7.2.2</see>.
/// </summary>
public enum SpanForm
{
    /// <summary>The span came from, or serializes as, one <c>pc</c> element.</summary>
    Paired = 0,

    /// <summary>The span came from, or serializes as, a separate <c>sc</c> and <c>ec</c> pair.</summary>
    Split = 1
}
