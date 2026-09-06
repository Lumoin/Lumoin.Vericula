namespace Lumoin.Vericula.MessageFormat.DataModel;

/// <summary>
/// The base of a Unicode MessageFormat message: either a <see cref="PatternMessage"/> with a single
/// pattern and no selectors, or a <see cref="SelectMessage"/> that selects a pattern by matching
/// selector values against variant keys. Concrete messages are sealed records. See UTS #35 part 9
/// (MessageFormat), version 48.2, section "Message Model" (data model) and "The Message" (syntax).
/// </summary>
public abstract record Message;
