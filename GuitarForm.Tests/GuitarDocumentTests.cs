using System;
using System.Linq;
using System.Text.Json.Nodes;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Guitars through JSON: the document's shape, round trips, documents this version can't read, and converting a
    // version 1 design document.
    public class GuitarDocumentTests
    {
        static Guitar FullOM()
        {
            var guitar = TestGuitars.OM();
            return guitar with
            {
                Neck = guitar.Neck with { Fretwire = TestGuitars.Fretwire with { Id = 7 }, StringSet = TestGuitars.StringSet },
            };
        }

        static JsonObject Parse(string json) => (JsonObject)JsonNode.Parse(json);

        [Test]
        public void RoundTrip_GivesTheSameValues()
        {
            var guitar = FullOM();

            TestGuitars.AssertSameValues(GuitarDocument.FromJson(GuitarDocument.ToJson(guitar)), guitar);
        }

        [Test]
        public void RoundTrip_WithoutLibraryItems()
        {
            var guitar = TestGuitars.OM();
            var read = GuitarDocument.FromJson(GuitarDocument.ToJson(guitar));

            Assert.That(read.Neck.Fretwire, Is.Null);
            Assert.That(read.Neck.StringSet, Is.Null);
        }

        [Test]
        public void Shape_ValuesByAbbreviationWithListsAndParts()
        {
            var json = Parse(GuitarDocument.ToJson(FullOM()));
            var guitar = json["guitar"];

            Assert.That((int)json["schemaVersion"], Is.EqualTo(3));
            Assert.That((string)json["units"], Is.EqualTo("mm"));
            Assert.That((string)guitar["values"]["IN"], Is.EqualTo("OM"));
            Assert.That((double)guitar["values"]["SL"], Is.EqualTo(645));
            Assert.That((double)guitar["neck"]["values"]["NTW"], Is.EqualTo(44.5));
            Assert.That((int)guitar["neck"]["markers"][2]["MKF"], Is.EqualTo(12));
            Assert.That((string)guitar["neck"]["fretwire"]["values"]["FWPC"], Is.EqualTo("47104"));
            Assert.That(guitar["neck"]["stringSet"]["strings"].AsArray(), Has.Count.EqualTo(2));
            Assert.That((string)guitar["body"]["values"]["CTT"], Is.EqualTo("Venetian"));
            Assert.That((bool)guitar["body"]["values"]["SH"], Is.True);
            Assert.That((double)guitar["body"]["bracing"]["values"]["XBA"], Is.EqualTo(49));
        }

        [Test]
        public void LibraryIdsAreNotWritten()
        {
            var read = GuitarDocument.FromJson(GuitarDocument.ToJson(FullOM()));

            Assert.That(read.Neck.Fretwire.Id, Is.Null);
        }

        [Test]
        public void MissingPartsReadAsEmpty()
        {
            var guitar = GuitarDocument.FromJson(@"{ ""schemaVersion"": 2, ""units"": ""mm"", ""guitar"": { ""values"": { ""SL"": 650 } } }");

            Assert.That(guitar.ScaleLength.Value, Is.EqualTo(650));
            Assert.That(guitar.Neck, Is.Not.Null);
            Assert.That(guitar.Body.TopPlate.Bracing, Is.Not.Null);
        }

        [TestCase("not json")]
        [TestCase("[1, 2]")]
        [TestCase(@"{ ""units"": ""mm"", ""guitar"": {} }")]
        [TestCase(@"{ ""schemaVersion"": 4, ""units"": ""mm"", ""guitar"": {} }")]
        [TestCase(@"{ ""schemaVersion"": 2, ""units"": ""in"", ""guitar"": {} }")]
        [TestCase(@"{ ""schemaVersion"": 2, ""units"": ""mm"" }")]
        [TestCase(@"{ ""schemaVersion"": 2, ""units"": ""mm"", ""guitar"": { ""values"": { ""SL"": [645] } } }")]
        [TestCase(@"{ ""schemaVersion"": 2, ""units"": ""mm"", ""guitar"": { ""body"": { ""values"": { ""CTT"": ""Spanish"" } } } }")]
        [TestCase(@"{ ""schemaVersion"": 2, ""units"": ""mm"", ""guitar"": { ""values"": { ""NJF"": 14.5 } } }")]
        [TestCase(@"{ ""schemaVersion"": 2, ""units"": ""mm"", ""guitar"": { ""neck"": { ""markers"": [3] } } }")]
        public void UnreadableDocuments_Throw(string json)
        {
            Assert.Throws<FormatException>(() => GuitarDocument.FromJson(json));
        }

        [Test]
        public void Version2DocumentWithOldProfileValues_LoadsAndDropsThem()
        {
            var guitar = GuitarDocument.FromJson(@"{ ""schemaVersion"": 2, ""units"": ""mm"", ""guitar"": { ""neck"": { ""values"": {
                ""PBRF"": 30, ""PSAF"": 70, ""PMRF"": 10, ""PCOF"": 1, ""PBRS"": 35, ""PSAS"": 70, ""PMRS"": 10, ""PCOS"": 1,
                ""PBRE"": 40, ""PSAE"": 80, ""PMRE"": 15, ""PBRH"": 40, ""PSAH"": 80, ""PMRH"": 15,
                ""PSLE"": 10, ""NKA"": 1.5 } } } }");

            Assert.That(guitar.Neck.Angle.Value, Is.EqualTo(1.5));
            Assert.That(guitar.Neck.SideLengthAtHeelEnd.Value, Is.EqualTo(10));
            Assert.That(guitar.Neck.SquarenessAtFirstFret.Value, Is.Null);
            Assert.That(guitar.Neck.StoredValues.Select(v => v.Abbreviation),
                Has.None.Matches<string>(a => a.StartsWith("PBR") || a.StartsWith("PSA") || a.StartsWith("PMR") || a.StartsWith("PCO")));
            Assert.That(GuitarDocument.ToJson(guitar), Does.Not.Contain("PBRF"));
        }

        [Test]
        public void Version3_RoundTripsTheProfileValues()
        {
            var guitar = FullOM();
            guitar = guitar with
            {
                Neck = guitar.Neck with
                {
                    ProfileAdvancedMode = new(Neck.Definitions.ProfileAdvancedMode, true),
                    SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, 2),
                    AsymmetryAtFirstFret = new(Neck.Definitions.AsymmetryAtFirstFret, -10),
                    BlendAtSelectedFret = new(Neck.Definitions.BlendAtSelectedFret, 75),
                    SquarenessAtHeelEnd = new(Neck.Definitions.SquarenessAtHeelEnd, 3.2),
                },
            };

            string json = GuitarDocument.ToJson(guitar);
            var read = GuitarDocument.FromJson(json);

            Assert.That((double)Parse(json)["guitar"]["neck"]["values"]["PSQF"], Is.EqualTo(2));
            Assert.That((bool)Parse(json)["guitar"]["neck"]["values"]["PADV"], Is.True);
            TestGuitars.AssertSameValues(read, guitar);
            Assert.That(read.Neck.AsymmetryAtFirstFret.Value, Is.EqualTo(-10));
            Assert.That(read.Neck.BlendAtSelectedFret.Value, Is.EqualTo(75));
        }

        [Test]
        public void Version4_IsRejected()
        {
            Assert.Throws<FormatException>(() =>
                GuitarDocument.FromJson(@"{ ""schemaVersion"": 4, ""units"": ""mm"", ""guitar"": {} }"));
        }

        [Test]
        public void NewerVersion_SaysSo()
        {
            var error = Assert.Throws<FormatException>(() =>
                GuitarDocument.FromJson(@"{ ""schemaVersion"": 4, ""units"": ""mm"", ""guitar"": {} }"));

            Assert.That(error.Message, Does.Contain("made by a newer GuitarForm"));
        }

        [Test]
        public void Version1Design_IsConverted()
        {
            var design = new Model.GuitarDesign
            {
                Name = "Old OM",
                Instrument = new Model.Instrument { ScaleLength = 645, NeckJoinFret = 14 },
                Body = new Model.Body { BodyLength = 495, UbWidth = 290, HeelWidth = 56, TailDepth = 105 },
                Soundhole = new Model.Soundhole { Diameter = 100, TailOffset = 330 },
            };

            var guitar = GuitarDocument.FromJson(Model.DesignDocument.ToJson(design));

            Assert.That(guitar.Name.Value, Is.EqualTo("Old OM"));
            Assert.That(guitar.ScaleLength.Value, Is.EqualTo(645));
            Assert.That(guitar.NeckJoinFret.Value, Is.EqualTo(14));
            Assert.That(guitar.Body.Length.Value, Is.EqualTo(495));
            Assert.That(guitar.Body.UpperBoutWidth.Value, Is.EqualTo(290));
            Assert.That(guitar.Body.HeelFlatWidth.Value, Is.EqualTo(56));
            Assert.That(guitar.Body.DepthAtTailEnd.Value, Is.EqualTo(105));
            Assert.That(guitar.Body.TopPlate.Soundhole.Value, Is.True);
            Assert.That(guitar.Body.TopPlate.SoundholeDiameter.Value, Is.EqualTo(100));
            Assert.That(guitar.Body.TopPlate.SoundholeOffsetFromTailEnd.Value, Is.EqualTo(330));
        }

        [Test]
        public void Version1DesignWithoutSoundhole_HasNone()
        {
            var design = new Model.GuitarDesign
            {
                Name = "Old OM",
                Instrument = new Model.Instrument { ScaleLength = 645, NeckJoinFret = 14 },
                Body = new Model.Body { BodyLength = 495 },
            };

            Assert.That(GuitarDocument.FromJson(Model.DesignDocument.ToJson(design)).Body.TopPlate.Soundhole.Value, Is.False);
        }

        [Test]
        public void EveryVersion1FieldIsConverted()
        {
            // Each field DesignDocument writes for an instrument, body or soundhole must be one FromDesign knows.
            var design = new Model.GuitarDesign
            {
                Name = "All",
                Instrument = new Model.Instrument { ScaleLength = 1, NeckJoinFret = 1 },
                Body = new Model.Body
                {
                    BodyLength = 1, UbWidth = 1, UbOffset = 1, UbRadius = 1, UbSecondaryOffset = 1, WaistRadius = 1,
                    WaistWidth = 1, WaistOffset = 1, LbWidth = 1, LbOffset = 1, LbRadius = 1, LbSecondaryOffset = 1,
                    HeelWidth = 1, TailDepth = 1, NeckDepth = 1,
                },
                Soundhole = new Model.Soundhole { Diameter = 1, HorizontalOffset = 1, TailOffset = 1 },
            };
            // neckJoinDistance is written too, but it's calculated from the scale length and neck join fret.
            var sections = Parse(Model.DesignDocument.ToJson(design))["design"].AsObject();
            int written = sections.Where(s => s.Value is JsonObject)
                .Sum(s => s.Value.AsObject().Count(field => field.Key != "neckJoinDistance"));

            var guitar = GuitarDocument.FromJson(Model.DesignDocument.ToJson(design));
            int read = guitar.StoredValues.Concat(guitar.Body.StoredValues)
                .Count(v => v.Value is double or int && Convert.ToDouble(v.Value) == 1);

            Assert.That(read, Is.EqualTo(written));
        }
    }
}
