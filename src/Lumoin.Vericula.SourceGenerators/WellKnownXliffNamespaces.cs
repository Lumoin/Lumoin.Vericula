namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The well-known XML namespace name of the XLIFF 2.x core, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#namespaces">XLIFF 2.1, Namespaces</see>.
/// </summary>
/// <remarks>
/// This mirrors <c>Lumoin.Vericula.Parsing.WellKnownXliffNamespaces</c>; the generator assembly cannot
/// reference the library, so it carries its own copy of the one member it needs.
/// </remarks>
internal static class WellKnownXliffNamespaces
{
    /// <summary>The UTF-8 source literal of <see cref="Core"/>.</summary>
    public static ReadOnlySpan<byte> CoreUtf8 => "urn:oasis:names:tc:xliff:document:2.0"u8;

    /// <summary>
    /// The XLIFF 2.0 and 2.1 core namespace per
    /// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#namespaces">XLIFF 2.1, Namespaces</see>.
    /// </summary>
    public static readonly string Core = Utf8Constants.ToInternedString(CoreUtf8);

    /// <summary>Determines if a namespace name is the XLIFF core namespace, <see cref="Core"/>.</summary>
    /// <param name="namespaceName">The namespace name of an element or attribute.</param>
    /// <returns><see langword="true"/> if the name is the core namespace; otherwise, <see langword="false"/>.</returns>
    public static bool IsCore(string? namespaceName) => string.Equals(namespaceName, Core, StringComparison.Ordinal);
}
