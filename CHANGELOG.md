# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]

### Added

- `Lumoin.Vericula.MessageFormat`: a parser (`MessageFormatReader.Parse` and `TryParse`) for the MessageFormat 2.0 data model, reporting syntax and data model errors as `VFX2xx` diagnostics with line and position; not yet packaged.
- Vendored the official Unicode MessageFormat 2.0 conformance test suite (tag `LDML48.2`, Unicode License V3) into the test project.

### Changed

- Every enum now declares explicit, dense member values that are part of the public contract and will not be renumbered; `MarkupKind` gains `None = 0` for the uninitialized state, so `Open`, `Standalone` and `Close` are 1, 2 and 3.
- README rewritten for the first NuGet publication: it describes only the current behaviour of the packages, and every link is absolute so it renders on nuget.org.

### Fixed

- `ResxCooker` and the CLI `compile` command now decide whether a name segment is a culture using predefined cultures only, so the check behaves the same on Windows (NLS) and on Linux and macOS (ICU). Before, on ICU any word passed as a culture: a base name such as `Wallet.notaculture` was refused and a `wallet.Designer.resx` in the output directory was deleted as a stale satellite.
- Package descriptions of `Lumoin.Vericula` and `Lumoin.Vericula.SourceGenerators` now describe the packages accurately and no longer end with an internal note about where the metadata is inherited from.
- `XliffReader` refuses more inputs it previously accepted silently or inconsistently, rather than corrupting or dropping data.
- `XliffWriter` refuses more outputs that would violate the XLIFF 2.1 content model or lose data on round trip, rather than emitting them silently.
- `Linter` folds a unit's translation completeness correctly instead of the earlier faulty aggregation.
- `ResxCooker` normalizes line endings in cooked resources instead of carrying through the source document's own line endings.
- `Lumoin.Vericula.Cli` hardens unsafe input handling in its commands.
- `Lumoin.Vericula.SourceGenerators` fixes generated identifier and language-resolution bugs.
- `XliffGraphProjection` fixes malformed IRIs in the emitted RDF terms.
- Bumped the `Lumoin.Base` dependency to 0.0.12 and pinned the generator's `Microsoft.CodeAnalysis.CSharp`/`Microsoft.CodeAnalysis.Analyzers` references to the lowest version the generator compiles against (4.14.0), so the packed analyzer loads under older .NET 10 SDK feature bands.
- Corrected `.github/workflows/main.yml` tag-version parsing: it no longer mangles prerelease labels containing the letter `v`, and `AssemblyVersion`/`FileVersion` no longer receive a non-numeric prerelease suffix.
- `Directory.Build.props` now sets the SDK's actual `InformationalVersion` property (the prior `AssemblyInformationalVersion` name was inert) and generates the XML documentation file shipped in the packages.
- `Lumoin.Vericula` now builds and packs as AOT-compatible, matching the README's existing claim.
- README: corrected the source generator's `AdditionalFiles` wiring and generated type location, added the missing `using` in the streaming snippet, replaced the packaged logo's unrenderable raw-HTML image with a Markdown image at an absolute URL, and removed a dangling comment left over from the removed `Verify.MSTest` reference.

## [0.0.1] - 2026-09-02

### Added

- Initial packages: `Lumoin.Vericula` and `Lumoin.Vericula.SourceGenerators`.
- The XLIFF 2.0 and 2.1 document model: files, groups, units with their segments and ignorables, notes, scopes, metadata, tone/register profiles, glossaries on files and units, and validation rules including start and end constraints.
- `XliffReader`: reads XLIFF 2.0/2.1 documents from a stream or pipe, including the Validation, Glossary and Metadata modules, and streams units one at a time as `IEnumerable` or `IAsyncEnumerable`.
- `XliffWriter`: writes the document model back out to XLIFF 2.0/2.1 through a stream or pipe, validating first and round-tripping text byte for byte.
- `Linter`: evaluates a document's validation rules, reporting missing targets and glossary misses, with stable `VFX1xx` codes in `WellKnownDiagnostics`.
- Well-known vocabularies for the XLIFF namespaces, elements, attributes and values, in UTF-8 and string form.
- `ResxCooker`: cooks a set of XLIFF documents into a neutral `.resx` plus one satellite per target culture.
- Out-of-band `Tag` metadata carried through the `Lumoin.Base` dependency.
- `XliffGraphProjection`: projects a document into RDF quads over published vocabularies and writes them as Turtle through the `Lumoin.Veritas` dependency.
- `Lumoin.Vericula.SourceGenerators`: a Roslyn incremental generator that turns `.xliff` documents into strongly-typed translation accessors at compile time, consumed as an analyzer.
