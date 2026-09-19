using System.Collections.Immutable;
using System.Globalization;
using Lumoin.Vericula.MessageFormat.Diagnostics;
using Lumoin.Vericula.MessageFormat.Evaluation;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Construction contract tests for the evaluator records that are not covered by their own dedicated
/// test class: a non-null dictionary parameter is refused when null, and a default (never-assigned)
/// <see cref="ImmutableArray{T}"/> parameter is normalized to empty rather than left default.
/// <see cref="MessageResolvedValue"/>, <see cref="MessageOperation{T}"/> and
/// <see cref="MessageResolvedValues"/> have their own dedicated test classes.
/// </summary>
[TestClass]
public sealed class MessageEvaluationConstructionContractTests
{
    /// <summary>A minimal, valid <see cref="MessageResolvedValue"/>, reused as filler content for the round-trip checks below.</summary>
    private static readonly MessageResolvedValue SampleResolvedValue = new("v",
        new MessageResolvedOptions(ImmutableDictionary<string, MessageResolvedValue>.Empty), false, false, "src",
        MessageDirection.Unknown, false, static () => new MessageOperation<string>("v", true, []),
        static () => new MessageOperation<ImmutableArray<MessagePart>>([], true, []), null, null);

    /// <summary>A null <see cref="MessageResolvedOptions.Values"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void MessageResolvedOptionsRefusesANullValues()
    {
        //Named killer: MessageResolvedOptions.cs's Values property initializer, the "?? throw" removed:
        //a null Values would then be stored silently instead of throwing here.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => new MessageResolvedOptions(null!));

        Assert.AreEqual("Values", exception.ParamName);
    }

    /// <summary>A null <see cref="MessageFunctionRegistry.Functions"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void MessageFunctionRegistryRefusesANullFunctions()
    {
        //Named killer: MessageFunctionRegistry.cs's Functions property initializer, the "?? throw"
        //removed: a null Functions would then be stored silently instead of throwing here.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => new MessageFunctionRegistry(null!));

        Assert.AreEqual("Functions", exception.ParamName);
    }

    /// <summary>A null <see cref="MessagePartOptions.Values"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void MessagePartOptionsRefusesANullValues()
    {
        //Named killer: MessagePartOptions.cs's Values property initializer, the "?? throw" removed: a
        //null Values would then be stored silently instead of throwing here.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => new MessagePartOptions(null!));

        Assert.AreEqual("Values", exception.ParamName);
    }

    /// <summary>A null <see cref="MessageFieldPart.Fields"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void MessageFieldPartRefusesANullFields()
    {
        //Named killer: MessageFieldPart.cs's Fields property initializer, the "?? throw" removed: a
        //null Fields would then be stored silently instead of throwing here.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() => new MessageFieldPart("integer", 1, null!));

        Assert.AreEqual("Fields", exception.ParamName);
    }

    /// <summary>A null <see cref="MessageFormattingContext.Inputs"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void MessageFormattingContextRefusesANullInputs()
    {
        //Named killer: MessageFormattingContext.cs's Inputs property initializer, the "?? throw"
        //removed: a null Inputs would then be stored silently instead of throwing here.
        var options = new MessageEvaluationOptions(MessageBidiStrategy.Default, MessageDirection.Unknown, null,
            new MessageFunctionRegistry(ImmutableDictionary<string, MessageFunction>.Empty));

        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(
            () => new MessageFormattingContext([], null!, options));

        Assert.AreEqual("Inputs", exception.ParamName);
    }

    /// <summary>A default (never-assigned) <see cref="MessageFormattingContext.Locales"/> array normalizes to empty rather than throwing.</summary>
    [TestMethod]
    public void MessageFormattingContextNormalizesADefaultLocalesArrayToEmpty()
    {
        //Named killer: MessageFormattingContext.cs's Locales property initializer, the
        //"Locales.IsDefault ? [] : Locales" normalization removed: reading Locales.Length below would
        //then throw NullReferenceException on the stored default array instead of returning 0.
        var options = new MessageEvaluationOptions(MessageBidiStrategy.Default, MessageDirection.Unknown, null,
            new MessageFunctionRegistry(ImmutableDictionary<string, MessageFunction>.Empty));

        var context = new MessageFormattingContext(default, ImmutableDictionary<string, object?>.Empty, options);

        Assert.IsTrue(context.Locales.IsEmpty);
    }

    /// <summary>A default (never-assigned) <see cref="MessageFunctionContext.Locales"/> array normalizes to empty rather than throwing.</summary>
    [TestMethod]
    public void MessageFunctionContextNormalizesADefaultLocalesArrayToEmpty()
    {
        //Named killer: MessageFunctionContext.cs's Locales property initializer, the
        //"Locales.IsDefault ? [] : Locales" normalization removed: reading Locales.IsEmpty below would
        //then throw NullReferenceException on the stored default array instead of returning true.
        var context = new MessageFunctionContext(default, MessageDirection.Unknown, null);

        Assert.IsTrue(context.Locales.IsEmpty);
    }

    /// <summary>A default (never-assigned) <see cref="MessageValuePart.Parts"/> array normalizes to empty rather than throwing.</summary>
    [TestMethod]
    public void MessageValuePartNormalizesADefaultPartsArrayToEmpty()
    {
        //Named killer: MessageValuePart.cs's Parts property initializer, the "Parts.IsDefault ? [] :
        //Parts" normalization removed: reading Parts.IsEmpty below would then throw
        //NullReferenceException on the stored default array instead of returning true.
        var options = new MessagePartOptions(ImmutableDictionary<string, object?>.Empty);

        var part = new MessageValuePart("string", "hi", default, null, MessageDirection.Unknown, null, options);

        Assert.IsTrue(part.Parts.IsEmpty);
    }

    /// <summary>A default (never-assigned) <see cref="MessageEvaluationResult{T}.Diagnostics"/> array normalizes to empty rather than throwing.</summary>
    [TestMethod]
    public void MessageEvaluationResultNormalizesADefaultDiagnosticsArrayToEmpty()
    {
        //Named killer: MessageEvaluationResult.cs's Diagnostics property initializer, the
        //"Diagnostics.IsDefault ? [] : Diagnostics" normalization removed: reading Diagnostics.IsEmpty
        //below would then throw NullReferenceException on the stored default array instead of
        //returning true.
        var result = new MessageEvaluationResult<string>("formatted", default(ImmutableArray<MessageFormatDiagnostic>));

        Assert.IsTrue(result.Diagnostics.IsEmpty);
    }

    /// <summary>Locale-carrying contexts still round-trip a non-default array unchanged.</summary>
    [TestMethod]
    public void ANonDefaultLocalesArrayIsKeptAsGiven()
    {
        ImmutableArray<CultureInfo> locales = [CultureInfo.InvariantCulture];

        var context = new MessageFunctionContext(locales, MessageDirection.Ltr, "id");

        Assert.HasCount(1, context.Locales);
        Assert.AreEqual(CultureInfo.InvariantCulture, context.Locales[0]);
    }

    /// <summary>A non-default <see cref="MessageResolvedOptions.Values"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void MessageResolvedOptionsValuesIsKeptAsGiven()
    {
        //Named killer: MessageResolvedOptions.cs's Values property initializer, the "?? throw" kept but
        //the caller's dictionary silently replaced (for example with an empty one): a registry that
        //silently drops every registered value is the most consequential minimal wrong change this
        //record admits, and this AreSame catches it.
        ImmutableDictionary<string, MessageResolvedValue> distinctive = ImmutableDictionary<string, MessageResolvedValue>.Empty.Add("k", SampleResolvedValue);

        var options = new MessageResolvedOptions(distinctive);

        Assert.AreSame(distinctive, options.Values);
    }

    /// <summary>A non-default <see cref="MessageFunctionRegistry.Functions"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void MessageFunctionRegistryFunctionsIsKeptAsGiven()
    {
        //Named killer: MessageFunctionRegistry.cs's Functions property initializer, the "?? throw" kept
        //but the caller's dictionary silently replaced: a registry that silently drops every registered
        //function is the most consequential minimal wrong change this record admits.
        MessageFunction stub = static (operand, options, context)
            => new MessageOperation<MessageResolvedValue>(null, false, [new MessageFunctionError(MessageFunctionErrorKind.BadOperand, "x")]);
        ImmutableDictionary<string, MessageFunction> distinctive = ImmutableDictionary<string, MessageFunction>.Empty.Add("string", stub);

        var registry = new MessageFunctionRegistry(distinctive);

        Assert.AreSame(distinctive, registry.Functions);
    }

    /// <summary>A non-default <see cref="MessagePartOptions.Values"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void MessagePartOptionsValuesIsKeptAsGiven()
    {
        //Named killer: MessagePartOptions.cs's Values property initializer, the "?? throw" kept but the
        //caller's dictionary silently replaced.
        ImmutableDictionary<string, object?> distinctive = ImmutableDictionary<string, object?>.Empty.Add("k", "v");

        var options = new MessagePartOptions(distinctive);

        Assert.AreSame(distinctive, options.Values);
    }

    /// <summary>A non-default <see cref="MessageFieldPart.Fields"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void MessageFieldPartFieldsIsKeptAsGiven()
    {
        //Named killer: MessageFieldPart.cs's Fields property initializer, the "?? throw" kept but the
        //caller's dictionary silently replaced.
        ImmutableDictionary<string, object?> distinctive = ImmutableDictionary<string, object?>.Empty.Add("k", "v");

        var part = new MessageFieldPart("integer", 1, distinctive);

        Assert.AreSame(distinctive, part.Fields);
    }

    /// <summary>A non-default <see cref="MessageFormattingContext.Inputs"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void MessageFormattingContextInputsIsKeptAsGiven()
    {
        //Named killer: MessageFormattingContext.cs's Inputs property initializer, the "?? throw" kept
        //but the caller's dictionary silently replaced.
        var options = new MessageEvaluationOptions(MessageBidiStrategy.Default, MessageDirection.Unknown, null,
            new MessageFunctionRegistry(ImmutableDictionary<string, MessageFunction>.Empty));
        ImmutableDictionary<string, object?> distinctive = ImmutableDictionary<string, object?>.Empty.Add("name", "value");

        var context = new MessageFormattingContext([], distinctive, options);

        Assert.AreSame(distinctive, context.Inputs);
    }

    /// <summary>A non-default <see cref="MessageFormattingContext.Locales"/> array round-trips unchanged rather than being silently discarded.</summary>
    [TestMethod]
    public void MessageFormattingContextLocalesIsKeptAsGiven()
    {
        //Named killer: MessageFormattingContext.cs's Locales property initializer, the
        //"Locales.IsDefault ? [] : Locales" kept but the caller's non-default array silently discarded
        //(for example "Locales.IsDefault ? [] : []"): a caller-supplied locale list would then be
        //silently dropped.
        var options = new MessageEvaluationOptions(MessageBidiStrategy.Default, MessageDirection.Unknown, null,
            new MessageFunctionRegistry(ImmutableDictionary<string, MessageFunction>.Empty));
        ImmutableArray<CultureInfo> locales = [CultureInfo.InvariantCulture];

        var context = new MessageFormattingContext(locales, ImmutableDictionary<string, object?>.Empty, options);

        Assert.HasCount(1, context.Locales);
        Assert.AreEqual(CultureInfo.InvariantCulture, context.Locales[0]);
    }

    /// <summary>A non-default <see cref="MessageValuePart.Parts"/> array round-trips unchanged rather than being silently discarded.</summary>
    [TestMethod]
    public void MessageValuePartPartsIsKeptAsGiven()
    {
        //Named killer: MessageValuePart.cs's Parts property initializer, the "Parts.IsDefault ? [] :
        //Parts" kept but the caller's non-default array silently discarded.
        var options = new MessagePartOptions(ImmutableDictionary<string, object?>.Empty);
        ImmutableArray<MessageFieldPart> parts = [new MessageFieldPart("integer", 1, ImmutableDictionary<string, object?>.Empty)];

        var part = new MessageValuePart("number", 1, parts, null, MessageDirection.Unknown, null, options);

        Assert.HasCount(1, part.Parts);
        Assert.AreEqual("integer", part.Parts[0].Type);
    }

    /// <summary>A non-default <see cref="MessageEvaluationResult{T}.Diagnostics"/> array round-trips unchanged rather than being silently discarded.</summary>
    [TestMethod]
    public void MessageEvaluationResultDiagnosticsIsKeptAsGiven()
    {
        //Named killer: MessageEvaluationResult.cs's Diagnostics property initializer, the
        //"Diagnostics.IsDefault ? [] : Diagnostics" kept but the caller's non-default array silently
        //discarded.
        ImmutableArray<MessageFormatDiagnostic> diagnostics = [new MessageFormatDiagnostic(WellKnownMessageFormatDiagnostics.UnresolvedVariable, "msg", -1, 0, 0)];

        var result = new MessageEvaluationResult<string>("formatted", diagnostics);

        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual(WellKnownMessageFormatDiagnostics.UnresolvedVariable, result.Diagnostics[0].Id);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can set a null <see cref="MessageResolvedOptions.Values"/> where the constructor would have refused it.</summary>
    [TestMethod]
    public void WithBypassesTheResolvedOptionsValuesValidation()
    {
        //Pins the documented with-bypass (MessageResolvedOptions.cs's <remarks>): current, intended
        //behavior, not a mutant to kill.
        var options = new MessageResolvedOptions(ImmutableDictionary<string, MessageResolvedValue>.Empty);

        MessageResolvedOptions withNullValues = options with { Values = null! };

        Assert.IsNull(withNullValues.Values);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can set a null <see cref="MessageFunctionRegistry.Functions"/> where the constructor would have refused it.</summary>
    [TestMethod]
    public void WithBypassesTheFunctionRegistryFunctionsValidation()
    {
        //Pins the documented with-bypass (MessageFunctionRegistry.cs's <remarks>): current, intended
        //behavior, not a mutant to kill.
        var registry = new MessageFunctionRegistry(ImmutableDictionary<string, MessageFunction>.Empty);

        MessageFunctionRegistry withNullFunctions = registry with { Functions = null! };

        Assert.IsNull(withNullFunctions.Functions);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can set a null <see cref="MessagePartOptions.Values"/> where the constructor would have refused it.</summary>
    [TestMethod]
    public void WithBypassesThePartOptionsValuesValidation()
    {
        //Pins the documented with-bypass (MessagePartOptions.cs's <remarks>): current, intended
        //behavior, not a mutant to kill.
        var options = new MessagePartOptions(ImmutableDictionary<string, object?>.Empty);

        MessagePartOptions withNullValues = options with { Values = null! };

        Assert.IsNull(withNullValues.Values);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can set a null <see cref="MessageFieldPart.Fields"/> where the constructor would have refused it.</summary>
    [TestMethod]
    public void WithBypassesTheFieldPartFieldsValidation()
    {
        //Pins the documented with-bypass (MessageFieldPart.cs's <remarks>): current, intended behavior,
        //not a mutant to kill.
        var part = new MessageFieldPart("integer", 1, ImmutableDictionary<string, object?>.Empty);

        MessageFieldPart withNullFields = part with { Fields = null! };

        Assert.IsNull(withNullFields.Fields);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can restore a default <see cref="MessageFormattingContext.Locales"/> array where the constructor would have normalized it to empty.</summary>
    [TestMethod]
    public void WithBypassesTheFormattingContextLocalesNormalization()
    {
        //Pins the documented with-bypass (MessageFormattingContext.cs's <remarks>): restores exactly
        //the default array the normalization exists to remove; reading a member such as IsEmpty on the
        //result would otherwise throw NullReferenceException far from this assignment. Current,
        //intended behavior, not a mutant to kill.
        var options = new MessageEvaluationOptions(MessageBidiStrategy.Default, MessageDirection.Unknown, null,
            new MessageFunctionRegistry(ImmutableDictionary<string, MessageFunction>.Empty));
        var context = new MessageFormattingContext([CultureInfo.InvariantCulture], ImmutableDictionary<string, object?>.Empty, options);

        MessageFormattingContext withDefaultLocales = context with { Locales = default };

        Assert.IsTrue(withDefaultLocales.Locales.IsDefault);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can restore a default <see cref="MessageFunctionContext.Locales"/> array where the constructor would have normalized it to empty.</summary>
    [TestMethod]
    public void WithBypassesTheFunctionContextLocalesNormalization()
    {
        //Pins the documented with-bypass (MessageFunctionContext.cs's <remarks>): current, intended
        //behavior, not a mutant to kill.
        var context = new MessageFunctionContext([CultureInfo.InvariantCulture], MessageDirection.Unknown, null);

        MessageFunctionContext withDefaultLocales = context with { Locales = default };

        Assert.IsTrue(withDefaultLocales.Locales.IsDefault);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can restore a default <see cref="MessageValuePart.Parts"/> array where the constructor would have normalized it to empty.</summary>
    [TestMethod]
    public void WithBypassesTheValuePartPartsNormalization()
    {
        //Pins the documented with-bypass (MessageValuePart.cs's <remarks>): current, intended behavior,
        //not a mutant to kill.
        var options = new MessagePartOptions(ImmutableDictionary<string, object?>.Empty);
        var part = new MessageValuePart("string", "hi", [], null, MessageDirection.Unknown, null, options);

        MessageValuePart withDefaultParts = part with { Parts = default };

        Assert.IsTrue(withDefaultParts.Parts.IsDefault);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can restore a default <see cref="MessageEvaluationResult{T}.Diagnostics"/> array where the constructor would have normalized it to empty.</summary>
    [TestMethod]
    public void WithBypassesTheEvaluationResultDiagnosticsNormalization()
    {
        //Pins the documented with-bypass (MessageEvaluationResult.cs's <remarks>): current, intended
        //behavior, not a mutant to kill.
        var result = new MessageEvaluationResult<string>("formatted", []);

        MessageEvaluationResult<string> withDefaultDiagnostics = result with { Diagnostics = default };

        Assert.IsTrue(withDefaultDiagnostics.Diagnostics.IsDefault);
    }
}
