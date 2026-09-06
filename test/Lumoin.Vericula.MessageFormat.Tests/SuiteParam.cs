using System.Diagnostics;
using System.Text.Json;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// One named value from a suite case's <c>params</c> array, resolving an external variable that the
/// case's <see cref="SuiteCase.Src"/> refers to.
/// </summary>
/// <param name="Name">The variable's name, without the <c>$</c> sigil.</param>
/// <param name="Value">
/// The value to resolve the variable to, kept as a <see cref="JsonElement"/> because the suite mixes
/// strings, numbers and booleans, and (for a <see cref="Type"/> of <c>"datetime"</c>) a string that
/// names its own conversion.
/// </param>
/// <param name="Type">The value's conversion hint, currently only ever <c>"datetime"</c> in the suite; <see langword="null"/> when the value needs no conversion beyond its own JSON shape.</param>
[DebuggerDisplay("SuiteParam: {Name,nq}")]
public sealed record SuiteParam(string Name, JsonElement Value, string? Type);
