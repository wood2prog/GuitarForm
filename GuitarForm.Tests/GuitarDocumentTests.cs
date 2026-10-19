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
        public void SlotDepthPriceAndOverage_RoundTripByAbbreviation()
        {
            var guitar = FullOM();
            guitar = guitar with
            {
                Neck = guitar.Neck with
                {
                    Fretwire = guitar.Neck.Fretwire with
                    {
                        Manufacturer = new(Fretwire.Definitions.Manufacturer),
                        SlotDepth = new(Fretwire.Definitions.SlotDepth, 1.7),
                        Price = new(Fretwire.Definitions.Price, 12.5),
                    },
                    Fretboard = guitar.Neck.Fretboard with { SlotOverage = new(Fretboard.Definitions.SlotOverage, 0.4) },
                },
            };

            string text = GuitarDocument.ToJson(guitar);
            var json = Parse(text);
            Assert.That((int)json["schemaVersion"], Is.EqualTo(3));
            Assert.That((double)json["guitar"]["neck"]["fretwire"]["values"]["FWSD"], Is.EqualTo(1.7));
            Assert.That((double)json["guitar"]["neck"]["fretwire"]["values"]["FWPR"], Is.EqualTo(12.5));
            Assert.That((double)json["guitar"]["neck"]["values"]["FBSO"], Is.EqualTo(0.4));
            Assert.That(json["guitar"]["neck"]["fretwire"]["values"]["FWM"], Is.Null);
            TestGuitars.AssertSameValues(GuitarDocument.FromJson(text), guitar);
        }

        [Test]
        public void DocumentWithoutTheNewValues_StillLoads()
        {
            var guitar = GuitarDocument.FromJson(@"{ ""schemaVersion"": 3, ""units"": ""mm"", ""guitar"": { ""values"": { ""SL"": 650 },
                ""neck"": { ""values"": { ""NTW"": 44 }, ""fretwire"": { ""values"": { ""FWPC"": ""47104"" } } } } }");

            Assert.That(guitar.Neck.Fretboard.SlotOverage.HasValue, Is.False);
            Assert.That(guitar.Neck.Fretwire.SlotDepth.HasValue, Is.False);
            Assert.That(guitar.Neck.Fretwire.Price.HasValue, Is.False);
            Assert.That(guitar.Neck.Fretwire.Manufacturer.HasValue, Is.False);
            Assert.That(guitar.Neck.Fretwire.ProductCode.Value, Is.EqualTo("47104"));
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
            var json = V1Documents.Document("Old OM", V1Documents.Instrument(645, 14),
                V1Documents.Obj(("bodyLength", 495), ("ubWidth", 290), ("heelWidth", 56), ("tailDepth", 105)),
                V1Documents.Obj(("diameter", 100), ("tailOffset", 330))).ToJsonString();

            var guitar = GuitarDocument.FromJson(json);

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
            string json = V1Documents.Json("Old OM", 645, 14, 495);

            Assert.That(GuitarDocument.FromJson(json).Body.TopPlate.Soundhole.Value, Is.False);
        }

        // The Examples/Quick Test.json file the panel imports for testing is a version 1 document.
        [Test]
        public void QuickTestExample_Reads()
        {
            string path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..",
                "Examples", "Quick Test.json");

            var guitar = GuitarDocument.FromJson(System.IO.File.ReadAllText(path));

            Assert.That(guitar.Body.Length.Value, Is.EqualTo(600));
            Assert.That(guitar.Body.TopPlate.SoundholeOffsetFromTailEnd.Value, Is.EqualTo(380));
        }

        // A hand-written document, so a change to the JSON names is caught here and not only by a round trip.
        [Test]
        public void Version1_ReadsAHandWrittenDocument()
        {
            const string json = @"{
                ""schemaVersion"": 1,
                ""units"": ""mm"",
                ""design"": {
                    ""name"": ""Parlour"",
                    ""instrument"": { ""scaleLength"": 610, ""neckJoinFret"": 12 },
                    ""body"": { ""bodyLength"": 450, ""lbWidth"": 330, ""tailDepth"": 95 },
                    ""soundhole"": { ""diameter"": 85, ""horizontalOffset"": 0, ""tailOffset"": 300 }
                }
            }";

            var guitar = GuitarDocument.FromJson(json);

            Assert.That(guitar.Name.Value, Is.EqualTo("Parlour"));
            Assert.That(guitar.ScaleLength.Value, Is.EqualTo(610));
            Assert.That(guitar.NeckJoinFret.Value, Is.EqualTo(12));
            Assert.That(guitar.Body.Length.Value, Is.EqualTo(450));
            Assert.That(guitar.Body.LowerBoutWidth.Value, Is.EqualTo(330));
            Assert.That(guitar.Body.DepthAtTailEnd.Value, Is.EqualTo(95));
            Assert.That(guitar.Body.TopPlate.SoundholeDiameter.Value, Is.EqualTo(85));
            Assert.That(guitar.Body.TopPlate.SoundholeOffsetFromCenterline.Value, Is.EqualTo(0));
            Assert.That(guitar.Body.TopPlate.SoundholeOffsetFromTailEnd.Value, Is.EqualTo(300));
            Assert.That(guitar.Body.UpperBoutWidth.HasValue, Is.False);
        }

        [TestCase(@"{ ""schemaVersion"": 1, ""units"": ""in"", ""design"": {} }")]
        [TestCase(@"{ ""schemaVersion"": 1, ""units"": ""mm"" }")]
        [TestCase(@"{ ""schemaVersion"": 1, ""units"": ""mm"", ""design"": { ""body"": { ""bodyLength"": 450 } } }")]
        [TestCase(@"{ ""schemaVersion"": 1, ""units"": ""mm"", ""design"": { ""instrument"": { ""scaleLength"": 610 } } }")]
        public void Version1_RejectsDocumentsItCantRead(string json)
        {
            Assert.That(() => GuitarDocument.FromJson(json), Throws.TypeOf<FormatException>());
        }

        [Test]
        public void EveryVersion1FieldIsConverted()
        {
            // Each field the version 1 writer wrote for an instrument, body or soundhole must be one FromDesign knows.
            var document = V1Documents.Document("All", V1Documents.Instrument(1, 1),
                V1Documents.Obj(("bodyLength", 1), ("ubWidth", 1), ("ubOffset", 1), ("ubRadius", 1),
                    ("ubSecondaryOffset", 1), ("waistRadius", 1), ("waistWidth", 1), ("waistOffset", 1),
                    ("lbWidth", 1), ("lbOffset", 1), ("lbRadius", 1), ("lbSecondaryOffset", 1), ("heelWidth", 1),
                    ("tailDepth", 1), ("neckDepth", 1)),
                V1Documents.Obj(("diameter", 1), ("horizontalOffset", 1), ("tailOffset", 1)));
            // neckJoinDistance is written too, but it's calculated from the scale length and neck join fret.
            int written = document["design"].AsObject().Where(s => s.Value is JsonObject)
                .Sum(s => s.Value.AsObject().Count(field => field.Key != "neckJoinDistance"));

            var guitar = GuitarDocument.FromJson(document.ToJsonString());
            int read = guitar.StoredValues.Concat(guitar.Body.StoredValues)
                .Count(v => v.Value is double or int && Convert.ToDouble(v.Value) == 1);

            Assert.That(read, Is.EqualTo(written));
        }

        [TestCase(null, 3, 5)]
        [TestCase(3, null, 5)]
        [TestCase(3, 5, null)]
        public void BlankMarkers_SurviveJson(int? a, int? b, int? c)
        {
            var neck = new Neck
            {
                Fretboard = new Fretboard
                {
                    Markers = new[] { a, b, c }.Select(f => f == null
                        ? new DotMarker()
                        : new DotMarker { Fret = new(DotMarker.Definitions.Fret, f.Value) }).ToArray(),
                },
            };
            var guitar = TestGuitars.OM() with { Neck = neck };

            var read = GuitarDocument.FromJson(GuitarDocument.ToJson(guitar));

            Assert.That(read.Neck.Fretboard.Markers.Select(m => m.Fret.Value).ToArray(),
                Is.EqualTo(new double?[] { a, b, c }));
        }

        [Test]
        public void BlankStrings_SurviveJson()
        {
            var set = TestGuitars.StringSet.WithStrings(new[] { new GuitarString(), new GuitarString() });
            var guitar = FullOM() with { Neck = FullOM().Neck with { StringSet = set } };

            var read = GuitarDocument.FromJson(GuitarDocument.ToJson(guitar));

            Assert.That(read.Neck.StringSet.Strings, Has.Count.EqualTo(2));
        }
}
}
