using GuitarForm.Geometry;
using NUnit.Framework;
using Rhino.Testing.Fixtures;

namespace GuitarForm.Tests
{
    // Checks SideViewGeometry against the Quick Test values in docs/SideView.md.
    [RhinoTestFixture]
    public class SideViewGeometryTests
    {
        const double Tolerance = 0.001;

        // The docs/SideView.md Quick Test inputs, before any of its "change to" rows.
        static readonly SideViewDimensions QuickTest = new SideViewDimensions
        {
            BodyLength = 500,
        };

        [Test]
        public void QuickTest_CenterlineAtOrigin()
        {
            var side = SideViewGeometry.Solve(QuickTest);

            Assert.That(side.Failed, Is.False);
            Assert.That(side.Messages, Is.Empty);
            Assert.That(side.ConstructionLines, Has.Count.EqualTo(1));
            var centerline = side.ConstructionLines[0];
            Assert.That(centerline.From.X, Is.EqualTo(0).Within(Tolerance));
            Assert.That(centerline.From.Y, Is.EqualTo(0).Within(Tolerance));
            Assert.That(centerline.To.X, Is.EqualTo(0).Within(Tolerance));
            Assert.That(centerline.To.Y, Is.EqualTo(500).Within(Tolerance));
        }

        [Test]
        public void DrawingOffset_MovesRight()
        {
            var side = SideViewGeometry.Solve(QuickTest with { DrawingOffset = 600 });

            Assert.That(side.Failed, Is.False);
            var centerline = side.ConstructionLines[0];
            Assert.That(centerline.From.X, Is.EqualTo(600).Within(Tolerance));
            Assert.That(centerline.From.Y, Is.EqualTo(0).Within(Tolerance));
            Assert.That(centerline.To.X, Is.EqualTo(600).Within(Tolerance));
            Assert.That(centerline.To.Y, Is.EqualTo(500).Within(Tolerance));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void NonPositiveBodyLength_Fails(double length)
        {
            var side = SideViewGeometry.Solve(QuickTest with { BodyLength = length });

            Assert.That(side.Failed, Is.True);
            Assert.That(side.Messages, Has.Count.EqualTo(1));
            Assert.That(side.Messages[0].Level, Is.EqualTo(MessageLevel.Error));
        }
    }
}
