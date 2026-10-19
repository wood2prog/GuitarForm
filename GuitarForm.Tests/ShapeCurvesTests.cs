using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Geometry;
using GuitarForm.Model;
using GuitarForm.Shapes;
using NUnit.Framework;
using Rhino.Testing.Fixtures;

namespace GuitarForm.Tests
{
    // The Rhino curves made from a GuitarDrawing keep each item's component and role.
    [RhinoTestFixture]
    public class ShapeCurvesTests
    {
        static DesignDrawing FromShapes(GuitarDrawing shapes) =>
            DesignDrawing.FromShapes(shapes, new GeometryMessage[0], new GeometryMessage[0], new GeometryMessage[0]);

        static LineSegment Line(double x1, double y1, double x2, double y2) =>
            new LineSegment(new Point2(x1, y1), new Point2(x2, y2));

        // A segment of a kind ShapeCurves has no curve for.
        sealed record UnknownSegment : Segment
        {
            static NotSupportedException No() => new NotSupportedException();
            public override Point2 Start => new Point2(10, 0);
            public override Point2 End => new Point2(20, 0);
            public override double Length => 10;
            public override Vector2 TangentAt(double parameter) => throw No();
            public override Point2 PointAt(double parameter) => throw No();
            public override Segment Reversed() => throw No();
            public override Segment MirrorX() => throw No();
            public override Bounds Bounds => throw No();
            public override IReadOnlyList<Crossing> CrossingsAtX(double x) => throw No();
            public override IReadOnlyList<Crossing> CrossingsAtY(double y) => throw No();
            public override IReadOnlyList<Crossing> CrossingsWithLine(Point2 through, Vector2 direction) => throw No();
            public override Segment Moved(Vector2 by) => throw No();
            public override Segment Between(double from, double to) => throw No();
        }

        [Test]
        public void FromShapes_TagsFretboardItems()
        {
            var shapes = GuitarDrawing.Draw(FretboardShapeTests.TestGuitar(FretboardShapeTests.Set(new[] { .012, .016, .024, .032, .042, .053 })), 0.001, 50);
            var drawing = FromShapes(shapes);

            Assert.That(drawing.Curves.Count, Is.EqualTo(shapes.Items.Count));
            Assert.That(drawing.Curves.Count(c => c.Component == DrawingComponent.Strings && c.Role == DrawingRole.Outline), Is.EqualTo(6));
            Assert.That(drawing.Curves.Count(c => c.Component == DrawingComponent.Frets && c.Role == DrawingRole.Centerline), Is.EqualTo(20));
            Assert.That(drawing.Curves.Count(c => c.Component == DrawingComponent.FaceDots && c.Role == DrawingRole.Outline), Is.EqualTo(3));
            // Centerlines count as construction in the legacy views.
            Assert.That(drawing.Construction.Count, Is.EqualTo(shapes.ConstructionLines.Count + shapes.ConstructionCircles.Count));
            Assert.That(drawing.Outline.Count, Is.EqualTo(shapes.OutlinePaths.Count + shapes.OutlineCircles.Count));
        }

        [Test]
        public void FromShapes_TagsTheBlocks()
        {
            var shapes = GuitarDrawing.Draw(BracingShapeTests.TestGuitar(), 0.001, 50);
            var drawing = FromShapes(shapes);

            Assert.That(drawing.Curves.Count(c => c.Component == DrawingComponent.Headblock), Is.EqualTo(1));
            Assert.That(drawing.Curves.Count(c => c.Component == DrawingComponent.Tailblock), Is.EqualTo(1));
            Assert.That(drawing.Curves.Count(c => c.Component == DrawingComponent.TopPlate && c.Role == DrawingRole.Outline), Is.GreaterThan(0));
            Assert.That(drawing.Construction.Count + drawing.Outline.Count, Is.EqualTo(drawing.Curves.Count));
        }

        [Test]
        public void Curves_PathsWithATinyGapStillJoin()
        {
            foreach (double gap in new[] { 1e-6, 1e-4 })
            {
                var path = new Path(new Segment[]
                {
                    Line(0, 0, 10, 0),
                    new ArcSegment(new Point2(10 + gap, 5), 5, -Math.PI / 2, Math.PI),
                    Line(10 + 2 * gap, 10, 0, 10),
                }, false);
                var shapes = new GuitarDrawing();
                shapes.Items.Add(new DrawnItem(DrawingComponent.TopPlate, DrawingRole.Outline, path));

                var curves = ShapeCurves.Curves(shapes, out int failed);
                Assert.That(failed, Is.EqualTo(0), $"gap {gap}");
                Assert.That(curves.Count, Is.EqualTo(1));
                Assert.That(curves[0].Curve.GetLength(), Is.GreaterThan(30));
            }
        }

        static Path Rectangle(double gap = 0) => new Path(new Segment[]
        {
            Line(0, 0, 10, 0), Line(10 + gap, 0, 10, 5), Line(10, 5 + gap, 0, 5), Line(0, 5, 0, gap),
        }, true);

        [Test]
        public void Curves_AnEmptyDrawing_HasNoCurvesAndNoFailures()
        {
            var curves = ShapeCurves.Curves(new GuitarDrawing(), out int failed);

            Assert.That(curves, Is.Empty);
            Assert.That(failed, Is.EqualTo(0));
            Assert.That(ShapeCurves.Outline(new GuitarDrawing()), Is.Empty);
            Assert.That(ShapeCurves.Construction(new GuitarDrawing()), Is.Empty);
        }

        [Test]
        public void Curves_ClosedPathsJoinClosed_OpenPathsStayOpen()
        {
            var shapes = new GuitarDrawing();
            shapes.Items.Add(new DrawnItem(DrawingComponent.TopPlate, DrawingRole.Outline, Rectangle()));
            shapes.Items.Add(new DrawnItem(DrawingComponent.Sides, DrawingRole.Outline,
                new Path(new Segment[] { Line(0, 0, 10, 0), Line(10, 0, 10, 5), Line(10, 5, 0, 5) }, false)));

            var curves = ShapeCurves.Curves(shapes, out int failed);

            Assert.That(failed, Is.EqualTo(0));
            Assert.That(curves.Count, Is.EqualTo(2));
            Assert.That(curves[0].Curve.IsClosed, Is.True);
            Assert.That(curves[0].Curve.GetLength(), Is.EqualTo(30).Within(1e-9));
            Assert.That(curves[1].Curve.IsClosed, Is.False);
            Assert.That(curves[1].Curve.GetLength(), Is.EqualTo(25).Within(1e-9));
        }

        [Test]
        public void Curves_AClosedPathWithATinyGap_StillJoins()
        {
            // 0.0005 mm is under the 0.001 join tolerance. (Larger gaps are not asserted: Rhino's PolyCurve.AppendSegment
            // snaps even a 50 mm gap, so the path does not fail.)
            var shapes = new GuitarDrawing();
            shapes.Items.Add(new DrawnItem(DrawingComponent.Sides, DrawingRole.Outline, Rectangle(0.0005)));

            var curves = ShapeCurves.Curves(shapes, out int failed);

            Assert.That(failed, Is.EqualTo(0));
            Assert.That(curves.Single().Curve.GetLength(), Is.EqualTo(30).Within(0.01));
        }

        [Test]
        public void Curves_AnEmptyPath_IsCountedAsFailed()
        {
            var shapes = new GuitarDrawing();
            shapes.Items.Add(new DrawnItem(DrawingComponent.TopPlate, DrawingRole.Outline, new Path(new Segment[0], false)));

            var curves = ShapeCurves.Curves(shapes, out int failed);

            Assert.That(curves, Is.Empty);
            Assert.That(failed, Is.EqualTo(1));
        }

        [Test]
        public void Curves_PathsComeFirstThenCircles_AndItemsWithNeitherAreIgnored()
        {
            var shapes = new GuitarDrawing();
            shapes.Items.Add(new DrawnItem(DrawingComponent.Soundhole, DrawingRole.Outline, Circle: new Circle(new Point2(5, 5), 3)));
            shapes.Items.Add(new DrawnItem(DrawingComponent.Bracing, DrawingRole.Construction));
            shapes.Items.Add(new DrawnItem(DrawingComponent.TopPlate, DrawingRole.Centerline,
                new Path(new Segment[] { Line(0, 0, 0, 10) }, false)));
            shapes.Items.Add(new DrawnItem(DrawingComponent.FaceDots, DrawingRole.Outline, Circle: new Circle(new Point2(1, 1), 1)));

            var curves = ShapeCurves.Curves(shapes, out int failed);

            Assert.That(failed, Is.EqualTo(0));
            Assert.That(curves.Select(c => c.Component), Is.EqualTo(new[]
                { DrawingComponent.TopPlate, DrawingComponent.Soundhole, DrawingComponent.FaceDots }));
            // The circles are closed curves of the right size.
            Assert.That(curves[1].Curve.IsClosed, Is.True);
            Assert.That(curves[1].Curve.GetLength(), Is.EqualTo(2 * Math.PI * 3).Within(1e-9));
        }

        [Test]
        public void OutlineAndConstruction_SplitByRole_AndOutlineCountsFailures()
        {
            var shapes = new GuitarDrawing();
            shapes.Items.Add(new DrawnItem(DrawingComponent.TopPlate, DrawingRole.Outline, Rectangle()));
            shapes.Items.Add(new DrawnItem(DrawingComponent.TopPlate, DrawingRole.Construction, new Path(new Segment[] { Line(0, 0, 5, 5) }, false)));
            shapes.Items.Add(new DrawnItem(DrawingComponent.TopPlate, DrawingRole.Centerline, new Path(new Segment[] { Line(0, 0, 0, 5) }, false)));
            shapes.Items.Add(new DrawnItem(DrawingComponent.Sides, DrawingRole.Outline, new Path(new Segment[] { new UnknownSegment() }, false)));
            shapes.Items.Add(new DrawnItem(DrawingComponent.Soundhole, DrawingRole.Construction, Circle: new Circle(Point2.Origin, 2)));

            var outline = ShapeCurves.Outline(shapes, out int failed);
            var construction = ShapeCurves.Construction(shapes);

            Assert.That(outline.Length, Is.EqualTo(1));
            Assert.That(failed, Is.EqualTo(1));
            // The centerline and the construction line and circle all count as construction.
            Assert.That(construction.Length, Is.EqualTo(3));
        }

        [Test]
        public void ToCurve_Segments_MatchTheirShapes()
        {
            var line = ShapeCurves.ToCurve(Line(0, 0, 3, 4));
            Assert.That(line, Is.InstanceOf<Rhino.Geometry.LineCurve>());
            Assert.That(line.GetLength(), Is.EqualTo(5).Within(1e-9));

            var arc = ShapeCurves.ToCurve(new ArcSegment(Point2.Origin, 10, 0, Math.PI / 2));
            Assert.That(arc.GetLength(), Is.EqualTo(5 * Math.PI).Within(1e-9));
            Assert.That(arc.PointAtEnd.Y, Is.EqualTo(10).Within(1e-9));

            var polyline = ShapeCurves.ToCurve(new PolylineSegment(new[] { new Point2(0, 0), new Point2(10, 0), new Point2(10, 10) }));
            Assert.That(polyline, Is.InstanceOf<Rhino.Geometry.PolylineCurve>());
            Assert.That(polyline.GetLength(), Is.EqualTo(20).Within(1e-9));

            Assert.That(ShapeCurves.ToCurve(new UnknownSegment()), Is.Null);
        }

        [Test]
        public void ToCurve_ASinglePathSegment_IsThatSegmentsCurve_NotAPolyCurve()
        {
            var curve = ShapeCurves.ToCurve(new Path(new Segment[] { Line(0, 0, 10, 0) }, false));

            Assert.That(curve, Is.InstanceOf<Rhino.Geometry.LineCurve>());
        }

        [Test]
        public void Curves_SkipAndCountPathsThatCantBeConverted()
        {
            var shapes = new GuitarDrawing();
            shapes.Items.Add(new DrawnItem(DrawingComponent.TopPlate, DrawingRole.Outline,
                new Path(new Segment[] { Line(0, 0, 10, 0) }, false)));
            // A segment kind with no curve can't be appended to the curve.
            shapes.Items.Add(new DrawnItem(DrawingComponent.Sides, DrawingRole.Outline,
                new Path(new Segment[] { Line(0, 0, 10, 0), new UnknownSegment() }, false)));
            shapes.Items.Add(new DrawnItem(DrawingComponent.Soundhole, DrawingRole.Outline, Circle: new Circle(new Point2(5, 5), 3)));

            var curves = ShapeCurves.Curves(shapes, out int failed);
            Assert.That(failed, Is.EqualTo(1));
            Assert.That(curves.Count, Is.EqualTo(2));

            var drawing = FromShapes(shapes);
            Assert.That(drawing.Outline.Count, Is.EqualTo(2));
            Assert.That(drawing.BodyMessages.Any(m => m.Level == MessageLevel.Error), Is.True);
        }
    }
}
