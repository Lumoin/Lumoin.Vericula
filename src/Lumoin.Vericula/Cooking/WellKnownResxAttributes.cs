using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Cooking;

/// <summary>
/// The well-known attribute NAMES of a .NET resource (.resx) file, as documented for
/// <see href="https://learn.microsoft.com/dotnet/api/system.resources.resxresourcereader">ResXResourceReader</see>.
/// </summary>
/// <remarks>
/// These are attribute names, not element names or values; elements live in
/// <see cref="WellKnownResxElements"/>. Each name is spelled once as a UTF-8 source literal and
/// carried alongside as an interned string.
/// </remarks>
public static class WellKnownResxAttributes
{
    /// <summary>The UTF-8 source literal of <see cref="Name"/>.</summary>
    public static ReadOnlySpan<byte> NameUtf8 => "name"u8;

    /// <summary>The identifying name on a <see cref="WellKnownResxElements.ResHeader"/> or a <see cref="WellKnownResxElements.Data"/> entry.</summary>
    public static readonly string Name = Utf8Constants.ToInternedString(NameUtf8);

    /// <summary>Determines if an attribute's local name is <see cref="Name"/>.</summary>
    /// <param name="localName">The attribute's local name.</param>
    /// <returns><see langword="true"/> if the attribute is the name attribute; otherwise, <see langword="false"/>.</returns>
    public static bool IsName(string localName) => string.Equals(localName, Name, StringComparison.Ordinal);
}
