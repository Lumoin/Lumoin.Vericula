using System.IO.Pipelines;
using System.Text;
using Lumoin.Vericula.Content;
using Lumoin.Vericula.Documents;
using Lumoin.Vericula.Glossaries;
using Lumoin.Vericula.Parsing;
using Lumoin.Vericula.Tone;
using Lumoin.Vericula.Units;
using Lumoin.Vericula.Validation;

namespace Lumoin.Vericula.Tests;

[TestClass]
public sealed class XliffModuleReaderTests
{
    private static string[] ShellAndCards { get; } = ["shell", "cards"];

    private static string[] ShellOnly { get; } = ["shell"];

    private static string[] CardsAndHome { get; } = ["cards", "home"];

    private const string Prologue = """<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" xmlns:mda="urn:oasis:names:tc:xliff:metadata:2.0" xmlns:val="urn:oasis:names:tc:xliff:validation:2.0" xmlns:gls="urn:oasis:names:tc:xliff:glossary:2.0" xmlns:vericula="urn:lumoin:vericula:xliff:1.0" version="2.1" srcLang="en" trgLang="fi">""";

    [TestMethod]
    public void ReadsAToneProfileFromFileMetadata()
    {
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:tone">
                    <mda:meta type="version">1.0</mda:meta>
                    <mda:meta type="authority">house-tone</mda:meta>
                    <mda:meta type="register">Formal</mda:meta>
                    <mda:meta type="voice">brand voice</mda:meta>
                    <mda:meta type="orientation">Vertical</mda:meta>
                    <mda:meta type="calendar">gregorian</mda:meta>
                    <mda:meta type="dateFormat">yyyy-MM-dd</mda:meta>
                    <mda:meta type="digitStyle">FullWidth</mda:meta>
                    <mda:metaGroup category="vericula:toneMetadata"><mda:meta type="channel">mobile</mda:meta></mda:metaGroup>
                    <mda:metaGroup category="other-tool:thing"><mda:meta type="rogue">desktop</mda:meta></mda:metaGroup>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        ToneProfile? tone = Read(xliff).Files[0].ToneProfile;

        Assert.IsNotNull(tone);
        Assert.AreEqual("1.0", tone.Version);
        Assert.AreEqual("house-tone", tone.Authority);
        Assert.AreEqual(ToneRegister.Formal, tone.Register);
        Assert.AreEqual("brand voice", tone.Voice);
        Assert.AreEqual(TextOrientation.Vertical, tone.Orientation);
        Assert.AreEqual("gregorian", tone.Calendar);
        Assert.AreEqual("yyyy-MM-dd", tone.DateFormat);
        Assert.AreEqual(DigitStyle.FullWidth, tone.DigitStyle);
        Assert.AreEqual("mobile", tone.Metadata["channel"]);

        //XliffReader.cs:1017 ParseTone: removing the `continue;` for a nested metaGroup whose
        //category is not vericula:toneMetadata would fold this other-tool:thing group's meta into
        //the tone's free-form metadata too, growing it past the one entry the toneMetadata group
        //actually contributes.
        Assert.HasCount(1, tone.Metadata);
    }

    [TestMethod]
    public void RejectsAToneProfileMetadataGroupWithoutAVersion()
    {
        //XliffReader.cs:1009 ParseTone: removing the throw (statement mutation) would let a null
        //version reach ToneProfile's constructor instead of failing the read, and blanking the
        //message (string mutation) would leave the caller unable to tell what was missing; the
        //message assertion catches both, since a swallowed throw never reaches Assert.ThrowsExactly.
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:tone">
                    <mda:meta type="authority">house-tone</mda:meta>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("tone profile metadata group has no version", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsAToneProfileDefaultingOmittedEnumFieldsToTheirZeroValue()
    {
        //XliffReader.cs:1473 "return default;" removed from its block (id 770): when register,
        //orientation and digitStyle are absent, ParseEnum must return each enum's zero value rather
        //than falling through to the "not a valid ..." throw for a null value.
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:tone">
                    <mda:meta type="version">1.0</mda:meta>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        ToneProfile? tone = Read(xliff).Files[0].ToneProfile;

        Assert.IsNotNull(tone);
        Assert.AreEqual(ToneRegister.Casual, tone.Register);
        Assert.AreEqual(TextOrientation.Horizontal, tone.Orientation);
        Assert.AreEqual(DigitStyle.HalfWidth, tone.DigitStyle);
    }

    [TestMethod]
    public void RejectsANumericToneRegisterValueThatIsNotADefinedMember()
    {
        //XliffReader.cs:1477 && => ||: Enum.TryParse("99", ...) succeeds because .NET parses a purely
        //numeric string as the enum's raw underlying value without checking membership, so TryParse
        //alone returns true here; only && with Enum.IsDefined stops the undefined ordinal 99 from being
        //accepted as ToneRegister.Casual (the enum's zero member). With || the condition would be true
        //from TryParse alone and no exception would be thrown.
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:tone">
                    <mda:meta type="version">1.0</mda:meta>
                    <mda:meta type="register">99</mda:meta>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("not a valid register", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAToneRegisterValueThatOnlyDiffersFromAMemberByCase()
    {
        //XliffReader.cs:1477 ignoreCase: false => true: "formal" only matches ToneRegister.Formal when
        //case is folded, so with ignoreCase:true this lowercase value would parse successfully and pass
        //Enum.IsDefined, silently accepting a differently-cased register the writer would never emit
        //instead of throwing.
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:tone">
                    <mda:meta type="version">1.0</mda:meta>
                    <mda:meta type="register">formal</mda:meta>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("not a valid register", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAToneProfileWithAnUnrecognizedRegisterValue()
    {
        //XliffReader.cs:1482 throw(...) => ";" (id 775) and the message => "" (id 776): an enum value
        //outside the defined members must still throw XliffFormatException, and the message must name
        //the offending value and the field so a caller can act on it.
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:tone">
                    <mda:meta type="version">1.0</mda:meta>
                    <mda:meta type="register">Sarcastic</mda:meta>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("Sarcastic", exception.Message, StringComparison.Ordinal);
        Assert.Contains("register", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsAToneProfileEvenWhenAnUnrecognizedMetaHasNoType()
    {
        //XliffReader.cs:968, continue => ;: removing the continue after ParseTone lets the tone
        //metaGroup's own <meta> children fall into the generic named-metadata loop that requires a
        //type attribute, so an untyped <meta> that ParseTone itself ignores would make the mutant
        //throw where the reader must not.
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:tone">
                    <mda:meta type="version">1.0</mda:meta>
                    <mda:meta>untyped and ignored</mda:meta>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        ToneProfile? tone = Read(xliff).Files[0].ToneProfile;

        Assert.IsNotNull(tone);
        Assert.AreEqual("1.0", tone.Version);
    }

    [TestMethod]
    public void RejectsAMetadataElementWithoutATypeAttribute()
    {
        //XliffReader.cs:1418 RequiredType: removing the throw lets a null type reach the metadata
        //dictionary's indexer as a null key, which throws ArgumentNullException instead of the
        //intended XliffFormatException, so asserting the exact exception type alone kills this
        //mutant; the message assertion additionally guards the paired string mutation on this line.
        string xliff = Prologue + """
              <file id="wallet">
                <unit id="A">
                  <mda:metadata>
                    <mda:metaGroup category="vericula:metadata"><mda:meta>orphan</mda:meta></mda:metaGroup>
                  </mda:metadata>
                  <segment><source>Home</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("required type attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsAFileWideGlossaryFromMetadata()
    {
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:glossary">
                    <mda:metaGroup category="vericula:glossaryEntry">
                      <mda:meta type="term">wallet</mda:meta>
                      <mda:meta type="translation">lompakko</mda:meta>
                      <mda:meta type="definition">a place to store claims</mda:meta>
                      <mda:meta type="status">Forbidden</mda:meta>
                      <mda:meta type="rationale">brand term</mda:meta>
                      <mda:meta type="scope">shell</mda:meta>
                      <mda:meta type="scope">cards</mda:meta>
                    </mda:metaGroup>
                    <mda:metaGroup category="other-tool:thing"/>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        //XliffReader.cs:1053 ParseGlossaryMetadata: removing the `continue;` for a nested metaGroup
        //whose category is not vericula:glossaryEntry would treat the trailing other-tool:thing
        //group as an entry too, and since it carries no term or translation, Read would throw where
        //it must instead skip the foreign group and return the one real entry below.
        GlossaryEntry entry = Read(xliff).Files[0].Glossary!.Entries.Single();

        Assert.AreEqual("wallet", entry.Term);
        Assert.AreEqual("lompakko", entry.Translation);
        Assert.AreEqual("a place to store claims", entry.Definition);
        Assert.AreEqual(GlossaryEntryStatus.Forbidden, entry.Status);
        Assert.AreEqual("brand term", entry.Rationale);
        CollectionAssert.AreEqual(ShellAndCards, entry.Scopes.Select(scope => scope.Value).ToArray());
    }

    [TestMethod]
    public void IgnoresAStrayMetaSiblingAfterParsingAFileWideGlossaryMetadataGroup()
    {
        //id 573, XliffReader.cs:975: `continue;` => `;`. Without the continue, a stray <mda:meta>
        //sibling of the vericula:glossary metaGroup would fall into the generic metadata catch-all,
        //which requires a type attribute and throws; the reader must stop at the glossary group instead.
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:glossary">
                    <mda:metaGroup category="vericula:glossaryEntry">
                      <mda:meta type="term">wallet</mda:meta>
                      <mda:meta type="translation">lompakko</mda:meta>
                    </mda:metaGroup>
                    <mda:meta>stray</mda:meta>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        GlossaryEntry entry = Read(xliff).Files[0].Glossary!.Entries.Single();

        Assert.AreEqual("wallet", entry.Term);
        Assert.AreEqual("lompakko", entry.Translation);
    }

    [TestMethod]
    public void RejectsAFileWideGlossaryEntryWithoutATranslation()
    {
        //The writer refuses this shape too (see XliffWriterTests), but the reader must refuse it
        //independently, since a hand-authored document never goes through the writer's checks.
        string xliff = Prologue + """
              <file id="wallet">
                <mda:metadata>
                  <mda:metaGroup category="vericula:glossary">
                    <mda:metaGroup category="vericula:glossaryEntry">
                      <mda:meta type="term">wallet</mda:meta>
                    </mda:metaGroup>
                  </mda:metaGroup>
                </mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("lacks its term or translation", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsTheGlossaryModuleOnAUnitOneEntryPerTranslation()
    {
        string xliff = Prologue + """
              <file id="wallet">
                <unit id="A">
                  <gls:glossary>
                    <gls:glossEntry vericula:rationale="brand term" vericula:scopes="shell cards">
                      <gls:term>wallet</gls:term>
                      <gls:translation>lompakko</gls:translation>
                      <gls:translation>kukkaro</gls:translation>
                      <gls:translation vericula:status="Forbidden">pussi</gls:translation>
                      <gls:definition>a place to store claims</gls:definition>
                    </gls:glossEntry>
                  </gls:glossary>
                  <segment><source>Home</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        Glossary? glossary = Read(xliff).Files[0].Units[0].Glossary;

        Assert.IsNotNull(glossary);
        Assert.HasCount(3, glossary.Entries);
        Assert.AreEqual(GlossaryEntryStatus.Preferred, glossary.Entries[0].Status);
        Assert.AreEqual("lompakko", glossary.Entries[0].Translation);
        Assert.AreEqual(GlossaryEntryStatus.Allowed, glossary.Entries[1].Status);
        Assert.AreEqual(GlossaryEntryStatus.Forbidden, glossary.Entries[2].Status);
        foreach(GlossaryEntry entry in glossary.Entries)
        {
            Assert.AreEqual("wallet", entry.Term);
            Assert.AreEqual("a place to store claims", entry.Definition);
            Assert.AreEqual("brand term", entry.Rationale);
            CollectionAssert.AreEqual(ShellAndCards, entry.Scopes.Select(scope => scope.Value).ToArray());
        }
    }

    [TestMethod]
    public void RejectsAGlossaryEntryWithoutATranslation()
    {
        string xliff = Prologue + """
              <file id="wallet">
                <unit id="A">
                  <gls:glossary><gls:glossEntry><gls:term>wallet</gls:term></gls:glossEntry></gls:glossary>
                  <segment><source>Home</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        //id 632, XliffReader.cs:1123: message mutated to "" would erase which term and unit lacked a
        //translation.
        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("for 'wallet' in unit 'A'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAGlossaryEntryWithoutATerm()
    {
        //XliffReader.cs:1100 ParseGlossaryModule: removing the throw (statement mutation) would let
        //a null term reach GlossaryEntry's constructor instead of failing the read, and blanking the
        //message (string mutation) would drop the unit id the caller needs to find the offending
        //entry; the message assertion catches both, since a swallowed throw never reaches
        //Assert.ThrowsExactly at all.
        string xliff = Prologue + """
              <file id="wallet">
                <unit id="A">
                  <gls:glossary><gls:glossEntry><gls:translation>lompakko</gls:translation></gls:glossEntry></gls:glossary>
                  <segment><source>Home</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("unit 'A' has no <term>", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsValidationRulesOfEveryKindAndKeepsDisabledOnes()
    {
        //r1-13: XLIFF 2.1 §5.8.4.3 <rule> Processing Requirements say "Modifiers MUST NOT remove
        //either <rule> elements or their attributes defined in this module", so a disabled="yes" rule
        //must survive the read into the model with Disabled set true; it is the linter, at evaluation
        //time, that skips it, not the reader that drops it. This test used to assert the disabled
        //rule was absent from the array entirely.
        string xliff = Prologue + """
              <file id="wallet">
                <val:validation>
                  <val:rule isPresent="{0}"/>
                  <val:rule isNotPresent="TODO"/>
                  <val:rule startsWith="Hei"/>
                  <val:rule endsWith="!"/>
                  <val:rule vericula:maxLength="40"/>
                  <val:rule vericula:regex="^[A-Z]"/>
                  <val:rule isPresent="skipped" disabled="yes"/>
                  <val:rule isPresent="kept" disabled="no" caseSensitive="yes" normalization="nfc"/>
                </val:validation>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        ValidationRule[] rules = Read(xliff).Files[0].ValidationRules!.Rules.ToArray();

        ValidationRule[] expected =
        [
            new PresenceRule("{0}"),
            new AbsenceRule("TODO"),
            new StartsWithRule("Hei"),
            new EndsWithRule("!"),
            new LengthBudgetRule(40),
            new RegexRule("^[A-Z]"),
            new PresenceRule("skipped") with { Disabled = true },
            new PresenceRule("kept") with { Disabled = false, Normalization = TextNormalization.Nfc }
        ];
        CollectionAssert.AreEqual(expected, rules);
    }

    [TestMethod]
    public void RejectsRuleSemanticsTheLinterCannotHonour()
    {
        //ids 653/1192 (caseSensitive="no" => "asks for case-insensitive matching"), 664/1222
        //(normalization="nfkc" => "asks for 'nfkc' normalization"), 657/1197 (existsInSource="yes" =>
        //"is conditioned on the source text") and 661/1202 (occurs="2" => "constrains the number of
        //occurrences") all had their messages mutated to "". Checking a distinctive fragment of each
        //stops the wording from being erased without a test noticing.
        (string Attributes, string ExpectedFragment)[] cases =
        [
            ("isPresent=\"x\" caseSensitive=\"no\"", "asks for case-insensitive matching"),
            ("isPresent=\"x\" normalization=\"nfkc\"", "asks for 'nfkc' normalization"),
            ("isPresent=\"x\" existsInSource=\"yes\"", "is conditioned on the source text"),
            ("isPresent=\"x\" occurs=\"2\"", "constrains the number of occurrences")
        ];

        foreach((string attributes, string expectedFragment) in cases)
        {
            string xliff = Prologue + $"""
                  <file id="wallet">
                    <val:validation><val:rule {attributes}/></val:validation>
                    <unit id="A"><segment><source>Home</source></segment></unit>
                  </file>
                </xliff>
                """;

            XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff), attributes);

            Assert.Contains(expectedFragment, exception.Message, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void RejectsNfdNormalizationByName()
    {
        //XliffReader.cs:1221 ParseNormalization: the nfd-specific branch's message names the
        //requested normalization; asserting only the exception type would let a mutant blank that
        //message survive, since the catch-all branch below throws the same shape of exception for
        //any other unsupported value.
        string xliff = Prologue + """
              <file id="wallet">
                <val:validation><val:rule isPresent="x" normalization="nfd"/></val:validation>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("'nfd' normalization", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsARuleWithoutExactlyOneRuleAttribute()
    {
        //ids 706/1279 ("" and the two-attribute case => "must carry exactly one rule attribute; this
        //one carries {count}") and 688/1261 (the non-numeric maxLength => "non-numeric length budget
        //'ten'") both had their messages mutated to "". Checking each one's distinctive fragment,
        //including the reported candidate count, stops the wording from being silently erased.
        (string Attributes, string ExpectedFragment)[] cases =
        [
            ("", "carries 0"),
            ("isPresent=\"a\" isNotPresent=\"b\"", "carries 2"),
            ("vericula:maxLength=\"ten\"", "non-numeric length budget 'ten'")
        ];

        foreach((string attributes, string expectedFragment) in cases)
        {
            string xliff = Prologue + $"""
                  <file id="wallet">
                    <val:validation><val:rule {attributes}/></val:validation>
                    <unit id="A"><segment><source>Home</source></segment></unit>
                  </file>
                </xliff>
                """;

            XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff), attributes);

            Assert.Contains(expectedFragment, exception.Message, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void ARuleAttributeWithoutANamespacePrefixIsNeverTreatedAsAForeignRuleAttribute()
    {
        //ids 710/712/713, XliffReader.cs:1298: each && in turn changed to ||. An unprefixed attribute
        //like disabled (not one of the six recognized rule attributes) has ns == XNamespace.None, so
        //the leading `ns != XNamespace.None` term is false; every one of these mutants turns that false
        //into true through an ||, misreporting it as a foreign rule attribute instead of falling
        //through to the correct "carries 0 rule attributes" message.
        string xliff = Prologue + """
              <file id="wallet">
                <val:validation><val:rule disabled="yes"/></val:validation>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("must carry exactly one rule attribute", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsACustomRuleAttributeFromAnUnknownNamespaceByName()
    {
        //r1-11: XLIFF 2.1 §5.8.4.3 <rule>, Constraints: "a custom rule defined by attributes from any
        //namespace" is the fifth legal alternative to the four standard rule attributes, so a rule
        //carrying only such a foreign attribute is not malformed XLIFF. The reader cannot evaluate it,
        //but must refuse it by naming the attribute rather than misreporting it as carrying zero rule
        //attributes, which is what the old candidate-count message said.
        string xliff = Prologue + """
              <file id="wallet" xmlns:okp="urn:example:tool">
                <val:validation><val:rule okp:minWords="3"/></val:validation>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;

        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("minWords", exception.Message, StringComparison.Ordinal);
        Assert.Contains("urn:example:tool", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAnEmptyValueOnEveryTextBearingRuleAttribute()
    {
        //(b): an isNotPresent rule with an empty value would flag every target, since the empty
        //string is present in every string; the same reasoning rules out an empty isPresent,
        //startsWith or endsWith value, so all four must be refused rather than silently accepted.
        foreach(string attributes in new[] { "isPresent=\"\"", "isNotPresent=\"\"", "startsWith=\"\"", "endsWith=\"\"" })
        {
            string xliff = Prologue + $"""
                  <file id="wallet">
                    <val:validation><val:rule {attributes}/></val:validation>
                    <unit id="A"><segment><source>Home</source></segment></unit>
                  </file>
                </xliff>
                """;

            //id 723, XliffReader.cs:1317: message mutated to "" would erase which element's rule
            //carried the empty value and why it was refused.
            XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff), attributes);

            Assert.Contains("has an empty value", exception.Message, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void RejectsValidationOnGroupsAndUnits()
    {
        string onUnit = Prologue + """
              <file id="wallet"><unit id="A"><val:validation><val:rule isPresent="x"/></val:validation><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;
        string onGroup = Prologue + """
              <file id="wallet"><group id="G"><val:validation><val:rule isPresent="x"/></val:validation><unit id="A"><segment><source>Home</source></segment></unit></group></file>
            </xliff>
            """;

        XliffFormatException unitException = Assert.ThrowsExactly<XliffFormatException>(() => Read(onUnit));
        XliffFormatException groupException = Assert.ThrowsExactly<XliffFormatException>(() => Read(onGroup));

        //XliffReader.cs:1333 RejectValidation's interpolated message => "": with an empty message the
        //two throws above are indistinguishable from each other or from any other format refusal, so
        //this proves the element name that names which one was hit actually reaches the message.
        Assert.Contains("<unit>", unitException.Message, StringComparison.Ordinal);
        Assert.Contains("<group>", groupException.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ReadsScopesAndMetadataOnGroupsAndUnits()
    {
        string xliff = Prologue + """
              <file id="wallet">
                <group id="Shell">
                  <mda:metadata>
                    <mda:metaGroup category="vericula:scopes"><mda:meta type="scope">shell</mda:meta></mda:metaGroup>
                    <mda:metaGroup category="vericula:metadata"><mda:meta type="owner">growth</mda:meta><mda:meta type="tier">1</mda:meta></mda:metaGroup>
                  </mda:metadata>
                  <unit id="A">
                    <mda:metadata>
                      <mda:metaGroup category="vericula:scopes"><mda:meta type="scope">cards</mda:meta><mda:meta type="scope">home</mda:meta></mda:metaGroup>
                      <mda:metaGroup category="vericula:metadata"><mda:meta type="reviewer">ann</mda:meta></mda:metaGroup>
                      <mda:metaGroup category="other-tool:thing"><mda:meta type="x">ignored</mda:meta></mda:metaGroup>
                    </mda:metadata>
                    <segment><source>Home</source></segment>
                  </unit>
                </group>
              </file>
            </xliff>
            """;

        XliffGroup group = Read(xliff).Files[0].Groups.Single();
        XliffUnit unit = group.Units.Single();

        CollectionAssert.AreEqual(ShellOnly, group.Scopes.Select(scope => scope.Value).ToArray());
        Assert.AreEqual("growth", group.Metadata["owner"]);
        Assert.AreEqual("1", group.Metadata["tier"]);
        CollectionAssert.AreEqual(CardsAndHome, unit.Scopes.Select(scope => scope.Value).ToArray());
        Assert.AreEqual("ann", unit.Metadata["reviewer"]);
        Assert.HasCount(1, unit.Metadata);
    }

    [TestMethod]
    public void RejectsAVericulaCategoryOnTheWrongElement()
    {
        string scopesOnFile = Prologue + """
              <file id="wallet">
                <mda:metadata><mda:metaGroup category="vericula:scopes"><mda:meta type="scope">shell</mda:meta></mda:metaGroup></mda:metadata>
                <unit id="A"><segment><source>Home</source></segment></unit>
              </file>
            </xliff>
            """;
        string toneOnUnit = Prologue + """
              <file id="wallet">
                <unit id="A">
                  <mda:metadata><mda:metaGroup category="vericula:tone"><mda:meta type="version">1</mda:meta></mda:metaGroup></mda:metadata>
                  <segment><source>Home</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        //XliffReader.cs:961, message => "": the message names the disallowed category and its
        //element; without asserting on it, an emptied message would still pass.
        XliffFormatException fileException = Assert.ThrowsExactly<XliffFormatException>(() => Read(scopesOnFile));
        XliffFormatException unitException = Assert.ThrowsExactly<XliffFormatException>(() => Read(toneOnUnit));

        Assert.Contains("A <file> element carries a metadata group in the category 'vericula:scopes', which does not belong on a <file>", fileException.Message, StringComparison.Ordinal);
        Assert.Contains("A <unit> element carries a metadata group in the category 'vericula:tone', which does not belong on a <unit>", unitException.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsToneAndGlossaryMetadataCategoriesOnAGroup()
    {
        //ids 481/482, XliffReader.cs:740: ParseGroup passes allowTone:false and allowGlossary:false
        //to ParseMetadata; RejectsAVericulaCategoryOnTheWrongElement only proves this for a <file> and
        //a <unit>, so flipping either boolean on a <group> would let the category through silently
        //(XliffGroup has no slot for either, so it would simply be dropped rather than causing some
        //other visible failure) unless a group-specific case exists.
        string toneOnGroup = Prologue + """
              <file id="wallet">
                <group id="G">
                  <mda:metadata><mda:metaGroup category="vericula:tone"><mda:meta type="version">1</mda:meta></mda:metaGroup></mda:metadata>
                  <unit id="A"><segment><source>Home</source></segment></unit>
                </group>
              </file>
            </xliff>
            """;
        string glossaryOnGroup = Prologue + """
              <file id="wallet">
                <group id="G">
                  <mda:metadata><mda:metaGroup category="vericula:glossary"><mda:metaGroup category="vericula:glossaryEntry"><mda:meta type="term">wallet</mda:meta><mda:meta type="translation">lompakko</mda:meta></mda:metaGroup></mda:metaGroup></mda:metadata>
                  <unit id="A"><segment><source>Home</source></segment></unit>
                </group>
              </file>
            </xliff>
            """;

        Assert.ThrowsExactly<XliffFormatException>(() => Read(toneOnGroup));
        Assert.ThrowsExactly<XliffFormatException>(() => Read(glossaryOnGroup));
    }

    [TestMethod]
    public void RejectsAGlossaryMetadataCategoryOnAUnit()
    {
        //id 494, XliffReader.cs:769: ParseUnit passes allowTone:false and allowGlossary:false to
        //ParseMetadata. RejectsAVericulaCategoryOnTheWrongElement already proves the allowTone flip is
        //caught on a unit, but nothing proved the allowGlossary flip is too, so a vericula:glossary
        //metadata category on a <unit> could have slipped past silently.
        string xliff = Prologue + """
              <file id="wallet">
                <unit id="A">
                  <mda:metadata><mda:metaGroup category="vericula:glossary"></mda:metaGroup></mda:metadata>
                  <segment><source>Home</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));
    }

    [TestMethod]
    public void ReadsSegmentIdsStatesSubStatesAndIgnorables()
    {
        string xliff = Prologue + """
              <file id="wallet">
                <unit id="A">
                  <segment id="s1" state="translated" subState="tool:draft"><source>First.</source><target>Eka.</target></segment>
                  <ignorable id="i1"><source> </source><target> </target></ignorable>
                  <segment id="s2" state="reviewed"><source>Second.</source><target>Toka.</target></segment>
                  <segment state="final"><source>Third.</source><target>Kolmas.</target></segment>
                  <segment subState="vericula:needsTranslation"><source>Fourth.</source></segment>
                  <segment><source>Fifth.</source></segment>
                </unit>
              </file>
            </xliff>
            """;

        XliffSegment[] segments = Read(xliff).Files[0].Units[0].Segments.ToArray();

        Assert.HasCount(6, segments);
        Assert.AreEqual(new XliffSegment("s1", SegmentKind.Translatable, InlineContent.FromText("First."), InlineContent.FromText("Eka."), SegmentState.Translated, "tool:draft"), segments[0]);
        Assert.AreEqual(new XliffSegment("i1", SegmentKind.Ignorable, InlineContent.FromText(" "), InlineContent.FromText(" "), SegmentState.Initial, null), segments[1]);
        Assert.AreEqual(new XliffSegment("s2", SegmentKind.Translatable, InlineContent.FromText("Second."), InlineContent.FromText("Toka."), SegmentState.Reviewed, null), segments[2]);
        Assert.AreEqual(new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("Third."), InlineContent.FromText("Kolmas."), SegmentState.Final, null), segments[3]);
        Assert.AreEqual(new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("Fourth."), null, SegmentState.NeedsTranslation, null), segments[4]);
        Assert.AreEqual(new XliffSegment(null, SegmentKind.Translatable, InlineContent.FromText("Fifth."), null, SegmentState.Initial, null), segments[5]);
    }

    [TestMethod]
    public void RejectsAnUnknownSegmentState()
    {
        string xliff = Prologue + """
              <file id="wallet"><unit id="A"><segment state="approved"><source>Home</source></segment></unit></file>
            </xliff>
            """;

        //XliffReader.cs:890, message => "": the message names the unknown state value and its unit;
        //without asserting on it, an emptied message would still pass.
        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(xliff));

        Assert.Contains("Unit 'A' has a segment with the unknown state 'approved'", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void RejectsAUnitWithoutSegmentsAndASegmentWithoutSource()
    {
        //XliffReader.cs:849, message => "": the message names the element and unit missing a
        //<source>; without asserting on it, an emptied message would still pass.
        string noSegments = Prologue + """
              <file id="wallet"><unit id="A"><notes><note>n</note></notes></unit></file>
            </xliff>
            """;
        string noSource = Prologue + """
              <file id="wallet"><unit id="A"><segment><target>Koti</target></segment></unit></file>
            </xliff>
            """;

        Assert.ThrowsExactly<XliffFormatException>(() => Read(noSegments));
        XliffFormatException exception = Assert.ThrowsExactly<XliffFormatException>(() => Read(noSource));

        Assert.Contains("A <segment> in unit 'A' has no <source> element", exception.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void IgnorablesAlwaysParseAsInitialStateEvenWhenTheStateAttributeIsPresent()
    {
        //XliffReader.cs:858, block => {}: removing the ignorable branch's early return would let an
        //<ignorable> fall through into the translatable-segment state parser and pick up a stray
        //state attribute instead of always being Initial.
        string xliff = Prologue + """
              <file id="wallet"><unit id="A"><ignorable id="i1" state="translated"><source> </source></ignorable><segment><source>Home</source></segment></unit></file>
            </xliff>
            """;

        XliffSegment ignorable = Read(xliff).Files[0].Units[0].Segments.Single(segment => segment.Id == "i1");

        Assert.AreEqual(SegmentState.Initial, ignorable.State);
    }

    [TestMethod]
    public async Task ThrowsForNullPipeArguments()
    {
        //XliffReader.cs:72 ArgumentNullException.ThrowIfNull(input) => ; in Read(PipeReader) would let
        //a null pipe reach AsStream() and surface as a NullReferenceException instead of the documented
        //ArgumentNullException.
        Assert.ThrowsExactly<ArgumentNullException>(() => XliffReader.Read((PipeReader)null!));

        //XliffReader.cs:91 ArgumentNullException.ThrowIfNull(input) => ; in ReadAsync(PipeReader,
        //CancellationToken); the method is itself async, so a removed guard would surface through the
        //returned task rather than synchronously, hence ThrowsExactlyAsync here.
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => XliffReader.ReadAsync((PipeReader)null!, TestContext.CancellationToken));
    }

    [TestMethod]
    public void ReadFromAPipeAsksAsStreamToLeaveThePipeReaderOpen()
    {
        //XliffReader.cs:74 input.AsStream(leaveOpen: true) in Read(PipeReader) was mutated to
        //leaveOpen: false. AsStream is virtual purely so a caller can be told which value it was
        //called with, since the flag only changes Dispose-time behaviour Read's own call chain never
        //triggers otherwise (Load only `using`s the XmlReader, whose default CloseInput=false does not
        //cascade to the wrapper stream, which is itself never disposed on this path). Read(PipeReader)
        //runs synchronously to completion, so calling it already exercises the AsStream call.
        using var innerStream = new MemoryStream(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));
        PipeReader inner = PipeReader.Create(innerStream);
        var spy = new LeaveOpenRecordingPipeReader(inner);

        _ = XliffReader.Read(spy);

        Assert.AreEqual(true, spy.CapturedLeaveOpen);
    }

    [TestMethod]
    public async Task ReadAsyncFromAPipeAsksAsStreamToLeaveThePipeReaderOpen()
    {
        //XliffReader.cs:95 input.AsStream(leaveOpen: true) in ReadAsync(PipeReader, CancellationToken)
        //was mutated to leaveOpen: false; the async counterpart of
        //ReadFromAPipeAsksAsStreamToLeaveThePipeReaderOpen, same reasoning.
        using var innerStream = new MemoryStream(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff));
        PipeReader inner = PipeReader.Create(innerStream);
        var spy = new LeaveOpenRecordingPipeReader(inner);

        _ = await XliffReader.ReadAsync(spy, TestContext.CancellationToken);

        Assert.AreEqual(true, spy.CapturedLeaveOpen);
    }

    /// <summary>
    /// A <see cref="PipeReader"/> that forwards every operation to an inner reader but records the
    /// leaveOpen argument it was called with, so a test can observe that otherwise Dispose-time-only flag.
    /// </summary>
    private sealed class LeaveOpenRecordingPipeReader : PipeReader
    {
        private readonly PipeReader _inner;

        /// <summary>Creates a spy that forwards every operation to <paramref name="inner"/>.</summary>
        /// <param name="inner">The reader to delegate to.</param>
        public LeaveOpenRecordingPipeReader(PipeReader inner)
        {
            _inner = inner;
        }

        /// <summary>The leaveOpen argument most recently passed to <see cref="AsStream"/>; false until then.</summary>
        public bool CapturedLeaveOpen { get; private set; }

        /// <inheritdoc/>
        public override Stream AsStream(bool leaveOpen = false)
        {
            CapturedLeaveOpen = leaveOpen;

            return _inner.AsStream(leaveOpen);
        }

        /// <inheritdoc/>
        public override void AdvanceTo(SequencePosition consumed) => _inner.AdvanceTo(consumed);

        /// <inheritdoc/>
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => _inner.AdvanceTo(consumed, examined);

        /// <inheritdoc/>
        public override void CancelPendingRead() => _inner.CancelPendingRead();

        /// <inheritdoc/>
        public override void Complete(Exception? exception = null) => _inner.Complete(exception);

        /// <inheritdoc/>
        public override bool TryRead(out ReadResult result) => _inner.TryRead(out result);

        /// <inheritdoc/>
        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => _inner.ReadAsync(cancellationToken);
    }

    [TestMethod]
    public void ReadsThroughAPipe()
    {
        PipeReader input = PipeReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff)));

        XliffDocument document = XliffReader.Read(input);

        Assert.AreEqual("wallet", document.Files[0].Id);
        Assert.AreEqual("Koti", document.Files[0].Groups[0].Units[0].Target);
    }

    [TestMethod]
    public async Task ReadsThroughAPipeAsynchronously()
    {
        PipeReader input = PipeReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(XliffReaderTests.WalletXliff)));

        XliffDocument document = await XliffReader.ReadAsync(input, TestContext.CancellationToken);

        Assert.AreEqual("wallet", document.Files[0].Id);
    }

    [TestMethod]
    public async Task ReadAsyncFromAStalledPipeIsCancelledPromptly()
    {
        //r1-3: ReadAsync used to hang after Cancel() until the pipe delivered more bytes, because
        //XmlReader.ReadAsync() has no CancellationToken overload of its own; the fix registers
        //CancelPendingRead on the token so a stalled read is interrupted directly on the pipe. The
        //Task.WhenAny bound keeps this test from hanging forever if the fix regresses.
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(
            Encoding.UTF8.GetBytes("""<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en"><file id="f"><unit id="A"><segment><source>"""),
            TestContext.CancellationToken);

        using var cts = new CancellationTokenSource();
        Task<XliffDocument> readTask = XliffReader.ReadAsync(pipe.Reader, cts.Token);
        cts.Cancel();

        Task completed = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(5), TestContext.CancellationToken));
        Assert.AreSame(readTask, completed);
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => readTask);
    }

    [TestMethod]
    public async Task ReadUnitsAsyncFromAStalledPipeIsCancelledPromptly()
    {
        //r1-3: the streaming overload shares the same fix; proven separately since it wraps the pipe in
        //its own async iterator rather than delegating straight to ReadAsync.
        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(
            Encoding.UTF8.GetBytes("""<xliff xmlns="urn:oasis:names:tc:xliff:document:2.0" version="2.0" srcLang="en"><file id="f"><unit id="A"><segment><source>"""),
            TestContext.CancellationToken);

        using var cts = new CancellationTokenSource();

        async Task Enumerate()
        {
            await foreach(XliffUnit unit in XliffReader.ReadUnitsAsync(pipe.Reader, cts.Token))
            {
                _ = unit;
            }
        }

        Task enumerateTask = Enumerate();
        cts.Cancel();

        Task completed = await Task.WhenAny(enumerateTask, Task.Delay(TimeSpan.FromSeconds(5), TestContext.CancellationToken));
        Assert.AreSame(enumerateTask, completed);
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => enumerateTask);
    }

    public TestContext TestContext { get; set; } = null!;

    private static XliffDocument Read(string xliff)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));

        return XliffReader.Read(stream);
    }
}
