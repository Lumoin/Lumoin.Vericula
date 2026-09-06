using System.Reflection;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Reflection tests holding every enum in the MessageFormat assembly, public or private, to the
/// owner's numeric contract, plus the specifics of <see cref="MarkupKind.None"/> as the uninitialized
/// value the parser never produces. The source-text scan lives in the core project's
/// <c>EnumContractTests</c>, which walks the whole <c>src</c> tree including this project.
/// </summary>
[TestClass]
public sealed class EnumContractTests
{
    /// <summary>The number of enum types, public and private (including nested), the MessageFormat assembly is expected to declare.</summary>
    private const int ExpectedMessageFormatEnumCount = 2;

    /// <summary>
    /// Walks every enum type in the MessageFormat assembly, public or private, and asserts its members
    /// carry dense values 0, 1, 2, ... in declaration order, with no gaps, duplicates, or reordering.
    /// Also asserts the number of enum types found, so a future enum added to the assembly is noticed.
    /// </summary>
    [TestMethod]
    public void EveryEnumMemberCarriesADenseValueInDeclarationOrder()
    {
        //Kills M-02 (MarkupKind.cs:24,27, Standalone/Close values swapped: both out of position) and
        //the non-colliding gap variant of M-01 (MarkupKind.cs:21, Open = 4: a gap at position 1, no
        //collision with Standalone/Close): each throws off the 0,1,2,... check.
        Type[] enumTypes = [.. EnumTypes(typeof(MarkupKind).Assembly)];
        var failures = new List<string>();

        foreach(Type type in enumTypes)
        {
            failures.AddRange(DenseValueFailures(type));
        }

        Assert.HasCount(0, failures, string.Join(Environment.NewLine, failures));
        Assert.AreEqual(
            ExpectedMessageFormatEnumCount,
            enumTypes.Length,
            $"Expected {ExpectedMessageFormatEnumCount} enum types in the MessageFormat assembly, found {enumTypes.Length}: {string.Join(", ", enumTypes.Select(static type => type.FullName))}.");
    }

    /// <summary>
    /// <see cref="MarkupKind.None"/> is the value a default-initialized <see cref="MarkupKind"/> holds,
    /// numerically zero, and the parser never assigns it to a part it builds from source: parsing an
    /// open, a standalone, and a close markup element each yields a <see cref="MarkupKind"/> other than
    /// <see cref="MarkupKind.None"/>.
    /// </summary>
    [TestMethod]
    public void MarkupKindNoneIsTheDefaultValueAndIsNeverParsed()
    {
        //Kills M-04: src/Lumoin.Vericula.MessageFormat/Parsing/MessageParser.Expressions.cs:298, the
        //"/" opener's ternary arm assigning MarkupKind.None instead of MarkupKind.Close; the close part
        //of {#b}bold{/b} then reads None and the AreEqual(MarkupKind.Close, close.Kind) below fails.
        MarkupKind defaultValue = default;
        Assert.AreEqual(MarkupKind.None, defaultValue);

        int noneValue = (int)MarkupKind.None;
        Assert.AreEqual(0, noneValue);

        MarkupPart open = ParsedMarkup("{#b}bold{/b}", partIndex: 0);
        Assert.AreEqual(MarkupKind.Open, open.Kind);
        Assert.AreNotEqual(MarkupKind.None, open.Kind);

        MarkupPart standalone = ParsedMarkup("{#img/}", partIndex: 0);
        Assert.AreEqual(MarkupKind.Standalone, standalone.Kind);
        Assert.AreNotEqual(MarkupKind.None, standalone.Kind);

        MarkupPart close = ParsedMarkup("{#b}bold{/b}", partIndex: 2);
        Assert.AreEqual(MarkupKind.Close, close.Kind);
        Assert.AreNotEqual(MarkupKind.None, close.Kind);
    }

    /// <summary>
    /// The three source shapes of markup carry values 1 to 3, leaving 0 to <see cref="MarkupKind.None"/>.
    /// These numbers are now a persisted public contract and must never be renumbered.
    /// </summary>
    [TestMethod]
    public void MarkupKindSourceShapesCarryValuesOneToThree()
    {
        //These values are a persisted public contract and are never renumbered. Kills M-01
        //(MarkupKind.cs:21, Open = 2 or any other value: openValue != 1) and M-02 (MarkupKind.cs:24,27,
        //Standalone/Close swapped: standaloneValue reads 3).
        int openValue = (int)MarkupKind.Open;
        int standaloneValue = (int)MarkupKind.Standalone;
        int closeValue = (int)MarkupKind.Close;

        Assert.AreEqual(1, openValue);
        Assert.AreEqual(2, standaloneValue);
        Assert.AreEqual(3, closeValue);
    }

    /// <summary>Parses <paramref name="source"/> and returns the <see cref="MarkupPart"/> at <paramref name="partIndex"/> of its pattern.</summary>
    /// <param name="source">The MessageFormat source text to parse.</param>
    /// <param name="partIndex">The zero-based index of the expected markup part within the pattern.</param>
    /// <returns>The markup part at that index.</returns>
    private static MarkupPart ParsedMarkup(string source, int partIndex)
    {
        MessageParseResult result = MessageFormatReader.TryParse(source);

        Assert.HasCount(0, result.Diagnostics);
        var message = result.Message as PatternMessage;
        Assert.IsNotNull(message);

        var part = message.Pattern.Parts[partIndex] as MarkupPart;
        Assert.IsNotNull(part);

        return part;
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
}
