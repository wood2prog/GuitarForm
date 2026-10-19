using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarForm.Shapes
{
    // Pure maths on a closed outline made of segments: its winding, whether a point is inside it, and the outline moved
    // inside itself.
    internal static class PolygonMath
    {
        // Twice the outline's area, from points along it: negative when it runs clockwise.
        internal static double SignedArea(IReadOnlyList<Segment> outline)
        {
            var points = outline.SelectMany(segment => Enumerable.Range(0, 8).Select(i => segment.PointAt(i / 8.0))).ToList();
            return points.Select((p, i) => p.X * points[(i + 1) % points.Count].Y - points[(i + 1) % points.Count].X * p.Y)
                .Sum();
        }

        // Whether a point is inside the outline: a line from it toward +X crosses the outline an odd number of times.
        internal static bool Contains(IReadOnlyList<Segment> outline, Point2 point)
        {
            var crossings = outline.SelectMany(segment => segment.CrossingsAtY(point.Y))
                .Where(crossing => crossing.Point.X > point.X)
                .Select(crossing => crossing.Point.X)
                .OrderBy(x => x)
                .ToList();
            // A crossing at the joint between two segments is found on both.
            int count = crossings.Where((x, i) => i == 0 || x - crossings[i - 1] > ShapeSupport.BracingSlack).Count();
            return count % 2 == 1;
        }

        // The outline moved inside itself by the offset: lines moved square to themselves, arcs' radii changed. An arc
        // the offset would shrink to nothing is left out. An offset of 0 returns the segments as they are.
        internal static List<Segment> OffsetInward(IReadOnlyList<Segment> outline, bool clockwise, double offset)
        {
            var offsetOutline = new List<Segment>();
            foreach (var segment in outline)
            {
                var tangent = segment.TangentAt(0);
                var inward = clockwise ? tangent.Right : tangent.Left;
                if (offset == 0)
                    offsetOutline.Add(segment);
                else if (segment is LineSegment line)
                    offsetOutline.Add(line.Moved(inward * offset));
                else if (segment is ArcSegment arc)
                {
                    double radius = (arc.Center - arc.Start).Dot(inward) > 0 ? arc.Radius - offset : arc.Radius + offset;
                    if (radius > ShapeSupport.BracingSlack)
                        offsetOutline.Add(arc with { Radius = radius });
                }
                else
                    throw new NotSupportedException(
                        "The bracing can't be drawn: the plate outline has a piece that isn't a line or an arc.");
            }
            return offsetOutline;
        }
    }
}
