using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.Diagnostics;

/// <summary>
/// The catalogue of stable diagnostic ids the <see cref="Linting.Linter"/> reports against a
/// document. Each id is spelled once as a UTF-8 source literal and carried alongside as an interned
/// string.
/// </summary>
/// <remarks>
/// Ids carry the <c>VFX</c> prefix and a three-digit number, and are stable across versions: an id
/// is never renumbered or reused, and a new condition takes the next free number. VFX100 to VFX199
/// is the linter's own range; the source generator owns VFX300 to VFX399 and carries its own copy of
/// its codes because an analyzer assembly cannot reference this library.
/// </remarks>
public static class WellKnownDiagnostics
{
    /// <summary>The UTF-8 source literal of <see cref="PresenceViolation"/>.</summary>
    public static ReadOnlySpan<byte> PresenceViolationUtf8 => "VFX100"u8;

    /// <summary>
    /// The id reported when a <see cref="Validation.PresenceRule"/> text is missing from a target. A
    /// warning: a missing phrase is often a paraphrase rather than a defect.
    /// </summary>
    public static readonly string PresenceViolation = Utf8Constants.ToInternedString(PresenceViolationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="AbsenceViolation"/>.</summary>
    public static ReadOnlySpan<byte> AbsenceViolationUtf8 => "VFX101"u8;

    /// <summary>
    /// The id reported when an <see cref="Validation.AbsenceRule"/> text appears in a target. An
    /// error: the rule names a word the translation must not contain.
    /// </summary>
    public static readonly string AbsenceViolation = Utf8Constants.ToInternedString(AbsenceViolationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="LengthBudgetViolation"/>.</summary>
    public static ReadOnlySpan<byte> LengthBudgetViolationUtf8 => "VFX102"u8;

    /// <summary>
    /// The id reported when a target exceeds a <see cref="Validation.LengthBudgetRule"/>. An error:
    /// the budget is a layout limit measured in UTF-16 code units.
    /// </summary>
    public static readonly string LengthBudgetViolation = Utf8Constants.ToInternedString(LengthBudgetViolationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="RegexViolation"/>.</summary>
    public static ReadOnlySpan<byte> RegexViolationUtf8 => "VFX103"u8;

    /// <summary>
    /// The id reported when a target does not match a <see cref="Validation.RegexRule"/>. An error:
    /// the pattern encodes a required format.
    /// </summary>
    public static readonly string RegexViolation = Utf8Constants.ToInternedString(RegexViolationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="InvalidRegexPattern"/>.</summary>
    public static ReadOnlySpan<byte> InvalidRegexPatternUtf8 => "VFX104"u8;

    /// <summary>
    /// The id reported when a <see cref="Validation.RegexRule"/> pattern does not compile. An error
    /// reported once per rule on the file, not per unit.
    /// </summary>
    public static readonly string InvalidRegexPattern = Utf8Constants.ToInternedString(InvalidRegexPatternUtf8);

    /// <summary>The UTF-8 source literal of <see cref="MissingTarget"/>.</summary>
    public static ReadOnlySpan<byte> MissingTargetUtf8 => "VFX105"u8;

    /// <summary>
    /// The id reported when a unit has no complete target although its file declares a target
    /// language. A warning: the unit is still waiting for its translation.
    /// </summary>
    public static readonly string MissingTarget = Utf8Constants.ToInternedString(MissingTargetUtf8);

    /// <summary>The UTF-8 source literal of <see cref="GlossaryRenderingMissing"/>.</summary>
    public static ReadOnlySpan<byte> GlossaryRenderingMissingUtf8 => "VFX106"u8;

    /// <summary>
    /// The id reported when a unit uses a preferred glossary term but its target lacks the required
    /// rendering. A warning: the file-wide glossary is consulted first, then the unit's own.
    /// </summary>
    public static readonly string GlossaryRenderingMissing = Utf8Constants.ToInternedString(GlossaryRenderingMissingUtf8);

    /// <summary>The UTF-8 source literal of <see cref="StartsWithViolation"/>.</summary>
    public static ReadOnlySpan<byte> StartsWithViolationUtf8 => "VFX107"u8;

    /// <summary>
    /// The id reported when a target does not start with a <see cref="Validation.StartsWithRule"/>
    /// text. An error: the rule pins the beginning of the translation.
    /// </summary>
    public static readonly string StartsWithViolation = Utf8Constants.ToInternedString(StartsWithViolationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="EndsWithViolation"/>.</summary>
    public static ReadOnlySpan<byte> EndsWithViolationUtf8 => "VFX108"u8;

    /// <summary>
    /// The id reported when a target does not end with an <see cref="Validation.EndsWithRule"/> text.
    /// An error: the rule pins the end of the translation.
    /// </summary>
    public static readonly string EndsWithViolation = Utf8Constants.ToInternedString(EndsWithViolationUtf8);

    /// <summary>The UTF-8 source literal of <see cref="RegexTimedOut"/>.</summary>
    public static ReadOnlySpan<byte> RegexTimedOutUtf8 => "VFX109"u8;

    /// <summary>
    /// The id reported when matching a <see cref="Validation.RegexRule"/> pattern against a target
    /// exceeds the linter's matching timeout. An error, reported on the unit and segment being
    /// matched rather than thrown, so one hostile pattern-target pair never discards the diagnostics
    /// already collected for the rest of the document.
    /// </summary>
    public static readonly string RegexTimedOut = Utf8Constants.ToInternedString(RegexTimedOutUtf8);

    /// <summary>Determines if a diagnostic id is <see cref="PresenceViolation"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the presence-violation id; otherwise, <see langword="false"/>.</returns>
    public static bool IsPresenceViolation(string id) => string.Equals(id, PresenceViolation, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="AbsenceViolation"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the absence-violation id; otherwise, <see langword="false"/>.</returns>
    public static bool IsAbsenceViolation(string id) => string.Equals(id, AbsenceViolation, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="LengthBudgetViolation"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the length-budget-violation id; otherwise, <see langword="false"/>.</returns>
    public static bool IsLengthBudgetViolation(string id) => string.Equals(id, LengthBudgetViolation, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="RegexViolation"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the regex-violation id; otherwise, <see langword="false"/>.</returns>
    public static bool IsRegexViolation(string id) => string.Equals(id, RegexViolation, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="InvalidRegexPattern"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the invalid-regex-pattern id; otherwise, <see langword="false"/>.</returns>
    public static bool IsInvalidRegexPattern(string id) => string.Equals(id, InvalidRegexPattern, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="MissingTarget"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the missing-target id; otherwise, <see langword="false"/>.</returns>
    public static bool IsMissingTarget(string id) => string.Equals(id, MissingTarget, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="GlossaryRenderingMissing"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the glossary-rendering-missing id; otherwise, <see langword="false"/>.</returns>
    public static bool IsGlossaryRenderingMissing(string id) => string.Equals(id, GlossaryRenderingMissing, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="StartsWithViolation"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the starts-with-violation id; otherwise, <see langword="false"/>.</returns>
    public static bool IsStartsWithViolation(string id) => string.Equals(id, StartsWithViolation, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="EndsWithViolation"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the ends-with-violation id; otherwise, <see langword="false"/>.</returns>
    public static bool IsEndsWithViolation(string id) => string.Equals(id, EndsWithViolation, StringComparison.Ordinal);

    /// <summary>Determines if a diagnostic id is <see cref="RegexTimedOut"/>.</summary>
    /// <param name="id">The diagnostic id to test.</param>
    /// <returns><see langword="true"/> if the id is the regex-timed-out id; otherwise, <see langword="false"/>.</returns>
    public static bool IsRegexTimedOut(string id) => string.Equals(id, RegexTimedOut, StringComparison.Ordinal);
}
