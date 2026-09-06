using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// The JSON shape of one entry in a suite case's <c>expErrors</c> array: only the error's type
/// string, which is all the schema's <c>expErrors</c> item defines. Deserialized only inside the
/// loader; <see cref="SuiteCase.ExpErrors"/> carries the flattened type strings this record's array
/// reduces to.
/// </summary>
/// <param name="Type">One of the suite's thirteen well-known error type strings; see <see cref="WellKnownSuiteErrorTypes"/>.</param>
[DebuggerDisplay("SuiteRawExpectedError: {Type,nq}")]
internal sealed record SuiteRawExpectedError(string Type);
