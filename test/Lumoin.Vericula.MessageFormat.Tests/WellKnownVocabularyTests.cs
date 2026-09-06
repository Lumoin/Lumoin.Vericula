using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Lumoin.Vericula.MessageFormat.Parsing;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Reflection-based tests that walk every public <c>WellKnown*</c> vocabulary class in the
/// MessageFormat assembly and in this test assembly, and verify the pair-form contract the whole
/// vocabulary layer depends on: an interned string field decodes to exactly the UTF-8 source literal
/// it is built from, and an <c>IsX</c> predicate compares its own value ordinally rather than by a
/// looser or case-insensitive rule. Mirrors <c>Lumoin.Vericula.Tests.WellKnownVocabularyTests</c> for
/// this pair of assemblies: today it exercises the MessageFormat assembly's
/// <see cref="Lumoin.Vericula.MessageFormat.Diagnostics.WellKnownMessageFormatDiagnostics"/> and this
/// test assembly's <see cref="WellKnownSuiteErrorTypes"/>, and picks up any future <c>WellKnown*</c>
/// class in either assembly automatically.
/// </summary>
[TestClass]
public sealed class WellKnownVocabularyTests
{
    /// <summary>Every interned string field of every scanned <c>WellKnown*</c> class decodes to exactly its matching UTF-8 literal.</summary>
    [TestMethod]
    public void EveryWellKnownInternedStringMatchesItsUtf8Literal()
    {
        var mismatches = new List<string>();

        foreach(Type type in WellKnownTypes())
        {
            foreach(PropertyInfo utf8Property in Utf8Properties(type))
            {
                string baseName = utf8Property.Name[..^"Utf8".Length];
                FieldInfo? stringField = type.GetField(baseName, BindingFlags.Public | BindingFlags.Static);

                if(stringField is null || stringField.FieldType != typeof(string))
                {
                    mismatches.Add($"{type.FullName}.{utf8Property.Name} has no matching public static string field '{baseName}'.");

                    continue;
                }

                string decoded = Encoding.UTF8.GetString(ReadUtf8Bytes(utf8Property));
                var interned = (string)stringField.GetValue(null)!;

                if(!string.Equals(decoded, interned, StringComparison.Ordinal))
                {
                    mismatches.Add($"{type.FullName}.{baseName} is \"{interned}\" but {utf8Property.Name} decodes to \"{decoded}\".");
                }
            }
        }

        Assert.HasCount(0, mismatches, string.Join(Environment.NewLine, mismatches));
    }

    /// <summary>Every <c>IsX(value)</c> predicate of every scanned <c>WellKnown*</c> class accepts its own value and rejects a case-swapped variant.</summary>
    [TestMethod]
    public void EveryWellKnownIsPredicateComparesOrdinally()
    {
        var failures = new List<string>();

        foreach(Type type in WellKnownTypes())
        {
            foreach(MethodInfo predicate in IsPredicates(type))
            {
                string baseName = predicate.Name["Is".Length..];
                FieldInfo? stringField = type.GetField(baseName, BindingFlags.Public | BindingFlags.Static);

                if(stringField is null || stringField.FieldType != typeof(string))
                {
                    //Composite or non-value predicates (for example IsStatic) have no single field of
                    //their own to compare against and are out of scope for this check.
                    continue;
                }

                var value = (string)stringField.GetValue(null)!;

                if(!(bool)predicate.Invoke(null, [value])!)
                {
                    failures.Add($"{type.FullName}.{predicate.Name}(\"{value}\") returned false for its own value.");
                }

                string caseSwapped = SwapCase(value);
                if(!string.Equals(caseSwapped, value, StringComparison.Ordinal)
                    && (bool)predicate.Invoke(null, [caseSwapped])!)
                {
                    failures.Add($"{type.FullName}.{predicate.Name}(\"{caseSwapped}\") returned true; the comparison is not ordinal.");
                }
            }
        }

        Assert.HasCount(0, failures, string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Finds every public, sealed, static (abstract-and-sealed) <c>WellKnown*</c> class in the
    /// MessageFormat assembly and in this test assembly.
    /// </summary>
    /// <returns>The well-known vocabulary types from both assemblies.</returns>
    private static IEnumerable<Type> WellKnownTypes()
    {
        return WellKnownTypesIn(typeof(MessageFormatReader).Assembly)
            .Concat(WellKnownTypesIn(typeof(WellKnownSuiteErrorTypes).Assembly));
    }

    /// <summary>Finds every public, sealed, static (abstract-and-sealed) <c>WellKnown*</c> class in one assembly.</summary>
    /// <param name="assembly">The assembly to search.</param>
    /// <returns>The assembly's well-known vocabulary types.</returns>
    private static IEnumerable<Type> WellKnownTypesIn(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(type => type.IsClass && type.IsAbstract && type.IsSealed && type.IsPublic
                && type.Name.StartsWith("WellKnown", StringComparison.Ordinal));
    }

    /// <summary>
    /// Finds every public static property of <paramref name="type"/> whose name ends in "Utf8" and
    /// whose type is <see cref="ReadOnlySpan{T}"/> of <see langword="byte"/>: the UTF-8 half of each
    /// well-known pair.
    /// </summary>
    /// <param name="type">The well-known vocabulary type to inspect.</param>
    /// <returns>The type's UTF-8 span properties.</returns>
    private static IEnumerable<PropertyInfo> Utf8Properties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(ReadOnlySpan<byte>)
                && property.Name.EndsWith("Utf8", StringComparison.Ordinal));
    }

    /// <summary>Finds every public static <c>Is*(string)</c> predicate method of <paramref name="type"/>.</summary>
    /// <param name="type">The well-known vocabulary type to inspect.</param>
    /// <returns>The type's <c>Is*</c> predicate methods.</returns>
    private static IEnumerable<MethodInfo> IsPredicates(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name.StartsWith("Is", StringComparison.Ordinal)
                && method.ReturnType == typeof(bool)
                && method.GetParameters() is [{ ParameterType.FullName: "System.String" }]);
    }

    /// <summary>
    /// Reads a static <see cref="ReadOnlySpan{T}"/>-returning property through a compiled expression
    /// rather than <see cref="PropertyInfo.GetValue(object?)"/>, which cannot invoke a member whose
    /// return type is a byref-like type such as <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="property">The UTF-8 span property to read.</param>
    /// <returns>The span's bytes, copied into an array.</returns>
    private static byte[] ReadUtf8Bytes(PropertyInfo property)
    {
        MethodCallExpression call = Expression.Call(property.GetMethod!);
        MethodInfo toArray = typeof(ReadOnlySpan<byte>).GetMethod("ToArray")!;
        MethodCallExpression toArrayCall = Expression.Call(call, toArray);

        return Expression.Lambda<Func<byte[]>>(toArrayCall).Compile()();
    }

    /// <summary>Inverts the case of every cased character in <paramref name="value"/>, leaving uncased characters untouched.</summary>
    /// <param name="value">The value to case-swap.</param>
    /// <returns>The case-swapped value.</returns>
    private static string SwapCase(string value)
    {
        return string.Create(value.Length, value, static (span, source) =>
        {
            for(int index = 0; index < source.Length; index++)
            {
                char character = source[index];
                span[index] = char.IsUpper(character) ? char.ToLowerInvariant(character)
                    : char.IsLower(character) ? char.ToUpperInvariant(character)
                    : character;
            }
        });
    }
}
