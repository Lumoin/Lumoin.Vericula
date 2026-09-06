using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Cooking;

/// <summary>
/// The well-known resheader NAMES and VALUES of a .NET resource (.resx) file, as documented for
/// <see href="https://learn.microsoft.com/dotnet/api/system.resources.resxresourcereader">ResXResourceReader</see>:
/// the four resheaders the reader expects before any data entry, and the fixed values
/// <see cref="ResxCooker"/> writes for each of them.
/// </summary>
/// <remarks>
/// A resheader name rides on a <see cref="WellKnownResxAttributes.Name"/> attribute; its value is the
/// nested <see cref="WellKnownResxElements.Value"/> element's text. Each string is spelled once as a
/// UTF-8 source literal and carried alongside as an interned string.
/// </remarks>
public static class WellKnownResxHeaderValues
{
    /// <summary>The UTF-8 source literal of <see cref="ResMimeTypeHeader"/>.</summary>
    public static ReadOnlySpan<byte> ResMimeTypeHeaderUtf8 => "resmimetype"u8;

    /// <summary>The resheader name declaring the resx mime type.</summary>
    public static readonly string ResMimeTypeHeader = Utf8Constants.ToInternedString(ResMimeTypeHeaderUtf8);

    /// <summary>The UTF-8 source literal of <see cref="VersionHeader"/>.</summary>
    public static ReadOnlySpan<byte> VersionHeaderUtf8 => "version"u8;

    /// <summary>The resheader name declaring the resx format version.</summary>
    public static readonly string VersionHeader = Utf8Constants.ToInternedString(VersionHeaderUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ReaderHeader"/>.</summary>
    public static ReadOnlySpan<byte> ReaderHeaderUtf8 => "reader"u8;

    /// <summary>The resheader name declaring the assembly-qualified reader type.</summary>
    public static readonly string ReaderHeader = Utf8Constants.ToInternedString(ReaderHeaderUtf8);

    /// <summary>The UTF-8 source literal of <see cref="WriterHeader"/>.</summary>
    public static ReadOnlySpan<byte> WriterHeaderUtf8 => "writer"u8;

    /// <summary>The resheader name declaring the assembly-qualified writer type.</summary>
    public static readonly string WriterHeader = Utf8Constants.ToInternedString(WriterHeaderUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ResMimeType"/>.</summary>
    public static ReadOnlySpan<byte> ResMimeTypeUtf8 => "text/microsoft-resx"u8;

    /// <summary>The value of <see cref="ResMimeTypeHeader"/> every cooked resx carries.</summary>
    public static readonly string ResMimeType = Utf8Constants.ToInternedString(ResMimeTypeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Version"/>.</summary>
    public static ReadOnlySpan<byte> VersionUtf8 => "2.0"u8;

    /// <summary>The value of <see cref="VersionHeader"/> every cooked resx carries.</summary>
    public static readonly string Version = Utf8Constants.ToInternedString(VersionUtf8);

    /// <summary>The UTF-8 source literal of <see cref="ReaderTypeName"/>.</summary>
    public static ReadOnlySpan<byte> ReaderTypeNameUtf8 => "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"u8;

    /// <summary>The value of <see cref="ReaderHeader"/> every cooked resx carries.</summary>
    public static readonly string ReaderTypeName = Utf8Constants.ToInternedString(ReaderTypeNameUtf8);

    /// <summary>The UTF-8 source literal of <see cref="WriterTypeName"/>.</summary>
    public static ReadOnlySpan<byte> WriterTypeNameUtf8 => "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"u8;

    /// <summary>The value of <see cref="WriterHeader"/> every cooked resx carries.</summary>
    public static readonly string WriterTypeName = Utf8Constants.ToInternedString(WriterTypeNameUtf8);

    /// <summary>Determines if a resheader name is <see cref="ResMimeTypeHeader"/>.</summary>
    /// <param name="name">The resheader's name attribute value.</param>
    /// <returns><see langword="true"/> if the name is the mime-type header; otherwise, <see langword="false"/>.</returns>
    public static bool IsResMimeTypeHeader(string? name) => string.Equals(name, ResMimeTypeHeader, StringComparison.Ordinal);

    /// <summary>Determines if a resheader name is <see cref="VersionHeader"/>.</summary>
    /// <param name="name">The resheader's name attribute value.</param>
    /// <returns><see langword="true"/> if the name is the version header; otherwise, <see langword="false"/>.</returns>
    public static bool IsVersionHeader(string? name) => string.Equals(name, VersionHeader, StringComparison.Ordinal);

    /// <summary>Determines if a resheader name is <see cref="ReaderHeader"/>.</summary>
    /// <param name="name">The resheader's name attribute value.</param>
    /// <returns><see langword="true"/> if the name is the reader header; otherwise, <see langword="false"/>.</returns>
    public static bool IsReaderHeader(string? name) => string.Equals(name, ReaderHeader, StringComparison.Ordinal);

    /// <summary>Determines if a resheader name is <see cref="WriterHeader"/>.</summary>
    /// <param name="name">The resheader's name attribute value.</param>
    /// <returns><see langword="true"/> if the name is the writer header; otherwise, <see langword="false"/>.</returns>
    public static bool IsWriterHeader(string? name) => string.Equals(name, WriterHeader, StringComparison.Ordinal);
}
