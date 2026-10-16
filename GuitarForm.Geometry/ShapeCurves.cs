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

        // A path's segments joined into one curve.
        public static Curve ToCurve(Path path)
        {
            var curve = new PolyCurve();
            foreach (var segment in path.Segments)
                curve.AppendSegment(ToCurve(segment));
            return curve.SegmentCount == 1 ? curve.SegmentCurve(0).DuplicateCurve() : curve;
        }

        public static Curve ToCurve(Circle circle) =>
            new ArcCurve(new Rhino.Geometry.Circle(ToPoint(circle.Center), circle.Radius));

        public static Curve[] Construction(GuitarDrawing drawing) =>
            drawing.ConstructionLines.Select(line => ToCurve(line))
                .Concat(drawing.ConstructionCircles.Select(ToCurve))
                .ToArray();

        public static Curve[] Outline(GuitarDrawing drawing) =>
            drawing.OutlinePaths.Select(ToCurve)
                .Concat(drawing.OutlineCircles.Select(ToCurve))
                .ToArray();
    }
}
