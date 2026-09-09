using System.Collections.Immutable;
using Lumoin.Vericula.Content;

namespace Lumoin.Vericula.Tests;

/// <summary>Tests for <see cref="InlineTranslatability"/>.</summary>
[TestClass]
public sealed class InlineTranslatabilityTests
{
    [TestMethod]
    public void WalkThrowsForANullContent()
    {
        //Named killer: InlineTranslatability.cs:32, ArgumentNullException.ThrowIfNull(content) deleted
        //(replaced with an empty statement); without the guard the null reaches the foreach over
        //content.Parts and throws NullReferenceException instead of the documented ArgumentNullException.
        Assert.ThrowsExactly<ArgumentNullException>(() => InlineTranslatability.Walk(null!, ImmutableStack<bool>.Empty));
    }

    [TestMethod]
    public void WalkThrowsForANullStack()
    {
        //Named killer: InlineTranslatability.cs:33, ArgumentNullException.ThrowIfNull(stack) deleted
        //(replaced with an empty statement); without the guard the null reaches CurrentlyTranslatable's
        //stack.IsEmpty and throws NullReferenceException instead of the documented ArgumentNullException.
        Assert.ThrowsExactly<ArgumentNullException>(() => InlineTranslatability.Walk(InlineContent.FromText("Cancel"), null!));
    }
}
