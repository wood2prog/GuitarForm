using System;

namespace GuitarForm.Shapes
{
    // A point in the drawing plane, in millimeters. The guitar is drawn with the tail end at the origin and the body
    // length up +Y; +X is the treble side.
    public readonly record struct Point2(double X, double Y)
    {
        public static readonly Point2 Origin = new Point2(0, 0);

        public double DistanceTo(Point2 other) => (other - this).Length;

        // The same point on the other side of the centerline.
        public Point2 MirrorX() => new Point2(-X, Y);

        public static Point2 operator +(Point2 point, Vector2 vector) => new Point2(point.X + vector.X, point.Y + vector.Y);

        public static Point2 operator -(Point2 point, Vector2 vector) => new Point2(point.X - vector.X, point.Y - vector.Y);

        public static Vector2 operator -(Point2 to, Point2 from) => new Vector2(to.X - from.X, to.Y - from.Y);
    }

    public readonly record struct Vector2(double X, double Y)
    {
        public static readonly Vector2 XAxis = new Vector2(1, 0);
        public static readonly Vector2 YAxis = new Vector2(0, 1);

        public double Length => Math.Sqrt(X * X + Y * Y);

        // The direction at an angle in radians, counterclockwise from +X.
        public static Vector2 AtAngle(double angle) => new Vector2(Math.Cos(angle), Math.Sin(angle));

        // Counterclockwise from +X, in radians, from -π to π.
        public double Angle => Math.Atan2(Y, X);

        public Vector2 Unit => this / Length;

        // Turned a quarter turn counterclockwise (to the left of this direction) or clockwise (to its right).
        public Vector2 Left => new Vector2(-Y, X);

        public Vector2 Right => new Vector2(Y, -X);

        public double Dot(Vector2 other) => X * other.X + Y * other.Y;

        // Positive when other turns counterclockwise from this direction, negative when clockwise.
        public double Cross(Vector2 other) => X * other.Y - Y * other.X;

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);

        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);

        public static Vector2 operator -(Vector2 v) => new Vector2(-v.X, -v.Y);

        public static Vector2 operator *(Vector2 v, double factor) => new Vector2(v.X * factor, v.Y * factor);

        public static Vector2 operator *(double factor, Vector2 v) => v * factor;

        public static Vector2 operator /(Vector2 v, double divisor) => new Vector2(v.X / divisor, v.Y / divisor);
    }
}
