using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.DataModel;
using Lumoin.Vericula.MessageFormat.Evaluation;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Unit tests for <see cref="MessageEvaluator"/>'s scaffold: each of the four methods refuses a null
/// <see cref="DataModel.Message"/> or a null <see cref="MessageFormattingContext"/>, and otherwise
/// throws <see cref="NotImplementedException"/>, since the evaluator itself arrives in a later step.
/// </summary>
[TestClass]
public sealed class MessageEvaluatorTests
{
    /// <summary>A minimal message: no declarations, an empty pattern.</summary>
    private static readonly PatternMessage TinyMessage = new(ImmutableArray<Declaration>.Empty, new Pattern(ImmutableArray<PatternPart>.Empty));

    /// <summary>A minimal formatting context: no locales, no inputs, the default evaluation options.</summary>
    private static readonly MessageFormattingContext TinyContext = new(
        [],
        ImmutableDictionary<string, object?>.Empty,
        new MessageEvaluationOptions(MessageBidiStrategy.Default, MessageDirection.Unknown, null,
            new MessageFunctionRegistry(ImmutableDictionary<string, MessageFunction>.Empty)));

    /// <summary><see cref="MessageEvaluator.Format(Message, MessageFormattingContext)"/> refuses a null message.</summary>
    [TestMethod]
    public void FormatRefusesANullMessage()
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageEvaluator.Format(null!, TinyContext));

        Assert.AreEqual("message", exception.ParamName);
    }

    /// <summary><see cref="MessageEvaluator.Format(Message, MessageFormattingContext)"/> refuses a null context.</summary>
    [TestMethod]
    public void FormatRefusesANullContext()
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageEvaluator.Format(TinyMessage, null!));

        Assert.AreEqual("ctx", exception.ParamName);
    }

    /// <summary><see cref="MessageEvaluator.Format(Message, MessageFormattingContext)"/> is not yet implemented for valid arguments.</summary>
    [TestMethod]
    public void FormatThrowsNotImplementedForValidArguments()
    {
        //Named killer: MessageEvaluator.cs's Format, the null checks reordered after the throw (or
        //removed): a null message or ctx would then surface NotImplementedException instead of
        //ArgumentNullException, which FormatRefusesANullMessage/FormatRefusesANullContext would catch;
        //this test pins the current (correct) valid-argument behavior so a future real implementation
        //change here is a deliberate one.
        Assert.ThrowsExactly<NotImplementedException>(() => MessageEvaluator.Format(TinyMessage, TinyContext));
    }

    /// <summary><see cref="MessageEvaluator.TryFormat(Message, MessageFormattingContext)"/> refuses a null message.</summary>
    [TestMethod]
    public void TryFormatRefusesANullMessage()
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageEvaluator.TryFormat(null!, TinyContext));

        Assert.AreEqual("message", exception.ParamName);
    }

    /// <summary><see cref="MessageEvaluator.TryFormat(Message, MessageFormattingContext)"/> refuses a null context.</summary>
    [TestMethod]
    public void TryFormatRefusesANullContext()
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageEvaluator.TryFormat(TinyMessage, null!));

        Assert.AreEqual("ctx", exception.ParamName);
    }

    /// <summary><see cref="MessageEvaluator.TryFormat(Message, MessageFormattingContext)"/> is not yet implemented for valid arguments.</summary>
    [TestMethod]
    public void TryFormatThrowsNotImplementedForValidArguments()
    {
        Assert.ThrowsExactly<NotImplementedException>(() => MessageEvaluator.TryFormat(TinyMessage, TinyContext));
    }

    /// <summary><see cref="MessageEvaluator.FormatToParts(Message, MessageFormattingContext)"/> refuses a null message.</summary>
    [TestMethod]
    public void FormatToPartsRefusesANullMessage()
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageEvaluator.FormatToParts(null!, TinyContext));

        Assert.AreEqual("message", exception.ParamName);
    }

    /// <summary><see cref="MessageEvaluator.FormatToParts(Message, MessageFormattingContext)"/> refuses a null context.</summary>
    [TestMethod]
    public void FormatToPartsRefusesANullContext()
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageEvaluator.FormatToParts(TinyMessage, null!));

        Assert.AreEqual("ctx", exception.ParamName);
    }

    /// <summary><see cref="MessageEvaluator.FormatToParts(Message, MessageFormattingContext)"/> is not yet implemented for valid arguments.</summary>
    [TestMethod]
    public void FormatToPartsThrowsNotImplementedForValidArguments()
    {
        Assert.ThrowsExactly<NotImplementedException>(() => MessageEvaluator.FormatToParts(TinyMessage, TinyContext));
    }

    /// <summary><see cref="MessageEvaluator.TryFormatToParts(Message, MessageFormattingContext)"/> refuses a null message.</summary>
    [TestMethod]
    public void TryFormatToPartsRefusesANullMessage()
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageEvaluator.TryFormatToParts(null!, TinyContext));

        Assert.AreEqual("message", exception.ParamName);
    }

    /// <summary><see cref="MessageEvaluator.TryFormatToParts(Message, MessageFormattingContext)"/> refuses a null context.</summary>
    [TestMethod]
    public void TryFormatToPartsRefusesANullContext()
    {
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => MessageEvaluator.TryFormatToParts(TinyMessage, null!));

        Assert.AreEqual("ctx", exception.ParamName);
    }

    /// <summary><see cref="MessageEvaluator.TryFormatToParts(Message, MessageFormattingContext)"/> is not yet implemented for valid arguments.</summary>
    [TestMethod]
    public void TryFormatToPartsThrowsNotImplementedForValidArguments()
    {
        Assert.ThrowsExactly<NotImplementedException>(() => MessageEvaluator.TryFormatToParts(TinyMessage, TinyContext));
    }
}
