namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// The one well-known Vericula-specific value this generator reads: the sub-state a Modifier sets on
/// an initial segment to mark it as needing translation rather than merely untouched.
/// </summary>
/// <remarks>
/// This mirrors <c>Lumoin.Vericula.Parsing.WellKnownVericulaMetadata</c>; the generator assembly cannot
/// reference the library, so it carries its own copy of the one member it needs.
/// </remarks>
internal static class WellKnownVericulaMetadata
{
    /// <summary>The UTF-8 source literal of <see cref="NeedsTranslationSubState"/>.</summary>
    public static ReadOnlySpan<byte> NeedsTranslationSubStateUtf8 => "vericula:needsTranslation"u8;

    /// <summary>The sub-state on an initial segment meaning a Modifier still needs to translate it.</summary>
    public static readonly string NeedsTranslationSubState = Utf8Constants.ToInternedString(NeedsTranslationSubStateUtf8);

    /// <summary>Determines if a sub-state is <see cref="NeedsTranslationSubState"/>.</summary>
    /// <param name="subState">The segment's <c>subState</c> attribute value, or <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> if the sub-state is the needs-translation sub-state; otherwise, <see langword="false"/>.</returns>
    public static bool IsNeedsTranslationSubState(string? subState) => string.Equals(subState, NeedsTranslationSubState, StringComparison.Ordinal);
}
