namespace System.Runtime.CompilerServices;

/// <summary>
/// A compiler-recognized marker type that lets records and <c>init</c> accessors compile on
/// <c>netstandard2.0</c>, which predates this type in the BCL.
/// </summary>
/// <remarks>
/// The C# compiler looks this type up by name and namespace only, never by assembly, so declaring it
/// here is enough for the compiler to accept <c>init</c> accessors and positional records in this
/// project; it carries no members and is never used directly from source.
/// </remarks>
internal static class IsExternalInit;
