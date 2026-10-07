using System.Collections.Generic;
using System.Linq;
using GuitarForm.Geometry;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace GuitarForm.Tests
{
    // Checks PlateGeometry against the Quick Test values in docs/Plate.md.
    [RhinoTestFixture]
    public class PlateGeometryTests
    {
        const double Tolerance = 0.001;

        // The docs/Plate.md Quick Test inputs, before any of its "change to" rows.
        static readonly PlateDimensions QuickTest = new PlateDimensions
        {
            BodyLength = 600,
            UbWidth = 280,
            UbOffset = 5,
            UbRadius = 100,
            UbSecondaryOffset = 30,
            WaistRadius = 60,
            WaistWidth = 240,
            WaistOffset = 300,
            LbWidth = 380,
            LbOffset = 10,
            LbRadius = 120,
            LbSecondaryOffset = 40,
            HeelWidth = 56,
        };

        static PlateGeometry Solve(PlateDimensions dimensions) => PlateGeometry.Solve(dimensions, Tolerance);

        static void AssertPoint(Point3d actual, double x, double y, double within = 0.05)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(within), $"X of {actual}");
            Assert.That(actual.Y, Is.EqualTo(y).Within(within), $"Y of {actual}");
        }

        static string Errors(PlateGeometry plate) =>
            string.Join(" ", plate.Messages.Where(m => m.Level == MessageLevel.Error).Select(m => m.Text));

        [Test]
        public void QuickTest_SolvesWithoutMessages()
        {
            var plate = Solve(QuickTest);

            Assert.That(plate.Failed, Is.False);
            Assert.That(plate.Messages, Is.Empty);
        }

        [Test]
        public void QuickTest_ConstructionLines()
        {
            var lines = Solve(QuickTest).ConstructionLines;

            Assert.That(lines, Has.Count.EqualTo(6));
            AssertPoint(lines[0].From, 0, 0);
            AssertPoint(lines[0].To, 0, 600);
            AssertPoint(lines[1].From, -140, 495); // upper bout
            AssertPoint(lines[1].To, 140, 495);
            AssertPoint(lines[2].From, -120, 300); // waist
            AssertPoint(lines[2].To, 120, 300);
            AssertPoint(lines[3].From, -190, 130); // lower bout
            AssertPoint(lines[3].To, 190, 130);
            AssertPoint(lines[4].From, 28, 597); // heel marks, right then left: 1% of the body length, centred on y = L
            AssertPoint(lines[4].To, 28, 603);
            AssertPoint(lines[5].From, -28, 597);
        }

        [Test]
        public void QuickTest_OutlineRadii()
        {
            var circles = Solve(QuickTest).OutlineRadii;

            // Right then left for each: upper primary, upper secondary, waist, lower primary, lower secondary.
            var expected = new (double X, double Y, double Radius)[]
            {
                (40, 495, 100), (-40, 495, 100),
                (10, 495, 130), (-10, 495, 130),
                (180, 300, 60), (-180, 300, 60),
                (70, 130, 120), (-70, 130, 120),
                (30, 130, 160), (-30, 130, 160),
            };
            Assert.That(circles, Has.Count.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                AssertPoint(circles[i].Center, expected[i].X, expected[i].Y, Tolerance);
                Assert.That(circles[i].Radius, Is.EqualTo(expected[i].Radius).Within(Tolerance));
            }
        }

        [Test]
        public void QuickTest_ShoulderArcs()
        {
            var arcs = Solve(QuickTest).OutlineArcs;

            Assert.That(arcs[0].Radius, Is.EqualTo(116.9).Within(0.05));
            AssertPoint(arcs[0].StartPoint, 28, 600, Tolerance);
            AssertPoint(arcs[0].EndPoint, 111.0, 565.4);
            AssertPoint(arcs[1].StartPoint, -28, 600, Tolerance);
            AssertPoint(arcs[1].EndPoint, -111.0, 565.4);
        }

        [Test]
        public void QuickTest_TailArc()
        {
            var tail = Solve(QuickTest).OutlineArcs.Last();

            Assert.That(tail.Radius, Is.EqualTo(370).Within(Tolerance));
            AssertPoint(tail.Center, 0, 370, Tolerance);
            AssertPoint(tail.StartPoint, -103.6, 14.8);
            AssertPoint(tail.EndPoint, 103.6, 14.8);
        }

        [Test]
        public void QuickTest_OutlineLines()
        {
            var lines = Solve(QuickTest).OutlineLines;

            // Heel flat, then the waist tangent lines: upper right, upper left, lower right, lower left.
            Assert.That(lines, Has.Count.EqualTo(5));
            AssertPoint(lines[0].From, -28, 600, Tolerance);
            AssertPoint(lines[0].To, 28, 600, Tolerance);
            AssertPoint(lines[1].From, 139.2, 481.0);
            AssertPoint(lines[1].To, 120.3, 306.5);
            AssertPoint(lines[2].From, -139.2, 481.0);
            AssertPoint(lines[3].From, 161.7, 220.8);
            AssertPoint(lines[3].To, 130.6, 265.9);
            AssertPoint(lines[4].From, -161.7, 220.8);
        }

        [Test]
        public void UpperBoutOffsetZero_LengthensHeelFlatWithoutShoulderArcs()
        {
            var plate = Solve(QuickTest with { UbOffset = 0 });

            Assert.That(plate.Failed, Is.False);
            Assert.That(plate.OutlineArcs.Any(arc => arc.StartPoint.DistanceTo(new Point3d(28, 600, 0)) < Tolerance), Is.False);
            AssertPoint(plate.OutlineLines[0].From, -40, 600, Tolerance);
            AssertPoint(plate.OutlineLines[0].To, 40, 600, Tolerance);
        }

        [Test]
        public void LowerBoutOffsetZero_StraightTail()
        {
            var plate = Solve(QuickTest with { LbOffset = 0 });

            Assert.That(plate.Failed, Is.False);
            Assert.That(plate.OutlineArcs.Any(arc => arc.Radius > 300), Is.False, "no tail arc");
            var tail = plate.OutlineLines.Last();
            AssertPoint(tail.From, -70, 0, Tolerance);
            AssertPoint(tail.To, 70, 0, Tolerance);
        }

        // Every end of every outline piece meets exactly one other piece, so the outline is one closed loop.
        [TestCase(5, 10)]
        [TestCase(0, 10)]
        [TestCase(5, 0)]
        [TestCase(0, 0)]
        public void Outline_IsClosed(double ubOffset, double lbOffset)
        {
            var plate = Solve(QuickTest with { UbOffset = ubOffset, LbOffset = lbOffset });
            Assert.That(plate.Failed, Is.False);

            var ends = new List<Point3d>();
            foreach (var arc in plate.OutlineArcs) ends.AddRange(new[] { arc.StartPoint, arc.EndPoint });
            foreach (var line in plate.OutlineLines) ends.AddRange(new[] { line.From, line.To });

            // Plain loops rather than a lambda: a lambda capturing a Point3d compiles to a hidden class with a RhinoCommon
            // field, and NUnit loads it before Rhino is set up, so no tests are found.
            foreach (var end in ends)
            {
                int meeting = 0;
                foreach (var other in ends)
                    if (other.DistanceTo(end) < Tolerance) meeting++;
                Assert.That(meeting, Is.EqualTo(2), $"pieces meeting at {end}");
            }

            Assert.That(plate.Outline, Is.Not.Null, "joined outline");
            Assert.That(plate.Outline.IsClosed, Is.True);
        }

        [Test]
        public void Outline_IsEmptyUntilTheOutlineIsComplete()
        {
            // Without the waist radius there are no waist tangent lines or waist segment, so the outline has gaps.
            var plate = Solve(QuickTest with { WaistRadius = null });

            Assert.That(plate.Failed, Is.False);
            Assert.That(plate.OutlineArcs, Is.Not.Empty);
            Assert.That(plate.Outline, Is.Null);
        }

        [Test]
        public void LowerSecondaryOffset_LimitIsWhereItTouchesTheWaist()
        {
            // docs/Plate.md says the most LbSO can be with these values is about 61.4.
            Assert.That(Solve(QuickTest with { LbSecondaryOffset = 61.4 }).Failed, Is.False);

            var plate = Solve(QuickTest with { LbSecondaryOffset = 70 });
            Assert.That(plate.Failed, Is.True);
            Assert.That(Errors(plate), Does.Contain("lower bout secondary radius overlaps the waist radius"));
        }

        [Test]
        public void Failure_OutputsNoMoreGeometryAfterTheError()
        {
            var plate = Solve(QuickTest with { LbSecondaryOffset = 70 });

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.OutlineArcs, Is.Empty);
            Assert.That(plate.OutlineLines, Is.Empty);
        }

        [TestCase(0, TestName = "BodyLength zero")]
        [TestCase(-1, TestName = "BodyLength negative")]
        public void BodyLength_MustBePositive(double length)
        {
            var plate = Solve(QuickTest with { BodyLength = length });

            Assert.That(plate.Failed, Is.True);
            Assert.That(Errors(plate), Does.Contain("Body Length must be greater than zero"));
        }

        [Test]
        public void SecondaryOffset_CantBeNegative()
        {
            var plate = Solve(QuickTest with { UbSecondaryOffset = -1 });

            Assert.That(plate.Failed, Is.True);
            Assert.That(Errors(plate), Does.Contain("Upper bout secondary offset can't be negative"));
        }

        [Test]
        public void HeelWidth_CantExceedUpperBoutCentreSpacing()
        {
            // The upper bout radius centres are 80 apart.
            var plate = Solve(QuickTest with { HeelWidth = 81 });

            Assert.That(plate.Failed, Is.True);
            Assert.That(Errors(plate), Does.Contain("less than the heel width"));
        }

        [Test]
        public void ShoulderArc_FailsWhenItWouldMeetTheCircleBelowItsCentre()
        {
            // The gap from the heel flat end to the upper bout radius centre is 40 - 28 = 12.
            Assert.That(Solve(QuickTest with { UbOffset = 12 }).Failed, Is.False);

            var plate = Solve(QuickTest with { UbOffset = 12.5 });
            Assert.That(plate.Failed, Is.True);
            Assert.That(Errors(plate), Does.Contain("shoulder arc meets the upper bout radius below its centre"));
        }

        [Test]
        public void TailArc_FailsWhenItWouldMeetTheCircleAboveItsCentre()
        {
            // The lower bout radius centres are 70 from the centerline.
            var plate = Solve(QuickTest with { LbOffset = 70.5 });

            Assert.That(plate.Failed, Is.True);
            Assert.That(Errors(plate), Does.Contain("tail arc meets the lower bout radius above its centre"));
        }

        [Test]
        public void PartialUpperBoutInputs_GiveARemarkAndNoUpperBout()
        {
            var plate = Solve(new PlateDimensions { BodyLength = 600, UbWidth = 280 });

            Assert.That(plate.Failed, Is.False);
            Assert.That(plate.Messages.Single().Level, Is.EqualTo(MessageLevel.Remark));
            Assert.That(plate.ConstructionLines, Has.Count.EqualTo(1), "centerline only");
        }
    }
}
