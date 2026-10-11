using System;
using System.Linq;
using GuitarForm.Model;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;
using Body = GuitarForm.Model.Parts.Body;

namespace GuitarForm.Tests
{
    // The neck's cross-sections at the first fret and the selected fret and through the heel, and where they're placed
    // beside the side view.
    public class NeckProfilesShapeTests
    {
        const double Tolerance = 0.001;
        const double Left = 1000;
        const double Gap = 50;

        // The fretboard's 645 scale and 600 body from FretboardShapeTests, with a side view and the neck's profiles. At
        // the first fret the fretboard is 44.954137 wide and 6.918984 thick; at the body's neck end it's 53.427361
        // wide.
        static Guitar TestGuitar(Func<Neck, Neck> change = null)
        {
            var neck = new Neck
            {
                Angle = new(Neck.Definitions.Angle, 1.5),
                Overstand = new(Neck.Definitions.Overstand, 2),
                ThicknessAtFirstFret = new(Neck.Definitions.ThicknessAtFirstFret, 21),
                ThicknessAtSelectedFret = new(Neck.Definitions.ThicknessAtSelectedFret, 23),
                ThicknessFret = new(Neck.Definitions.ThicknessFret, 9),
                BottomRadiusAtFirstFret = new(Neck.Definitions.BottomRadiusAtFirstFret, 30),
                ShoulderAngleAtFirstFret = new(Neck.Definitions.ShoulderAngleAtFirstFret, 70),
                MidshoulderRadiusAtFirstFret = new(Neck.Definitions.MidshoulderRadiusAtFirstFret, 10),
                BottomRadiusAtSelectedFret = new(Neck.Definitions.BottomRadiusAtSelectedFret, 35),
                ShoulderAngleAtSelectedFret = new(Neck.Definitions.ShoulderAngleAtSelectedFret, 70),
                MidshoulderRadiusAtSelectedFret = new(Neck.Definitions.MidshoulderRadiusAtSelectedFret, 10),
                HeelEndThickness = new(Neck.Definitions.HeelEndThickness, 70),
                HeelEndOffsetFromBack = new(Neck.Definitions.HeelEndOffsetFromBack, 3),
                HeelEndWidth = new(Neck.Definitions.HeelEndWidth, 60),
                HeelCap = new(Neck.Definitions.HeelCap, true),
                HeelCapThickness = new(Neck.Definitions.HeelCapThickness, 5),
                BottomRadiusAtHeelEnd = new(Neck.Definitions.BottomRadiusAtHeelEnd, 40),
                ShoulderAngleAtHeelEnd = new(Neck.Definitions.ShoulderAngleAtHeelEnd, 80),
                MidshoulderRadiusAtHeelEnd = new(Neck.Definitions.MidshoulderRadiusAtHeelEnd, 15),
                SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, 10),
                HeelSectionThickness = new(Neck.Definitions.HeelSectionThickness, 55),
                HeelSectionOffsetFromHeelEnd = new(Neck.Definitions.HeelSectionOffsetFromHeelEnd, 30),
                HeelOutlineStraightSides = new(Neck.Definitions.HeelOutlineStraightSides, true),
                BottomRadiusAtHeelSection = new(Neck.Definitions.BottomRadiusAtHeelSection, 40),
                ShoulderAngleAtHeelSection = new(Neck.Definitions.ShoulderAngleAtHeelSection, 80),
                MidshoulderRadiusAtHeelSection = new(Neck.Definitions.MidshoulderRadiusAtHeelSection, 15),
                SideLengthAtHeelSection = new(Neck.Definitions.SideLengthAtHeelSection, 8),
                Fretboard = new Fretboard
                {
                    FretCount = new(Fretboard.Definitions.FretCount, 20),
                    LastFretOverhang = new(Fretboard.Definitions.LastFretOverhang, 5),
                    NutWidth = new(Fretboard.Definitions.NutWidth, 44),
                    StringSpacingAtBridge = new(Fretboard.Definitions.StringSpacingAtBridge, 54),
                    BassStringSetback = new(Fretboard.Definitions.BassStringSetback, 3.5),
                    TrebleStringSetback = new(Fretboard.Definitions.TrebleStringSetback, 3.5),
                    ThicknessAtNut = new(Fretboard.Definitions.ThicknessAtNut, 7),
                    ThicknessAtEnd = new(Fretboard.Definitions.ThicknessAtEnd, 6),
                    Radius = new(Fretboard.Definitions.Radius, 400),
                },
            };
            return new Guitar
            {
                ScaleLength = new(Guitar.Definitions.ScaleLength, 645),
                NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, 14),
                Neck = change == null ? neck : change(neck),
                Body = new Body
                {
                    Length = new(Body.Definitions.Length, 600),
                    DepthAtTailEnd = new(Body.Definitions.DepthAtTailEnd, 120),
                    DepthAtNeckEnd = new(Body.Definitions.DepthAtNeckEnd, 95),
                },
            };
        }

        static FretboardSideShape FretboardSide(Guitar guitar) =>
            FretboardSideShape.Solve(guitar, SideViewShape.Solve(guitar.Body, 0));

        static NeckProfilesShape Solve(Guitar guitar) =>
            NeckProfilesShape.Solve(guitar, FretboardSide(guitar), Left, Gap, Tolerance);

        static void AssertPoint(Point2 actual, double x, double y, double within = 1e-6)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(within), $"x of {actual}");
            Assert.That(actual.Y, Is.EqualTo(y).Within(within), $"y of {actual}");
        }

        // The joins in a closed outline where the direction changes: corners rather than tangent joins.
        static int Corners(Path outline) =>
            outline.Segments.Select((segment, i) => (segment, next: outline.Segments[(i + 1) % outline.Segments.Count]))
                .Count(pair => pair.segment.TangentAt(1).Dot(pair.next.TangentAt(0)) < 1 - 1e-9);

        static ArcSegment BottomArc(NeckProfileShape profile, double radius) =>
            profile.Outline.Segments.OfType<ArcSegment>().Single(arc => Math.Abs(arc.Radius - radius) < 1e-9);

        [Test]
        public void FirstFret_FretboardOnTopOfAThicknessDeepProfile()
        {
            var profile = Solve(TestGuitar()).FirstFret;

            var bounds = profile.Outline.Bounds;
            Assert.That(profile.Outline.IsClosed, Is.True);
            Assert.That(bounds.MaxY - bounds.MinY, Is.EqualTo(21).Within(1e-6), "crown to the bottom arc's low point");
            Assert.That(bounds.MaxX - bounds.MinX, Is.EqualTo(44.954137).Within(1e-6), "the fretboard's width");
            Assert.That(profile.FretboardUnderside.Length, Is.EqualTo(44.954137).Within(1e-6));
            Assert.That(bounds.MaxY - profile.FretboardUnderside.From.Y, Is.EqualTo(6.918984).Within(1e-6));
            // The top arc, the fretboard's edges down to its underside, and tangent from there round the bottom.
            Assert.That(Corners(profile.Outline), Is.EqualTo(4));
            Assert.That(profile.Outline.Segments[0], Is.TypeOf<ArcSegment>());
            Assert.That(((ArcSegment)profile.Outline.Segments[0]).Radius, Is.EqualTo(400));
        }

        [Test]
        public void NeckProfiles_ShareACenterlineAndAreCenteredOnTheirFrets()
        {
            var guitar = TestGuitar();
            var side = FretboardSide(guitar);
            var profiles = Solve(guitar);

            foreach (var (profile, fret) in new[] { (profiles.FirstFret, 1), (profiles.SelectedFret, 9) })
            {
                var bounds = profile.Outline.Bounds;
                Assert.That((bounds.MinY + bounds.MaxY) / 2,
                    Is.EqualTo(side.PlayingSurfaceAt(guitar.FretDistance(fret).Value).Y).Within(1e-6), $"fret {fret}");
                Assert.That((bounds.MinX + bounds.MaxX) / 2, Is.EqualTo(Left + 25.445870).Within(1e-6),
                    $"fret {fret}'s centerline, half the selected fret's width right of the left");
            }
        }

        [Test]
        public void CenterOffset_MovesTheBottomArcTowardTheTreble()
        {
            var profiles = Solve(TestGuitar(n => n with { CenterOffsetAtFirstFret = new(Neck.Definitions.CenterOffsetAtFirstFret, 2) }));
            var profile = profiles.FirstFret;

            double crown = profile.Outline.Segments[0].PointAt(0.5).X;
            var bottom = BottomArc(profile, 30);
            Assert.That(bottom.Center.X - crown, Is.EqualTo(2).Within(1e-6));
            Assert.That(profile.Outline.Bounds.MaxY - profile.Outline.Bounds.MinY, Is.EqualTo(21).Within(1e-6));
            Assert.That(Corners(profile.Outline), Is.EqualTo(4));
        }

        [Test]
        public void IncludesFretboard_StartsTheShoulderAtTheEdgesTop()
        {
            var profile = Solve(TestGuitar(n => n with { IncludesFretboardAtFirstFret = new(Neck.Definitions.IncludesFretboardAtFirstFret, true) })).FirstFret;

            Assert.That(Corners(profile.Outline), Is.EqualTo(2));
            Assert.That(profile.Outline.Segments[1], Is.TypeOf<LineSegment>());
            // The shoulder cuts in from the edge, so the underside is narrower than the fretboard.
            Assert.That(profile.FretboardUnderside.Length, Is.LessThan(44.954137));
            Assert.That(profile.Outline.Bounds.MaxX - profile.Outline.Bounds.MinX, Is.EqualTo(44.954137).Within(1e-6));
        }

        [Test]
        public void SelectedFret_UsesTheFirstFretsSettings()
        {
            var profile = Solve(TestGuitar(n => n with
            {
                UsesFirstFretAtSelectedFret = new(Neck.Definitions.UsesFirstFretAtSelectedFret, true),
                BottomRadiusAtSelectedFret = new(Neck.Definitions.BottomRadiusAtSelectedFret),
            })).SelectedFret;

            Assert.That(BottomArc(profile, 30), Is.Not.Null);
            Assert.That(profile.Outline.Bounds.MaxY - profile.Outline.Bounds.MinY, Is.EqualTo(23).Within(1e-6));
        }

        [Test]
        public void HeelEnd_VerticalSides_BlendAtTheWorkedPoints()
        {
            // 50 wide and 50 thick, straight sides 5 long, the bottom arc's radius 30 around (0, -20) and the
            // midshoulder fillet's 10: its center is 15 from the centerline and 20 from the bottom arc's center, so
            // 13.228757 below it.
            var profiles = Solve(TestGuitar(n => n with
            {
                HeelEndWidth = new(Neck.Definitions.HeelEndWidth, 50),
                HeelEndThickness = new(Neck.Definitions.HeelEndThickness, 50),
                SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, 5),
                BottomRadiusAtHeelEnd = new(Neck.Definitions.BottomRadiusAtHeelEnd, 30),
                ShoulderAngleAtHeelEnd = new(Neck.Definitions.ShoulderAngleAtHeelEnd, 90),
                MidshoulderRadiusAtHeelEnd = new(Neck.Definitions.MidshoulderRadiusAtHeelEnd, 10),
            }));
            var segments = profiles.HeelEnd.Outline.Segments;

            // The joint along the top, 50 above the body's neck end.
            var joint = segments[0];
            var origin = new Point2((joint.Start.X + joint.End.X) / 2, joint.Start.Y);
            Assert.That(origin.Y, Is.EqualTo(650).Within(1e-6));
            Point2 Local(Point2 p) => new Point2(p.X - origin.X, p.Y - origin.Y);

            AssertPoint(Local(joint.End), 25, 0);
            AssertPoint(Local(segments[1].End), 25, -5);
            AssertPoint(Local(segments[2].End), 25, -33.228757);
            AssertPoint(Local(segments[3].End), 22.5, -39.843135);
            AssertPoint(Local(segments[4].End), -22.5, -39.843135);
            AssertPoint(Local(segments[5].End), -25, -33.228757);
            Assert.That(profiles.HeelEnd.Outline.Bounds.MinY, Is.EqualTo(600).Within(1e-6), "the front of the heel");
            Assert.That(Corners(profiles.HeelEnd.Outline), Is.EqualTo(2));
        }

        [Test]
        public void HeelSection_StraightSides_WidthOnTheLineFromHeelEndToFretboard()
        {
            // The heel end is 8 from the back (the offset plus the cap) and the fretboard's underside 97 (the depth
            // plus the overstand), so the section is 30 / 89 of the way from the heel end's 60 to the fretboard's
            // 53.427361.
            var profiles = Solve(TestGuitar());

            Assert.That(profiles.HeelSectionWidth, Is.EqualTo(57.784504).Within(1e-6));
            var bounds = profiles.HeelSection.Outline.Bounds;
            Assert.That(bounds.MaxX - bounds.MinX, Is.EqualTo(57.784504).Within(1e-6));
            Assert.That(bounds.MinY, Is.EqualTo(600).Within(1e-6));
            Assert.That(bounds.MaxY, Is.EqualTo(655).Within(1e-6));
        }

        [Test]
        public void HeelProfiles_FollowTheNeckProfilesInColumns()
        {
            var profiles = Solve(TestGuitar());

            double neckRight = Math.Max(profiles.FirstFret.Outline.Bounds.MaxX, profiles.SelectedFret.Outline.Bounds.MaxX);
            Assert.That(profiles.HeelSection.Outline.Bounds.MinX, Is.EqualTo(neckRight + Gap).Within(1e-6));
            Assert.That(profiles.HeelEnd.Outline.Bounds.MinX,
                Is.EqualTo(profiles.HeelSection.Outline.Bounds.MaxX + Gap).Within(1e-6));
            Assert.That(profiles.HeelEnd.Outline.Bounds.MaxY, Is.EqualTo(670).Within(1e-6));
            Assert.That(profiles.Messages, Is.Empty);
        }

        [Test]
        public void HeelSectionPastTheFretboard_IsError()
        {
            var profiles = Solve(TestGuitar(n => n with { HeelSectionOffsetFromHeelEnd = new(Neck.Definitions.HeelSectionOffsetFromHeelEnd, 89) }));

            Assert.That(profiles.HeelSection, Is.Null);
            Assert.That(profiles.Messages.Select(m => m.Text), Is.EqualTo(new[]
            {
                "The heel section must be between the heel end and the fretboard: the Heel Section Offset from Heel " +
                "End (HLCO) must be less than 89 mm.",
            }));
        }

        [Test]
        public void MidshoulderRadiusTooLarge_IsError()
        {
            var profiles = Solve(TestGuitar(n => n with { MidshoulderRadiusAtFirstFret = new(Neck.Definitions.MidshoulderRadiusAtFirstFret, 25) }));

            Assert.That(profiles.FirstFret, Is.Null);
            Assert.That(profiles.Messages.Single().Level, Is.EqualTo(MessageLevel.Error));
            Assert.That(profiles.Messages.Single().Text,
                Does.StartWith("At the first fret, the Profile Midshoulder Radius at First Fret (PMRF) is too large"));
        }

        [Test]
        public void ShoulderLineMissingTheBottomArc_IsError()
        {
            var profiles = Solve(TestGuitar(n => n with { BottomRadiusAtFirstFret = new(Neck.Definitions.BottomRadiusAtFirstFret, 20) }));

            Assert.That(profiles.FirstFret, Is.Null);
            Assert.That(profiles.Messages.Select(m => m.Text), Is.EqualTo(new[]
            {
                "At the first fret, the shoulder line doesn't meet the arc of the Profile Bottom Radius at First Fret (PBRF).",
            }));
        }

        [Test]
        public void NeckNoThickerThanTheFretboard_IsError()
        {
            var profiles = Solve(TestGuitar(n => n with { ThicknessAtFirstFret = new(Neck.Definitions.ThicknessAtFirstFret, 6.5) }));

            Assert.That(profiles.FirstFret, Is.Null);
            Assert.That(profiles.Messages.Select(m => m.Text), Is.EqualTo(new[]
            {
                "At the first fret, the Neck Thickness at First Fret (NKTF) must be more than the fretboard's thickness " +
                "there (6.92 mm).",
            }));
        }

        [Test]
        public void MissingValues_AreRemarks()
        {
            var profiles = Solve(TestGuitar(n => n with
            {
                BottomRadiusAtFirstFret = new(Neck.Definitions.BottomRadiusAtFirstFret),
                HeelEndWidth = new(Neck.Definitions.HeelEndWidth),
            }));

            Assert.That(profiles.FirstFret, Is.Null);
            Assert.That(profiles.HeelEnd, Is.Null);
            Assert.That(profiles.SelectedFret, Is.Not.Null);
            Assert.That(profiles.Messages.Select(m => (m.Level, m.Text)), Is.EqualTo(new[]
            {
                (MessageLevel.Remark, "Set the Profile Bottom Radius at First Fret (PBRF) to draw the neck profile at the first fret."),
                (MessageLevel.Remark, "Set the Heel End Width (HLEW) to draw the heel profile at the heel section."),
                (MessageLevel.Remark, "Set the Heel End Width (HLEW) to draw the heel profile at the heel end."),
            }));
        }

        [Test]
        public void GuitarDrawing_DrawsTheProfilesRightOfTheSideView()
        {
            var drawing = GuitarDrawing.Draw(TestGuitar(), Tolerance, Gap);

            var profiles = drawing.NeckProfiles;
            Assert.That(profiles.All.Count(), Is.EqualTo(4));
            double sideRight = Math.Max(drawing.SideView.Outline.Bounds.MaxX, drawing.FretboardSide.Outline.Bounds.MaxX);
            Assert.That(profiles.All.Min(p => p.Outline.Bounds.MinX), Is.EqualTo(sideRight + Gap).Within(1e-6));
            Assert.That(drawing.OutlinePaths, Has.Member(profiles.HeelEnd.Outline));
        }
    }
}
