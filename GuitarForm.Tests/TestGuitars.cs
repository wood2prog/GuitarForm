using System.Linq;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Guitars, fretwire and string sets for the saving and loading tests, with every kind of value set somewhere.
    static class TestGuitars
    {
        public static readonly Fretwire Fretwire = new Fretwire
        {
            Manufacturer = new(Fretwire.Definitions.Manufacturer, "Jescar"),
            ProductCode = new(Fretwire.Definitions.ProductCode, "47104"),
            BeadHeight = new(Fretwire.Definitions.BeadHeight, 1.19),
            BeadWidth = new(Fretwire.Definitions.BeadWidth, 2.79),
            TangThickness = new(Fretwire.Definitions.TangThickness, 0.53),
        };

        public static readonly StringSet StringSet = new StringSet
        {
            Manufacturer = new(StringSet.Definitions.Manufacturer, "D'Addario"),
            Name = new(StringSet.Definitions.Name, "EJ16"),
            StringCount = new(StringSet.Definitions.StringCount, 2),
            Strings = new[]
            {
                new GuitarString
                {
                    Description = new(GuitarString.Definitions.Description, "E4"),
                    Diameter = new(GuitarString.Definitions.Diameter, 0.305),
                    UnitWeight = new(GuitarString.Definitions.UnitWeight, 0.000584),
                    UsedInSaddleCompensation = new(GuitarString.Definitions.UsedInSaddleCompensation, true),
                },
                new GuitarString
                {
                    Description = new(GuitarString.Definitions.Description, "E2"),
                    Diameter = new(GuitarString.Definitions.Diameter, 1.346),
                    Construction = new(GuitarString.Definitions.Construction, StringConstruction.Wound),
                    UnitWeight = new(GuitarString.Definitions.UnitWeight, 0.00983),
                    CoreMaterial = new(GuitarString.Definitions.CoreMaterial, StringCoreMaterial.Steel),
                },
            },
        };

        public static Guitar OM(string name = "OM") => new Guitar
        {
            Name = new(Guitar.Definitions.Name, name),
            Designer = new(Guitar.Definitions.Designer, "Jesse"),
            ScaleLength = new(Guitar.Definitions.ScaleLength, 645),
            NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, 14),
            Neck = new Neck
            {
                Angle = new(Neck.Definitions.Angle, 1.5),
                HeelCap = new(Neck.Definitions.HeelCap, true),
                ThicknessFret = new(Neck.Definitions.ThicknessFret, 9),
                Fretboard = new Fretboard
                {
                    FretCount = new(Fretboard.Definitions.FretCount, 20),
                    NutWidth = new(Fretboard.Definitions.NutWidth, 44.5),
                    Markers = new[] { 3, 5, 12 }.Select(fret => new DotMarker
                    {
                        Fret = new(DotMarker.Definitions.Fret, fret),
                        DotCount = new(DotMarker.Definitions.DotCount, fret == 12 ? 2 : 1),
                    }).ToArray(),
                },
                Nut = new Nut { Reveal = new(Nut.Definitions.Reveal, 2.5) },
            },
            Body = new Body
            {
                Length = new(Body.Definitions.Length, 495),
                UpperBoutWidth = new(Body.Definitions.UpperBoutWidth, 290),
                CutawayType = new(Body.Definitions.CutawayType, CutawayType.Venetian),
                TopPlate = new TopPlate
                {
                    Thickness = new(TopPlate.Definitions.Thickness, 2.8),
                    Soundhole = new(TopPlate.Definitions.Soundhole, true),
                    SoundholeOffsetFromCenterline = new(TopPlate.Definitions.SoundholeOffsetFromCenterline, -3),
                    Bracing = new Bracing
                    {
                        XBraceAngle = new(Bracing.Definitions.XBraceAngle, 49),
                        XBraceProfileType = new(Bracing.Definitions.XBraceProfileType, BraceProfileType.Tapered),
                        ToneBarCount = new(Bracing.Definitions.ToneBarCount, 2),
                    },
                },
                BackPlate = new BackPlate { ArchingType = new(BackPlate.Definitions.ArchingType, ArchingType.Spherical) },
            },
        };

        // Checks that two guitars hold the same values, including their sub-assemblies and library items.
        public static void AssertSameValues(Guitar actual, Guitar expected)
        {
            AssertSameValues((Part)actual, expected);
            AssertSameValues(actual.Neck, expected.Neck);
            AssertSameValues(actual.Body, expected.Body);
            AssertSameValues(actual.Body.TopPlate.Bracing, expected.Body.TopPlate.Bracing);
            AssertSameValues(actual.Neck.Fretwire, expected.Neck.Fretwire);
            AssertSameValues(actual.Neck.StringSet, expected.Neck.StringSet);
        }

        public static void AssertSameValues(Part actual, Part expected)
        {
            if (expected == null)
            {
                Assert.That(actual, Is.Null);
                return;
            }
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.StoredValues, Is.EquivalentTo(expected.StoredValues), actual.GetType().Name);
        }
    }
}
