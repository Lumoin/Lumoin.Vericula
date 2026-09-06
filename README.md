![Lumoin.Vericula project logo.](https://raw.githubusercontent.com/Lumoin/Lumoin.Vericula/main/resources/lumoin-vericula-github-logo.svg)

# Lumoin.Vericula

**XLIFF 2.0 and 2.1 for .NET: a document model, a reader and writer, a linter, a resx cooker, an RDF/Turtle projection, and a source generator for compile-time translation accessors.**

![Main build workflow](https://github.com/Lumoin/Lumoin.Vericula/actions/workflows/main.yml/badge.svg)

## Packages

| Library | Purpose | NuGet |
|---------|---------|:-----:|
| **Lumoin.Vericula** | The XLIFF 2.0/2.1 document model, reader, writer, linter, resx cooker and RDF/Turtle projection | [![NuGet](https://img.shields.io/nuget/v/Lumoin.Vericula.svg?style=flat)](https://www.nuget.org/packages/Lumoin.Vericula/) |
| **Lumoin.Vericula.SourceGenerators** | Roslyn incremental generator that turns `.xliff` documents into strongly-typed translation accessors at compile time (consumed as an analyzer) | [![NuGet](https://img.shields.io/nuget/v/Lumoin.Vericula.SourceGenerators.svg?style=flat)](https://www.nuget.org/packages/Lumoin.Vericula.SourceGenerators/) |

### In the repository, not packaged

- **Lumoin.Vericula.MessageFormat** — the Unicode MessageFormat 2.0 (LDML 48.2) data model and a parser (`MessageFormatReader.Parse` and `TryParse`) that reports `VFX200` to `VFX206` diagnostics; the vendored official conformance suite (461 cases) passes; the evaluator is not implemented.
- **Lumoin.Vericula.Cli** — a command-line tool built from source (not published as a dotnet tool) whose `compile` command cooks XLIFF into resx; `lint`, `stats`, `sync` and the MCP server are not implemented.

## What it does today

- Reads and writes **XLIFF 2.0 and 2.1**: files, groups, units, notes, every segment with its id, state and sub-state, and every ignorable with its id.
- Reads the Validation module on files, the Glossary module on units, and the Metadata module — tone/register profiles, file-wide glossaries, scopes and named metadata in Vericula-named groups.
- Reads a whole document from a stream or a `PipeReader`.
- Streams units one at a time as an `IEnumerable` or `IAsyncEnumerable`, without materialising the document.
- Writes only after validating, so a rejected document never leaves a truncated file behind; round-trips text byte for byte.
- Lints validation rules, missing targets and glossary misses, reporting `VFX1xx` diagnostics.
- Cooks a set of XLIFF documents into a neutral `.resx` plus one satellite per target culture.
- Carries out-of-band `Tag` metadata through the [`Lumoin.Base`](https://www.nuget.org/packages/Lumoin.Base/) package.
- Projects a document into an RDF graph and writes it as Turtle, through [`Lumoin.Veritas`](https://www.nuget.org/packages/Lumoin.Veritas.Core/).
- Generates one `Translations` class per compilation, with one static string property per unit id resolved against the current UI culture, then its parent cultures, then its neutral two-letter subtag, then the source language, and finally the id itself; a file that fails to parse is `VFX300`, an invalid unit id is `VFX301`.

Lumoin.Vericula targets **.NET 10** and is AOT-compatible; both packed libraries have nullable enabled and declare no interfaces or DI container wiring.

## Known limitations

- Inline markup inside `<source>`/`<target>` is rejected with an error rather than flattened — the reader reads plain text only.
- XLIFF 2.2 and 1.2 are not supported.
- The reader rejects duplicate file ids in a document, duplicate unit ids within a file, duplicate segment ids within a unit, targets declared without a `trgLang`, unknown segment states, and validation rules on groups or units; validation rules attach to the file.
- Validation rules asking for case-insensitive matching, a normalisation other than none or NFC, occurrence counts, or source conditioning are rejected; the linter applies only NFC or no normalisation.
- A glossary entry without a translation is rejected, because the model requires one.
- Metadata groups in categories Vericula does not define are ignored on read, as is anything outside the vocabulary the reader models, such as `<notes>` on a `<file>` or `<group>`.
- The streaming unit reader flattens groups: a streamed unit carries its own scopes and metadata, not those of the groups enclosing it.

## Getting started

```bash
# The XLIFF document model, reader, writer, linter, and resx cooker
dotnet add package Lumoin.Vericula

# Compile-time strongly-typed translation accessors (analyzer)
dotnet add package Lumoin.Vericula.SourceGenerators
```

Read an XLIFF document, lint it, cook it into resx, and write it back out:

```csharp
using System.Collections.Immutable;
using Lumoin.Vericula;
using Lumoin.Vericula.Cooking;
using Lumoin.Vericula.Linting;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Units;

await using FileStream input = File.OpenRead("wallet.fi.xliff");
XliffDocument document = await XliffReader.ReadAsync(input, cancellationToken: default);

ImmutableArray<LintDiagnostic> diagnostics = Linter.Lint(document);
foreach(LintDiagnostic diagnostic in diagnostics)
{
    Console.WriteLine(diagnostic);
}

ImmutableArray<CookedResource> resources = ResxCooker.Cook([document]);
foreach(CookedResource resource in resources)
{
    File.WriteAllText(resource.FileName, resource.Content);
}

await using FileStream output = File.Create("wallet.fi.roundtrip.xliff");
await XliffWriter.WriteAsync(document, output, cancellationToken: default);
```

Stream the units of a large document one at a time instead of materialising it:

```csharp
await using FileStream input = File.OpenRead("wallet.fi.xliff");
await foreach(XliffUnit unit in XliffReader.ReadUnitsAsync(input, cancellationToken: default))
{
    Console.WriteLine($"{unit.Id}: {unit.Target}");
}
```

Both the reader and the writer also take `System.IO.Pipelines` pipes.

Generate strongly-typed translation accessors at compile time by adding the `.xliff` files to the project as additional files:

```xml
<ItemGroup>
  <AdditionalFiles Include="**/*.xliff" />
</ItemGroup>
```

The generator emits one `Translations` class per compilation. Its namespace is the consuming project's `RootNamespace` MSBuild property when one is set, or its assembly name otherwise, with `.Localization` appended either way:

```csharp
using MyProject.Localization;

string greeting = Translations.Greeting;
```

A `.xliff` file that fails to parse is reported as diagnostic `VFX300`; a unit id that cannot become an accessor (not a valid C# identifier, or colliding with a generated member) is reported as `VFX301` and skipped.

Project a document into an RDF graph and export it as Turtle, through [`Lumoin.Veritas`](https://www.nuget.org/packages/Lumoin.Veritas.Core/). The graph is a derived view using published vocabularies (RDF Schema labels and comments, Dublin Core identifiers, languages, part-of and subject relations), plus one project-defined term, `vericula:targetLanguage`, which marks a file's target language because `dcterms:language` cannot tell source from target:

```csharp
using Lumoin.Vericula.Projections;

await using FileStream turtle = File.Create("wallet.ttl");
XliffGraphProjection.WriteTurtle(document, new Uri("https://example.org/wallet/"), turtle);
```

## Status

Early, and under active development as a single-developer project; APIs are not yet stable. Changes are recorded in [CHANGELOG.md](https://github.com/Lumoin/Lumoin.Vericula/blob/main/CHANGELOG.md).

## Development

The codebase runs on Windows, Linux, and macOS.

## Vulnerability disclosure

Please report suspected security vulnerabilities privately through [GitHub security advisories](https://github.com/Lumoin/Lumoin.Vericula/security/advisories), not through public issues.

## Contributing

Issues that describe a concrete problem are welcome, even without attached code. Pull requests are welcome but expect substantive review.

## License

Apache License 2.0. See [LICENSE](https://github.com/Lumoin/Lumoin.Vericula/blob/main/LICENSE).
