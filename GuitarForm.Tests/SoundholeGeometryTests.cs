using GuitarForm.Geometry;
using NUnit.Framework;
using Rhino.Testing.Fixtures;

namespace GuitarForm.Tests
{
    // Checks SoundholeGeometry against the Quick Test values in docs/Soundhole.md.
    [RhinoTestFixture]
    public class SoundholeGeometryTests
    {
        const double Tolerance = 0.001;

        // The docs/Soundhole.md Quick Test inputs, before any of its "change to" rows.
        static readonly SoundholeDimensions QuickTest = new SoundholeDimensions
        {
            Diameter = 100,
            TailOffset = 420,
        };

        [Test]
        public void QuickTest_CentredOnBodyCentreline()
        {
            var soundhole = SoundholeGeometry.Solve(QuickTest);

            Assert.That(soundhole.Failed, Is.False);
            Assert.That(soundhole.Messages, Is.Empty);
            Assert.That(soundhole.Center.X, Is.EqualTo(0).Within(Tolerance));
            Assert.That(soundhole.Center.Y, Is.EqualTo(420).Within(Tolerance));
            Assert.That(soundhole.Circle.Radius, Is.EqualTo(50).Within(Tolerance));
            Assert.That(soundhole.Circle.Center.DistanceTo(soundhole.Center), Is.EqualTo(0).Within(Tolerance));
        }

        [Test]
        public void HorizontalOffset_MovesCentreAlongX()
        {
            var soundhole = SoundholeGeometry.Solve(QuickTest with { HorizontalOffset = -20 });

            Assert.That(soundhole.Failed, Is.False);
            Assert.That(soundhole.Center.X, Is.EqualTo(-20).Within(Tolerance));
            Assert.That(soundhole.Center.Y, Is.EqualTo(420).Within(Tolerance));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void NonPositiveDiameter_Fails(double diameter)
        {
            var soundhole = SoundholeGeometry.Solve(QuickTest with { Diameter = diameter });

            Assert.That(soundhole.Failed, Is.True);
            Assert.That(soundhole.Messages, Has.Count.EqualTo(1));
            Assert.That(soundhole.Messages[0].Level, Is.EqualTo(MessageLevel.Error));
        }
    }
}
