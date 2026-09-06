using System.Collections.Immutable;
using System.Diagnostics;

namespace Lumoin.Vericula.Tone;

/// <summary>
/// A declarative profile of how translated text should sound and render for one audience:
/// register, voice, text orientation, calendar and number conventions.
/// </summary>
[DebuggerDisplay("ToneProfile: {Register}, version: {Version}")]
public sealed record ToneProfile(
    string Version,
    string? Authority,
    ToneRegister Register,
    string? Voice,
    TextOrientation Orientation,
    string? Calendar,
    string? DateFormat,
    DigitStyle DigitStyle,
    ImmutableDictionary<string, string> Metadata);
