using System.Reflection;
using System.Text.RegularExpressions;

namespace Lumoin.Vericula.Tests;

/// <summary>
/// Reflection- and source-level tests holding every enum in the core assembly, public or private, to
/// the owner's numeric contract: every member carries an explicit, dense value starting at zero, in
/// declaration order, and the source text spells that value out rather than leaving it implicit.
/// </summary>
[TestClass]
public sealed partial class EnumContractTests
{
    /// <summary>The number of enum types, public and private (including nested), the core assembly is expected to declare.</summary>
    private const int ExpectedCoreEnumCount = 17;

    /// <summary>
    /// Walks every enum type in the core assembly, public or private, and asserts its members carry
    /// dense values 0, 1, 2, ... in declaration order, with no gaps, duplicates, or reordering. Also
    /// asserts the number of enum types found, so a future enum added to the assembly is noticed here.
    /// </summary>
    [TestMethod]
    public void EveryEnumMemberCarriesADenseValueInDeclarationOrder()
    {
        //Kills M-06 (src/Lumoin.Vericula/Tone/ToneRegister.cs:36, Kenjogo = 6: value 6 at position 5)
        //and M-07 (src/Lumoin.Vericula/Linting/LintSeverity.cs:16,21, Warning/Error values swapped:
        //both out of position), and the non-colliding gap variant of M-09
        //(src/Lumoin.Vericula/Parsing/XliffReader.cs:1629, MetadataKind.Scopes = 5: a gap at position
        //3): each throws off the 0,1,2,... check at the member's declaration index.
        Type[] enumTypes = [.. EnumTypes(typeof(XliffVersion).Assembly)];
        var failures = new List<string>();

        foreach(Type type in enumTypes)
        {
            failures.AddRange(DenseValueFailures(type));
        }

        Assert.HasCount(0, failures, string.Join(Environment.NewLine, failures));
        Assert.AreEqual(
            ExpectedCoreEnumCount,
            enumTypes.Length,
            $"Expected {ExpectedCoreEnumCount} enum types in the core assembly, found {enumTypes.Length}: {string.Join(", ", enumTypes.Select(static type => type.FullName))}.");
    }

    /// <summary>
    /// A source-level check reflection cannot make: <c>Open = 1</c> and an implicit <c>Open</c> compile
    /// to the same constant, so only the source text can tell them apart. Scans every enum declaration
    /// under <c>src</c>, public or private, and asserts every member line spells out its value
    /// explicitly.
    /// </summary>
    [TestMethod]
    public void EveryEnumMemberInTheSourceTreeDeclaresItsValueExplicitly()
    {
        //Kills M-05 (src/Lumoin.Vericula/Units/SegmentState.cs:31, "= 4" removed from
        //NeedsTranslation) and M-10 (src/Lumoin.Vericula.MessageFormat/Parsing/MessageParser.Scanning.cs:20,
        //"= 2" removed from Ok): the compiler still assigns the same constants either way, so reflection
        //stays green, but the bare member line fails ExplicitValueMemberLine and is reported here.
        string? repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        if(repositoryRoot is null)
        {
            Assert.Inconclusive("Could not find Lumoin.Vericula.slnx above the test binary's directory; this check only runs from a source checkout.");

            return;
        }

        string sourceRoot = Path.Combine(repositoryRoot, "src");
        var violations = new List<string>();

        foreach(string file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if(file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(static segment => segment is "bin" or "obj"))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
            violations.AddRange(Violations(relativePath, File.ReadLines(file)));
        }

        Assert.HasCount(0, violations, string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Feeds the source scanner an in-memory enum body missing an explicit value on one member, proving
    /// the scanner itself (not merely the files it happens to see today) would catch the regression.
    /// </summary>
    [TestMethod]
    public void SourceScannerFlagsAMemberWithoutAnExplicitValue()
    {
        string[] body =
        [
            "public enum Sample",
            "{",
            "    /// <summary>First.</summary>",
            "    First = 0,",
            "",
            "    /// <summary>Second, missing its value.</summary>",
            "    Second",
            "}"
        ];

        //Kills M-08: EnumContractTests.cs:292, ExplicitValueMemberLine widened to
        //^\s*\w+\s*(=\s*-?\d+)?\s*,?\s*(//.*)?$ (the value group made optional); the bare "Second"
        //line then matches and Violations yields nothing, so this HasCount(1) fails.
        List<string> violations = [.. Violations("Sample.cs", body)];

        Assert.HasCount(1, violations);
        Assert.AreEqual("Sample.cs:7: Second", violations[0]);
    }

    /// <summary>
    /// Feeds the source scanner an in-memory <c>private enum</c> body missing an explicit value on one
    /// member, proving the scanner covers private, nested enum declarations too, not only public ones.
    /// </summary>
    [TestMethod]
    public void SourceScannerFlagsAPrivateEnumMemberWithoutAnExplicitValue()
    {
        string[] body =
        [
            "private enum SampleState",
            "{",
            "    /// <summary>First.</summary>",
            "    First = 0,",
            "",
            "    /// <summary>Second, missing its value.</summary>",
            "    Second",
            "}"
        ];

        //Kills M-08 (same mutant as the public-enum self-check, proven again over a private enum body):
        //EnumContractTests.cs:292, ExplicitValueMemberLine widened to accept a bare member; "Second"
        //then yields no violation here either, so HasCount(1) fails.
        List<string> violations = [.. Violations("Sample.cs", body)];

        Assert.HasCount(1, violations);
        Assert.AreEqual("Sample.cs:7: Second", violations[0]);
    }

    /// <summary>Finds every enum type declared in <paramref name="assembly"/>, public or private, including nested ones.</summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The assembly's enum types.</returns>
    private static IEnumerable<Type> EnumTypes(Assembly assembly)
    {
        return assembly.GetTypes().Where(static type => type.IsEnum);
    }

    /// <summary>Checks one enum type's members for a dense, in-order 0, 1, 2, ... value sequence.</summary>
    /// <param name="type">The enum type to check.</param>
    /// <returns>One failure message per problem found; empty when the type is dense and in order.</returns>
    private static IEnumerable<string> DenseValueFailures(Type type)
    {
        if(Enum.GetUnderlyingType(type) != typeof(int))
        {
            yield return $"{type.FullName} has underlying type {Enum.GetUnderlyingType(type).Name}, expected int.";

            yield break;
        }

        //GetFields(Public | Static) returns each enum member in metadata order, which is the
        //compiler's declaration order; no additional sort is applied or needed here.
        FieldInfo[] members = type.GetFields(BindingFlags.Public | BindingFlags.Static);
        if(members.Length == 0)
        {
            yield return $"{type.FullName} declares no members.";

            yield break;
        }

        for(int index = 0; index < members.Length; index++)
        {
            var value = (int)members[index].GetRawConstantValue()!;
            if(value != index)
            {
                yield return $"{type.FullName}.{members[index].Name} is {value}, expected {index} for declaration position {index}.";
            }
        }
    }

    /// <summary>Walks upward from <paramref name="startDirectory"/> until a directory containing <c>Lumoin.Vericula.slnx</c> is found.</summary>
    /// <param name="startDirectory">The directory to start searching from.</param>
    /// <returns>The repository root, or <see langword="null"/> when none was found.</returns>
    private static string? FindRepositoryRoot(string startDirectory)
    {
        DirectoryInfo? directory = new(startDirectory);
        while(directory is not null)
        {
            if(File.Exists(Path.Combine(directory.FullName, "Lumoin.Vericula.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Scans the lines of one C# source file for enum declarations, public or private, and reports
    /// every member line that does not spell out an explicit value. A pure function over lines, so the
    /// scanner itself can be unit-tested without touching the file system. Blank, doc-comment, and
    /// attribute lines are recognized and skipped before the brace depth counter ever sees them, so an
    /// unbalanced brace inside a doc comment (for example a future <c>&lt;summary&gt;</c> mentioning a
    /// single <c>{</c>) cannot desynchronize the counter and end the body early. Enum bodies do not nest
    /// in this codebase, so the counter itself is otherwise a plain depth tally.
    /// </summary>
    /// <param name="relativePath">The path reported in each violation, relative to the repository root.</param>
    /// <param name="lines">The file's lines, in order.</param>
    /// <returns>One violation, formatted as <c>relative path:line: text</c>, per offending member line.</returns>
    internal static IEnumerable<string> Violations(string relativePath, IEnumerable<string> lines)
    {
        string[] lineArray = [.. lines];
        int lineIndex = 0;

        while(lineIndex < lineArray.Length)
        {
            if(!EnumDeclarationLine().IsMatch(lineArray[lineIndex]))
            {
                lineIndex++;

                continue;
            }

            int openLine = lineIndex;
            while(openLine < lineArray.Length && !lineArray[openLine].Contains('{'))
            {
                openLine++;
            }

            if(openLine >= lineArray.Length)
            {
                lineIndex++;

                continue;
            }

            int depth = 1;
            int bodyLine = openLine + 1;

            for(; bodyLine < lineArray.Length; bodyLine++)
            {
                string text = lineArray[bodyLine];
                string trimmed = text.Trim();

                if(trimmed.Length == 0 || trimmed.StartsWith("///", StringComparison.Ordinal)
                    || trimmed.StartsWith('['))
                {
                    continue;
                }

                foreach(char character in text)
                {
                    if(character == '{')
                    {
                        depth++;
                    }
                    else if(character == '}')
                    {
                        depth--;
                    }
                }

                if(depth == 0)
                {
                    break;
                }

                if(trimmed is "{" or "}")
                {
                    continue;
                }

                if(!ExplicitValueMemberLine().IsMatch(text))
                {
                    yield return $"{relativePath}:{bodyLine + 1}: {trimmed}";
                }
            }

            lineIndex = bodyLine + 1;
        }
    }

    /// <summary>
    /// Matches an enum declaration line with any accessibility (or none): <c>public</c>,
    /// <c>internal</c>, <c>private</c>, <c>protected</c>, or <c>file</c>. Every enum in this codebase
    /// is in scope for the numeric-value contract, including private nested state enums.
    /// </summary>
    [GeneratedRegex(@"^\s*(?:(?:public|internal|private|protected|file)\s+)*enum\s+\w+")]
    private static partial Regex EnumDeclarationLine();

    /// <summary>Matches an enum member line carrying an explicit integer value, optionally followed by a trailing comment.</summary>
    [GeneratedRegex(@"^\s*\w+\s*=\s*-?\d+\s*,?\s*(//.*)?$")]
    private static partial Regex ExplicitValueMemberLine();
}
