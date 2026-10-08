using GuitarForm.Geometry;
using GuitarForm.Model;
using NUnit.Framework;
using Rhino.Geometry;
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
            TailDepth = 120,
            NeckDepth = 95,
        };

        static void AssertLine(Line line, double fromX, double fromY, double toX, double toY)
        {
            Assert.That(line.From.X, Is.EqualTo(fromX).Within(Tolerance));
            Assert.That(line.From.Y, Is.EqualTo(fromY).Within(Tolerance));
            Assert.That(line.To.X, Is.EqualTo(toX).Within(Tolerance));
            Assert.That(line.To.Y, Is.EqualTo(toY).Within(Tolerance));
        }

        [Test]
        public void QuickTest_OutlineAtOrigin()
        {
            var side = SideViewGeometry.Solve(QuickTest);

            Assert.That(side.Failed, Is.False);
            Assert.That(side.Messages, Is.Empty);
            Assert.That(side.OutlineLines, Has.Count.EqualTo(4));
            AssertLine(side.OutlineLines[0], 0, 0, 120, 0);
            AssertLine(side.OutlineLines[1], 0, 500, 95, 500);
            AssertLine(side.OutlineLines[2], 0, 0, 0, 500);
            AssertLine(side.OutlineLines[3], 120, 0, 95, 500);
        }

        [Test]
        public void DrawingOffset_MovesRight()
        {
            var side = SideViewGeometry.Solve(QuickTest with { DrawingOffset = 600 });

            Assert.That(side.Failed, Is.False);
            AssertLine(side.OutlineLines[0], 600, 0, 720, 0);
            AssertLine(side.OutlineLines[1], 600, 500, 695, 500);
            AssertLine(side.OutlineLines[2], 600, 0, 600, 500);
            AssertLine(side.OutlineLines[3], 720, 0, 695, 500);
        }

        [TestCase(0, 120, 95)]
        [TestCase(-10, 120, 95)]
        [TestCase(500, 0, 95)]
        [TestCase(500, 120, -5)]
        public void NonPositiveDimension_Fails(double length, double tailDepth, double neckDepth)
        {
            var side = SideViewGeometry.Solve(QuickTest with { BodyLength = length, TailDepth = tailDepth, NeckDepth = neckDepth });

            Assert.That(side.Failed, Is.True);
            Assert.That(side.Messages, Has.Count.EqualTo(1));
            Assert.That(side.Messages[0].Level, Is.EqualTo(MessageLevel.Error));
        }
    }
}
