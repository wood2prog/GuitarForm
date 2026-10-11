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

        public Path Moved(Vector2 by) => new Path(Segments.Select(segment => segment.Moved(by)), IsClosed);

        public Bounds Bounds => Segments.Select(segment => segment.Bounds).Aggregate((a, b) => a.Union(b));
    }
}
