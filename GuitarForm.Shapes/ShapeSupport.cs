using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarForm.Shapes
{
    // Helpers and tolerances shared by the shapes. Each tolerance keeps the value it had where it was written inline;
    // they are separate constants because they mean different things.
    internal static class ShapeSupport
    {
        // How far outside 0..1 a parameter along a segment may fall and still count as on it.
        internal const double SegmentParameterSlack = 1e-9;

        // The smallest cross product of two directions that still counts as not parallel.
        internal const double ParallelCrossProduct = 1e-12;

        // Two crossings of a polyline closer than this in parameter are the same crossing (a vertex shared by pieces).
        internal const double PolylineCrossingParameter = 1e-9;

        // The distance a brace's end or crossing must clear to count as separate from the point it starts at.
        internal const double BracingSlack = 1e-9;

        // The floor under the caller's tolerance when refining a neck profile curve.
        internal const double ProfileCurveMinimumTolerance = 1e-6;

        // "a", "a and b", "a, b and c"; an empty list gives an empty string.
        internal static string Join(IEnumerable<string> items)
        {
            var list = items.ToList();
            return list.Count <= 1 ? string.Join("", list) : string.Join(", ", list.Take(list.Count - 1)) + " and " + list[^1];
        }

        // Where two lines cross (the first through p along d, the second through q along e), or null when they're
        // parallel.
        internal static Point2? Intersect(Point2 p, Vector2 d, Point2 q, Vector2 e)
        {
            double cross = d.Cross(e);
            if (Math.Abs(cross) < ParallelCrossProduct)
                return null;
            return p + d * ((q - p).Cross(e) / cross);
        }

        // A closed path through the corners in order.
        internal static Path Rectangle(params Point2[] corners) =>
            new Path(corners.Select((corner, i) => (Segment)new LineSegment(corner, corners[(i + 1) % corners.Length])),
                isClosed: true);
    }
}
