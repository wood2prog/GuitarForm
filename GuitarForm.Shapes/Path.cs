using System.Collections.Generic;
using System.Linq;

namespace GuitarForm.Shapes
{
    // Segments joined end to end, in order. A closed path ends where it starts.
    public sealed class Path
    {
        public IReadOnlyList<Segment> Segments { get; }
        public bool IsClosed { get; }

        public Path(IEnumerable<Segment> segments, bool isClosed)
        {
            Segments = segments.ToArray();
            IsClosed = isClosed;
        }

        public Point2 Start => Segments[0].Start;
        public Point2 End => Segments[^1].End;

        // The same path on the other side of the line x = 0, running the same way.
        public Path MirrorX() => new Path(Segments.Select(segment => segment.MirrorX()), IsClosed);

        public Path Moved(Vector2 by) => new Path(Segments.Select(segment => segment.Moved(by)), IsClosed);

        public Bounds Bounds => Segments.Select(segment => segment.Bounds).Aggregate((a, b) => a.Union(b));
    }
}
