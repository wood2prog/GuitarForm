using System;
using GuitarForm.Model;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Checks that designs survive a trip through JSON, and that documents this version can't read are rejected.
    public class DesignDocumentTests
    {
        static readonly GuitarDesign Design = new GuitarDesign
        {
            Name = "OM",
            Instrument = new Instrument { ScaleLength = 645, NeckJoinFret = 14 },
            Body = new Body
            {
                BodyLength = 495,
                UbWidth = 290,
                LbWidth = 390,
                WaistWidth = 240,
                HeelWidth = 56,
                TailDepth = 105,
                NeckDepth = 90,
            },
            Soundhole = new Soundhole { Diameter = 100, TailOffset = 330 },
        };

        [Test]
        public void RoundTrip_GivesTheSameDesign()
        {
            Assert.That(DesignDocument.FromJson(DesignDocument.ToJson(Design)), Is.EqualTo(Design));
        }

        [Test]
        public void RoundTrip_WithoutSoundhole_GivesTheSameDesign()
        {
            var design = Design with { Soundhole = null };
            Assert.That(DesignDocument.FromJson(DesignDocument.ToJson(design)), Is.EqualTo(design));
        }

        [Test]
        public void ToJson_HasSchemaVersionAndUnits()
        {
            string json = DesignDocument.ToJson(Design);
            Assert.That(json, Does.Contain("\"schemaVersion\": 1"));
            Assert.That(json, Does.Contain("\"units\": \"mm\""));
        }

        [Test]
        public void ToJson_LeavesOutValuesNotSet()
        {
            string json = DesignDocument.ToJson(Design with { Soundhole = null });
            Assert.That(json, Does.Not.Contain("ubOffset"));
            Assert.That(json, Does.Not.Contain("soundhole"));
        }

        // A hand-written document, so a change to the JSON names is caught here and not only by the round trip.
        [Test]
        public void FromJson_ReadsAWrittenDocument()
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

            var expected = new GuitarDesign
            {
                Name = "Parlour",
                Instrument = new Instrument { ScaleLength = 610, NeckJoinFret = 12 },
                Body = new Body { BodyLength = 450, LbWidth = 330, TailDepth = 95 },
                Soundhole = new Soundhole { Diameter = 85, HorizontalOffset = 0, TailOffset = 300 },
            };
            Assert.That(DesignDocument.FromJson(json), Is.EqualTo(expected));
        }

        [TestCase("not json")]
        [TestCase(@"{ ""units"": ""mm"", ""design"": {} }")]
        [TestCase(@"{ ""schemaVersion"": 2, ""units"": ""mm"", ""design"": {} }")]
        [TestCase(@"{ ""schemaVersion"": 1, ""units"": ""in"", ""design"": {} }")]
        [TestCase(@"{ ""schemaVersion"": 1, ""units"": ""mm"" }")]
        [TestCase(@"{ ""schemaVersion"": 1, ""units"": ""mm"", ""design"": { ""body"": { ""bodyLength"": 450 } } }")]
        [TestCase(@"{ ""schemaVersion"": 1, ""units"": ""mm"", ""design"": { ""instrument"": { ""scaleLength"": 610 } } }")]
        public void FromJson_RejectsDocumentsItCantRead(string json)
        {
            Assert.That(() => DesignDocument.FromJson(json), Throws.TypeOf<FormatException>());
        }
    }
}
