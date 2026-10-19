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
