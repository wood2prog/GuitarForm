using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;
using Body = GuitarForm.Model.Parts.Body;
using Path = GuitarForm.Shapes.Path;

namespace GuitarForm.Tests
{
    // The plate outline, worked out with no CAD: the Body tab's Quick Test (docs/Body.md), the outline in pieces while
    // values are missing, and the cutaways.
    public class PlateShapeTests
    {
        const double Tolerance = 0.001;

        // The Quick Test's body, with every outline value set.
        static Body QuickTestBody() => new Body
        {
            Length = new(Body.Definitions.Length, 600),
            UpperBoutWidth = new(Body.Definitions.UpperBoutWidth, 280),
            UpperBoutOffsetFromNeckEnd = new(Body.Definitions.UpperBoutOffsetFromNeckEnd, 5),
            UpperBoutPrimaryRadius = new(Body.Definitions.UpperBoutPrimaryRadius, 100),
            UpperBoutSecondaryOffset = new(Body.Definitions.UpperBoutSecondaryOffset, 30),
            WaistWidth = new(Body.Definitions.WaistWidth, 240),
            WaistOffsetFromTailEnd = new(Body.Definitions.WaistOffsetFromTailEnd, 300),
            WaistRadius = new(Body.Definitions.WaistRadius, 60),
            LowerBoutWidth = new(Body.Definitions.LowerBoutWidth, 380),
            LowerBoutOffsetFromTailEnd = new(Body.Definitions.LowerBoutOffsetFromTailEnd, 10),
            LowerBoutPrimaryRadius = new(Body.Definitions.LowerBoutPrimaryRadius, 120),
            LowerBoutSecondaryOffset = new(Body.Definitions.LowerBoutSecondaryOffset, 40),
            HeelFlatWidth = new(Body.Definitions.HeelFlatWidth, 56),
        };

        // A fretboard 44 wide with parallel edges, so its treble edge is 22 from the centerline everywhere.
        static Guitar QuickTestGuitar(Body body) => new Guitar
        {
            ScaleLength = new(Guitar.Definitions.ScaleLength, 645),
            NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, 14),
            Neck = new Neck
            {
                Fretboard = new Fretboard
                {
                    NutWidth = new(Fretboard.Definitions.NutWidth, 44),
                    StringSpacingAtBridge = new(Fretboard.Definitions.StringSpacingAtBridge, 37),
                    BassStringSetback = new(Fretboard.Definitions.BassStringSetback, 3.5),
                    TrebleStringSetback = new(Fretboard.Definitions.TrebleStringSetback, 3.5),
                },
            },
            Body = body,
        };

        static Body With(Body body, ParameterDefinition definition, double? value) =>
            (Body)body.With(new NumberParameter(definition, value));

        static Body WithCutaway(CutawayType type, double offset, double width, double depth)
        {
            var body = QuickTestBody() with { CutawayType = new(Body.Definitions.CutawayType, type) };
            body = With(body, Body.Definitions.CutawayOffsetFromNeck, offset);
            body = With(body, Body.Definitions.CutawayWidth, width);
            body = With(body, Body.Definitions.CutawayDepth, depth);
            return body;
        }

        static PlateShape Solve(Body body) => PlateShape.Solve(QuickTestGuitar(body), Tolerance);

        static IEnumerable<Segment> Segments(PlateShape plate) => plate.Outline.SelectMany(path => path.Segments);

        static void AssertPoint(Point2 actual, double x, double y, double within = 0.05)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(within), $"x of {actual}");
            Assert.That(actual.Y, Is.EqualTo(y).Within(within), $"y of {actual}");
        }

        // Each segment starts where the one before it ends, and a closed path ends where it starts.
        static void AssertJoined(Path path)
        {
            for (int i = 1; i < path.Segments.Count; i++)
                Assert.That(path.Segments[i].Start.DistanceTo(path.Segments[i - 1].End), Is.LessThan(1e-6), $"gap before segment {i}");
            if (path.IsClosed)
                Assert.That(path.End.DistanceTo(path.Start), Is.LessThan(1e-6), "gap closing the path");
        }

        static ArcSegment ArcWithRadius(PlateShape plate, double radius) =>
            Segments(plate).OfType<ArcSegment>().First(arc => Math.Abs(arc.Radius - radius) < 0.05 && arc.Start.X >= 0);

        [Test]
        public void BodyLengthOnly_DrawsTheCenterline()
        {
            var plate = Solve(new Body { Length = new(Body.Definitions.Length, 600) });

            Assert.That(plate.Failed, Is.False);
            Assert.That(plate.ConstructionLines, Is.EqualTo(new[] { new LineSegment(new Point2(0, 0), new Point2(0, 600)) }));
            Assert.That(plate.Outline, Is.Empty);
        }

        [Test]
        public void UpperBout_LineAndPrimaryRadii()
        {
            var plate = Solve(QuickTestBody());

            Assert.That(plate.ConstructionLines, Does.Contain(new LineSegment(new Point2(-140, 495), new Point2(140, 495))));
            Assert.That(plate.Radii, Does.Contain(new Circle(new Point2(40, 495), 100)));
            Assert.That(plate.Radii, Does.Contain(new Circle(new Point2(-40, 495), 100)));
        }

        [Test]
        public void SomeUpperBoutValues_Remark()
        {
            var plate = Solve(new Body
            {
                Length = new(Body.Definitions.Length, 600),
                UpperBoutWidth = new(Body.Definitions.UpperBoutWidth, 280),
            });

            Assert.That(plate.Messages.Single().Level, Is.EqualTo(MessageLevel.Remark));
            Assert.That(plate.Messages.Single().Text, Does.Contain("Upper Bout Primary Radius (UBPR)"));
        }

        [Test]
        public void Tail_ArcThroughTheOrigin()
        {
            var arc = ArcWithRadius(Solve(QuickTestBody()), 370);

            AssertPoint(arc.Center, 0, 370);
            AssertPoint(arc.Start, 103.6, 14.8);
            AssertPoint(arc.End, 0, 0, 1e-9);
        }

        [Test]
        public void HeelFlat_ShoulderArcs()
        {
            var plate = Solve(QuickTestBody());
            var arc = ArcWithRadius(plate, 116.9);

            AssertPoint(arc.Start, 28, 600);
            AssertPoint(arc.End, 111.0, 565.4);
            Assert.That(Segments(plate), Does.Contain(new LineSegment(new Point2(0, 600), new Point2(28, 600))));
            Assert.That(plate.ConstructionLines, Does.Contain(new LineSegment(new Point2(28, 597), new Point2(28, 603))));
        }

        [Test]
        public void UpperBoutAtTheNeckEnd_HeelFlatRunsToTheCenters()
        {
            var plate = Solve(With(QuickTestBody(), Body.Definitions.UpperBoutOffsetFromNeckEnd, 0));

            Assert.That(Segments(plate).OfType<ArcSegment>().Any(arc => Math.Abs(arc.Radius - 116.9) < 0.05), Is.False);
            Assert.That(Segments(plate), Does.Contain(new LineSegment(new Point2(0, 600), new Point2(40, 600))));
            Assert.That(plate.ClosedOutline, Is.Not.Null);
        }

        [Test]
        public void UpperWaistTangent()
        {
            var line = Segments(Solve(QuickTestBody())).OfType<LineSegment>().First(l => l.From.Y > 400 && l.From.X > 0);

            AssertPoint(line.From, 139.2, 481.0);
            AssertPoint(line.To, 120.3, 306.5);
        }

        [Test]
        public void LowerWaistTangent_RunsFromTheWaistDown()
        {
            var line = Segments(Solve(QuickTestBody())).OfType<LineSegment>()
                .First(l => l.From.Y is > 200 and < 300 && l.From.X > 0);

            AssertPoint(line.From, 130.6, 265.9);
            AssertPoint(line.To, 161.7, 220.8);
        }

        [Test]
        public void EveryValue_OneClosedOutlineClockwiseFromTheNeckEnd()
        {
            var plate = Solve(QuickTestBody());

            Assert.That(plate.ClosedOutline, Is.Not.Null);
            AssertJoined(plate.ClosedOutline);
            AssertPoint(plate.ClosedOutline.Start, 0, 600, 1e-9);
            Assert.That(plate.ClosedOutline.Segments[0].End.X, Is.GreaterThan(0), "starts toward the treble side");
            Assert.That(plate.ClosedOutline.Bounds, Is.EqualTo(new Bounds(-190, 0, 190, 600)).Using<Bounds>(
                (a, b) => Math.Abs(a.MinX - b.MinX) < 1e-6 && Math.Abs(a.MaxX - b.MaxX) < 1e-6 &&
                          Math.Abs(a.MinY - b.MinY) < 1e-6 && Math.Abs(a.MaxY - b.MaxY) < 1e-6));
        }

        [Test]
        public void NoSecondaryOffsets_SecondaryArcsAreThePrimaries()
        {
            var body = With(With(QuickTestBody(), Body.Definitions.UpperBoutSecondaryOffset, null),
                Body.Definitions.LowerBoutSecondaryOffset, null);
            var plate = Solve(body);

            Assert.That(plate.ClosedOutline, Is.Not.Null);
            AssertJoined(plate.ClosedOutline);
            Assert.That(plate.Radii, Has.Count.EqualTo(6));
        }

        [Test]
        public void NoHeelFlat_OutlineInOnePieceFromShoulderToShoulder()
        {
            var plate = Solve(With(QuickTestBody(), Body.Definitions.HeelFlatWidth, null));

            Assert.That(plate.ClosedOutline, Is.Null);
            var piece = plate.Outline.Single();
            AssertJoined(piece);
            Assert.That(piece.IsClosed, Is.False);
            // From the treble widest point of the upper bout round to the bass one.
            AssertPoint(piece.Start, 140, 495, 1e-9);
            AssertPoint(piece.End, -140, 495, 1e-9);
        }

        [Test]
        public void NoWaistRadius_UpperAndLowerPieces()
        {
            var plate = Solve(With(QuickTestBody(), Body.Definitions.WaistRadius, null));

            Assert.That(plate.Outline, Has.Count.EqualTo(2));
            foreach (var piece in plate.Outline)
                AssertJoined(piece);
        }

        [Test]
        public void SecondaryOverlapsWaist_StopsThePlateWithoutRepeatingTheModel()
        {
            var body = With(QuickTestBody(), Body.Definitions.LowerBoutSecondaryOffset, 70);
            var plate = Solve(body);

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages, Is.Empty);
            Assert.That(plate.Outline, Is.Empty);
            Assert.That(plate.ConstructionLines, Is.Empty);
            Assert.That(body.Validate(LengthUnit.Millimeters).Select(m => m.Text),
                Has.Some.Contains("lower bout's secondary radius overlaps"));
        }

        [Test]
        public void LowerBoutOnTheTailEnd_StraightTail()
        {
            var plate = Solve(With(QuickTestBody(), Body.Definitions.LowerBoutOffsetFromTailEnd, 0));

            Assert.That(Segments(plate), Does.Contain(new LineSegment(new Point2(70, 0), new Point2(0, 0))));
            Assert.That(Segments(plate), Does.Contain(new LineSegment(new Point2(0, 0), new Point2(-70, 0))));
            AssertJoined(plate.ClosedOutline);
        }

        [Test]
        public void FretboardTrebleEdge_TapersFromNutToSaddle()
        {
            var guitar = QuickTestGuitar(QuickTestBody());
            guitar = guitar with
            {
                Neck = guitar.Neck with
                {
                    Fretboard = guitar.Neck.Fretboard with
                    {
                        StringSpacingAtBridge = new(Fretboard.Definitions.StringSpacingAtBridge, 54),
                    },
                },
            };

            // 22 at the nut, 30.5 at the saddle, and the 14th fret is 357.69 of the 645 from the nut.
            Assert.That(FretboardShape.TrebleEdgeAtNeckEnd(guitar), Is.EqualTo(26.7137).Within(1e-4));
            Assert.That(FretboardShape.TrebleEdgeAtNeckEnd(QuickTestGuitar(QuickTestBody())), Is.EqualTo(22).Within(1e-9));
        }

        [Test]
        public void FlorentineCutaway()
        {
            // The offset line is 30 from the centerline, past the heel flat's end at 28, so the wall starts on the
            // shoulder arc. The width line at 120 meets the upper bout above its widest point, at (120, 555).
            var plate = Solve(WithCutaway(CutawayType.Florentine, 8, 90, 20));

            Assert.That(plate.Failed, Is.False, string.Join(" ", plate.Messages));
            var outline = plate.ClosedOutline;
            Assert.That(outline, Is.Not.Null);
            AssertJoined(outline);

            var wall = outline.Segments.OfType<LineSegment>().Single(l => l.From.X == 30 && l.To.X == 30);
            AssertPoint(wall.From, 30, 599.983, 1e-3);
            AssertPoint(wall.To, 30, 580, 1e-9);
            var arc = (ArcSegment)outline.Segments[outline.Segments.ToList().IndexOf(wall) + 1];
            Assert.That(arc.Radius, Is.EqualTo(48.4722).Within(1e-3));
            AssertPoint(arc.Center, 78.4722, 580, 1e-3);
            AssertPoint(arc.End, 120, 555, 1e-6);
            Assert.That(arc.IsClockwise, Is.False);
            // The outline then carries on round the upper bout, keeping its widest point.
            var next = (ArcSegment)outline.Segments[outline.Segments.ToList().IndexOf(arc) + 1];
            AssertPoint(next.Center, 40, 495, 1e-9);
            AssertPoint(next.End, 140, 495, 1e-9);
            Assert.That(outline.Bounds.MaxX, Is.EqualTo(190).Within(1e-6));
        }

        [Test]
        public void VenetianCutaway()
        {
            var body = WithCutaway(CutawayType.Venetian, 8, 90, 20);
            body = With(body, Body.Definitions.CutawayValleyRadius, 10);
            body = With(body, Body.Definitions.CutawayPointRadius, 10);
            var plate = Solve(body);

            Assert.That(plate.Failed, Is.False, string.Join(" ", plate.Messages));
            var outline = plate.ClosedOutline;
            AssertJoined(outline);

            int wall = outline.Segments.ToList().FindIndex(s => s is LineSegment l && l.From.X == 30 && l.To.X == 30);
            var valley = (ArcSegment)outline.Segments[wall + 1];
            var line = (LineSegment)outline.Segments[wall + 2];
            var point = (ArcSegment)outline.Segments[wall + 3];
            AssertPoint(valley.Center, 40, 580, 1e-9);
            Assert.That(valley.IsClockwise, Is.False);
            AssertPoint(line.From, 38.5196, 570.1102, 1e-3);
            AssertPoint(line.To, 113.4804, 558.8898, 1e-3);
            AssertPoint(point.Center, 112, 549, 1e-6);
            Assert.That(point.IsClockwise, Is.True);
            AssertPoint(point.End, 120, 555, 1e-6);
            // The point arc leaves the end point in the outline's own direction.
            var after = outline.Segments[wall + 4];
            Assert.That(point.TangentAt(1).Dot(after.TangentAt(0)), Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void CutawayFromTheHeelFlat_WallStartsAtTheNeckEnd()
        {
            var plate = Solve(WithCutaway(CutawayType.Florentine, 0, 98, 20));

            var outline = plate.ClosedOutline;
            AssertJoined(outline);
            Assert.That(outline.Segments[0], Is.EqualTo(new LineSegment(new Point2(0, 600), new Point2(22, 600))));
            Assert.That(outline.Segments[1], Is.EqualTo(new LineSegment(new Point2(22, 600), new Point2(22, 580))));
        }

        [Test]
        public void CutawayWithoutTheFretboardEdge_RemarkAndNoCutaway()
        {
            var guitar = QuickTestGuitar(WithCutaway(CutawayType.Florentine, 8, 90, 20));
            guitar = guitar with { Neck = new Neck() };
            var plate = PlateShape.Solve(guitar, Tolerance);

            Assert.That(plate.Messages.Single().Text, Is.EqualTo(
                "The cutaway is drawn once the fretboard's treble edge at the body is known: set the Nut Width (NTW), " +
                "String Spacing at Bridge (SSB), Bass String Setback (BSS) and Treble String Setback (TSS)."));
            Assert.That(plate.ClosedOutline.Bounds.MaxX, Is.EqualTo(190).Within(1e-6));
        }

        [Test]
        public void CutawayValuesMissing_OutlineWithoutIt()
        {
            var body = QuickTestBody() with { CutawayType = new(Body.Definitions.CutawayType, CutawayType.Venetian) };
            body = With(body, Body.Definitions.CutawayOffsetFromNeck, 8);
            var plate = Solve(body);

            Assert.That(plate.Failed, Is.False);
            Assert.That(plate.Messages, Is.Empty);
            Assert.That(plate.ClosedOutline, Is.Not.Null);
        }

        [Test]
        public void CutawayEndPastTheUpperBout_Error()
        {
            var plate = Solve(WithCutaway(CutawayType.Florentine, 8, 115, 20));

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages.Single().Level, Is.EqualTo(MessageLevel.Error));
            Assert.That(plate.Messages.Single().Text, Does.StartWith(
                "The cutaway's end point, 145 mm from the centerline, must be on the upper bout above its widest " +
                "point: no more than 140 mm from the centerline."));
        }

        [Test]
        public void CutawayArcAboveTheWall_Error()
        {
            // The wall meets the shoulder arc just below the neck end, so a depth of 0 starts the arc above it.
            var plate = Solve(WithCutaway(CutawayType.Florentine, 8, 90, 0));

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages.Single().Text, Does.Contain("Cutaway Depth (CTD) must be at least 0.02 mm"));
        }

        [Test]
        public void VenetianArcsOverlap_Error()
        {
            var body = WithCutaway(CutawayType.Venetian, 8, 90, 20);
            body = With(body, Body.Definitions.CutawayValleyRadius, 60);
            body = With(body, Body.Definitions.CutawayPointRadius, 40);
            var plate = Solve(body);

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages.Single().Text, Does.StartWith("The cutaway's valley and point arcs overlap"));
        }
    }
}
