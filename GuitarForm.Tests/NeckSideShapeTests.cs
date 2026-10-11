using System;
using System.Linq;
using GuitarForm.Model;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The neck in the side view: the shaft's back, the nut end, the heel and the heel shaft fillet, the heel cap and
    // the nut.
    public class NeckSideShapeTests
    {
        const double Tolerance = 0.001;

        // The side view is placed at x = 0: its back at the neck end is at x = 25, so the heel cap's outer face is at 28
        // and the heel end at 33. The heel end's front is 40 from the joint at y = 600; the heel section's is 55, 30
        // further in.
        static Guitar TestGuitar(Func<Neck, Neck> change = null) => NeckProfilesShapeTests.TestGuitar(change);

        static (NeckSideShape Neck, FretboardSideShape Fretboard) Solve(Guitar guitar)
        {
            var side = SideViewShape.Solve(guitar.Body, 0);
            var fretboard = FretboardSideShape.Solve(guitar, side);
            return (NeckSideShape.Solve(guitar, side, fretboard, Tolerance), fretboard);
        }

        static void AssertPoint(Point2 actual, double x, double y, double within = 1e-6)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(within), $"x of {actual}");
            Assert.That(actual.Y, Is.EqualTo(y).Within(within), $"y of {actual}");
        }

        static int Corners(Path outline) =>
            outline.Segments.Select((segment, i) => (segment, next: outline.Segments[(i + 1) % outline.Segments.Count]))
                .Count(pair => pair.segment.TangentAt(1).Dot(pair.next.TangentAt(0)) < 1 - 1e-9);

        // The distance from a point to the line through a segment.
        static double DistanceToLine(Point2 point, Segment line) =>
            Math.Abs((line.End - line.Start).Unit.Cross(point - line.Start));

        [Test]
        public void Outline_TopJointHeelEndFrontFilletBackAndNutEnd()
        {
            var (neck, fretboard) = Solve(TestGuitar());

            var segments = neck.Outline.Segments;
            Assert.That(neck.Outline.IsClosed, Is.True);
            Assert.That(segments, Has.Count.EqualTo(7));
            // The top runs along the fretboard's underside, from the nut's far face to the body's neck end.
            var boardToNut = (fretboard.NutUnder - fretboard.EndUnder).Unit;
            var nutEnd = fretboard.NutUnder + boardToNut * 6;
            AssertPoint(segments[0].Start, nutEnd.X, nutEnd.Y);
            Assert.That(segments[0].End.Y, Is.EqualTo(600).Within(1e-6));
            Assert.That(DistanceToLine(segments[0].End, new LineSegment(fretboard.NutUnder, fretboard.EndUnder)),
                Is.EqualTo(0).Within(1e-6));
            AssertPoint(segments[1].End, 33, 600);
            AssertPoint(segments[2].End, 33, 640);
            // The front of the heel runs through the heel section's front.
            Assert.That(DistanceToLine(new Point2(63, 655), segments[3]), Is.EqualTo(0).Within(1e-6));
            Assert.That(segments[4], Is.TypeOf<ArcSegment>());
            Assert.That(((ArcSegment)segments[4]).Radius, Is.EqualTo(15));
            // Corners at the top's ends, the heel end's ends and the nut end's far end; tangent round the heel shaft.
            Assert.That(Corners(neck.Outline), Is.EqualTo(5));
            Assert.That(neck.Messages, Is.Empty);
        }

        [Test]
        public void Back_ThroughTheThicknessesMeasuredSquareToThePlayingSurface()
        {
            var (neck, fretboard) = Solve(TestGuitar());

            var back = neck.Outline.Segments[5];
            var surface = (fretboard.EndTop - fretboard.NutTop).Unit;
            var thicknesses = neck.ConstructionLines.Take(2).ToArray();
            Assert.That(thicknesses.Select(t => t.Length), Is.EqualTo(new[] { 21.0, 23.0 }).Within(1e-6));
            foreach (var thickness in thicknesses)
            {
                Assert.That(DistanceToLine(thickness.To, back), Is.EqualTo(0).Within(1e-6));
                Assert.That((thickness.To - thickness.From).Unit.Dot(surface), Is.EqualTo(0).Within(1e-9));
            }
            // The nut end is cut square to the playing surface.
            var nutEnd = neck.Outline.Segments[6];
            Assert.That((nutEnd.End - nutEnd.Start).Unit.Dot(surface), Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void HeelShaftFillet_TangentToTheFrontAndTheBack()
        {
            var (neck, _) = Solve(TestGuitar());

            var segments = neck.Outline.Segments;
            var fillet = (ArcSegment)segments[4];
            Assert.That(DistanceToLine(fillet.Center, segments[3]), Is.EqualTo(15).Within(1e-6));
            Assert.That(DistanceToLine(fillet.Center, segments[5]), Is.EqualTo(15).Within(1e-6));
        }

        [Test]
        public void HeelShaftOffset_ArcsTheFrontThroughTheHeelEndAndSection()
        {
            var (neck, _) = Solve(TestGuitar(n => n with { HeelShaftOffset = new(Neck.Definitions.HeelShaftOffset, 10) }));

            var front = (ArcSegment)neck.Outline.Segments[3];
            AssertPoint(front.Start, 33, 640);
            Assert.That(front.Center.DistanceTo(new Point2(63, 655)), Is.EqualTo(front.Radius).Within(1e-6));
            Assert.That(Corners(neck.Outline), Is.EqualTo(5));
            // The straight line the arc is drawn from, from the heel end's front to the back of the neck.
            var straight = neck.ConstructionLines.Last();
            AssertPoint(straight.From, 33, 640);
            Assert.That(DistanceToLine(straight.To, neck.Outline.Segments[5]), Is.EqualTo(0).Within(1e-6));
            // The arc's third point is the offset along the back toward the nut.
            var back = neck.Outline.Segments[5];
            var moved = straight.To + (back.End - back.Start).Unit * 10;
            Assert.That(front.Center.DistanceTo(moved), Is.EqualTo(front.Radius).Within(1e-6));
        }

        [Test]
        public void HeelCapAndNut()
        {
            var (neck, fretboard) = Solve(TestGuitar());

            var cap = neck.HeelCap.Segments.Select(s => s.Start).ToArray();
            AssertPoint(cap[0], 28, 600);
            AssertPoint(cap[1], 33, 600);
            AssertPoint(cap[2], 33, 640);
            AssertPoint(cap[3], 28, 640);
            // The fretboard's 7 at the nut plus the 2.5 reveal, by the Nut Thickness.
            var nut = neck.Nut.Segments;
            AssertPoint(nut[0].Start, fretboard.NutUnder.X, fretboard.NutUnder.Y);
            Assert.That(nut[0].Length, Is.EqualTo(9.5).Within(1e-9));
            Assert.That(nut[1].Length, Is.EqualTo(6).Within(1e-9));
        }

        [Test]
        public void NoHeelCap_HeelEndAtTheOffsetFromBack()
        {
            var (neck, _) = Solve(TestGuitar(n => n with { HeelCap = new(Neck.Definitions.HeelCap, false) }));

            Assert.That(neck.HeelCap, Is.Null);
            AssertPoint(neck.Outline.Segments[1].End, 28, 600);
        }

        [Test]
        public void FilletCenterPastTheNeckThicknessFret_IsError()
        {
            // The 13th fret is nearer the body than where the heel meets the back of the neck.
            var (neck, _) = Solve(TestGuitar(n => n with { ThicknessFret = new(Neck.Definitions.ThicknessFret, 13) }));

            Assert.That(neck.Outline, Is.Null);
            Assert.That(neck.Messages.Select(m => m.Text), Is.EqualTo(new[]
            {
                "The Heel Shaft Radius (HLSR)'s center can't be past the Neck Thickness Fret (NKSF).",
            }));
        }

        [Test]
        public void FilletTouchingTheFrontBeforeTheHeelSection_IsError()
        {
            var (neck, _) = Solve(TestGuitar(n => n with
            {
                HeelSectionOffsetFromHeelEnd = new(Neck.Definitions.HeelSectionOffsetFromHeelEnd, 65),
            }));

            Assert.That(neck.Outline, Is.Null);
            Assert.That(neck.Messages.Select(m => m.Text), Is.EqualTo(new[]
            {
                "The Heel Shaft Radius (HLSR) can't touch the front of the heel between the heel section and the heel end.",
            }));
        }

        [Test]
        public void FrontMeetingTheBackBehindTheNeckEnd_IsError()
        {
            var (neck, _) = Solve(TestGuitar(n => n with { HeelSectionThickness = new(Neck.Definitions.HeelSectionThickness, 10) }));

            Assert.That(neck.Outline, Is.Null);
            Assert.That(neck.Messages.Single().Text, Does.StartWith("The front of the heel"));
        }

        [Test]
        public void ThicknessAtTheFirstFret_IsError()
        {
            var (neck, _) = Solve(TestGuitar(n => n with { ThicknessFret = new(Neck.Definitions.ThicknessFret, 1) }));

            Assert.That(neck.Outline, Is.Null);
            Assert.That(neck.Messages.Select(m => m.Text), Is.EqualTo(new[]
            {
                "The Neck Thickness Fret (NKSF) must be past the first fret to draw the back of the neck.",
            }));
        }

        [Test]
        public void MissingValues_AreRemarks()
        {
            var (neck, _) = Solve(TestGuitar(n => n with
            {
                HeelShaftRadius = new(Neck.Definitions.HeelShaftRadius),
                Nut = n.Nut with { Thickness = new(Nut.Definitions.Thickness) },
            }));

            Assert.That(neck.Outline, Is.Null);
            Assert.That(neck.Messages.Select(m => (m.Level, m.Text)), Is.EqualTo(new[]
            {
                (MessageLevel.Remark,
                    "Set the Nut Thickness (NTT) and Heel Shaft Radius (HLSR) to draw the neck in the side view."),
            }));
        }

        [Test]
        public void GuitarDrawing_DrawsTheNeckAndPlacesTheProfilesPastIt()
        {
            var drawing = GuitarDrawing.Draw(TestGuitar(), Tolerance, 50);

            Assert.That(drawing.OutlinePaths, Has.Member(drawing.NeckSide.Outline));
            Assert.That(drawing.OutlinePaths, Has.Member(drawing.NeckSide.Nut));
            double right = new[] { drawing.SideView.Outline, drawing.FretboardSide.Outline, drawing.NeckSide.Nut }
                .Max(path => path.Bounds.MaxX);
            Assert.That(drawing.NeckProfiles.All.Min(p => p.Outline.Bounds.MinX), Is.EqualTo(right + 50).Within(1e-6));
            Assert.That(drawing.NeckMessages, Is.Empty);
        }
    }
}
