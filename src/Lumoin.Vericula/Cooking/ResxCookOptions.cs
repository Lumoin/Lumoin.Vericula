using System.Diagnostics;

namespace Lumoin.Vericula.Cooking;

/// <summary>
/// Options that shape a <see cref="ResxCooker"/> run.
/// </summary>
[DebuggerDisplay("ResxCookOptions: {BaseName}")]
public sealed record ResxCookOptions
{
    /// <summary>
    /// The resource base name that prefixes every emitted file: <c>{BaseName}.resx</c> for the
    /// neutral source-language resources and <c>{BaseName}.{culture}.resx</c> for each target
    /// culture. When null, the base name defaults to the id of the first file encountered. The
    /// trailing dotted segment of the effective base name must not itself be a culture name (for
    /// example <c>wallet.en</c>), or <see cref="ResxCooker.Cook"/> refuses it: MSBuild's AssignCulture
    /// would then treat the neutral resource as a satellite of that culture.
    /// </summary>
    public string? BaseName { get; init; }
}
