using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Shapes;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The outline maths behind the bracing: winding, point-in-outline and the inward offset.
    public class PolygonMathTests
    {
        // A counterclockwise rectangle from (0, 0) to (w, h).
        static List<Segment> Rect(double w, double h) => new List<Segment>
        {
            new LineSegment(new Point2(0, 0), new Point2(w, 0)),
            new LineSegment(new Point2(w, 0), new Point2(w, h)),
            new LineSegment(new Point2(w, h), new Point2(0, h)),
            new LineSegment(new Point2(0, h), new Point2(0, 0)),
        };

        // An L shape, counterclockwise, with the notch at the top right.
        static List<Segment> LShape()
        {
            var pts = new[]
            {
                new Point2(0, 0), new Point2(10, 0), new Point2(10, 4), new Point2(4, 4), new Point2(4, 10),
                new Point2(0, 10),
            };
            return pts.Select((p, i) => (Segment)new LineSegment(p, pts[(i + 1) % pts.Length])).ToList();
        }

        static List<Segment> Circle(double radius) => new List<Segment>
        {
            new ArcSegment(Point2.Origin, radius, 0, Math.PI),
            new ArcSegment(Point2.Origin, radius, Math.PI, Math.PI),
        };

        static void AssertLine(Segment segment, double x1, double y1, double x2, double y2)
        {
            var line = (LineSegment)segment;
            Assert.That(line.From.X, Is.EqualTo(x1).Within(1e-9));
            Assert.That(line.From.Y, Is.EqualTo(y1).Within(1e-9));
            Assert.That(line.To.X, Is.EqualTo(x2).Within(1e-9));
            Assert.That(line.To.Y, Is.EqualTo(y2).Within(1e-9));
        }

        // A segment that is neither a line nor an arc.
        sealed record OtherSegment : Segment
        {
            public override Point2 Start => Point2.Origin;
            public override Point2 End => new Point2(1, 0);
            public override double Length => 1;
            public override Vector2 TangentAt(double parameter) => Vector2.XAxis;
            public override Point2 PointAt(double parameter) => new Point2(parameter, 0);
            public override Segment Reversed() => this;
            public override Segment MirrorX() => this;
            public override Bounds Bounds => new Bounds(0, 0, 1, 0);
            public override IReadOnlyList<Crossing> CrossingsAtX(double x) => Array.Empty<Crossing>();
            public override IReadOnlyList<Crossing> CrossingsAtY(double y) => Array.Empty<Crossing>();
            public override IReadOnlyList<Crossing> CrossingsWithLine(Point2 through, Vector2 direction) =>
                Array.Empty<Crossing>();
            public override Segment Moved(Vector2 by) => this;
            public override Segment Between(double from, double to) => this;
        }

        [Test]
        public void SignedArea_IsPositiveCounterclockwiseAndNegativeClockwise()
        {
            Assert.That(PolygonMath.SignedArea(Rect(10, 4)), Is.EqualTo(80).Within(1e-9));
            var clockwise = Rect(10, 4).AsEnumerable().Reverse().Select(s => s.Reversed()).ToList();
            Assert.That(PolygonMath.SignedArea(clockwise), Is.EqualTo(-80).Within(1e-9));
        }

        [Test]
        public void SignedArea_OfNothingIsZero() =>
            Assert.That(PolygonMath.SignedArea(new List<Segment>()), Is.EqualTo(0));

        [TestCase(5, 2, true)]
        [TestCase(0.1, 0.1, true)]
        [TestCase(-1, 2, false)]
        [TestCase(11, 2, false)]
        [TestCase(5, -1, false)]
        [TestCase(5, 5, false)]
        public void Contains_Rectangle(double x, double y, bool expected) =>
            Assert.That(PolygonMath.Contains(Rect(10, 4), new Point2(x, y)), Is.EqualTo(expected));

        // Pinned: only crossings strictly to the right count, so a point on an edge or vertex is inside unless the ray
        // from it meets nothing further right (the right edge and the top right corner).
        [Test]
        public void Contains_RectangleBoundary()
        {
            var rect = Rect(10, 4);
            Assert.That(PolygonMath.Contains(rect, new Point2(0, 2)), Is.True, "left edge");
            Assert.That(PolygonMath.Contains(rect, new Point2(10, 2)), Is.False, "right edge");
            Assert.That(PolygonMath.Contains(rect, new Point2(5, 0)), Is.True, "bottom edge");
            Assert.That(PolygonMath.Contains(rect, new Point2(5, 4)), Is.True, "top edge");
        }

        [Test]
        public void Contains_RectangleVertices()
        {
            var rect = Rect(10, 4);
            Assert.That(PolygonMath.Contains(rect, new Point2(0, 0)), Is.True, "bottom left");
            Assert.That(PolygonMath.Contains(rect, new Point2(10, 0)), Is.False, "bottom right");
            Assert.That(PolygonMath.Contains(rect, new Point2(0, 4)), Is.True, "top left");
            Assert.That(PolygonMath.Contains(rect, new Point2(10, 4)), Is.False, "top right");
        }

        [Test]
        public void Contains_ConcaveShape()
        {
            var l = LShape();
            Assert.That(PolygonMath.Contains(l, new Point2(2, 7)), Is.True, "upright of the L");
            Assert.That(PolygonMath.Contains(l, new Point2(7, 2)), Is.True, "foot of the L");
            Assert.That(PolygonMath.Contains(l, new Point2(2, 2)), Is.True, "corner");
            Assert.That(PolygonMath.Contains(l, new Point2(7, 7)), Is.False, "in the notch");
            Assert.That(PolygonMath.Contains(l, new Point2(5, 5)), Is.False, "just inside the notch");
            Assert.That(PolygonMath.Contains(l, new Point2(12, 2)), Is.False, "beyond the foot");
        }

        // Pinned (see the report): a point whose ray along +X runs exactly through the notch's reflex vertex gets two
        // crossings at the vertex's two ends one after another, and is judged outside though it is inside.
        [Test]
        public void Contains_RayThroughReflexVertex()
        {
            Assert.That(PolygonMath.Contains(LShape(), new Point2(2, 4)), Is.False);
        }

        [Test]
        public void Contains_Circle()
        {
            var circle = Circle(5);
            Assert.That(PolygonMath.Contains(circle, new Point2(0, 0)), Is.True);
            Assert.That(PolygonMath.Contains(circle, new Point2(3, 3)), Is.True);
            Assert.That(PolygonMath.Contains(circle, new Point2(4, 4)), Is.False);
        }

        [Test]
        public void Contains_EmptyOutlineIsOutside() =>
            Assert.That(PolygonMath.Contains(new List<Segment>(), new Point2(1, 1)), Is.False);

        [Test]
        public void OffsetInward_ZeroReturnsTheSameSegments()
        {
            var rect = Rect(10, 4);
            var result = PolygonMath.OffsetInward(rect, clockwise: false, 0);
            Assert.That(result, Is.EqualTo(rect));
            Assert.That(result[0], Is.SameAs(rect[0]));
            Assert.That(result, Is.Not.SameAs(rect));
        }

        [Test]
        public void OffsetInward_CounterclockwiseRectangleMovesEachSideIn()
        {
            var result = PolygonMath.OffsetInward(Rect(10, 4), clockwise: false, 1);
            Assert.That(result, Has.Count.EqualTo(4));
            AssertLine(result[0], 0, 1, 10, 1);
            AssertLine(result[1], 9, 0, 9, 4);
            AssertLine(result[2], 10, 3, 0, 3);
            AssertLine(result[3], 1, 4, 1, 0);
        }

        [Test]
        public void OffsetInward_ClockwiseRectangleMovesEachSideIn()
        {
            var clockwise = Rect(10, 4).AsEnumerable().Reverse().Select(s => s.Reversed()).ToList();
            var result = PolygonMath.OffsetInward(clockwise, clockwise: true, 1);
            Assert.That(result, Has.Count.EqualTo(4));
            // Reversed order: the left side first, going up.
            AssertLine(result[0], 1, 0, 1, 4);
            AssertLine(result[1], 0, 3, 10, 3);
            AssertLine(result[2], 9, 4, 9, 0);
            AssertLine(result[3], 10, 1, 0, 1);
        }

        [Test]
        public void OffsetInward_NegativeOffsetMovesOutward()
        {
            var result = PolygonMath.OffsetInward(Rect(10, 4), clockwise: false, -1);
            AssertLine(result[0], 0, -1, 10, -1);
            AssertLine(result[1], 11, 0, 11, 4);
        }

        // Pinned: the segments are moved square to themselves and not trimmed, so they no longer meet.
        [Test]
        public void OffsetInward_DoesNotTrimOrJoinTheSides()
        {
            var result = PolygonMath.OffsetInward(Rect(10, 4), clockwise: false, 1);
            Assert.That(((LineSegment)result[0]).To, Is.Not.EqualTo(((LineSegment)result[1]).From));
        }

        [Test]
        public void OffsetInward_ConcaveShapeMovesEachSideInward()
        {
            var result = PolygonMath.OffsetInward(LShape(), clockwise: false, 1);
            Assert.That(result, Has.Count.EqualTo(6));
            AssertLine(result[0], 0, 1, 10, 1);
            AssertLine(result[1], 9, 0, 9, 4);
            // The notch's sides move into the solid part, away from the notch.
            AssertLine(result[2], 10, 3, 4, 3);
            AssertLine(result[3], 3, 4, 3, 10);
            AssertLine(result[4], 4, 9, 0, 9);
            AssertLine(result[5], 1, 10, 1, 0);
        }

        [Test]
        public void OffsetInward_ArcsChangeRadius()
        {
            var inward = PolygonMath.OffsetInward(Circle(5), clockwise: false, 1);
            Assert.That(inward.Cast<ArcSegment>().Select(a => a.Radius), Is.All.EqualTo(4).Within(1e-9));
            var outward = PolygonMath.OffsetInward(Circle(5), clockwise: false, -1);
            Assert.That(outward.Cast<ArcSegment>().Select(a => a.Radius), Is.All.EqualTo(6).Within(1e-9));
        }

        [Test]
        public void OffsetInward_ArcShrunkToNothingIsLeftOut()
        {
            Assert.That(PolygonMath.OffsetInward(Circle(5), clockwise: false, 5), Is.Empty);
            Assert.That(PolygonMath.OffsetInward(Circle(5), clockwise: false, 6), Is.Empty);
        }

        [Test]
        public void OffsetInward_EmptyOutlineGivesNothing() =>
            Assert.That(PolygonMath.OffsetInward(new List<Segment>(), clockwise: false, 1), Is.Empty);

        // Pinned: a zero-length line has no direction, so its offset is not a number.
        [Test]
        public void OffsetInward_ZeroLengthLineMovesToNaN()
        {
            var point = new List<Segment> { new LineSegment(new Point2(1, 1), new Point2(1, 1)) };
            var result = PolygonMath.OffsetInward(point, clockwise: false, 1);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(double.IsNaN(((LineSegment)result[0]).From.X), Is.True);
        }

        [Test]
        public void OffsetInward_ThrowsForASegmentThatIsNotALineOrAnArc()
        {
            var outline = new List<Segment> { new OtherSegment() };
            var e = Assert.Throws<NotSupportedException>(() => PolygonMath.OffsetInward(outline, false, 1));
            Assert.That(e.Message, Does.Contain("isn't a line or an arc"));
        }

        // Pinned: an offset of 0 returns before the segment kind is checked.
        [Test]
        public void OffsetInward_ZeroOffsetAcceptsAnySegmentKind()
        {
            var outline = new List<Segment> { new OtherSegment() };
            Assert.That(PolygonMath.OffsetInward(outline, false, 0), Has.Count.EqualTo(1));
        }
    }
}
