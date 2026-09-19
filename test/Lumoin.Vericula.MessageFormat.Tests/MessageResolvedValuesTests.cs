using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.Evaluation;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Unit tests for <see cref="MessageResolvedValues.Unwrap(MessageResolvedValue)"/>: it projects the
/// plain value apart from handler state, and refuses a null argument.
/// </summary>
[TestClass]
public sealed class MessageResolvedValuesTests
{
    /// <summary>A no-op options value with no entries.</summary>
    private static readonly MessageResolvedOptions EmptyOptions = new(ImmutableDictionary<string, MessageResolvedValue>.Empty);

    /// <summary><see cref="MessageResolvedValues.Unwrap(MessageResolvedValue)"/> returns the resolved value's plain <see cref="MessageResolvedValue.Value"/>.</summary>
    [TestMethod]
    public void UnwrapReturnsThePlainValue()
    {
        //Named killer: MessageResolvedValues.cs's Unwrap, "return value.Value;" changed to return
        //something else (for example the whole MessageResolvedValue, or a fixed constant): this
        //AreEqual against the exact boxed 42 would then fail.
        var resolved = new MessageResolvedValue(42, EmptyOptions, false, false, "42", MessageDirection.Unknown, false,
            static () => new MessageOperation<string>("42", true, []),
            static () => new MessageOperation<ImmutableArray<MessagePart>>([], true, []),
            null, null);

        object? unwrapped = MessageResolvedValues.Unwrap(resolved);

        Assert.AreEqual(42, unwrapped);
    }

    /// <summary><see cref="MessageResolvedValues.Unwrap(MessageResolvedValue)"/> returns null when the resolved value's own <see cref="MessageResolvedValue.Value"/> is null.</summary>
    [TestMethod]
    public void UnwrapReturnsNullWhenTheResolvedValueIsNull()
    {
        var resolved = new MessageResolvedValue(null, EmptyOptions, false, true, "{$x}", MessageDirection.Unknown, false,
            static () => new MessageOperation<string>("{$x}", true, []),
            static () => new MessageOperation<ImmutableArray<MessagePart>>([], true, []),
            null, null);

        Assert.IsNull(MessageResolvedValues.Unwrap(resolved));
    }

    /// <summary>A null argument is refused, naming the parameter.</summary>
    [TestMethod]
    public void ANullValueIsRefused()
    {
        //Named killer: MessageResolvedValues.cs's Unwrap, ArgumentNullException.ThrowIfNull(value)
        //removed: the null-conditional-free "value.Value" would then throw NullReferenceException
        //instead of this ArgumentNullException, and ParamName would not be reported at all.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageResolvedValues.Unwrap(null!));

        Assert.AreEqual("value", exception.ParamName);
    }
}
