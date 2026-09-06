using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lumoin.Vericula.Diagnostics;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Scopes;
using Lumoin.Vericula.Units;
using Lumoin.Vericula.Validation;

namespace Lumoin.Vericula.Linting;

/// <summary>
/// Lints XLIFF documents against the validation rules, glossary, and structural invariants
/// attached to each file. Every diagnostic carries a stable id in the VFX100-VFX199 range:
/// <list type="bullet">
/// <item><description>VFX100 - a <see cref="PresenceRule"/> text is missing from a target (warning).</description></item>
/// <item><description>VFX101 - an <see cref="AbsenceRule"/> text appears in a target (error).</description></item>
/// <item><description>VFX102 - a target exceeds a <see cref="LengthBudgetRule"/> (error).</description></item>
/// <item><description>VFX103 - a target does not match a <see cref="RegexRule"/> (error).</description></item>
/// <item><description>VFX104 - a <see cref="RegexRule"/> pattern does not compile (error, reported once per rule on the file).</description></item>
/// <item><description>VFX105 - a unit has no complete target although its file declares a target language (warning).</description></item>
/// <item><description>VFX106 - a unit uses a preferred glossary term but its target lacks the required rendering (warning).</description></item>
/// <item><description>VFX107 - a target does not start with a <see cref="StartsWithRule"/> text (error).</description></item>
/// <item><description>VFX108 - a target does not end with an <see cref="EndsWithRule"/> text (error).</description></item>
/// <item><description>VFX109 - matching a <see cref="RegexRule"/> pattern against a target timed out (error).</description></item>
/// </list>
/// The glossary check consults the file-wide glossary first and then the unit's own glossary.
/// Presence is a warning because a missing phrase is often a paraphrase rather than a defect;
/// absence, the length budget, and the regular expression are errors because each encodes a hard
/// constraint (a forbidden word, a layout limit, a required format) that a translation must satisfy.
/// Because the model carries no source position information, every <see cref="LintLocation"/>
/// reports <c>LineNumber</c> as 0; callers must not treat it as a real line.
/// </summary>
/// <remarks>
/// XLIFF 2.1 §5.8.4.2 validation's Processing Requirements say a file's rules "MUST be applied to
/// all &lt;target&gt; elements within the scope" of the file, so the linter evaluates every
/// validation rule against each translatable segment's own target text, not against the unit's
/// folded, all-segments-complete-or-nothing <see cref="Units.XliffUnit.Target"/>: a unit with three
/// segments and one validation rule can raise that rule up to three times, once per translated
/// segment, each diagnostic naming its segment when the segment has an id. A <see cref="ValidationRule"/>
/// whose <see cref="ValidationRule.Disabled"/> is true is skipped entirely, per §5.8.5.9 disabled.
/// </remarks>
public static class Linter
{
    /// <summary>
    /// The matching timeout given to every compiled <see cref="RegexRule"/>, so a pathological
    /// pattern-target pair reports as a VFX109 diagnostic instead of hanging the whole lint run.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Lints the given document and returns the diagnostics found.
    /// </summary>
    /// <remarks>
    /// The result is pure and deterministic: for the same document it is always the same
    /// sequence, ordered by file (in <see cref="XliffDocument.Files"/> order), then by unit
    /// traversal order within the file (each file's own units, then its groups' units,
    /// depth-first, in document order), then by rule order. A file-level diagnostic that is not
    /// tied to a unit - an invalid <see cref="RegexRule"/> pattern - is reported for its file
    /// before that file's per-unit diagnostics, in the order its rule appears in
    /// <see cref="Documents.XliffFile.ValidationRules"/>. Within a unit, diagnostics are reported
    /// in this order: the file's validation rules (in their declared order, evaluated once per
    /// translatable segment that carries a target, in document order), the missing-target check,
    /// then the glossary check.
    /// </remarks>
    /// <param name="document">The document to lint.</param>
    /// <returns>The diagnostics found; empty when the document is clean.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="document"/> is null.</exception>
    public static ImmutableArray<LintDiagnostic> Lint(XliffDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return LintDocument(document).ToImmutableArray();
    }

    /// <summary>
    /// Lints every file of the document, in document order.
    /// </summary>
    private static IEnumerable<LintDiagnostic> LintDocument(XliffDocument document)
    {
        foreach(XliffFile file in document.Files)
        {
            foreach(LintDiagnostic diagnostic in LintFile(file))
            {
                yield return diagnostic;
            }
        }
    }

    /// <summary>
    /// Lints one file: its rule-compilation problems first, then every unit's diagnostics in
    /// traversal order.
    /// </summary>
    private static IEnumerable<LintDiagnostic> LintFile(XliffFile file)
    {
        (ImmutableArray<CompiledRule> compiledRules, ImmutableArray<LintDiagnostic> problems) = CompileRules(file);
        foreach(LintDiagnostic problem in problems)
        {
            yield return problem;
        }

        foreach(XliffUnit unit in EnumerateUnits(file))
        {
            foreach(LintDiagnostic diagnostic in LintUnit(file, unit, compiledRules))
            {
                yield return diagnostic;
            }
        }
    }

    /// <summary>
    /// Builds each enabled rule's <see cref="Regex"/> once per file rather than once per segment. A
    /// disabled rule (<see cref="ValidationRule.Disabled"/>) is skipped entirely rather than compiled,
    /// per XLIFF 2.1 §5.8.5.9 disabled. A rule whose pattern fails to compile is reported as a problem
    /// on the file, here, instead of throwing or being retried per segment.
    /// </summary>
    /// <returns>The rules ready to evaluate, and the problems found compiling them.</returns>
    private static (ImmutableArray<CompiledRule> Rules, ImmutableArray<LintDiagnostic> Problems) CompileRules(XliffFile file)
    {
        ValidationRuleSet? ruleSet = file.ValidationRules;
        if(ruleSet is null)
        {
            return (ImmutableArray<CompiledRule>.Empty, ImmutableArray<LintDiagnostic>.Empty);
        }

        var compiled = ImmutableArray.CreateBuilder<CompiledRule>(ruleSet.Rules.Length);
        var problems = ImmutableArray.CreateBuilder<LintDiagnostic>();
        foreach(ValidationRule rule in ruleSet.Rules)
        {
            if(rule.Disabled)
            {
                continue;
            }

            if(rule is RegexRule regexRule)
            {
                try
                {
                    compiled.Add(new CompiledRule(rule, new Regex(regexRule.Pattern, RegexOptions.CultureInvariant, RegexTimeout)));
                }
                catch(ArgumentException exception)
                {
                    problems.Add(new LintDiagnostic(
                        WellKnownDiagnostics.InvalidRegexPattern,
                        LintSeverity.Error,
                        $"The regular expression pattern '{regexRule.Pattern}' in the validation rules for file '{file.Id}' does not compile: {exception.Message}",
                        new LintLocation(file.Id, null, 0)));
                }

                continue;
            }

            compiled.Add(new CompiledRule(rule, Regex: null));
        }

        return (compiled.ToImmutable(), problems.ToImmutable());
    }

    /// <summary>
    /// Lints one unit: every compiled rule against every translatable segment that carries a target,
    /// the missing-target check, then the glossary check.
    /// </summary>
    private static IEnumerable<LintDiagnostic> LintUnit(XliffFile file, XliffUnit unit, ImmutableArray<CompiledRule> compiledRules)
    {
        foreach(XliffSegment segment in unit.Segments)
        {
            if(segment.Kind is not SegmentKind.Translatable || segment.Target is not { } segmentTarget)
            {
                continue;
            }

            foreach(CompiledRule compiled in compiledRules)
            {
                LintDiagnostic? violation = Evaluate(compiled, segmentTarget, file, unit, segment.Id);
                if(violation is not null)
                {
                    yield return violation;
                }
            }
        }

        string? target = unit.Target;
        if(file.TargetLanguage is not null && target is null)
        {
            yield return new LintDiagnostic(
                WellKnownDiagnostics.MissingTarget,
                LintSeverity.Warning,
                $"Unit '{unit.Id}' in file '{file.Id}' has no target text, although the file declares a target language.",
                Locate(file, unit));
        }

        if(target is not null && (file.Glossary is not null || unit.Glossary is not null))
        {
            foreach(LintDiagnostic diagnostic in LintGlossary(file, unit, unit.Source, target))
            {
                yield return diagnostic;
            }
        }
    }

    /// <summary>
    /// Evaluates one compiled rule against a single segment's target text, producing the matching
    /// diagnostic when the rule's constraint is violated.
    /// </summary>
    /// <param name="compiled">The compiled rule and, for a <see cref="RegexRule"/>, its compiled pattern.</param>
    /// <param name="target">The segment's target text.</param>
    /// <param name="file">The file the unit belongs to, used to build the diagnostic's location.</param>
    /// <param name="unit">The unit the violating segment belongs to.</param>
    /// <param name="segmentId">The violating segment's id, or <see langword="null"/> when it has none.</param>
    /// <returns>The violation diagnostic, or <see langword="null"/> when the target satisfies the rule.</returns>
    private static LintDiagnostic? Evaluate(CompiledRule compiled, string target, XliffFile file, XliffUnit unit, string? segmentId)
    {
        ValidationRule rule = compiled.Rule;
        LintLocation location = Locate(file, unit);
        string label = SegmentLabel(unit, segmentId);

        return rule switch
        {
            PresenceRule presence when !Comparable(target, presence.Normalization).Contains(Comparable(presence.Text, presence.Normalization), StringComparison.Ordinal) => new LintDiagnostic(
                WellKnownDiagnostics.PresenceViolation,
                LintSeverity.Warning,
                $"Target for {label} does not contain the required text '{presence.Text}'.",
                location,
                segmentId),

            AbsenceRule absence when Comparable(target, absence.Normalization).Contains(Comparable(absence.Text, absence.Normalization), StringComparison.Ordinal) => new LintDiagnostic(
                WellKnownDiagnostics.AbsenceViolation,
                LintSeverity.Error,
                $"Target for {label} contains the forbidden text '{absence.Text}'.",
                location,
                segmentId),

            LengthBudgetRule budget when Comparable(target, budget.Normalization).Length > budget.MaximumLength => new LintDiagnostic(
                WellKnownDiagnostics.LengthBudgetViolation,
                LintSeverity.Error,
                $"Target for {label} is {Comparable(target, budget.Normalization).Length} UTF-16 code units long, exceeding the maximum of {budget.MaximumLength}.",
                location,
                segmentId),

            RegexRule regexRule when compiled.Regex is not null => EvaluateRegex(compiled.Regex, Comparable(target, regexRule.Normalization), regexRule, label, location, segmentId),

            StartsWithRule prefix when !Comparable(target, prefix.Normalization).StartsWith(Comparable(prefix.Text, prefix.Normalization), StringComparison.Ordinal) => new LintDiagnostic(
                WellKnownDiagnostics.StartsWithViolation,
                LintSeverity.Error,
                $"Target for {label} does not start with the required text '{prefix.Text}'.",
                location,
                segmentId),

            EndsWithRule suffix when !Comparable(target, suffix.Normalization).EndsWith(Comparable(suffix.Text, suffix.Normalization), StringComparison.Ordinal) => new LintDiagnostic(
                WellKnownDiagnostics.EndsWithViolation,
                LintSeverity.Error,
                $"Target for {label} does not end with the required text '{suffix.Text}'.",
                location,
                segmentId),

            _ => null
        };
    }

    /// <summary>
    /// Matches a compiled <see cref="RegexRule"/> against the target, which the caller has already
    /// normalized per the rule's <see cref="ValidationRule.Normalization"/>, catching a
    /// <see cref="RegexMatchTimeoutException"/> from a pathological pattern-target pair and turning it
    /// into a VFX109 diagnostic rather than letting it escape <see cref="Lint"/> and discard every
    /// diagnostic already collected for the document.
    /// </summary>
    private static LintDiagnostic? EvaluateRegex(Regex regex, string target, RegexRule rule, string label, LintLocation location, string? segmentId)
    {
        bool matches;
        try
        {
            matches = regex.IsMatch(target);
        }
        catch(RegexMatchTimeoutException)
        {
            return new LintDiagnostic(
                WellKnownDiagnostics.RegexTimedOut,
                LintSeverity.Error,
                $"Matching the pattern '{rule.Pattern}' against the target for {label} timed out.",
                location,
                segmentId);
        }

        if(matches)
        {
            return null;
        }

        return new LintDiagnostic(
            WellKnownDiagnostics.RegexViolation,
            LintSeverity.Error,
            $"Target for {label} does not match the required pattern '{rule.Pattern}'.",
            location,
            segmentId);
    }

    /// <summary>
    /// Applies the rule's normalization to a piece of text before comparison, per XLIFF 2.1 §5.8.5.8
    /// normalization: Normalization Form C for <see cref="TextNormalization.Nfc"/>, the text unchanged
    /// for <see cref="TextNormalization.None"/>.
    /// </summary>
    private static string Comparable(string value, TextNormalization normalization)
    {
        return normalization is TextNormalization.Nfc ? value.Normalize(NormalizationForm.FormC) : value;
    }

    /// <summary>
    /// Names a unit for a diagnostic message, including the segment id when the violating segment has
    /// one.
    /// </summary>
    private static string SegmentLabel(XliffUnit unit, string? segmentId)
    {
        return segmentId is null ? $"unit '{unit.Id}'" : $"unit '{unit.Id}' segment '{segmentId}'";
    }

    /// <summary>
    /// Checks the file-wide glossary and then the unit's own glossary against the unit's text. Only a
    /// <see cref="GlossaryEntryStatus.Preferred"/> entry gives a well-defined pass or fail: an allowed
    /// rendering is one of several acceptable choices, and a forbidden entry names no expected rendering.
    /// </summary>
    private static IEnumerable<LintDiagnostic> LintGlossary(XliffFile file, XliffUnit unit, string source, string target)
    {
        if(file.Glossary is { } fileGlossary)
        {
            foreach(LintDiagnostic diagnostic in LintGlossary(file, unit, fileGlossary, source, target))
            {
                yield return diagnostic;
            }
        }

        if(unit.Glossary is { } unitGlossary)
        {
            foreach(LintDiagnostic diagnostic in LintGlossary(file, unit, unitGlossary, source, target))
            {
                yield return diagnostic;
            }
        }
    }

    /// <summary>
    /// Checks every <see cref="GlossaryEntryStatus.Preferred"/> entry in one glossary whose scope
    /// applies to the unit against the unit's source and target text. Both the target and the
    /// required rendering are normalized to Unicode Normalization Form C before the containment
    /// check, mirroring <see cref="ContainsTerm"/>'s own normalization of the source and term, so a
    /// precomposed rendering matches a decomposed target and vice versa.
    /// </summary>
    /// <param name="file">The file the unit belongs to, used to build a diagnostic's location.</param>
    /// <param name="unit">The unit being checked.</param>
    /// <param name="glossary">The glossary to check.</param>
    /// <param name="source">The unit's source text, checked for a use of a glossary term.</param>
    /// <param name="target">The unit's target text, checked for the term's required rendering.</param>
    /// <returns>A VFX106 diagnostic for each preferred term used without its required rendering.</returns>
    private static IEnumerable<LintDiagnostic> LintGlossary(XliffFile file, XliffUnit unit, Glossary glossary, string source, string target)
    {
        foreach(GlossaryEntry entry in glossary.Entries)
        {
            if(entry.Status != GlossaryEntryStatus.Preferred)
            {
                continue;
            }

            if(!AppliesToScope(entry.Scopes, unit.Scopes))
            {
                continue;
            }

            if(ContainsTerm(source, entry.Term) && !target.Normalize(NormalizationForm.FormC).Contains(entry.Translation.Normalize(NormalizationForm.FormC), StringComparison.Ordinal))
            {
                yield return new LintDiagnostic(
                    WellKnownDiagnostics.GlossaryRenderingMissing,
                    LintSeverity.Warning,
                    $"Unit '{unit.Id}' uses the glossary term '{entry.Term}' but its target does not contain the required rendering '{entry.Translation}'.",
                    Locate(file, unit));
            }
        }
    }

    /// <summary>
    /// Determines whether a glossary entry applies to a unit: an entry with no scopes applies
    /// everywhere, otherwise it applies only when the entry's scopes and the unit's scopes intersect.
    /// </summary>
    private static bool AppliesToScope(ImmutableArray<Scope> entryScopes, ImmutableArray<Scope> unitScopes)
    {
        return entryScopes.IsEmpty || entryScopes.Any(entryScope => unitScopes.Any(unitScope => string.Equals(entryScope.Value, unitScope.Value, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Determines whether a glossary term occurs in the source on a Unicode word boundary: bounded on
    /// each side by a non-letter, non-digit character, by the start or end of the string, or by a
    /// script that needs no inter-word spacing to separate one word from the next (see
    /// <see cref="IsWordBoundary"/>). Comparison is done after normalizing both strings to Unicode
    /// Normalization Form C, so a precomposed term matches a decomposed source and vice versa. A raw
    /// substring test would let "art" match inside "Start", or a term at the very start of a sentence
    /// go unmatched because of case, so boundaries are checked on code points rather than on a
    /// case-sensitive substring alone.
    /// </summary>
    private static bool ContainsTerm(string source, string term)
    {
        if(term.Length == 0)
        {
            return false;
        }

        string normalizedSource = source.Normalize(NormalizationForm.FormC);
        string normalizedTerm = term.Normalize(NormalizationForm.FormC);

        int index = 0;
        while((index = normalizedSource.IndexOf(normalizedTerm, index, StringComparison.Ordinal)) >= 0)
        {
            int afterIndex = index + normalizedTerm.Length;
            bool leftBoundary = IsWordBoundary(
                index == 0 ? null : normalizedSource[index - 1],
                normalizedTerm[0]);
            bool rightBoundary = IsWordBoundary(
                afterIndex == normalizedSource.Length ? null : normalizedSource[afterIndex],
                normalizedTerm[^1]);
            if(leftBoundary && rightBoundary)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a term's edge sits on a word boundary against its neighbouring character.
    /// A boundary holds when the neighbour is absent (the term sits at the start or end of the
    /// source), is not a letter or digit, is itself a letter in Unicode category
    /// <see cref="UnicodeCategory.OtherLetter"/> (Lo - CJK ideographs, kana, Thai consonants and
    /// similar scripts written without inter-word spaces), or the term's own edge character is in
    /// that category: such scripts carry no whitespace between words, so a neighbouring ideograph
    /// cannot be used to rule the term out the way a neighbouring Latin letter can.
    /// </summary>
    /// <param name="neighbor">The character immediately outside the matched term, or null when the term sits at the start or end of the source.</param>
    /// <param name="edge">The term's own character on that side.</param>
    private static bool IsWordBoundary(char? neighbor, char edge)
    {
        if(neighbor is not char c)
        {
            return true;
        }

        return !char.IsLetterOrDigit(c)
            || char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter
            || char.GetUnicodeCategory(edge) == UnicodeCategory.OtherLetter;
    }

    /// <summary>
    /// Builds a unit's diagnostic location. <see cref="LintLocation.LineNumber"/> is always 0 because
    /// the document model carries no source position information.
    /// </summary>
    /// <param name="file">The file the unit belongs to.</param>
    /// <param name="unit">The unit the diagnostic is about.</param>
    /// <returns>The unit's location.</returns>
    private static LintLocation Locate(XliffFile file, XliffUnit unit)
    {
        return new LintLocation(file.Id, unit.Id, 0);
    }

    /// <summary>
    /// Mirrors the depth-first, units-before-groups traversal the resx cook uses, so every unit in a
    /// file, whether declared directly or nested under groups, is visited exactly once.
    /// </summary>
    private static IEnumerable<XliffUnit> EnumerateUnits(XliffFile file)
    {
        foreach(XliffUnit unit in file.Units)
        {
            yield return unit;
        }

        foreach(XliffGroup group in file.Groups)
        {
            foreach(XliffUnit unit in EnumerateUnits(group))
            {
                yield return unit;
            }
        }
    }

    /// <summary>
    /// Recursively enumerates a group's own units, then every unit of its nested groups, depth-first.
    /// </summary>
    /// <param name="group">The group to enumerate units from.</param>
    /// <returns>Every unit under the group, in document order.</returns>
    private static IEnumerable<XliffUnit> EnumerateUnits(XliffGroup group)
    {
        foreach(XliffUnit unit in group.Units)
        {
            yield return unit;
        }

        foreach(XliffGroup nested in group.Groups)
        {
            foreach(XliffUnit unit in EnumerateUnits(nested))
            {
                yield return unit;
            }
        }
    }

    /// <summary>
    /// One validation rule ready to evaluate, paired with its compiled pattern when it is a
    /// <see cref="RegexRule"/>, so the pattern is compiled once per file rather than once per segment.
    /// </summary>
    /// <param name="Rule">The rule to evaluate.</param>
    /// <param name="Regex">The rule's compiled pattern, when it is a <see cref="RegexRule"/>; otherwise <see langword="null"/>.</param>
    private sealed record CompiledRule(ValidationRule Rule, Regex? Regex);
}
