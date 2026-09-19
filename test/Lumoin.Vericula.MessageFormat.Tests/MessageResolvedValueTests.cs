using System.Collections.Immutable;
using Lumoin.Vericula.MessageFormat.Evaluation;

namespace Lumoin.Vericula.MessageFormat.Tests;

/// <summary>
/// Construction contract tests for <see cref="MessageResolvedValue"/>: a custom function author builds
/// this type directly, so a broken value is refused at construction rather than left to surface later.
/// </summary>
[TestClass]
public sealed class MessageResolvedValueTests
{
    /// <summary>A no-op options value with no entries, reused across the valid arguments below.</summary>
    private static readonly MessageResolvedOptions EmptyOptions = new(ImmutableDictionary<string, MessageResolvedValue>.Empty);

    /// <summary>A <see cref="Evaluation.FormatValue"/> stub that always succeeds with a fixed string.</summary>
    private static readonly FormatValue Format = static () => new MessageOperation<string>("formatted", true, []);

    /// <summary>A <see cref="Evaluation.FormatValueParts"/> stub that always succeeds with no parts.</summary>
    private static readonly FormatValueParts FormatParts = static () => new MessageOperation<ImmutableArray<MessagePart>>([], true, []);

    /// <summary>A <see cref="Evaluation.MatchValue"/> stub that always matches.</summary>
    private static readonly MatchValue Match = static _ => new MessageOperation<bool>(true, true, []);

    /// <summary>A <see cref="Evaluation.PreferKey"/> stub that never prefers.</summary>
    private static readonly PreferKey BetterThan = static (_, _) => new MessageOperation<bool>(false, true, []);

    /// <summary>A null <see cref="MessageResolvedValue.Options"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void ANullOptionsIsRefused()
    {
        //Named killer: MessageResolvedValue.cs's Options property initializer, the "?? throw" removed:
        //a null Options would then be stored silently instead of throwing here.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(
            () => new MessageResolvedValue(null, null!, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, null, null));

        Assert.AreEqual("Options", exception.ParamName);
    }

    /// <summary>A null <see cref="MessageResolvedValue.Source"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void ANullSourceIsRefused()
    {
        //Named killer: MessageResolvedValue.cs's Source property initializer, the "?? throw" removed:
        //a null Source would then be stored silently instead of throwing here.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(
            () => new MessageResolvedValue("v", EmptyOptions, false, false, null!, MessageDirection.Unknown, false, Format, FormatParts, null, null));

        Assert.AreEqual("Source", exception.ParamName);
    }

    /// <summary>A null <see cref="MessageResolvedValue.Format"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void ANullFormatIsRefused()
    {
        //Named killer: MessageResolvedValue.cs's Format property initializer, the "?? throw" removed:
        //a null Format would then be stored silently instead of throwing here.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(
            () => new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, null!, FormatParts, null, null));

        Assert.AreEqual("Format", exception.ParamName);
    }

    /// <summary>A null <see cref="MessageResolvedValue.FormatParts"/> is refused, naming the parameter.</summary>
    [TestMethod]
    public void ANullFormatPartsIsRefused()
    {
        //Named killer: MessageResolvedValue.cs's FormatParts property initializer, the "?? throw"
        //removed: a null FormatParts would then be stored silently instead of throwing here.
        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(
            () => new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, null!, null, null));

        Assert.AreEqual("FormatParts", exception.ParamName);
    }

    /// <summary>A present <see cref="MessageResolvedValue.Match"/> with a null <see cref="MessageResolvedValue.BetterThan"/> is refused, naming <c>BetterThan</c> as the missing half.</summary>
    [TestMethod]
    public void MatchWithoutBetterThanIsRefused()
    {
        //Named killer: MessageResolvedValue.cs's Match property initializer, the (Match is null) ==
        //(BetterThan is null) equality check replaced with a constant true: this mismatched pair would
        //then construct successfully instead of throwing.
        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(
            () => new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, Match, null));

        Assert.AreEqual("BetterThan", exception.ParamName);
    }

    /// <summary>A present <see cref="MessageResolvedValue.BetterThan"/> with a null <see cref="MessageResolvedValue.Match"/> is refused, naming <c>Match</c> as the missing half.</summary>
    [TestMethod]
    public void BetterThanWithoutMatchIsRefused()
    {
        //Named killer: MessageResolvedValue.cs's Match property initializer, the (Match is null) ==
        //(BetterThan is null) equality check replaced with a constant true: this mismatched pair would
        //then construct successfully instead of throwing.
        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(
            () => new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, null, BetterThan));

        Assert.AreEqual("Match", exception.ParamName);
    }

    /// <summary>Both <see cref="MessageResolvedValue.Match"/> and <see cref="MessageResolvedValue.BetterThan"/> null constructs successfully.</summary>
    [TestMethod]
    public void BothMatchAndBetterThanNullConstructsSuccessfully()
    {
        var value = new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, null, null);

        Assert.IsNull(value.Match);
        Assert.IsNull(value.BetterThan);
    }

    /// <summary>Both <see cref="MessageResolvedValue.Match"/> and <see cref="MessageResolvedValue.BetterThan"/> present constructs successfully.</summary>
    [TestMethod]
    public void BothMatchAndBetterThanPresentConstructsSuccessfully()
    {
        var value = new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, Match, BetterThan);

        Assert.AreSame(Match, value.Match);
        Assert.AreSame(BetterThan, value.BetterThan);
    }

    /// <summary>A non-default <see cref="MessageResolvedValue.Options"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void OptionsIsKeptAsGiven()
    {
        //Named killer: MessageResolvedValue.cs's Options property initializer, the "?? throw" kept but
        //the caller's value silently replaced (for example with EmptyOptions): this AreSame against the
        //distinctive instance would then fail.
        var distinctive = new MessageResolvedOptions(ImmutableDictionary<string, MessageResolvedValue>.Empty.Add("k",
            new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, null, null)));

        var value = new MessageResolvedValue("v", distinctive, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, null, null);

        Assert.AreSame(distinctive, value.Options);
    }

    /// <summary>A non-default <see cref="MessageResolvedValue.Source"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void SourceIsKeptAsGiven()
    {
        //Named killer: MessageResolvedValue.cs's Source property initializer, the "?? throw" kept but
        //the caller's value silently replaced (for example with string.Empty): this AreEqual against
        //the distinctive string would then fail.
        var value = new MessageResolvedValue("v", EmptyOptions, false, false, "distinctive-source", MessageDirection.Unknown, false, Format, FormatParts, null, null);

        Assert.AreEqual("distinctive-source", value.Source);
    }

    /// <summary>A non-default <see cref="MessageResolvedValue.Format"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void FormatIsKeptAsGiven()
    {
        //Named killer: MessageResolvedValue.cs's Format property initializer, the "?? throw" kept but
        //the caller's delegate silently replaced: this AreSame against the given delegate would then fail.
        var value = new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, null, null);

        Assert.AreSame(Format, value.Format);
    }

    /// <summary>A non-default <see cref="MessageResolvedValue.FormatParts"/> round-trips unchanged rather than being silently replaced.</summary>
    [TestMethod]
    public void FormatPartsIsKeptAsGiven()
    {
        //Named killer: MessageResolvedValue.cs's FormatParts property initializer, the "?? throw" kept
        //but the caller's delegate silently replaced: this AreSame against the given delegate would
        //then fail.
        var value = new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, null, null);

        Assert.AreSame(FormatParts, value.FormatParts);
    }

    /// <summary>A <c>with</c> expression is not revalidated: it can set a null <see cref="MessageResolvedValue.Options"/> where the constructor would have refused it.</summary>
    [TestMethod]
    public void WithBypassesTheOptionsValidation()
    {
        //Pins the documented with-bypass (MessageResolvedValue.cs's <remarks>): the primary constructor
        //validates, but "with" uses the synthesized copy constructor and the plain init accessor, which
        //runs no validation. Current, intended behavior, not a mutant to kill.
        var value = new MessageResolvedValue("v", EmptyOptions, false, false, "src", MessageDirection.Unknown, false, Format, FormatParts, null, null);

        MessageResolvedValue withNullOptions = value with { Options = null! };

        Assert.IsNull(withNullOptions.Options);
    }
}
