using System.Text.Json.Serialization;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// The source-generated <see cref="JsonSerializerContext"/> for the vendored suite's JSON shape,
/// configured with <see cref="JsonKnownNamingPolicy.CamelCase"/> so every generated property maps to
/// the suite's own camelCase JSON names by name alone: no JSON property-name string literal appears
/// anywhere in this project.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SuiteFileDocument))]
internal sealed partial class MessageFormatSuiteJsonContext: JsonSerializerContext
{
}
