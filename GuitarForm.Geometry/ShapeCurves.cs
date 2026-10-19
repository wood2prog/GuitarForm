using System.Collections.Generic;
using System.Linq;
using GuitarForm.Shapes;
using Rhino.Geometry;
using Circle = GuitarForm.Shapes.Circle;
using Path = GuitarForm.Shapes.Path;

namespace GuitarForm.Geometry
{
    // Turns the CAD-neutral shapes into Rhino curves, in the same millimeters, on the XY plane.
    public static class ShapeCurves
    {
        public static Point3d ToPoint(Point2 point) => new Point3d(point.X, point.Y, 0);

        public static Curve ToCurve(Segment segment) => segment switch
        {
            LineSegment line => new LineCurve(ToPoint(line.From), ToPoint(line.To)),
            ArcSegment arc => new ArcCurve(new Arc(ToPoint(arc.Start), ToPoint(arc.Mid), ToPoint(arc.End))),
            PolylineSegment polyline => new PolylineCurve(polyline.Points.Select(ToPoint)),
            _ => null,
        };

        // How far apart (mm) the end of one segment and the start of the next may be and still join.
        const double JoinTolerance = 1e-3;

        // A path's segments joined into one curve; null when a piece can't be converted or the pieces can't be joined.
        public static Curve ToCurve(Path path)
        {
            var pieces = new List<Curve>();
            foreach (var segment in path.Segments)
            {
                var piece = ToCurve(segment);
                if (piece == null)
                    return null;
                pieces.Add(piece);
            }
            if (pieces.Count == 0)
                return null;

            var curve = new PolyCurve();
            if (pieces.All(piece => curve.AppendSegment(piece)))
                return curve.SegmentCount == 1 ? curve.SegmentCurve(0).DuplicateCurve() : curve;

            // Slightly non-contiguous pieces: join them within a small tolerance (in order, without reordering).
            var joined = Curve.JoinCurves(pieces.Select(piece => piece.DuplicateCurve()), JoinTolerance, false);
            return joined.Length == 1 ? joined[0] : null;
        }

        public static Curve ToCurve(Circle circle) =>
            new ArcCurve(new Rhino.Geometry.Circle(ToPoint(circle.Center), circle.Radius));

        // Every drawn item as a curve, keeping its component and role: paths first, then circles (each in drawing
        // order). A path that can't be turned into a curve is left out and counted in failed.
        public static List<DrawnCurve> Curves(GuitarDrawing drawing, out int failed)
        {
            var result = new List<DrawnCurve>();
            int dropped = 0;
            foreach (var item in drawing.Items.Where(item => item.Path != null))
            {
                var curve = ToCurve(item.Path);
                if (curve == null)
                    dropped++;
                else
                    result.Add(new DrawnCurve(curve, item.Component, item.Role));
            }
            foreach (var item in drawing.Items.Where(item => item.Path == null && item.Circle.HasValue))
                result.Add(new DrawnCurve(ToCurve(item.Circle.Value), item.Component, item.Role));
            failed = dropped;
            return result;
        }

        // Construction (and center) lines and circles.
        public static Curve[] Construction(GuitarDrawing drawing) =>
            Curves(drawing, out _).Where(c => c.Role != DrawingRole.Outline).Select(c => c.Curve).ToArray();

        public static Curve[] Outline(GuitarDrawing drawing) => Outline(drawing, out _);

        // The same, leaving out any path that can't be turned into a curve and counting those in failed.
        public static Curve[] Outline(GuitarDrawing drawing, out int failed) =>
            Curves(drawing, out failed).Where(c => c.Role == DrawingRole.Outline).Select(c => c.Curve).ToArray();
    }

    // A Rhino curve with the component of the guitar it belongs to and how it is used.
    public sealed record DrawnCurve(Curve Curve, DrawingComponent Component, DrawingRole Role);
}
