using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarForm.Shapes
{
    // Straight pieces joined end to end through a list of points (at least two). A parameter runs from 0 at the first
    // point to 1 at the last, in proportion to the distance along the polyline.
    public sealed record PolylineSegment : Segment
    {
        readonly Point2[] _points;
        readonly double[] _cumulative; // distance along the polyline to each point

        public PolylineSegment(IEnumerable<Point2> points)
        {
            _points = points.ToArray();
            if (_points.Length < 2)
                throw new ArgumentException("A polyline needs at least two points.", nameof(points));
            _cumulative = new double[_points.Length];
            for (int i = 1; i < _points.Length; i++)
                _cumulative[i] = _cumulative[i - 1] + _points[i - 1].DistanceTo(_points[i]);
        }

        public IReadOnlyList<Point2> Points => _points;

        public override Point2 Start => _points[0];
        public override Point2 End => _points[^1];
        public override double Length => _cumulative[^1];

        public override Vector2 TangentAt(double parameter)
        {
            int i = PieceAt(parameter);
            if (_cumulative[i + 1] == _cumulative[i])
            {
                // A zero-length piece has no direction: use the nearest piece that has one.
                int best = -1;
                for (int j = 0; j < _points.Length - 1; j++)
                    if (_cumulative[j + 1] > _cumulative[j] && (best < 0 || Math.Abs(j - i) < Math.Abs(best - i)))
                        best = j;
                if (best < 0)
                    return new Vector2(0, 0);
                i = best;
            }
            return (_points[i + 1] - _points[i]).Unit;
        }

        public override Point2 PointAt(double parameter)
        {
            int i = PieceAt(parameter);
            return PointOnPiece(i, LocalParameter(i, parameter));
        }

        public override Segment Reversed() => new PolylineSegment(Enumerable.Reverse(_points));

        public override Segment MirrorX() => new PolylineSegment(_points.Select(point => point.MirrorX()));

        public override Bounds Bounds => Bounds.Of(_points);

        public override IReadOnlyList<Crossing> CrossingsAtX(double x) =>
            Crossings(piece => new LineSegment(_points[piece], _points[piece + 1]).CrossingsAtX(x));

        public override IReadOnlyList<Crossing> CrossingsAtY(double y) =>
            Crossings(piece => new LineSegment(_points[piece], _points[piece + 1]).CrossingsAtY(y));

        public override IReadOnlyList<Crossing> CrossingsWithLine(Point2 through, Vector2 direction) =>
            Crossings(
                piece => new LineSegment(_points[piece], _points[piece + 1]).CrossingsWithLine(through, direction));

        public override Segment Moved(Vector2 by) => new PolylineSegment(_points.Select(point => point + by));

        public override Segment Between(double from, double to)
        {
            if (from > to)
                return Between(to, from).Reversed();
            var points = new List<Point2> { PointAt(from) };
            double a = from * Length, b = to * Length;
            for (int i = 1; i < _points.Length - 1; i++)
                if (_cumulative[i] > a && _cumulative[i] < b)
                    points.Add(_points[i]);
            points.Add(PointAt(to));
            return new PolylineSegment(points);
        }

        public bool Equals(PolylineSegment other) => other is not null && _points.SequenceEqual(other._points);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var point in _points)
                hash.Add(point);
            return hash.ToHashCode();
        }

        // The crossings of every piece, with parameters turned into the whole polyline's, in order, and one crossing
        // where two pieces meet on the line.
        IReadOnlyList<Crossing> Crossings(Func<int, IReadOnlyList<Crossing>> onPiece)
        {
            var result = new List<Crossing>();
            double total = Length;
            for (int i = 0; i < _points.Length - 1; i++)
            {
                double pieceLength = _cumulative[i + 1] - _cumulative[i];
                foreach (var crossing in onPiece(i))
                {
                    double parameter = total == 0 ? 0 : (_cumulative[i] + crossing.Parameter * pieceLength) / total;
                    if (result.Count > 0 && Math.Abs(result[^1].Parameter - parameter) < 1e-9)
                        continue;
                    result.Add(new Crossing(crossing.Point, parameter));
                }
            }
            return result;
        }

        // The piece a parameter falls on, clamped to the ends.
        int PieceAt(double parameter)
        {
            double distance = Math.Clamp(parameter, 0, 1) * Length;
            int i = 0;
            while (i < _points.Length - 2 && _cumulative[i + 1] < distance)
                i++;
            return i;
        }

        double LocalParameter(int piece, double parameter)
        {
            double pieceLength = _cumulative[piece + 1] - _cumulative[piece];
            if (pieceLength == 0)
                return 0;
            return Math.Clamp((Math.Clamp(parameter, 0, 1) * Length - _cumulative[piece]) / pieceLength, 0, 1);
        }

        Point2 PointOnPiece(int piece, double local) =>
            _points[piece] + (_points[piece + 1] - _points[piece]) * local;
    }
}
