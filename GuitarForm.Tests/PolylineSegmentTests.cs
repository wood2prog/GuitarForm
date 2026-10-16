using System.Linq;
using GuitarForm.Shapes;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // A polyline as a segment: an L-shape (0,0) -> (10,0) -> (10,10), 20 long, parameter 0.5 at the corner.
    public class PolylineSegmentTests
    {
        static PolylineSegment L() => new PolylineSegment(new[] { new Point2(0, 0), new Point2(10, 0), new Point2(10, 10) });

        [Test]
        public void LengthPointsAndTangents()
        {
            var line = L();
            Assert.That(line.Length, Is.EqualTo(20).Within(1e-9));
            Assert.That(line.PointAt(0.25), Is.EqualTo(new Point2(5, 0)));
            Assert.That(line.PointAt(0.75), Is.EqualTo(new Point2(10, 5)));
            Assert.That(line.TangentAt(0.25), Is.EqualTo(new Vector2(1, 0)));
            Assert.That(line.TangentAt(0.75), Is.EqualTo(new Vector2(0, 1)));
        }

        [Test]
        public void TangentSkipsZeroLengthPieces()
        {
            var line = new PolylineSegment(new[] { new Point2(0, 0), new Point2(0, 0), new Point2(10, 0), new Point2(10, 0) });
            foreach (var parameter in new[] { 0.0, 0.5, 1.0 })
                Assert.That(line.TangentAt(parameter), Is.EqualTo(new Vector2(1, 0)), $"at {parameter}");
        }

        [Test]
        public void ReversedRunsTheOtherWay()
        {
            var reversed = (PolylineSegment)L().Reversed();
            Assert.That(reversed.Start, Is.EqualTo(new Point2(10, 10)));
            Assert.That(reversed.End, Is.EqualTo(new Point2(0, 0)));
            Assert.That(reversed.Points.Count, Is.EqualTo(3));
        }

        [Test]
        public void MirrorXAndMoved()
        {
            var mirrored = (PolylineSegment)L().MirrorX();
            Assert.That(mirrored.Points.Select(p => p.X), Is.EqualTo(new[] { 0.0, -10, -10 }));
            var moved = (PolylineSegment)L().Moved(new Vector2(1, 2));
            Assert.That(moved.Start, Is.EqualTo(new Point2(1, 2)));
            Assert.That(moved.End, Is.EqualTo(new Point2(11, 12)));
        }

        [Test]
        public void BoundsCoverEveryPoint() =>
            Assert.That(L().Bounds, Is.EqualTo(new Bounds(0, 0, 10, 10)));

        [Test]
        public void CrossingsAtYAndX()
        {
            var atY = L().CrossingsAtY(5);
            Assert.That(atY, Has.Count.EqualTo(1));
            Assert.That(atY[0].Point, Is.EqualTo(new Point2(10, 5)));
            Assert.That(atY[0].Parameter, Is.EqualTo(0.75).Within(1e-9));

            var atX = L().CrossingsAtX(4);
            Assert.That(atX, Has.Count.EqualTo(1));
            Assert.That(atX[0].Parameter, Is.EqualTo(0.2).Within(1e-9));
        }

        [Test]
        public void CrossingAtACornerIsCountedOnce()
        {
            Assert.That(L().CrossingsAtX(10).Count, Is.EqualTo(1).Or.EqualTo(0)); // the second piece lies along x = 10
            var atCorner = L().CrossingsWithLine(new Point2(5, 5), new Vector2(1, -1));
            Assert.That(atCorner, Has.Count.EqualTo(1));
            Assert.That(atCorner[0].Parameter, Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void BetweenKeepsInnerCorners()
        {
            var middle = (PolylineSegment)L().Between(0.25, 0.75);
            Assert.That(middle.Points, Is.EqualTo(new[] { new Point2(5, 0), new Point2(10, 0), new Point2(10, 5) }));
            var early = (PolylineSegment)L().Between(0.1, 0.4);
            Assert.That(early.Points.Count, Is.EqualTo(2));
            Assert.That(early.Length, Is.EqualTo(6).Within(1e-9));
        }

        [Test]
        public void PathMayHoldAPolyline()
        {
            var path = new Shapes.Path(new Segment[] { L(), new LineSegment(new Point2(10, 10), new Point2(0, 0)) }, true);
            Assert.That(path.Bounds, Is.EqualTo(new Bounds(0, 0, 10, 10)));
            Assert.That(path.MirrorX().Segments[0], Is.InstanceOf<PolylineSegment>());
            Assert.That(path.Moved(new Vector2(5, 0)).Start, Is.EqualTo(new Point2(5, 0)));
        }
    }
}
