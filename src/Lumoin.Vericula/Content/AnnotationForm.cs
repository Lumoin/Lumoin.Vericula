namespace Lumoin.Vericula.Content;

/// <summary>
/// The two ways XLIFF can serialize a spanning annotation: as one well-formed <c>mrk</c> element, or
/// as a separate <c>sm</c> and <c>em</c> pair that may cross segment boundaries. See
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#annotations">XLIFF 2.1 §4.7.3</see>.
/// </summary>
public enum AnnotationForm
{
    /// <summary>The annotation came from, or serializes as, one <c>mrk</c> element.</summary>
    Marker = 0,

    /// <summary>The annotation came from, or serializes as, a separate <c>sm</c> and <c>em</c> pair.</summary>
    Split = 1
}
