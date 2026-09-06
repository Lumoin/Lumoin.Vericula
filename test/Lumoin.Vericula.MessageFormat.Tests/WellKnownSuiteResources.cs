using Lumoin.Vericula.Text;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// The embedded-resource logical names <see cref="MessageFormatSuite"/> reads: the three
/// documentation files, the prefix and suffix every vendored test file's logical name is built from,
/// and the sixteen vendored test files themselves. Each is spelled once as a UTF-8 source literal and
/// carried alongside as an interned string, the same pair-form contract
/// <see cref="Lumoin.Vericula.Diagnostics.WellKnownDiagnostics"/> uses in the core library.
/// </summary>
public static class WellKnownSuiteResources
{
    /// <summary>The UTF-8 source literal of <see cref="License"/>.</summary>
    public static ReadOnlySpan<byte> LicenseUtf8 => "LICENSE"u8;

    /// <summary>The embedded-resource logical name of the suite's licence text.</summary>
    public static readonly string License = Utf8Constants.ToInternedString(LicenseUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Notice"/>.</summary>
    public static ReadOnlySpan<byte> NoticeUtf8 => "NOTICE.md"u8;

    /// <summary>The embedded-resource logical name of this vendoring's own notice.</summary>
    public static readonly string Notice = Utf8Constants.ToInternedString(NoticeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="Readme"/>.</summary>
    public static ReadOnlySpan<byte> ReadmeUtf8 => "README.md"u8;

    /// <summary>The embedded-resource logical name of the suite's own read-me.</summary>
    public static readonly string Readme = Utf8Constants.ToInternedString(ReadmeUtf8);

    /// <summary>The UTF-8 source literal of <see cref="TestFilePrefix"/>.</summary>
    public static ReadOnlySpan<byte> TestFilePrefixUtf8 => "tests/"u8;

    /// <summary>The prefix every vendored test-file resource's logical name starts with.</summary>
    public static readonly string TestFilePrefix = Utf8Constants.ToInternedString(TestFilePrefixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="TestFileSuffix"/>.</summary>
    public static ReadOnlySpan<byte> TestFileSuffixUtf8 => ".json"u8;

    /// <summary>The suffix every vendored test-file resource's logical name ends with.</summary>
    public static readonly string TestFileSuffix = Utf8Constants.ToInternedString(TestFileSuffixUtf8);

    /// <summary>The UTF-8 source literal of <see cref="BidiJson"/>.</summary>
    public static ReadOnlySpan<byte> BidiJsonUtf8 => "tests/bidi.json"u8;

    /// <summary>The vendored file of bidi-isolation cases.</summary>
    public static readonly string BidiJson = Utf8Constants.ToInternedString(BidiJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="DataModelErrorsJson"/>.</summary>
    public static ReadOnlySpan<byte> DataModelErrorsJsonUtf8 => "tests/data-model-errors.json"u8;

    /// <summary>The vendored file of data-model-error cases.</summary>
    public static readonly string DataModelErrorsJson = Utf8Constants.ToInternedString(DataModelErrorsJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FallbackJson"/>.</summary>
    public static ReadOnlySpan<byte> FallbackJsonUtf8 => "tests/fallback.json"u8;

    /// <summary>The vendored file of fallback-formatting cases.</summary>
    public static readonly string FallbackJson = Utf8Constants.ToInternedString(FallbackJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsCurrencyJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsCurrencyJsonUtf8 => "tests/functions/currency.json"u8;

    /// <summary>The vendored file of <c>:currency</c> cases.</summary>
    public static readonly string FunctionsCurrencyJson = Utf8Constants.ToInternedString(FunctionsCurrencyJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsDateJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsDateJsonUtf8 => "tests/functions/date.json"u8;

    /// <summary>The vendored file of <c>:date</c> cases.</summary>
    public static readonly string FunctionsDateJson = Utf8Constants.ToInternedString(FunctionsDateJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsDatetimeJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsDatetimeJsonUtf8 => "tests/functions/datetime.json"u8;

    /// <summary>The vendored file of <c>:datetime</c> cases.</summary>
    public static readonly string FunctionsDatetimeJson = Utf8Constants.ToInternedString(FunctionsDatetimeJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsIntegerJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsIntegerJsonUtf8 => "tests/functions/integer.json"u8;

    /// <summary>The vendored file of <c>:integer</c> cases.</summary>
    public static readonly string FunctionsIntegerJson = Utf8Constants.ToInternedString(FunctionsIntegerJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsNumberJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsNumberJsonUtf8 => "tests/functions/number.json"u8;

    /// <summary>The vendored file of <c>:number</c> cases.</summary>
    public static readonly string FunctionsNumberJson = Utf8Constants.ToInternedString(FunctionsNumberJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsOffsetJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsOffsetJsonUtf8 => "tests/functions/offset.json"u8;

    /// <summary>The vendored file of <c>:offset</c> cases.</summary>
    public static readonly string FunctionsOffsetJson = Utf8Constants.ToInternedString(FunctionsOffsetJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsPercentJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsPercentJsonUtf8 => "tests/functions/percent.json"u8;

    /// <summary>The vendored file of <c>:percent</c> cases.</summary>
    public static readonly string FunctionsPercentJson = Utf8Constants.ToInternedString(FunctionsPercentJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsStringJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsStringJsonUtf8 => "tests/functions/string.json"u8;

    /// <summary>The vendored file of <c>:string</c> cases.</summary>
    public static readonly string FunctionsStringJson = Utf8Constants.ToInternedString(FunctionsStringJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="FunctionsTimeJson"/>.</summary>
    public static ReadOnlySpan<byte> FunctionsTimeJsonUtf8 => "tests/functions/time.json"u8;

    /// <summary>The vendored file of <c>:time</c> cases.</summary>
    public static readonly string FunctionsTimeJson = Utf8Constants.ToInternedString(FunctionsTimeJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="PatternSelectionJson"/>.</summary>
    public static ReadOnlySpan<byte> PatternSelectionJsonUtf8 => "tests/pattern-selection.json"u8;

    /// <summary>The vendored file of pattern-selection cases.</summary>
    public static readonly string PatternSelectionJson = Utf8Constants.ToInternedString(PatternSelectionJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SyntaxErrorsJson"/>.</summary>
    public static ReadOnlySpan<byte> SyntaxErrorsJsonUtf8 => "tests/syntax-errors.json"u8;

    /// <summary>The vendored file of syntax-error cases.</summary>
    public static readonly string SyntaxErrorsJson = Utf8Constants.ToInternedString(SyntaxErrorsJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="SyntaxJson"/>.</summary>
    public static ReadOnlySpan<byte> SyntaxJsonUtf8 => "tests/syntax.json"u8;

    /// <summary>The vendored file of valid-syntax cases.</summary>
    public static readonly string SyntaxJson = Utf8Constants.ToInternedString(SyntaxJsonUtf8);

    /// <summary>The UTF-8 source literal of <see cref="UOptionsJson"/>.</summary>
    public static ReadOnlySpan<byte> UOptionsJsonUtf8 => "tests/u-options.json"u8;

    /// <summary>The vendored file of <c>u:</c>-namespaced option cases.</summary>
    public static readonly string UOptionsJson = Utf8Constants.ToInternedString(UOptionsJsonUtf8);
}
