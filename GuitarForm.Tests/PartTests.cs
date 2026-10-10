using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The guitar's parts: their parameters, changing values, checking values, and agreement with docs/Glossary.md.
    public class PartTests
    {
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        // The least a guitar needs to be valid: its required values.
        static readonly Guitar Minimal = new Guitar
        {
            Name = new(Guitar.Definitions.Name, "Dreadnought"),
            ScaleLength = new(Guitar.Definitions.ScaleLength, 645),
            NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, 14),
            Body = new Body { Length = new(Body.Definitions.Length, 508) },
        };

        static string[] Messages(Part part) =>
            part.Validate(LengthUnit.Millimeters, Culture).Select(m => m.Text).ToArray();

        // Every kind of part in the model.
        static IEnumerable<Part> AllParts() =>
            typeof(Part).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Part)))
                .Select(t => (Part)Activator.CreateInstance(t));

        static IEnumerable<ParameterDefinition> AllDefinitions() =>
            AllParts().SelectMany(part => part.Parameters).Select(p => p.Definition);

        [Test]
        public void Parameters_InDeclaredOrder()
        {
            Assert.That(new Nut().Parameters.Select(p => p.Definition.Abbreviation),
                Is.EqualTo(new[] { "NTT", "NTH", "NTR" }));
        }

        [Test]
        public void Subparts_InDeclaredOrder()
        {
            Assert.That(new Guitar().Subparts.Select(p => p.GetType()), Is.EqualTo(new[] { typeof(Neck), typeof(Body) }));
        }

        [Test]
        public void Subparts_LeaveOutReferencesNotChosen()
        {
            var neck = new Neck { Fretwire = new Fretwire() };

            Assert.That(neck.Subparts.Select(p => p.GetType()),
                Is.EqualTo(new[] { typeof(Fretboard), typeof(Nut), typeof(Headshape), typeof(Fretwire) }));
        }

        [Test]
        public void With_ReplacesParameterAndLeavesOriginal()
        {
            var nut = new Nut();
            var changed = (Nut)nut.With(nut.Height with { Value = 9.5 });

            Assert.That(changed.Height.Value, Is.EqualTo(9.5));
            Assert.That(nut.Height.HasValue, Is.False);
        }

        [Test]
        public void With_ParameterFromAnotherPart_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Nut().With(new Fretboard().Radius with { Value = 400 }));
        }

        [Test]
        public void ParameterKinds_MatchParameterTypes()
        {
            foreach (var parameter in AllParts().SelectMany(part => part.Parameters))
            {
                var kind = parameter.Definition.Kind;
                bool matches = parameter switch
                {
                    NumberParameter => kind is ParameterKind.Length or ParameterKind.Angle or ParameterKind.Number,
                    CountParameter => kind == ParameterKind.Count,
                    FlagParameter => kind == ParameterKind.Flag,
                    TextParameter => kind == ParameterKind.Text,
                    _ => kind == ParameterKind.Choice,
                };
                Assert.That(matches, Is.True, $"{parameter.Label} is a {kind} in a {parameter.GetType().Name}.");
            }
        }

        [Test]
        public void NewGuitar_ReportsRequiredValues()
        {
            Assert.That(Messages(new Guitar()), Is.EqualTo(new[]
            {
                "Instrument Name (IN) is required.",
                "Scale Length (SL) is required.",
                "Neck Join Fret (NJF) is required.",
                "Body Length (BDL) is required.",
            }));
        }

        [Test]
        public void MinimalGuitar_IsValid()
        {
            Assert.That(Messages(Minimal), Is.Empty);
        }

        [Test]
        public void NestedValuesAreChecked()
        {
            var guitar = Minimal with
            {
                Body = Minimal.Body with
                {
                    TopPlate = new TopPlate { Thickness = new(TopPlate.Definitions.Thickness, 0) },
                },
            };

            Assert.That(Messages(guitar), Is.EqualTo(new[] { "Top Plate Thickness (TPT) must be greater than 0 mm." }));
        }

        [Test]
        public void NeckJoinFretPastLastFret_IsError()
        {
            var guitar = Minimal with
            {
                Neck = new Neck { Fretboard = new Fretboard { FretCount = new(Fretboard.Definitions.FretCount, 12) } },
            };

            Assert.That(Messages(guitar),
                Is.EqualTo(new[] { "Neck Join Fret (NJF) can't be past the last fret (Fret Count (FC) is 12)." }));
        }

        [Test]
        public void FretDistances()
        {
            Assert.That(Minimal.FretDistance(0), Is.EqualTo(0).Within(0.001));
            Assert.That((Minimal with { NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, 12) }).NeckJoinDistance,
                Is.EqualTo(322.5).Within(0.001));
            Assert.That(new Guitar().FretDistance(12), Is.Null);
        }

        [Test]
        public void Markers_CheckedWithTheirPlaceInTheList()
        {
            var fretboard = new Fretboard
            {
                Markers = new[]
                {
                    new DotMarker { Fret = new(DotMarker.Definitions.Fret, 3), DotCount = new(DotMarker.Definitions.DotCount, 1) },
                    new DotMarker { Fret = new(DotMarker.Definitions.Fret, 12), DotCount = new(DotMarker.Definitions.DotCount, 3) },
                },
            };

            Assert.That(Messages(fretboard), Is.EqualTo(new[] { "Marker 2: Marker Dot Count (MKDC) must be at most 2." }));
        }

        [Test]
        public void StringCountDisagreesWithStrings_IsError()
        {
            var set = new StringSet
            {
                StringCount = new(StringSet.Definitions.StringCount, 6),
                Strings = new[] { new GuitarString(), new GuitarString() },
            };

            Assert.That(Messages(set), Is.EqualTo(new[] { "String Count (SC) is 6, but the set has 2 strings." }));
        }

        [Test]
        public void Abbreviations_AreUnique()
        {
            var repeated = AllDefinitions().GroupBy(d => d.Abbreviation).Where(g => g.Count() > 1).Select(g => g.Key);

            Assert.That(repeated, Is.Empty);
        }

        [Test]
        public void Definitions_MatchGlossary()
        {
            var model = AllDefinitions().Select(d => d.Label).OrderBy(l => l).ToArray();

            Assert.That(model, Is.EqualTo(GlossaryLabels().OrderBy(l => l).ToArray()));
        }

        // The "Name (ABBR)" of every setting in docs/Glossary.md that isn't marked planned.
        static IEnumerable<string> GlossaryLabels()
        {
            var entry = new Regex(@"^\*\*(.+ \([A-Z]+\))\*\*(\s*\*\(planned\)\*)?\s*$");
            foreach (string line in File.ReadLines(GlossaryPath()))
            {
                var match = entry.Match(line);
                if (match.Success && !match.Groups[2].Success)
                    yield return match.Groups[1].Value;
            }
        }

        static string GlossaryPath()
        {
            for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                 directory != null; directory = directory.Parent)
            {
                string path = Path.Combine(directory.FullName, "docs", "Glossary.md");
                if (File.Exists(path))
                    return path;
            }
            throw new FileNotFoundException("docs/Glossary.md wasn't found above the test directory.");
        }
    }
}
