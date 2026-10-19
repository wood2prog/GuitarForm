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
    }
}
