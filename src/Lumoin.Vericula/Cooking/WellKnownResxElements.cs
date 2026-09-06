using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Cooking;

/// <summary>
/// The well-known element NAMES of a .NET resource (.resx) file, as documented for
/// <see href="https://learn.microsoft.com/dotnet/api/system.resources.resxresourcereader">ResXResourceReader</see>:
/// the schema-less shape <see cref="ResxCooker"/> writes and <c>ResXResourceReader</c> reads back,
/// a <see cref="Root"/> carrying <see cref="ResHeader"/> entries and <see cref="Data"/> entries, each
/// of which nests one <see cref="Value"/>.
/// </summary>
/// <remarks>
/// These are element names, not attribute names or values; attributes live in
/// <see cref="WellKnownResxAttributes"/> and the header names and values in
/// <see cref="WellKnownResxHeaderValues"/>. Each name is spelled once as a UTF-8 source literal and
/// carried alongside as an interned string.
/// </remarks>
public static class WellKnownResxElements
{
    /// <summary>The UTF-8 source literal of <see cref="Root"/>.</summary>
    public static ReadOnlySpan<byte> RootUtf8 => "root"u8;

    /// <summary>The document element of a resx file, carrying the resheaders and data entries.</summary>
    public static readonly string Root = Utf8Constants.ToInternedString(RootUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ResHeader"/>.</summary>
    public static ReadOnlySpan<byte> ResHeaderUtf8 => "resheader"u8;

    /// <summary>One resource-pipeline setting, such as the mime type or the reader/writer type, read before any data entry.</summary>
    public static readonly string ResHeader = Utf8Constants.ToInternedString(ResHeaderUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Data"/>.</summary>
    public static ReadOnlySpan<byte> DataUtf8 => "data"u8;

    /// <summary>One resource entry, a name and its <see cref="Value"/>.</summary>
    public static readonly string Data = Utf8Constants.ToInternedString(DataUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Value"/>.</summary>
    public static ReadOnlySpan<byte> ValueUtf8 => "value"u8;

    /// <summary>The text content of a <see cref="ResHeader"/> or a <see cref="Data"/> entry.</summary>
    public static readonly string Value = Utf8Constants.ToInternedString(ValueUtf8);

    /// <summary>Determines if an element's local name is <see cref="Root"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is the document root; otherwise, <see langword="false"/>.</returns>
    public static bool IsRoot(string localName) => string.Equals(localName, Root, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="ResHeader"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is a resheader; otherwise, <see langword="false"/>.</returns>
    public static bool IsResHeader(string localName) => string.Equals(localName, ResHeader, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="Data"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is a data entry; otherwise, <see langword="false"/>.</returns>
    public static bool IsData(string localName) => string.Equals(localName, Data, StringComparison.Ordinal);

    /// <summary>Determines if an element's local name is <see cref="Value"/>.</summary>
    /// <param name="localName">The element's local name.</param>
    /// <returns><see langword="true"/> if the element is a value; otherwise, <see langword="false"/>.</returns>
    public static bool IsValue(string localName) => string.Equals(localName, Value, StringComparison.Ordinal);
}
