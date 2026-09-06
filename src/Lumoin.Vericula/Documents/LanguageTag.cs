using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lumoin.Vericula.Documents;

/// <summary>
/// A BCP 47 language tag, carried as the raw string value.
/// </summary>
[DebuggerDisplay("LanguageTag: {Value}")]
public sealed record LanguageTag(string Value)
{
    //A permissive shape check, not a full BCP 47 registry validation: one or more subtags of 1 to 8
    //ASCII letters or digits, separated by single hyphens, with the first subtag letters only (the
    //primary language subtag) and no surrounding or embedded whitespace.
    private static readonly Regex Shape = new("^[A-Za-z]{1,8}(-[A-Za-z0-9]{1,8})*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Determines whether <paramref name="value"/> has the shape of a BCP 47 language tag. XLIFF 2.1
    /// §4.3.1.29 srcLang and §4.3.1.37 trgLang both describe their value as "A language code as
    /// described in [BCP 47]"; an empty string, whitespace, or a tag with an underscore or an embedded
    /// space does not have that shape.
    /// </summary>
    /// <param name="value">The candidate language tag, or null.</param>
    /// <returns><see langword="true"/> if the value has the shape of a BCP 47 language tag; otherwise, <see langword="false"/>.</returns>
    public static bool IsWellFormed(string? value) => value is not null && Shape.IsMatch(value);
}
