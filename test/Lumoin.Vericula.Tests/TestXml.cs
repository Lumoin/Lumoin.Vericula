using System.Xml;
using System.Xml.Linq;

namespace Lumoin.Vericula.Tests;

/// <summary>
/// Shares the secure <see cref="XmlReader"/> settings every production reader in this codebase
/// uses (DTDs prohibited, no external resolver) with the tests that parse XML through
/// <see cref="XDocument"/>, so a test fixture is never a weaker parse path than the code it exercises.
/// </summary>
internal static class TestXml
{
    /// <summary>
    /// Parses <paramref name="xml"/> into an <see cref="XDocument"/> through an
    /// <see cref="XmlReader"/> with DTDs prohibited and no resolver.
    /// </summary>
    /// <param name="xml">The XML text to parse.</param>
    /// <returns>The parsed document.</returns>
    public static XDocument Parse(string xml)
    {
        using var stringReader = new StringReader(xml);
        using XmlReader xmlReader = XmlReader.Create(stringReader, Settings());

        return XDocument.Load(xmlReader);
    }

    /// <summary>
    /// Loads an <see cref="XDocument"/> from <paramref name="stream"/> through an
    /// <see cref="XmlReader"/> with DTDs prohibited and no resolver.
    /// </summary>
    /// <param name="stream">The stream to parse.</param>
    /// <returns>The parsed document.</returns>
    public static XDocument Load(Stream stream)
    {
        using XmlReader xmlReader = XmlReader.Create(stream, Settings());

        return XDocument.Load(xmlReader);
    }

    /// <summary>
    /// Builds the shared reader settings: DTDs prohibited and no resolver, matching
    /// <c>XliffReader</c>'s and <c>ResxCooker</c>'s own <see cref="XmlReaderSettings"/>.
    /// </summary>
    /// <returns>The settings every helper in this class parses with.</returns>
    private static XmlReaderSettings Settings()
    {
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
    }
}
