using System;
using System.Linq;
using GuitarForm.Shapes;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Lines and arcs as segments: where they cross a line (their only "intersection"; there is no segment-segment or
    // arc-arc intersection), and the edge cases of the tolerances. Values are worked out by hand in the comments.
    public class SegmentTests
    {
        const double Eps = 1e-9;
        static readonly double Quarter = Math.PI / 2;

        // (0,0) to (10,0).
        static LineSegment Horizontal() => new LineSegment(new Point2(0, 0), new Point2(10, 0));

        // The quarter circle of radius 10 about the origin from (10,0) counterclockwise to (0,10).
        static ArcSegment QuarterArc() => new ArcSegment(Point2.Origin, 10, 0, Quarter);

        // --- Line crossings ---

        [Test]
        public void Line_CrossingAtX_FindsThePointAndParameter()
        {
            var crossings = Horizontal().CrossingsAtX(4);

            Assert.That(crossings, Has.Count.EqualTo(1));
            Assert.That(crossings[0].Point, Is.EqualTo(new Point2(4, 0)));
            Assert.That(crossings[0].Parameter, Is.EqualTo(0.4).Within(Eps));
        }

        [Test]
        public void Line_CrossingAtItsEnds_CountsAndStaysWithinZeroToOne()
        {
            var line = Horizontal();
            Assert.That(line.CrossingsAtX(0).Single().Parameter, Is.EqualTo(0));
            Assert.That(line.CrossingsAtX(10).Single().Parameter, Is.EqualTo(1));
            // Just past the end, within the parameter slack (1e-9): kept, and clamped to the end.
            var past = line.CrossingsAtX(10 + 5e-9).Single();
            Assert.That(past.Parameter, Is.EqualTo(1));
            // Beyond the slack, or before the start: none.
            Assert.That(line.CrossingsAtX(10 + 1e-6), Is.Empty);
            Assert.That(line.CrossingsAtX(-1e-6), Is.Empty);
            Assert.That(line.CrossingsAtX(11), Is.Empty);
        }

        [Test]
        public void Line_ParallelToTheCrossingLine_HasNoCrossing()
        {
            // A horizontal line never crosses a horizontal line, even lying on it; a vertical one likewise at X.
            Assert.That(Horizontal().CrossingsAtY(0), Is.Empty);
            Assert.That(Horizontal().CrossingsAtY(3), Is.Empty);
            var vertical = new LineSegment(new Point2(2, 0), new Point2(2, 10));
            Assert.That(vertical.CrossingsAtX(2), Is.Empty);
            Assert.That(vertical.CrossingsAtY(5).Single().Point, Is.EqualTo(new Point2(2, 5)));
        }

        [Test]
        public void Line_CrossingWithALine_UsesAnyDirectionLength()
        {
            // Through (5,-5) going up crosses at (5,0), halfway; the direction need not be a unit vector.
            foreach (var direction in new[] { new Vector2(0, 1), new Vector2(0, 2), new Vector2(0, -3) })
            {
                var crossing = Horizontal().CrossingsWithLine(new Point2(5, -5), direction).Single();
                Assert.That(crossing.Point.X, Is.EqualTo(5).Within(Eps), $"direction y {direction.Y}");
                Assert.That(crossing.Point.Y, Is.EqualTo(0).Within(Eps), $"direction y {direction.Y}");
                Assert.That(crossing.Parameter, Is.EqualTo(0.5).Within(Eps), $"direction y {direction.Y}");
            }
            // A slanted line: through (0,-5) toward (1,1) reaches y = 0 at x = 5.
            Assert.That(Horizontal().CrossingsWithLine(new Point2(0, -5), new Vector2(1, 1)).Single().Point.X,
                Is.EqualTo(5).Within(Eps));
        }

        [Test]
        public void Line_CrossingWithALine_ToleratesNearParallel()
        {
            // Cross product of (10,0) and (1,1e-14) is 1e-13, under the 1e-12 limit: treated as parallel.
            Assert.That(Horizontal().CrossingsWithLine(new Point2(5, 0), new Vector2(1, 1e-14)), Is.Empty);
            Assert.That(Horizontal().CrossingsWithLine(new Point2(5, 1), new Vector2(1, 0)), Is.Empty);
            // 10 * 1e-3 = 0.01 is well over it: through (5,0) at a shallow angle it crosses halfway.
            var shallow = Horizontal().CrossingsWithLine(new Point2(5, 0), new Vector2(1, 1e-3)).Single();
            Assert.That(shallow.Parameter, Is.EqualTo(0.5).Within(Eps));
        }

        [Test]
        public void Line_CrossingWithALine_AtEndpointsAndBeyond()
        {
            var line = Horizontal();
            Assert.That(line.CrossingsWithLine(new Point2(10, -5), Vector2.YAxis).Single().Parameter, Is.EqualTo(1));
            Assert.That(line.CrossingsWithLine(new Point2(0, 5), Vector2.YAxis).Single().Parameter, Is.EqualTo(0));
            Assert.That(line.CrossingsWithLine(new Point2(10.001, -5), Vector2.YAxis), Is.Empty);
            Assert.That(line.CrossingsWithLine(new Point2(-0.001, -5), Vector2.YAxis), Is.Empty);
        }

        [Test]
        public void Line_ZeroLength_HasNoCrossings()
        {
            var point = new LineSegment(new Point2(3, 3), new Point2(3, 3));

            Assert.That(point.Length, Is.EqualTo(0));
            Assert.That(point.CrossingsAtX(3), Is.Empty);
            Assert.That(point.CrossingsAtY(3), Is.Empty);
            Assert.That(point.CrossingsWithLine(new Point2(3, 0), Vector2.YAxis), Is.Empty);
            Assert.That(point.Bounds, Is.EqualTo(new Bounds(3, 3, 3, 3)));
        }

        [Test]
        public void Line_BetweenReversedMovedMirrored()
        {
            var line = new LineSegment(new Point2(2, 1), new Point2(12, 1));

            Assert.That(line.Between(0.25, 0.75), Is.EqualTo(new LineSegment(new Point2(4.5, 1), new Point2(9.5, 1))));
            Assert.That(line.Reversed(), Is.EqualTo(new LineSegment(new Point2(12, 1), new Point2(2, 1))));
            Assert.That(line.Moved(new Vector2(-2, 4)), Is.EqualTo(new LineSegment(new Point2(0, 5), new Point2(10, 5))));
            Assert.That(line.MirrorX(), Is.EqualTo(new LineSegment(new Point2(-2, 1), new Point2(-12, 1))));
            Assert.That(line.TangentAt(0.3), Is.EqualTo(Vector2.XAxis));
        }

        // --- Arc crossings ---

        [Test]
        public void Arc_CrossingAtX_KeepsOnlyThePointsOnTheArc()
        {
            // At x = 0 the circle is met at (0,10) (the arc's end, parameter 1) and (0,-10) (not on the quarter).
            var crossings = QuarterArc().CrossingsAtX(0);

            Assert.That(crossings, Has.Count.EqualTo(1));
            Assert.That(crossings[0].Point.Y, Is.EqualTo(10).Within(Eps));
            Assert.That(crossings[0].Parameter, Is.EqualTo(1).Within(Eps));
        }

        [Test]
        public void Arc_CrossingAtY_AtItsStart_IsParameterZero()
        {
            // y = 0 meets the circle at (10,0), the start, and (-10,0), which is not on the quarter.
            var crossings = QuarterArc().CrossingsAtY(0);

            Assert.That(crossings, Has.Count.EqualTo(1));
            Assert.That(crossings[0].Parameter, Is.EqualTo(0).Within(Eps));
            Assert.That(crossings[0].Point.X, Is.EqualTo(10).Within(Eps));
        }

        [Test]
        public void Arc_Tangent_ReportsTheTouchPointAndNothingBeyond()
        {
            // x = 10 only touches the circle at (10,0); the point may be reported twice but is the same point.
            var touch = QuarterArc().CrossingsAtX(10);
            Assert.That(touch.Select(c => c.Point).Distinct().Count(), Is.EqualTo(1));
            Assert.That(touch.All(c => Math.Abs(c.Parameter) < Eps), Is.True);
            // A line through (10,-5) going up is tangent there too.
            var tangent = QuarterArc().CrossingsWithLine(new Point2(10, -5), Vector2.YAxis);
            Assert.That(tangent.Select(c => c.Point.Y).Distinct().Count(), Is.EqualTo(1));
            Assert.That(tangent[0].Point.X, Is.EqualTo(10).Within(Eps));
            // Just outside the circle: nothing.
            Assert.That(QuarterArc().CrossingsAtX(10.0000001), Is.Empty);
            Assert.That(QuarterArc().CrossingsAtY(-10.0000001), Is.Empty);
            Assert.That(QuarterArc().CrossingsWithLine(new Point2(11, 0), Vector2.YAxis), Is.Empty);
        }

        [Test]
        public void Arc_CrossingWithALine_ThroughTheCenter()
        {
            // The diagonal meets the circle at 45 degrees (on the arc, halfway) and 225 degrees (not on it).
            var crossings = QuarterArc().CrossingsWithLine(Point2.Origin, new Vector2(3, 3));

            Assert.That(crossings, Has.Count.EqualTo(1));
            Assert.That(crossings[0].Parameter, Is.EqualTo(0.5).Within(Eps));
            Assert.That(crossings[0].Point.X, Is.EqualTo(Math.Sqrt(50)).Within(Eps));
        }

        [Test]
        public void Arc_ClockwiseCrossings_ParameterRunsFromTheStart()
        {
            // From (0,10) clockwise to (10,0). y = 5 meets the circle at 30 degrees: 60 degrees from the start of a
            // 90 degree sweep, parameter 2/3. The other point, (-8.66,5), is off the arc.
            var arc = new ArcSegment(Point2.Origin, 10, Quarter, -Quarter);

            var crossings = arc.CrossingsAtY(5);

            Assert.That(crossings, Has.Count.EqualTo(1));
            Assert.That(crossings[0].Parameter, Is.EqualTo(2.0 / 3).Within(Eps));
            Assert.That(crossings[0].Point.X, Is.EqualTo(Math.Sqrt(75)).Within(Eps));
        }

        [Test]
        public void Arc_AcrossTheXAxis_TreatsAnglesJustBeforeTheStartAsOffTheArc()
        {
            // From -30 to +30 degrees about the origin (through angle 0).
            var arc = new ArcSegment(Point2.Origin, 10, -Math.PI / 6, Math.PI / 3);

            Assert.That(arc.CrossingsAtY(0).Single().Parameter, Is.EqualTo(0.5).Within(Eps));
            // Exactly at the start: the rounding of the angle must not drop it.
            Assert.That(arc.CrossingsAtY(10 * Math.Sin(-Math.PI / 6)).Single().Parameter, Is.EqualTo(0).Within(Eps));
            // A hair before the start (0.01 radian) and after the end are off the arc.
            Assert.That(arc.CrossingsAtY(10 * Math.Sin(-Math.PI / 6 - 0.01)), Is.Empty);
            Assert.That(arc.CrossingsAtY(10 * Math.Sin(Math.PI / 6 + 0.01)), Is.Empty);
        }

        [Test]
        public void Arc_TwoCrossingsComeBackInOrderAlongTheArc()
        {
            // The lower semicircle from (-10,0) counterclockwise to (10,0): x = 6 meets it once, at (6,-8).
            var lower = new ArcSegment(Point2.Origin, 10, Math.PI, Math.PI);
            Assert.That(lower.CrossingsAtX(6).Single().Point.Y, Is.EqualTo(-8).Within(Eps));
            // An arc from 0 to 330 degrees is crossed twice by x = 6: at (6,8) then (6,-8).
            var big = new ArcSegment(Point2.Origin, 10, 0, 11 * Math.PI / 6);
            var crossings = big.CrossingsAtX(6);
            Assert.That(crossings.Select(c => c.Point.Y), Is.EqualTo(new[] { 8.0, -8.0 }).Within(Eps));
            Assert.That(crossings[0].Parameter, Is.LessThan(crossings[1].Parameter));
        }

        // --- Arc construction and shape ---

        [Test]
        public void Around_TolerancesAndDegenerateInputs()
        {
            var circle = new Circle(Point2.Origin, 10);
            var start = new Point2(10, 0);

            // Tolerance 0.01 on radius 10 is 0.001 radian: 0.0005 is too short, 0.002 is drawn.
            Assert.That(ArcSegment.Around(circle, start, new Point2(10 * Math.Cos(0.0005), 10 * Math.Sin(0.0005)), false, 0.01), Is.Null);
            var tiny = ArcSegment.Around(circle, start, new Point2(10 * Math.Cos(0.002), 10 * Math.Sin(0.002)), false, 0.01);
            Assert.That(tiny.Sweep, Is.EqualTo(0.002).Within(1e-12));
            // The same point (either way) and a nearly full turn are nothing to draw.
            Assert.That(ArcSegment.Around(circle, start, start, false, 0.01), Is.Null);
            Assert.That(ArcSegment.Around(circle, start, start, true, 0.01), Is.Null);
            Assert.That(ArcSegment.Around(circle, start, new Point2(10 * Math.Cos(-0.0005), 10 * Math.Sin(-0.0005)), false, 0.01), Is.Null);
            // A zero radius has no arc.
            Assert.That(ArcSegment.Around(new Circle(Point2.Origin, 0), start, new Point2(0, 10), false, 0.01), Is.Null);
        }

        [Test]
        public void Around_ASemicircleEitherWay()
        {
            var circle = new Circle(Point2.Origin, 10);

            var counterclockwise = ArcSegment.Around(circle, new Point2(10, 0), new Point2(-10, 0), false, 0.001);
            var clockwise = ArcSegment.Around(circle, new Point2(10, 0), new Point2(-10, 0), true, 0.001);

            Assert.That(counterclockwise.Sweep, Is.EqualTo(Math.PI).Within(1e-12));
            Assert.That(clockwise.Sweep, Is.EqualTo(-Math.PI).Within(1e-12));
            Assert.That(counterclockwise.Mid.Y, Is.EqualTo(10).Within(1e-9));
            Assert.That(clockwise.Mid.Y, Is.EqualTo(-10).Within(1e-9));
            Assert.That(counterclockwise.Length, Is.EqualTo(10 * Math.PI).Within(1e-9));
        }

        [Test]
        public void Arc_TangentsFollowTheDirectionOfTravel()
        {
            Assert.That(QuarterArc().TangentAt(0).Y, Is.EqualTo(1).Within(Eps));
            Assert.That(QuarterArc().TangentAt(0).X, Is.EqualTo(0).Within(Eps));
            var clockwise = new ArcSegment(Point2.Origin, 10, Quarter, -Quarter);
            Assert.That(clockwise.TangentAt(0).X, Is.EqualTo(1).Within(Eps));
            Assert.That(clockwise.TangentAt(0).Y, Is.EqualTo(0).Within(Eps));
        }

        [Test]
        public void Arc_BetweenReversedMovedAndMirrored()
        {
            var arc = QuarterArc();

            var second = (ArcSegment)arc.Between(0.5, 1);
            Assert.That(second.StartAngle, Is.EqualTo(Quarter / 2).Within(Eps));
            Assert.That(second.Sweep, Is.EqualTo(Quarter / 2).Within(Eps));
            Assert.That(second.End.Y, Is.EqualTo(10).Within(Eps));

            var reversed = (ArcSegment)arc.Reversed();
            Assert.That(reversed.Start.Y, Is.EqualTo(10).Within(Eps));
            Assert.That(reversed.End.X, Is.EqualTo(10).Within(Eps));
            Assert.That(reversed.IsClockwise, Is.True);

            var moved = (ArcSegment)arc.Moved(new Vector2(5, -5));
            Assert.That(moved.Center, Is.EqualTo(new Point2(5, -5)));
            Assert.That(moved.Start.X, Is.EqualTo(15).Within(Eps));
            Assert.That(moved.Start.Y, Is.EqualTo(-5).Within(Eps));

            var mirrored = (ArcSegment)arc.MirrorX();
            Assert.That(mirrored.Start.X, Is.EqualTo(-10).Within(Eps));
            Assert.That(mirrored.End.Y, Is.EqualTo(10).Within(Eps));
        }

        [Test]
        public void Arc_BoundsOfAnArcAcrossTheXAxis()
        {
            // -45 to +45 degrees: the rightmost point (10,0) is passed; the leftmost x is the ends' (7.07).
            var bounds = new ArcSegment(Point2.Origin, 10, -Math.PI / 4, Quarter).Bounds;

            Assert.That(bounds.MaxX, Is.EqualTo(10).Within(Eps));
            Assert.That(bounds.MinX, Is.EqualTo(Math.Sqrt(50)).Within(Eps));
            Assert.That(bounds.MinY, Is.EqualTo(-Math.Sqrt(50)).Within(Eps));
            Assert.That(bounds.MaxY, Is.EqualTo(Math.Sqrt(50)).Within(Eps));
        }

        [Test]
        public void Bounds_UnionCoversBoth()
        {
            Assert.That(new Bounds(0, 0, 5, 5).Union(new Bounds(-1, 2, 3, 9)), Is.EqualTo(new Bounds(-1, 0, 5, 9)));
            Assert.That(Bounds.Of(new[] { new Point2(1, 2), new Point2(-3, 4) }), Is.EqualTo(new Bounds(-3, 2, 1, 4)));
        }
    }
}
