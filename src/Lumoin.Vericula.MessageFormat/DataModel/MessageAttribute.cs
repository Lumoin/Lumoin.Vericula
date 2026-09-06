using System.Diagnostics;

namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// An attribute attached to an expression or to markup, written as <c>@name</c> or
/// <c>@name=value</c>. Named <see cref="MessageAttribute"/> rather than <c>Attribute</c> so it never
/// collides with <see cref="System.Attribute"/>. See UTS #35 part 9 (MessageFormat), version 48.2,
/// section "Attribute Model" (data model) and "Attributes" (syntax).
/// </summary>
/// <param name="Name">The attribute's full identifier, including its namespace when one is present.</param>
/// <param name="Value">The attribute's literal value, or <see langword="null"/> for a valueless attribute.</param>
[DebuggerDisplay("MessageAttribute: {Name}")]
public sealed record MessageAttribute(string Name, Literal? Value);
