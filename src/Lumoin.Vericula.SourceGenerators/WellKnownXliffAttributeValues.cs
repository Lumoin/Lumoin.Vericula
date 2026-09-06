namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The well-known XLIFF 2.x attribute VALUES this generator reads, per
/// <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html">XLIFF Version 2.1</see>.
/// </summary>
/// <remarks>
/// This mirrors <c>Lumoin.Vericula.Parsing.WellKnownXliffAttributeValues</c>; the generator assembly cannot
/// reference the library, so it carries its own copy of the one value it needs.
/// </remarks>
internal static class WellKnownXliffAttributeValues
{
    /// <summary>The UTF-8 source literal of <see cref="StateInitial"/>.</summary>
    public static ReadOnlySpan<byte> StateInitialUtf8 => "initial"u8;

    /// <summary>The default, not-yet-worked-on segment state per <see href="https://docs.oasis-open.org/xliff/xliff-core/v2.1/os/xliff-core-v2.1-os.html#state">XLIFF 2.1, state</see>.</summary>
    public static readonly string StateInitial = Utf8Constants.ToInternedString(StateInitialUtf8);

    /// <summary>Determines if a segment's <c>state</c> value is <see cref="StateInitial"/>, explicit or by the attribute's absence.</summary>
    /// <param name="value">The segment's <c>state</c> attribute value, or <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> if the state is initial; otherwise, <see langword="false"/>.</returns>
    public static bool IsStateInitial(string? value) => value is null || string.Equals(value, StateInitial, StringComparison.Ordinal);
}
