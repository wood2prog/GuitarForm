using System;
using System.Linq;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;
using Body = GuitarForm.Model.Parts.Body;

namespace GuitarForm.Tests
{
    // The 2D shapes' primitives, the side view, the soundhole and the layout of a whole guitar's drawing.
    public class GuitarDrawingTests
    {
        const double Tolerance = 0.001;
        const double Gap = 50;

        // The Body tab's Quick Test body (docs/Body.md), widest at the lower bout, 190 from the centerline.
        static Body QuickTestBody(double? tailDepth = 120, double? neckDepth = 95, double? skew = null) => new Body
        {
            Length = new(Body.Definitions.Length, 600),
            DepthAtTailEnd = new(Body.Definitions.DepthAtTailEnd, tailDepth),
            DepthAtNeckEnd = new(Body.Definitions.DepthAtNeckEnd, neckDepth),
            ProfileSkew = new(Body.Definitions.ProfileSkew, skew),
            UpperBoutWidth = new(Body.Definitions.UpperBoutWidth, 280),
            UpperBoutOffsetFromNeckEnd = new(Body.Definitions.UpperBoutOffsetFromNeckEnd, 5),
            UpperBoutPrimaryRadius = new(Body.Definitions.UpperBoutPrimaryRadius, 100),
            WaistWidth = new(Body.Definitions.WaistWidth, 240),
            WaistOffsetFromTailEnd = new(Body.Definitions.WaistOffsetFromTailEnd, 300),
            WaistRadius = new(Body.Definitions.WaistRadius, 60),
            LowerBoutWidth = new(Body.Definitions.LowerBoutWidth, 380),
            LowerBoutOffsetFromTailEnd = new(Body.Definitions.LowerBoutOffsetFromTailEnd, 10),
            LowerBoutPrimaryRadius = new(Body.Definitions.LowerBoutPrimaryRadius, 120),
            HeelFlatWidth = new(Body.Definitions.HeelFlatWidth, 56),
        };

        static GuitarDrawing Draw(Body body) => GuitarDrawing.Draw(new Guitar { Body = body }, Tolerance, Gap);

        static Point2[] Corners(Path path) => path.Segments.Select(segment => segment.Start).ToArray();

        static Point2[] Points(params double[] xy) =>
            Enumerable.Range(0, xy.Length / 2).Select(i => new Point2(xy[2 * i], xy[2 * i + 1])).ToArray();

        [Test]
        public void ArcAround_TurnsTheWayAsked()
        {
            var circle = new Circle(new Point2(0, 0), 10);

            var clockwise = ArcSegment.Around(circle, new Point2(0, 10), new Point2(10, 0), clockwise: true, Tolerance);
            var counterclockwise = ArcSegment.Around(circle, new Point2(0, 10), new Point2(10, 0), clockwise: false, Tolerance);

            Assert.That(clockwise.Sweep, Is.EqualTo(-Math.PI / 2).Within(1e-12));
            Assert.That(counterclockwise.Sweep, Is.EqualTo(3 * Math.PI / 2).Within(1e-12));
            Assert.That(clockwise.Mid.X, Is.EqualTo(Math.Sqrt(50)).Within(1e-12));
            Assert.That(ArcSegment.Around(circle, new Point2(10, 0), new Point2(10, 0.0001), true, Tolerance), Is.Null);
        }

        [Test]
        public void ArcMirror_RunsTheSameWayOnTheOtherSide()
        {
            var arc = ArcSegment.Around(new Circle(new Point2(5, 0), 10), new Point2(5, 10), new Point2(15, 0), true, Tolerance);

            var mirror = arc.MirrorX();

            Assert.That(mirror.Start.X, Is.EqualTo(-5).Within(1e-12));
            Assert.That(mirror.Start.Y, Is.EqualTo(10).Within(1e-12));
            Assert.That(mirror.End.X, Is.EqualTo(-15).Within(1e-12));
            Assert.That(((ArcSegment)mirror).IsClockwise, Is.False);
        }

        [Test]
        public void ArcBounds_IncludeTheExtremesItPasses()
        {
            var arc = ArcSegment.Around(new Circle(new Point2(0, 0), 10), new Point2(0, 10), new Point2(0, -10), true, Tolerance);

            Assert.That(arc.Bounds.MaxX, Is.EqualTo(10).Within(1e-12));
            Assert.That(arc.Bounds.MinX, Is.EqualTo(0).Within(1e-12));
        }

        [Test]
        public void ArcCrossings_OnlyOnTheArcInOrder()
        {
            var arc = ArcSegment.Around(new Circle(new Point2(0, 0), 10), new Point2(0, 10), new Point2(0, -10), true, Tolerance);

            var crossings = arc.CrossingsAtX(6);

            Assert.That(crossings.Select(c => c.Point.Y), Is.EqualTo(new[] { 8.0, -8.0 }).Within(1e-12));
            Assert.That(crossings[0].Parameter, Is.LessThan(crossings[1].Parameter));
            Assert.That(arc.CrossingsAtX(-6), Is.Empty);
        }

        [Test]
        public void SideView_GapRightOfThePlatesWidestPoint()
        {
            var drawing = Draw(QuickTestBody());

            // Back on the left, top on the right: the top is straight and the back slants to the neck depth.
            Assert.That(Corners(drawing.SideView.Outline), Is.EqualTo(Points(240, 0, 360, 0, 360, 600, 265, 600)));
            Assert.That(drawing.SideView.Outline.IsClosed, Is.True);
            Assert.That(drawing.OutlinePaths, Does.Contain(drawing.SideView.Outline));
        }

        [Test]
        public void SideView_SkewMovesTheNeckEndTowardTheTop()
        {
            var drawing = Draw(QuickTestBody(skew: 10));

            Assert.That(Corners(drawing.SideView.Outline), Is.EqualTo(Points(240, 0, 360, 0, 370, 600, 275, 600)));
        }

        [Test]
        public void SideView_NegativeSkewKeepsTheLeftmostPointAtTheGap()
        {
            var drawing = Draw(QuickTestBody(skew: -30));

            Assert.That(Corners(drawing.SideView.Outline), Is.EqualTo(Points(245, 0, 365, 0, 335, 600, 240, 600)));
        }

        [Test]
        public void Soundhole_QuickTestValues()
        {
            // docs/Soundhole.md Quick Test: diameter 100 at tail offset 420 is a circle of radius 50 centerd on (0, 420);
            // a horizontal offset of -20 moves it left; a diameter of 0 leaves no soundhole.
            var top = new TopPlate
            {
                Soundhole = new(TopPlate.Definitions.Soundhole, true),
                SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, 100),
                SoundholeOffsetFromTailEnd = new(TopPlate.Definitions.SoundholeOffsetFromTailEnd, 420),
            };
            Assert.That(SoundholeShape.Solve(top), Is.EqualTo(new Circle(new Point2(0, 420), 50)));

            top = top with { SoundholeOffsetFromCenterline = new(TopPlate.Definitions.SoundholeOffsetFromCenterline, -20) };
            Assert.That(SoundholeShape.Solve(top), Is.EqualTo(new Circle(new Point2(-20, 420), 50)));

            top = top with { SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, 0) };
            Assert.That(SoundholeShape.Solve(top), Is.Null);
        }

        [Test]
        public void SideView_WaitsForBothDepths()
        {
            var drawing = Draw(QuickTestBody(neckDepth: null));

            Assert.That(drawing.SideView.Outline, Is.Null);
            Assert.That(drawing.BodyMessages.Single().Text, Is.EqualTo(
                "Set the Body Depth at Tail End (BDDT) and Body Depth at Neck End (BDDN) to draw the side view."));
        }

        [Test]
        public void Soundhole_CenteredByItsOffsets()
        {
            var body = QuickTestBody() with
            {
                TopPlate = new TopPlate
                {
                    Soundhole = new(TopPlate.Definitions.Soundhole, true),
                    SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, 100),
                    SoundholeOffsetFromTailEnd = new(TopPlate.Definitions.SoundholeOffsetFromTailEnd, 380),
                    SoundholeOffsetFromCenterline = new(TopPlate.Definitions.SoundholeOffsetFromCenterline, -3),
                },
            };

            var drawing = Draw(body);

            Assert.That(drawing.Soundhole, Is.EqualTo(new Circle(new Point2(-3, 380), 50)));
            Assert.That(drawing.OutlineCircles, Is.EqualTo(new[] { new Circle(new Point2(-3, 380), 50) }));
        }

        [Test]
        public void Soundhole_NoneWhenTheTopHasNone()
        {
            var body = QuickTestBody() with
            {
                TopPlate = new TopPlate { SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, 100) },
            };

            Assert.That(Draw(body).Soundhole, Is.Null);
        }

        [Test]
        public void Construction_KeptApartFromTheOutline()
        {
            var drawing = Draw(QuickTestBody());

            Assert.That(drawing.ConstructionLines, Has.Count.EqualTo(6));
            Assert.That(drawing.ConstructionCircles, Has.Count.EqualTo(6));
            Assert.That(drawing.OutlinePaths.Count(path => path.IsClosed), Is.EqualTo(2));
        }

        [Test]
        public void SideView_ZeroDepth_SaysItMustBeGreaterThanZero()
        {
            var drawing = Draw(QuickTestBody(tailDepth: 0));

            Assert.That(drawing.SideView.Outline, Is.Null);
            Assert.That(drawing.BodyMessages.Any(m => m.Level == MessageLevel.Error &&
                m.Text.Contains("must be greater than zero to draw the side view")), Is.True);
        }

        [Test]
        public void FailedPlate_StillDrawsTheOtherPartsWithAWarning()
        {
            // A negative upper bout radius fails the plate; the side view is independent of it.
            var body = QuickTestBody() with
            {
                UpperBoutPrimaryRadius = new(Body.Definitions.UpperBoutPrimaryRadius, -1),
            };

            var drawing = Draw(body);

            Assert.That(drawing.Plate.Failed, Is.True);
            Assert.That(drawing.SideView.Outline, Is.Not.Null);
            Assert.That(drawing.OutlinePaths, Does.Contain(drawing.SideView.Outline));
            Assert.That(drawing.BodyMessages.Any(m => m.Level == MessageLevel.Error), Is.True);
            Assert.That(drawing.BodyMessages.Any(m => m.Level == MessageLevel.Warning &&
                m.Text.Contains("the rest is drawn without it")), Is.True);
            // With no plate the side view's offset falls back to the gap from 0.
            Assert.That(drawing.SideView.Outline.Bounds.MinX, Is.EqualTo(Gap).Within(1e-9));
        }

        [Test]
        public void Soundhole_OnWithoutADiameter_IsReportedOnceByTheModel()
        {
            var top = new TopPlate { Soundhole = new(TopPlate.Definitions.Soundhole, true) };
            var drawing = Draw(QuickTestBody() with { TopPlate = top });

            Assert.That(drawing.Soundhole, Is.Null);
            var diameter = top.SoundholeDiameter.Label;
            // The panel lists the model's messages and the drawing's; the diameter is named exactly once.
            int count = top.Validate(LengthUnit.Millimeters).Count(m => m.Text.Contains(diameter)) +
                drawing.BodyMessages.Count(m => m.Text.Contains(diameter));
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Plate_WithoutALength_IsARemark_NotAnError()
        {
            var drawing = Draw(new Body());

            Assert.That(drawing.Plate.Failed, Is.True);
            Assert.That(drawing.BodyMessages.Any(m => m.Level == MessageLevel.Error || m.Level == MessageLevel.Warning), Is.False);
            Assert.That(drawing.BodyMessages.Any(m => m.Level == MessageLevel.Remark && m.Text.Contains("to draw the body outline")), Is.True);
        }

        [Test]
        public void Plate_WithAZeroLength_IsAnErrorWithAWarning()
        {
            var drawing = Draw(QuickTestBody() with { Length = new(Body.Definitions.Length, 0) });

            Assert.That(drawing.Plate.Failed, Is.True);
            Assert.That(drawing.BodyMessages.Any(m => m.Level == MessageLevel.Error), Is.True);
            Assert.That(drawing.BodyMessages.Any(m => m.Level == MessageLevel.Warning), Is.True);
        }

        [Test]
        public void Items_TagSideViewAndSoundhole()
        {
            var body = QuickTestBody() with
            {
                TopPlate = new TopPlate
                {
                    Soundhole = new(TopPlate.Definitions.Soundhole, true),
                    SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, 100),
                    SoundholeOffsetFromTailEnd = new(TopPlate.Definitions.SoundholeOffsetFromTailEnd, 380),
                },
            };

            var drawing = Draw(body);

            var side = drawing.Items.Single(i => i.Component == DrawingComponent.Sides);
            Assert.That(side.Path, Is.SameAs(drawing.SideView.Outline));
            Assert.That(side.Role, Is.EqualTo(DrawingRole.Outline));
            var hole = drawing.Items.Single(i => i.Component == DrawingComponent.Soundhole);
            Assert.That(hole.Circle, Is.EqualTo(drawing.Soundhole));
            Assert.That(drawing.Items.Where(i => i.Component == DrawingComponent.TopPlate && i.Role == DrawingRole.Construction).ToList(),
                Has.Count.EqualTo(12));
            Assert.That(drawing.Items.Count(i => i.Component == DrawingComponent.TopPlate && i.Role == DrawingRole.Outline), Is.EqualTo(1));
        }

        [Test]
        public void Items_FailedPlate_StillTagsTheOthers()
        {
            var body = QuickTestBody() with
            {
                UpperBoutPrimaryRadius = new(Body.Definitions.UpperBoutPrimaryRadius, -1),
            };

            var drawing = Draw(body);

            Assert.That(drawing.Items.Any(i => i.Component == DrawingComponent.Sides), Is.True);
            Assert.That(drawing.Items.Any(i => i.Component == DrawingComponent.TopPlate && i.Role == DrawingRole.Outline), Is.False);
        }
    }
}
