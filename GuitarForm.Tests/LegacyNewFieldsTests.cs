using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The plug-in's old per-design values (glossary abbreviation to invariant text) read onto a guitar.
    public class LegacyNewFieldsTests
    {
        static IEnumerable<Parameter> Parameters(Guitar g) =>
            g.Neck.Parameters.Concat(g.Neck.Fretboard.Parameters).Concat(g.Neck.Nut.Parameters)
                .Concat(g.Body.Parameters).Concat(g.Body.TopPlate.Parameters).Concat(g.Body.TopPlate.Bracing.Parameters);

        // A text every parameter of the neck, body and bracing can take.
        static string SampleText(Parameter p)
        {
            var candidates = p.OptionTexts.Count > 0 ? p.OptionTexts.Reverse().ToList() : new List<string> { "2", "1.5", "12.5" };
            foreach (string text in candidates)
                if (p.TryParse(text, LengthUnit.Millimeters, CultureInfo.InvariantCulture, out _, out _))
                    return text;
            Assert.Fail($"No sample text for {p.Definition.Abbreviation}.");
            return null;
        }

        static Dictionary<string, string> EveryTexts() =>
            Parameters(new Guitar()).ToDictionary(p => p.Definition.Abbreviation, SampleText);

        [Test]
        public void EveryParameter_IsApplied()
        {
            var texts = EveryTexts();
            Assert.That(texts.Count, Is.GreaterThan(60));

            var result = LegacyNewFields.Apply(new Guitar(), texts);

            Assert.That(result.Problems, Is.Empty);
            var empty = Parameters(new Guitar()).ToDictionary(p => p.Definition.Abbreviation);
            foreach (var p in Parameters(result.Guitar))
            {
                string abbreviation = p.Definition.Abbreviation;
                empty[abbreviation].TryParse(texts[abbreviation], LengthUnit.Millimeters, CultureInfo.InvariantCulture,
                    out var expected, out _);
                Assert.That(p.HasValue, abbreviation);
                Assert.That(p.StoredValue, Is.EqualTo(expected.StoredValue), abbreviation);
            }
        }

        [Test]
        public void MarkersIdsAndNames_AreRead()
        {
            var result = LegacyNewFields.Apply(new Guitar(), new Dictionary<string, string>
            {
                ["Markers"] = "3 5 12:2",
                ["StringSetId"] = "4",
                ["StringSetName"] = "D'Addario EJ16",
                ["FretwireId"] = "9",
                ["FretwireName"] = "Jescar 47104",
            });

            Assert.That(result.Problems, Is.Empty);
            var markers = result.Guitar.Neck.Fretboard.Markers;
            Assert.That(markers.Select(m => m.Fret.Value), Is.EqualTo(new int?[] { 3, 5, 12 }));
            Assert.That(markers.Select(m => m.DotCount.Value), Is.EqualTo(new int?[] { 1, 1, 2 }));
            Assert.That(result.StringSetId, Is.EqualTo(4));
            Assert.That(result.FretwireId, Is.EqualTo(9));
            Assert.That(result.StringSetName, Is.EqualTo("D'Addario EJ16"));
            Assert.That(result.FretwireName, Is.EqualTo("Jescar 47104"));
        }

        [Test]
        public void MarkerText_RoundTrips()
        {
            var markers = DotMarkerText.Parse("3, 5 7 12:2");
            Assert.That(DotMarkerText.Format(markers), Is.EqualTo("3 5 7 12:2"));
            Assert.That(DotMarkerText.Parse(""), Is.Empty);
            Assert.That(DotMarkerText.Parse("3 x"), Is.Null);
            Assert.That(DotMarkerText.Parse("3:2:1"), Is.Null);
        }

        [Test]
        public void UnknownAndBadTexts_BecomeProblems()
        {
            var good = EveryTexts().First();
            var result = LegacyNewFields.Apply(new Guitar(), new Dictionary<string, string>
            {
                ["ZZZZ"] = "5",
                ["NW"] = "wide",
                ["Markers"] = "3 x",
                ["StringSetId"] = "abc",
                [good.Key] = good.Value,
            });

            Assert.That(result.Problems, Has.Count.EqualTo(4));
            Assert.That(result.Problems.Any(p => p.StartsWith("ZZZZ")));
            Assert.That(result.Problems.Any(p => p.StartsWith("NW")));
            Assert.That(result.Problems.Any(p => p.StartsWith("Markers")));
            Assert.That(result.Problems.Any(p => p.StartsWith("StringSetId")));
            Assert.That(result.StringSetId, Is.Null);
            Assert.That(result.Guitar.Neck.Fretboard.Markers, Is.Empty);
        }

        [Test]
        public void DisplayNames_AreManufacturerAndProduct()
        {
            Assert.That(new StringSet
            {
                Manufacturer = new(StringSet.Definitions.Manufacturer, "Savarez"),
                Name = new(StringSet.Definitions.Name, "510"),
            }.DisplayName, Is.EqualTo("Savarez 510"));
            Assert.That(new Fretwire { ProductCode = new(Fretwire.Definitions.ProductCode, "47104") }.DisplayName,
                Is.EqualTo("47104"));
        }

        [Test]
        public void Version1Document_WithNewFields_ReadsThem()
        {
            var document = JsonNode.Parse(V1Documents.Json("Old OM", 645, 14, 495))!.AsObject();
            document["newFields"] = new JsonObject
            {
                ["Markers"] = "5 12:2",
                ["StringSetId"] = "4",
                ["StringSetName"] = "Savarez 510",
                ["FretwireName"] = "47104",
            };

            var read = GuitarDocument.Read(document.ToJsonString());

            Assert.That(read.Guitar.ScaleLength.Value, Is.EqualTo(645));
            Assert.That(read.Guitar.Neck.Fretboard.Markers, Has.Count.EqualTo(2));
            Assert.That(read.StringSet, Is.EqualTo(new GuitarDocument.ItemReference(4, "Savarez 510")));
            Assert.That(read.Fretwire, Is.EqualTo(new GuitarDocument.ItemReference(null, "47104")));
            Assert.That(read.Problems, Is.Empty);
            Assert.That(GuitarDocument.FromJson(document.ToJsonString()).Neck.Fretboard.Markers, Has.Count.EqualTo(2));
        }

        [Test]
        public void Version1Document_WithoutNewFields_HasNoReferences()
        {
            var read = GuitarDocument.Read(V1Documents.Json("Old OM", 645, 14, 495));
            Assert.That(read.StringSet, Is.Null);
            Assert.That(read.Fretwire, Is.Null);
        }
    }
}
