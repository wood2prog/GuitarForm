using GuitarForm.Geometry;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Checks Instrument against the Quick Test values in docs/Instrument.md.
    public class InstrumentTests
    {
        const double Tolerance = 0.001;

        // The docs/Instrument.md Quick Test inputs, before any of its "change to" rows.
        static readonly Instrument QuickTest = new Instrument
        {
            ScaleLength = 650,
            NeckJoinFret = 12,
        };

        [Test]
        public void QuickTest_IsValid()
        {
            Assert.That(QuickTest.Validate(), Is.Empty);
        }

        [Test]
        public void TwelfthFret_IsHalfTheScale()
        {
            Assert.That(QuickTest.NeckJoinDistance, Is.EqualTo(325).Within(Tolerance));
        }

        [Test]
        public void FourteenthFret_EqualTemperedDistance()
        {
            var instrument = QuickTest with { NeckJoinFret = 14 };

            Assert.That(instrument.NeckJoinDistance, Is.EqualTo(360.458).Within(Tolerance));
        }

        [Test]
        public void Nut_IsAtZero()
        {
            Assert.That(QuickTest.FretDistance(0), Is.EqualTo(0).Within(Tolerance));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void NonPositiveScaleLength_Fails(double scaleLength)
        {
            var messages = (QuickTest with { ScaleLength = scaleLength }).Validate();

            Assert.That(messages, Has.Count.EqualTo(1));
            Assert.That(messages[0].Level, Is.EqualTo(MessageLevel.Error));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NeckJoinFretBelowOne_Fails(int fret)
        {
            var messages = (QuickTest with { NeckJoinFret = fret }).Validate();

            Assert.That(messages, Has.Count.EqualTo(1));
            Assert.That(messages[0].Level, Is.EqualTo(MessageLevel.Error));
        }
    }
}
