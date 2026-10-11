using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarForm.Shapes
{
    // A rectangle around some geometry, square to the axes.
    public readonly record struct Bounds(double MinX, double MinY, double MaxX, double MaxY)
    {
        public static Bounds Of(IEnumerable<Point2> points)
        {
            var list = points.ToList();
            return new Bounds(list.Min(p => p.X), list.Min(p => p.Y), list.Max(p => p.X), list.Max(p => p.Y));
        }

        public Bounds Union(Bounds other) => new Bounds(Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY));
    }

    // Where a segment crosses a line, and how far along the segment that is, from 0 at its start to 1 at its end.
    public readonly record struct Crossing(Point2 Point, double Parameter)
    {
        // Rounding error allowed on a parameter, so a crossing at a segment's end isn't missed.
        const double Slack = 1e-9;

        internal static bool OnSegment(double parameter) => parameter is >= -Slack and <= 1 + Slack;

        internal static double Clamp(double parameter) => System.Math.Clamp(parameter, 0, 1);
    }

    // A piece of an outline with a direction: a straight line or an arc.
    public abstract record Segment
    {
        public abstract Point2 Start { get; }
        public abstract Point2 End { get; }
        public abstract double Length { get; }

        // The direction of travel at the given parameter, as a unit vector.
        public abstract Vector2 TangentAt(double parameter);

        public abstract Point2 PointAt(double parameter);

        public abstract Segment Reversed();

        // The same segment on the other side of the centerline, running the same way.
        public abstract Segment MirrorX();

        public abstract Bounds Bounds { get; }

        // Where the segment crosses the vertical line at x, in order along the segment.
        public abstract IReadOnlyList<Crossing> CrossingsAtX(double x);

        // The part of the segment between two parameters, from 0 at its start to 1 at its end.
        public abstract Segment Between(double from, double to);
    }

    public sealed record LineSegment(Point2 From, Point2 To) : Segment
    {
        public override Point2 Start => From;
        public override Point2 End => To;
        public override double Length => From.DistanceTo(To);

        public override Vector2 TangentAt(double parameter) => (To - From).Unit;

        public override Point2 PointAt(double parameter) => From + (To - From) * parameter;

        public override Segment Reversed() => new LineSegment(To, From);

        public override Segment MirrorX() => new LineSegment(From.MirrorX(), To.MirrorX());

        public override Bounds Bounds => Bounds.Of(new[] { From, To });

        public override IReadOnlyList<Crossing> CrossingsAtX(double x)
        {
            double dx = To.X - From.X;
            if (dx == 0)
                return Array.Empty<Crossing>();
            double t = (x - From.X) / dx;
            return Crossing.OnSegment(t)
                ? new[] { new Crossing(new Point2(x, PointAt(Crossing.Clamp(t)).Y), Crossing.Clamp(t)) }
                : Array.Empty<Crossing>();
        }

        public override Segment Between(double from, double to) => new LineSegment(PointAt(from), PointAt(to));
    }

    // An arc of a circle, from StartAngle (radians, counterclockwise from +X) turning through Sweep: counterclockwise
    // when Sweep is positive, clockwise when it's negative.
    public sealed record ArcSegment(Point2 Center, double Radius, double StartAngle, double Sweep) : Segment
    {
        public override Point2 Start => PointAt(0);
        public override Point2 End => PointAt(1);
        public Point2 Mid => PointAt(0.5);
        public override double Length => Radius * Math.Abs(Sweep);
        public bool IsClockwise => Sweep < 0;

        // The arc around a circle from one point on it to another, turning the given way. Null when the points are
        // within the tolerance of each other along the circle, so there's nothing to draw.
        public static ArcSegment Around(Circle circle, Point2 from, Point2 to, bool clockwise, double tolerance)
        {
            double start = (from - circle.Center).Angle;
            double end = (to - circle.Center).Angle;
            double sweep = Normalize(clockwise ? start - end : end - start);
            double minimum = tolerance / circle.Radius;
            if (sweep < minimum || sweep > 2 * Math.PI - minimum)
                return null;
            return new ArcSegment(circle.Center, circle.Radius, start, clockwise ? -sweep : sweep);
        }

        public double AngleAt(double parameter) => StartAngle + Sweep * parameter;

        public override Point2 PointAt(double parameter) => Center + Vector2.AtAngle(AngleAt(parameter)) * Radius;

        public override Vector2 TangentAt(double parameter)
        {
            var radial = Vector2.AtAngle(AngleAt(parameter));
            return IsClockwise ? radial.Right : radial.Left;
        }

        public override Segment Reversed() => new ArcSegment(Center, Radius, StartAngle + Sweep, -Sweep);

        public override Segment MirrorX() => new ArcSegment(Center.MirrorX(), Radius, Math.PI - StartAngle, -Sweep);

        // The ends, plus the circle's extreme points the arc passes.
        public override Bounds Bounds
        {
            get
            {
                var points = new List<Point2> { Start, End };
                for (int quarter = -8; quarter <= 8; quarter++)
                {
                    double t = ParameterOf(quarter * Math.PI / 2);
                    if (t is > 0 and < 1)
                        points.Add(PointAt(t));
                }
                return Bounds.Of(points);
            }
        }

        public override IReadOnlyList<Crossing> CrossingsAtX(double x)
        {
            double dx = x - Center.X;
            if (Math.Abs(dx) > Radius)
                return Array.Empty<Crossing>();
            double dy = Math.Sqrt(Radius * Radius - dx * dx);
            return new[] { new Point2(x, Center.Y + dy), new Point2(x, Center.Y - dy) }
                .Select(point => new Crossing(point, ParameterOf((point - Center).Angle)))
                .Where(crossing => Crossing.OnSegment(crossing.Parameter))
                .Select(crossing => crossing with { Parameter = Crossing.Clamp(crossing.Parameter) })
                .OrderBy(crossing => crossing.Parameter)
                .ToArray();
        }

        public override Segment Between(double from, double to) =>
            new ArcSegment(Center, Radius, AngleAt(from), Sweep * (to - from));

        // How far along the arc the given angle is: 0 at the start, 1 at the end, outside 0–1 when it isn't on the arc.
        double ParameterOf(double angle)
        {
            double turned = Normalize(IsClockwise ? StartAngle - angle : angle - StartAngle);
            double sweep = Math.Abs(Sweep);
            // An angle just short of the start comes out near 2π; count it as just before the start.
            if (turned > sweep && turned > Math.PI + sweep / 2)
                turned -= 2 * Math.PI;
            return turned / sweep;
        }

        // An angle turned into the range 0 to 2π.
        static double Normalize(double angle) => ((angle % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);
    }

    public readonly record struct Circle(Point2 Center, double Radius)
    {
        public Circle MirrorX() => new Circle(Center.MirrorX(), Radius);
    }
}
