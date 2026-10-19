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
                Is.EqualTo(new[] { "NTT", "NTR" }));
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
            var changed = (Nut)nut.With(nut.Reveal with { Value = 1.5 });

            Assert.That(changed.Reveal.Value, Is.EqualTo(1.5));
            Assert.That(nut.Reveal.HasValue, Is.False);
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
        public void NeckThicknessFretPastLastFret_IsError()
        {
            var neck = new Neck
            {
                ThicknessFret = new(Neck.Definitions.ThicknessFret, 15),
                Fretboard = new Fretboard { FretCount = new(Fretboard.Definitions.FretCount, 14) },
            };

            Assert.That(Messages(neck),
                Is.EqualTo(new[] { "Neck Thickness Fret (NKSF) can't be past the last fret (Fret Count (FC) is 14)." }));
        }

        [Test]
        public void HeelSideLengthNotLessThanThickness_IsError()
        {
            var neck = new Neck
            {
                HeelEndThickness = new(Neck.Definitions.HeelEndThickness, 60),
                SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, 60),
                HeelSectionThickness = new(Neck.Definitions.HeelSectionThickness, 50),
                SideLengthAtHeelSection = new(Neck.Definitions.SideLengthAtHeelSection, 49),
            };

            Assert.That(Messages(neck), Is.EqualTo(new[]
            {
                "Profile Side Length at Heel End (PSLE) must be less than Heel End Thickness (HLET).",
            }));
        }

        [Test]
        public void HeelSideLengths_AreAtLeastZero()
        {
            Neck With(double end, double section) => new Neck
            {
                HeelEndThickness = new(Neck.Definitions.HeelEndThickness, 60),
                SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, end),
                HeelSectionThickness = new(Neck.Definitions.HeelSectionThickness, 50),
                SideLengthAtHeelSection = new(Neck.Definitions.SideLengthAtHeelSection, section),
            };

            Assert.That(Messages(With(0, 0)), Is.Empty);
            var negative = Messages(With(-1, -1)).ToList();
            Assert.That(negative, Has.Count.EqualTo(2));
            Assert.That(negative[0], Does.Contain("(PSLE)"));
            Assert.That(negative[1], Does.Contain("(PSLH)"));
            Assert.That(Neck.Definitions.SideLengthAtHeelEnd.Minimum, Is.EqualTo(0));
            Assert.That(Neck.Definitions.SideLengthAtHeelEnd.MinimumExclusive, Is.False);
            Assert.That(Neck.Definitions.SideLengthAtHeelSection.Minimum, Is.EqualTo(0));
            Assert.That(Neck.Definitions.SideLengthAtHeelSection.MinimumExclusive, Is.False);
        }

        [Test]
        public void ReliefFretAndMarkerPastLastFret_AreErrors()
        {
            var fretboard = new Fretboard
            {
                FretCount = new(Fretboard.Definitions.FretCount, 20),
                ReliefFret = new(Fretboard.Definitions.ReliefFret, 21),
                Markers = new[]
                {
                    new DotMarker { Fret = new(DotMarker.Definitions.Fret, 12), DotCount = new(DotMarker.Definitions.DotCount, 2) },
                    new DotMarker { Fret = new(DotMarker.Definitions.Fret, 21), DotCount = new(DotMarker.Definitions.DotCount, 1) },
                },
            };

            Assert.That(Messages(fretboard), Is.EqualTo(new[]
            {
                "Fretboard Relief Fret (FBRF) can't be past the last fret (Fret Count (FC) is 20).",
                "Marker 2: Marker Fret (MKF) can't be past the last fret (Fret Count (FC) is 20).",
            }));
        }

        [Test]
        public void MarkerFretListedTwice_IsError()
        {
            var fretboard = new Fretboard
            {
                Markers = new[] { 12, 3, 12 }.Select(fret => new DotMarker
                {
                    Fret = new(DotMarker.Definitions.Fret, fret),
                    DotCount = new(DotMarker.Definitions.DotCount, 1),
                }).ToArray(),
            };

            Assert.That(Messages(fretboard), Is.EqualTo(new[] { "Marker 3: fret 12 already has marker 1." }));
        }

        static Fretboard WithSetbacks(double bass, double treble, double nutWidth) => new Fretboard
        {
            BassStringSetback = new(Fretboard.Definitions.BassStringSetback, bass),
            TrebleStringSetback = new(Fretboard.Definitions.TrebleStringSetback, treble),
            NutWidth = new(Fretboard.Definitions.NutWidth, nutWidth),
        };

        [Test]
        public void SetbacksFillingNutWidth_IsError()
        {
            Assert.That(Messages(WithSetbacks(20, 23, 43)), Is.EqualTo(new[]
            {
                "Bass String Setback (BSS) and Treble String Setback (TSS) together must be less than Nut Width (NTW).",
            }));
        }

        static StringSet Strings(params double[] diameters) => new StringSet
        {
            Strings = diameters.Select(String).ToArray(),
        };

        // A plain string with the values every string needs.
        static GuitarString String(double diameter) => new GuitarString
        {
            Diameter = new(GuitarString.Definitions.Diameter, diameter),
            UnitWeight = new(GuitarString.Definitions.UnitWeight, 0.0004),
        };

        [Test]
        public void MinimumNutWidth_SetbacksPlusTouchingStrings()
        {
            // Centerline span: 1 + 1.5 + 2 - (1 + 2) / 2 = 3; plus setbacks 3 + 4.
            var neck = new Neck { Fretboard = WithSetbacks(3, 4, 9), StringSet = Strings(1, 1.5, 2) };

            Assert.That(neck.MinimumNutWidth, Is.EqualTo(10).Within(1e-9));
            Assert.That(Messages(neck), Is.EqualTo(new[]
            {
                "Nut Width (NTW) is too narrow for the string set: with the setbacks it needs at least 10 mm.",
            }));
        }

        [Test]
        public void NutWidthFittingStrings_IsValid()
        {
            Assert.That(Messages(new Neck { Fretboard = WithSetbacks(3, 4, 10), StringSet = Strings(1, 1.5, 2) }), Is.Empty);
        }

        [Test]
        public void MinimumNutWidth_NeedsEveryDiameter()
        {
            var set = Strings(1, 2) with { Strings = new[] { new GuitarString() } };

            Assert.That(new Neck { Fretboard = WithSetbacks(3, 4, 43), StringSet = set }.MinimumNutWidth, Is.Null);
        }

        [Test]
        public void CourseSpacings_RequiredForTwoStringCourses()
        {
            var fretboard = new Fretboard { StringsPerCourse = new(Fretboard.Definitions.StringsPerCourse, 2) };

            Assert.That(Messages(fretboard), Is.EqualTo(new[]
            {
                "Course String Spacing at Nut (CSN) is required when Strings per Course (SPC) is 2.",
                "Course String Spacing at Bridge (CSB) is required when Strings per Course (SPC) is 2.",
            }));
            Assert.That(Messages(fretboard with { StringsPerCourse = new(Fretboard.Definitions.StringsPerCourse, 1) }), Is.Empty);
        }

        [Test]
        public void NutHeight_IsFretboardThicknessPlusReveal()
        {
            var neck = new Neck
            {
                Fretboard = new Fretboard { ThicknessAtNut = new(Fretboard.Definitions.ThicknessAtNut, 6.5) },
                Nut = new Nut { Reveal = new(Nut.Definitions.Reveal, 2.5) },
            };

            Assert.That(neck.NutHeight, Is.EqualTo(9));
            Assert.That(new Neck().NutHeight, Is.Null);
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
        public void FretDistance_TwelfthIsHalfTheScale_NutIsZero_FourteenthIsEqualTempered()
        {
            var guitar = Minimal;
            double scale = 645;

            Assert.That(guitar.FretDistance(0), Is.EqualTo(0).Within(1e-9));
            Assert.That(guitar.FretDistance(12), Is.EqualTo(scale / 2).Within(1e-9));
            Assert.That(guitar.FretDistance(14), Is.EqualTo(scale * (1 - Math.Pow(2, -14 / 12.0))).Within(1e-9));
            Assert.That(guitar.NeckJoinDistance, Is.EqualTo(guitar.FretDistance(14)).Within(1e-9));
        }

        [Test]
        public void Validate_RejectsScaleLengthNotPositive_AndNeckJoinFretBelowOne()
        {
            Assert.That(Messages(Minimal with { ScaleLength = new(Guitar.Definitions.ScaleLength, 0) }),
                Is.EqualTo(new[] { "Scale Length (SL) must be greater than 0 mm." }));
            Assert.That(Messages(Minimal with { ScaleLength = new(Guitar.Definitions.ScaleLength, -5) }),
                Is.EqualTo(new[] { "Scale Length (SL) must be greater than 0 mm." }));
            Assert.That(Messages(Minimal with { NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, 0) }),
                Is.EqualTo(new[] { "Neck Join Fret (NJF) must be at least 1." }));
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
                Strings = new[] { String(0.3), String(0.4) },
            };

            Assert.That(Messages(set), Is.EqualTo(new[] { "String Count (SC) is 6, but the set has 2 strings." }));
        }

        static Fretwire Fretwire(double beadWidth, double tangThickness, double barbThickness) => new Fretwire
        {
            BeadWidth = new(Model.Parts.Fretwire.Definitions.BeadWidth, beadWidth),
            TangThickness = new(Model.Parts.Fretwire.Definitions.TangThickness, tangThickness),
            BarbThickness = new(Model.Parts.Fretwire.Definitions.BarbThickness, barbThickness),
        };

        [Test]
        public void SlotDepthPriceAndOverage_Limits()
        {
            var good = Fretwire(2.4, 0.5, 0.6);
            Assert.That(Messages(good with { SlotDepth = new(Model.Parts.Fretwire.Definitions.SlotDepth, 1.7) }), Is.Empty);
            Assert.That(Messages(good with { SlotDepth = new(Model.Parts.Fretwire.Definitions.SlotDepth, 0) }), Is.Not.Empty);
            Assert.That(Messages(good with { Price = new(Model.Parts.Fretwire.Definitions.Price, 0) }), Is.Empty);
            Assert.That(Messages(good with { Price = new(Model.Parts.Fretwire.Definitions.Price, 12.5) }), Is.Empty);
            Assert.That(Messages(good with { Price = new(Model.Parts.Fretwire.Definitions.Price, -1) }), Is.Not.Empty);
            Assert.That(good.Manufacturer.Definition.Required, Is.False);
            Assert.That(Model.Parts.Fretwire.Definitions.Price.Unit, Is.Empty);

            var board = new Fretboard();
            Assert.That(Messages(board with { SlotOverage = new(Fretboard.Definitions.SlotOverage, 0) }), Is.Empty);
            Assert.That(Messages(board with { SlotOverage = new(Fretboard.Definitions.SlotOverage, 0.4) }), Is.Empty);
            Assert.That(Messages(board with { SlotOverage = new(Fretboard.Definitions.SlotOverage, -0.1) }), Is.Not.Empty);
        }

        [Test]
        public void Fretwire_BarbsWiderThanTangNarrowerThanBead()
        {
            Assert.That(Messages(Fretwire(2.4, 0.5, 0.6)), Is.Empty);
            Assert.That(Messages(Fretwire(2.4, 0.5, 0.5)),
                Is.EqualTo(new[] { "Fretwire Barb Thickness (FWBT) must be more than Fretwire Tang Thickness (FWTT)." }));
            Assert.That(Messages(Fretwire(0.5, 0.5, 0.6)),
                Is.EqualTo(new[] { "Fretwire Tang Thickness (FWTT) must be less than Fretwire Bead Width (FWBW)." }));
        }

        [Test]
        public void String_RequiresDiameterAndUnitWeight()
        {
            Assert.That(Messages(new GuitarString()), Is.EqualTo(new[]
            {
                "String Diameter (STDI) is required.",
                "String Unit Weight (STUW) is required.",
            }));
        }

        [Test]
        public void WoundStringWithoutCoreMaterial_RequiresCoreValues()
        {
            var wound = String(1.07) with
            {
                Construction = new(GuitarString.Definitions.Construction, StringConstruction.Wound),
            };

            Assert.That(Messages(wound), Is.EqualTo(new[]
            {
                "String Core Modulus (STCE) is required when String Core Material (STCM) is Not specified.",
                "String Core Strength (STCS) is required when String Core Material (STCM) is Not specified.",
            }));
            Assert.That(Messages(wound with
            {
                CoreMaterial = new(GuitarString.Definitions.CoreMaterial, StringCoreMaterial.Steel),
            }), Is.Empty);
        }

        [Test]
        public void PlainString_IgnoresCoreSettings()
        {
            var plain = String(0.3) with { HexCore = new(GuitarString.Definitions.HexCore, true) };

            Assert.That(Messages(plain), Is.Empty);
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

        // The "Name (ABBR)" of every setting in docs/Glossary.md that isn't marked planned or calculated.
        static IEnumerable<string> GlossaryLabels()
        {
            var entry = new Regex(@"^\*\*(.+ \([A-Z]+\))\*\*(\s*\*\((planned|calculated)\)\*)?\s*$");
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
