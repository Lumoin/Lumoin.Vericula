using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// The JSON shape of one vendored suite file: its shared property defaults, if any, plus its array
/// of test cases. Deserialized once per embedded <c>tests/**/*.json</c> resource and immediately
/// folded into <see cref="SuiteCase"/> records; nothing outside the loader references this type.
/// </summary>
/// <param name="DefaultTestProperties">The file's shared property defaults, merged into every case that does not set its own value; <see langword="null"/> when the file sets no defaults.</param>
/// <param name="Tests">The file's test cases, in source order.</param>
[DebuggerDisplay("SuiteFileDocument: {Tests.Length} tests")]
internal sealed record SuiteFileDocument(SuiteRawCase? DefaultTestProperties, SuiteRawCase[] Tests);
