# Vendored: Unicode MessageFormat 2.0 conformance test suite

This directory carries an unmodified copy of the data-driven conformance test suite from the
Unicode MessageFormat working group's repository.

- Repository: https://github.com/unicode-org/message-format-wg
- Tag: `LDML48.2`
- Commit: `7f142fb4f1f5ea6ab1eb34ce2b87e918ca9fd331`
- Copied: 2026-09-05

## Files

- `LICENSE` — the suite's own licence file, copied unchanged.
- `README.md` — the suite's `test/README.md`, copied unchanged.
- `schemas/v0/tests.schema.json` — the suite's `test/schemas/v0/tests.schema.json`, copied unchanged.
- `tests/**/*.json` (16 files) — the suite's `test/tests/**/*.json`, copied unchanged.

None of these files were edited after copying. A future update replaces the whole set from a newer
tag and updates the tag, commit and date above; it never patches an individual file in place.

## Licence

Unicode License V3, SPDX identifier `Unicode-3.0`. See `LICENSE` in this directory for the full text.

## What is deliberately not included

The MessageFormat 2.0 specification prose (the ABNF grammar, syntax, errors and formatting sections)
is not vendored here. Lumoin.Vericula's source and tests cite it by section instead, for example "UTS
#35 part 9 (MessageFormat), version 48.2, section Syntax Errors" — never by quoting more than a short
phrase.
