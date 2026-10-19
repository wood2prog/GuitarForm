using System;
using System.Linq;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Reading and setting a part's values by abbreviation, as they're saved.
    public class PartValuesTests
    {
        [Test]
        public void StoredValues_LeaveOutSeparatelySavedParts()
        {
            var abbreviations = TestGuitars.OM().StoredValues.Select(v => v.Abbreviation).ToArray();

            Assert.That(abbreviations, Is.EquivalentTo(new[] { "IN", "DSG", "SL", "NJF" }));
        }

        [Test]
        public void StoredValues_IncludePartsSavedWithIt()
        {
            var body = TestGuitars.OM().Body;

            Assert.That(body.StoredValues, Has.Member(new PartValue("TPT", null, 2.8)));
            Assert.That(body.StoredValues, Has.Member(new PartValue("BKAT", null, "Spherical")));
            Assert.That(body.StoredValues.Select(v => v.Abbreviation), Has.None.EqualTo("XBA"));
        }

        [Test]
        public void StoredValues_NumberListItems()
        {
            var markers = TestGuitars.OM().Neck.StoredValues.Where(v => v.Abbreviation == "MKF");

            Assert.That(markers, Is.EqualTo(new[]
            {
                new PartValue("MKF", 0, 3), new PartValue("MKF", 1, 5), new PartValue("MKF", 2, 12),
            }));
        }

        [Test]
        public void SeparatelySavedParts()
        {
            var guitar = TestGuitars.OM();

            Assert.That(guitar.SeparatelySavedParts, Is.EqualTo(new Part[] { guitar.Neck, guitar.Body }));
            Assert.That(guitar.Body.SeparatelySavedParts, Is.EqualTo(new Part[] { guitar.Body.TopPlate.Bracing }));
        }

        [Test]
        public void RoundTrip_EveryAssembly()
        {
            var guitar = TestGuitars.OM();
            Part[] parts = { guitar, guitar.Neck, guitar.Body, guitar.Body.TopPlate.Bracing, TestGuitars.StringSet };

            foreach (var part in parts)
            {
                var empty = (Part)Activator.CreateInstance(part.GetType());
                TestGuitars.AssertSameValues(empty.WithStoredValues(part.StoredValues), part);
            }
        }

        [Test]
        public void WholeNumbersReadAsLong_AreAccepted()
        {
            // SQLite reads integers as long, and a real with no fraction may come back as one too.
            var neck = (Neck)new Neck().WithStoredValues(new[]
            {
                new PartValue("NKSF", null, 9L),
                new PartValue("NKA", null, 2L),
                new PartValue("HCP", null, 1L),
            });

            Assert.That(neck.ThicknessFret.Value, Is.EqualTo(9));
            Assert.That(neck.Angle.Value, Is.EqualTo(2.0));
            Assert.That(neck.HeelCap.Value, Is.True);
        }

        [Test]
        public void UnknownAbbreviation_IsIgnored()
        {
            var nut = (Nut)new Nut().WithStoredValues(new[] { new PartValue("ZZZ", null, 1.0), new PartValue("NTR", null, 2.0) });

            Assert.That(nut.Reveal.Value, Is.EqualTo(2.0));
        }

        [Test]
        public void UnknownChoice_Throws()
        {
            Assert.Throws<FormatException>(() => new Body().WithStoredValues(new[] { new PartValue("CTT", null, "Spanish") }));
        }

        [Test]
        public void NumericChoiceName_Throws()
        {
            Assert.Throws<FormatException>(() => new Body().WithStoredValues(new[] { new PartValue("CTT", null, "1") }));
        }

        [Test]
        public void CountTooLargeForAnInt_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => new Guitar().WithStoredValues(new[] { new PartValue("NJF", null, 99999999999L) }));
            Assert.Throws<FormatException>(() => new Guitar().WithStoredValues(new[] { new PartValue("NJF", null, 1e20) }));
        }

        [Test]
        public void WronglyTypedValues_Throw()
        {
            Assert.Throws<FormatException>(() => new Guitar().WithStoredValues(new[] { new PartValue("SL", null, true) }));
            Assert.Throws<FormatException>(() => new Guitar().WithStoredValues(new[] { new PartValue("SL", null, "645") }));
            Assert.Throws<FormatException>(() => new Guitar().WithStoredValues(new[] { new PartValue("NJF", null, "12") }));
            Assert.Throws<FormatException>(() => new Guitar().WithStoredValues(new[] { new PartValue("IN", null, 12L) }));
        }

        [Test]
        public void NonFiniteNumbers_Throw()
        {
            Assert.Throws<FormatException>(() => new Guitar().WithStoredValues(new[] { new PartValue("SL", null, double.NaN) }));
            Assert.Throws<FormatException>(() => new Guitar().WithStoredValues(new[] { new PartValue("SL", null, double.PositiveInfinity) }));
        }
            static DotMarker Marker(int? fret) => fret == null
            ? new DotMarker()
            : new DotMarker { Fret = new(DotMarker.Definitions.Fret, fret.Value) };

        static Neck NeckWithMarkers(params int?[] frets) =>
            new Neck { Fretboard = new Fretboard { Markers = frets.Select(Marker).ToArray() } };

        [TestCase(null, 3, 5)]
        [TestCase(3, null, 5)]
        [TestCase(3, 5, null)]
        [TestCase(null, null, null)]
        public void BlankMarkers_SurviveStoredValues(int? a, int? b, int? c)
        {
            var neck = NeckWithMarkers(a, b, c);

            var read = (Neck)new Neck().WithStoredValues(neck.StoredValues);

            Assert.That(read.Fretboard.Markers.Select(m => m.Fret.Value).ToArray(),
                Is.EqualTo(new double?[] { a, b, c }));
        }

        [Test]
        public void BlankMarker_WritesAMarkerValue()
        {
            var values = NeckWithMarkers(3, null).StoredValues.Where(v => v.Abbreviation == PartValue.ListItemKey("Markers"));

            Assert.That(values, Is.EqualTo(new[] { new PartValue("#Markers", 0, 1), new PartValue("#Markers", 1, 1) }));
        }

        [Test]
        public void BlankStrings_SurviveStoredValues()
        {
            var set = TestGuitars.StringSet.WithStrings(new[]
            {
                new GuitarString(), new GuitarString { Description = new(GuitarString.Definitions.Description, "B") },
                new GuitarString(),
            });

            var read = (StringSet)new StringSet().WithStoredValues(set.StoredValues);

            Assert.That(read.Strings, Has.Count.EqualTo(3));
            Assert.That(read.Strings[1].Description.Value, Is.EqualTo("B"));
        }

        [Test]
        public void BadValue_IsSkippedWithAMessage()
        {
            var problems = new System.Collections.Generic.List<string>();

            var guitar = (Guitar)new Guitar().WithStoredValues(new[]
            {
                new PartValue("SL", null, "645"), new PartValue("NJF", null, 12),
            }, problems);

            Assert.That(guitar.ScaleLength.HasValue, Is.False);
            Assert.That(guitar.NeckJoinFret.Value, Is.EqualTo(12));
            Assert.That(problems, Has.Count.EqualTo(1));
            Assert.That(problems[0], Does.Contain("SL"));
        }

        [Test]
        public void BadValue_InAListItem_IsSkippedWithAMessage()
        {
            var problems = new System.Collections.Generic.List<string>();

            var neck = (Neck)new Neck().WithStoredValues(new[]
            {
                new PartValue("MKF", 0, "x"), new PartValue("MKF", 1, 5),
            }, problems);

            Assert.That(neck.Fretboard.Markers, Has.Count.EqualTo(2));
            Assert.That(neck.Fretboard.Markers[0].Fret.HasValue, Is.False);
            Assert.That(neck.Fretboard.Markers[1].Fret.Value, Is.EqualTo(5));
            Assert.That(problems, Has.Count.EqualTo(1));
        }

        [Test]
        public void FindParameter_ReachesPartsSavedWithIt()
        {
            var guitar = TestGuitars.OM();

            Assert.That(guitar.FindParameter("SL"), Is.SameAs(guitar.ScaleLength));
            Assert.That(guitar.Neck.FindParameter("FC"), Is.Not.Null);
            Assert.That(guitar.Neck.FindParameter("NTR"), Is.Not.Null);
            Assert.That(guitar.Body.FindParameter("TPT"), Is.SameAs(guitar.Body.TopPlate.Thickness));
            Assert.That(guitar.Body.FindParameter("BKAT"), Is.SameAs(guitar.Body.BackPlate.ArchingType));
            Assert.That(guitar.FindParameter("XXX"), Is.Null);
            Assert.That(guitar.Neck.FindParameter("SL"), Is.Null);
        }
}
}
