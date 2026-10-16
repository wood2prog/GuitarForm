using System;
using System.Collections.Generic;
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
        internal static Guitar TestGuitar(Func<Neck, Neck> change = null)
        {
            var neck = new Neck
            {
                Angle = new(Neck.Definitions.Angle, 1.5),
                Overstand = new(Neck.Definitions.Overstand, 2),
                ThicknessAtFirstFret = new(Neck.Definitions.ThicknessAtFirstFret, 21),
                ThicknessAtSelectedFret = new(Neck.Definitions.ThicknessAtSelectedFret, 23),
                ThicknessFret = new(Neck.Definitions.ThicknessFret, 9),
                SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, 2),
                SquarenessAtSelectedFret = new(Neck.Definitions.SquarenessAtSelectedFret, 2),
                SquarenessAtHeelEnd = new(Neck.Definitions.SquarenessAtHeelEnd, 2),
                SquarenessAtHeelSection = new(Neck.Definitions.SquarenessAtHeelSection, 2),
                HeelShaftRadius = new(Neck.Definitions.HeelShaftRadius, 15),
                HeelEndThickness = new(Neck.Definitions.HeelEndThickness, 40),
                HeelEndOffsetFromBack = new(Neck.Definitions.HeelEndOffsetFromBack, 3),
                HeelEndWidth = new(Neck.Definitions.HeelEndWidth, 60),
                HeelCap = new(Neck.Definitions.HeelCap, true),
                HeelCapThickness = new(Neck.Definitions.HeelCapThickness, 5),
                SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, 10),
                HeelSectionThickness = new(Neck.Definitions.HeelSectionThickness, 55),
                HeelSectionOffsetFromHeelEnd = new(Neck.Definitions.HeelSectionOffsetFromHeelEnd, 30),
                HeelOutlineStraightSides = new(Neck.Definitions.HeelOutlineStraightSides, true),
                SideLengthAtHeelSection = new(Neck.Definitions.SideLengthAtHeelSection, 8),
                Nut = new Nut
                {
                    Thickness = new(Nut.Definitions.Thickness, 6),
                    Reveal = new(Nut.Definitions.Reveal, 2.5),
                },
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

        static List<Point2> SidePoints(NeckProfileShape profile) =>
            profile.Outline.Segments.OfType<PolylineSegment>().SelectMany(s => s.Points).ToList();

        // The treble half's points, shoulder start to apex, and the bass half's, apex to shoulder start.
        static (IReadOnlyList<Point2> Treble, IReadOnlyList<Point2> Bass) Halves(NeckProfileShape profile)
        {
            var polylines = profile.Outline.Segments.OfType<PolylineSegment>().ToList();
            Assert.That(polylines, Has.Count.EqualTo(2));
            return (polylines[0].Points, polylines[1].Points);
        }

        static Func<Neck, Neck> Squareness(double n) =>
            neck => neck with { SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, n) };

        static Func<Neck, Neck> Advanced(double blend) =>
            neck => neck with
            {
                ProfileAdvancedMode = new(Neck.Definitions.ProfileAdvancedMode, true),
                BlendAtFirstFret = new(Neck.Definitions.BlendAtFirstFret, blend),
                // Ignored in advanced mode.
                SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, 7),
            };

        // The first fret's profile: half the fretboard's width there, with the shoulders starting at its underside.
        const double FirstFretHalf = 44.954137 / 2;
        const double FirstFretBoard = 6.918984;

        static (double CenterX, double Shoulder, double Bottom) FirstFretFrame(NeckProfileShape profile)
        {
            var bounds = profile.Outline.Bounds;
            return ((bounds.MinX + bounds.MaxX) / 2, profile.FretboardUnderside.From.Y, bounds.MinY);
        }

        [Test]
        public void FirstFret_DefaultsToARoundCWithTheShouldersAtTheFretboardsUnderside()
        {
            var profiles = Solve(TestGuitar());
            var profile = profiles.FirstFret;

            var bounds = profile.Outline.Bounds;
            Assert.That(profile.Outline.IsClosed, Is.True);
            Assert.That(bounds.MaxY - bounds.MinY, Is.EqualTo(21).Within(1e-6), "crown to the bottom");
            Assert.That(bounds.MaxX - bounds.MinX, Is.EqualTo(44.954137).Within(1e-6), "the fretboard's width");
            Assert.That(profile.FretboardUnderside.Length, Is.EqualTo(44.954137).Within(1e-6));
            Assert.That(bounds.MaxY - profile.FretboardUnderside.From.Y, Is.EqualTo(FirstFretBoard).Within(1e-6));
            Assert.That(profiles.Messages, Is.Empty, "unset squareness, asymmetry and blend use defaults silently");
            // The top arc, the fretboard's edges down to its underside, and the two halves.
            Assert.That(profile.Outline.Segments.Select(s => s.GetType()), Is.EqualTo(new[]
            {
                typeof(ArcSegment), typeof(LineSegment), typeof(PolylineSegment), typeof(PolylineSegment),
                typeof(LineSegment),
            }));
            Assert.That(((ArcSegment)profile.Outline.Segments[0]).Radius, Is.EqualTo(400));

            // A half ellipse: every point on x^2 / a^2 + y^2 / b^2 = 1 about the shoulder start's center.
            var (cx, shoulder, bottom) = FirstFretFrame(profile);
            double b = shoulder - bottom;
            Assert.That(b, Is.EqualTo(21 - FirstFretBoard).Within(1e-6));
            var points = SidePoints(profile);
            Assert.That(points.Count, Is.InRange(17, 1100));
            foreach (var p in points)
            {
                double x = (p.X - cx) / FirstFretHalf, y = (p.Y - shoulder) / b;
                Assert.That(x * x + y * y, Is.EqualTo(1).Within(1e-6), $"{p}");
            }
        }

        [Test]
        public void Profile_JoinsAtTheApexAndClosesWithoutGaps()
        {
            foreach (var change in new Func<Neck, Neck>[]
                     { n => n, Squareness(1), Squareness(3.2), Advanced(25), Advanced(100), n => n with
                         { AsymmetryAtFirstFret = new(Neck.Definitions.AsymmetryAtFirstFret, -30) } })
            {
                var path = Solve(TestGuitar(change)).FirstFret.Outline;
                var segments = path.Segments;
                for (int i = 0; i < segments.Count; i++)
                    Assert.That(segments[i].End.DistanceTo(segments[(i + 1) % segments.Count].Start), Is.LessThan(1e-9),
                        $"join {i}");
            }
        }

        [Test]
        public void Squareness1_GivesStraightShoulders()
        {
            var profile = Solve(TestGuitar(Squareness(1))).FirstFret;

            var (treble, bass) = Halves(profile);
            foreach (var half in new[] { treble, bass })
            {
                var line = half[^1] - half[0];
                foreach (var p in half)
                {
                    var along = p - half[0];
                    Assert.That(Math.Abs(line.X * along.Y - line.Y * along.X) / line.Length, Is.LessThan(1e-9));
                }
            }
            var (cx, shoulder, bottom) = FirstFretFrame(profile);
            AssertPoint(treble[0], cx + FirstFretHalf, shoulder);
            AssertPoint(treble[^1], cx, bottom);
        }

        [Test]
        public void Squareness_SquaresTheProfileAndKeepsItsSize()
        {
            var round = Solve(TestGuitar()).FirstFret;
            var square = Solve(TestGuitar(Squareness(4))).FirstFret;

            Assert.That(square.Outline.Bounds.MaxY - square.Outline.Bounds.MinY, Is.EqualTo(21).Within(1e-6));
            Assert.That(square.Outline.Bounds.MaxX - square.Outline.Bounds.MinX, Is.EqualTo(44.954137).Within(1e-6));
            // Squarer holds its width lower down: at half the depth the section is wider.
            double WidthAtHalfDepth(NeckProfileShape profile)
            {
                var (_, shoulder, bottom) = FirstFretFrame(profile);
                var crossings = profile.Outline.Segments.OfType<PolylineSegment>()
                    .SelectMany(s => s.CrossingsAtY((shoulder + bottom) / 2)).Select(c => c.Point.X).ToList();
                return crossings.Max() - crossings.Min();
            }
            Assert.That(WidthAtHalfDepth(square), Is.GreaterThan(WidthAtHalfDepth(round)));
        }

        [Test]
        public void Asymmetry_MovesTheApexTowardTheTreble()
        {
            var profile = Solve(TestGuitar(n => n with
            {
                AsymmetryAtFirstFret = new(Neck.Definitions.AsymmetryAtFirstFret, 20),
            })).FirstFret;

            var (treble, bass) = Halves(profile);
            var (cx, shoulder, bottom) = FirstFretFrame(profile);
            AssertPoint(treble[^1], cx + FirstFretHalf * 0.2, bottom);
            AssertPoint(bass[0], cx + FirstFretHalf * 0.2, bottom);
            AssertPoint(treble[0], cx + FirstFretHalf, shoulder);
            AssertPoint(bass[^1], cx - FirstFretHalf, shoulder);
            Assert.That(profile.Outline.Bounds.MaxX - profile.Outline.Bounds.MinX, Is.EqualTo(44.954137).Within(1e-6));
            // Each half is its own half ellipse about the apex's x.
            double apex = cx + FirstFretHalf * 0.2, b = shoulder - bottom;
            foreach (var (half, width) in new[] { (treble, FirstFretHalf * 0.8), (bass, FirstFretHalf * 1.2) })
                foreach (var p in half)
                {
                    double x = (p.X - apex) / width, y = (p.Y - shoulder) / b;
                    Assert.That(x * x + y * y, Is.EqualTo(1).Within(1e-6));
                }
        }

        [Test]
        public void Advanced_BlendReferencesAreSquarenessOneTwoAndThreePointTwo()
        {
            foreach (var (blend, n) in new[] { (0.0, 1.0), (50.0, 2.0), (100.0, 3.2) })
            {
                var advanced = SidePoints(Solve(TestGuitar(Advanced(blend))).FirstFret);
                var basic = SidePoints(Solve(TestGuitar(Squareness(n))).FirstFret);

                Assert.That(advanced.Count, Is.EqualTo(basic.Count), $"blend {blend}");
                for (int i = 0; i < basic.Count; i++)
                    AssertPoint(advanced[i], basic[i].X, basic[i].Y, 1e-9);
            }
        }

        [Test]
        public void Advanced_BlendBetweenReferencesLiesBetweenThem()
        {
            // How deep the section is below the shoulder at a quarter of the half-width in from the edge.
            double DepthAtQuarter(Func<Neck, Neck> change)
            {
                var profile = Solve(TestGuitar(change)).FirstFret;
                var (cx, shoulder, _) = FirstFretFrame(profile);
                var crossings = profile.Outline.Segments.OfType<PolylineSegment>().First()
                    .CrossingsAtX(cx + FirstFretHalf * 0.75);
                return shoulder - crossings.Single().Point.Y;
            }
            double v = DepthAtQuarter(Advanced(0)), mixed = DepthAtQuarter(Advanced(25)), round = DepthAtQuarter(Advanced(50));
            double between = DepthAtQuarter(Advanced(75)), d = DepthAtQuarter(Advanced(100));

            Assert.That(mixed, Is.GreaterThan(Math.Min(v, round)).And.LessThan(Math.Max(v, round)));
            Assert.That(between, Is.GreaterThan(Math.Min(round, d)).And.LessThan(Math.Max(round, d)));
            Assert.That(new[] { v, mixed, round, between, d }, Is.Ordered.Descending.Or.Ordered.Ascending);
        }

        [Test]
        public void Advanced_IgnoresSquarenessAndBasicIgnoresBlend()
        {
            var advanced = SidePoints(Solve(TestGuitar(n => Advanced(50)(n) with
            {
                SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, 1),
            })).FirstFret);
            var round = SidePoints(Solve(TestGuitar()).FirstFret);
            var basic = SidePoints(Solve(TestGuitar(n => n with
            {
                BlendAtFirstFret = new(Neck.Definitions.BlendAtFirstFret, 100),
            })).FirstFret);

            Assert.That(advanced, Is.EqualTo(round));
            Assert.That(basic, Is.EqualTo(round));
        }

        [Test]
        public void Sampling_IsDenserWhereCurvedAndBounded()
        {
            var round = SidePoints(Solve(TestGuitar()).FirstFret);
            var v = SidePoints(Solve(TestGuitar(Squareness(1))).FirstFret);
            var square = SidePoints(Solve(TestGuitar(Squareness(10))).FirstFret);

            Assert.That(v.Count, Is.LessThan(round.Count));
            Assert.That(square.Count, Is.LessThanOrEqualTo(2 * 8 * 64 + 2));
            // The chords stay within the tolerance of the curve.
            var (cx, shoulder, bottom) = FirstFretFrame(Solve(TestGuitar()).FirstFret);
            double b = shoulder - bottom;
            var polylines = Solve(TestGuitar()).FirstFret.Outline.Segments.OfType<PolylineSegment>().First();
            for (int i = 0; i < 200; i++)
            {
                var p = polylines.PointAt(i / 199.0);
                double x = (p.X - cx) / FirstFretHalf, y = (p.Y - shoulder) / b;
                // Radially the chord is inside the ellipse by at most about the tolerance.
                Assert.That(1 - Math.Sqrt(x * x + y * y), Is.InRange(-1e-9, 0.001 / 5));
            }
        }

        [Test]
        public void IncludesFretboard_StartsTheShoulderAtTheEdgesTop()
        {
            var profile = Solve(TestGuitar(n => n with { IncludesFretboardAtFirstFret = new(Neck.Definitions.IncludesFretboardAtFirstFret, true) })).FirstFret;

            Assert.That(profile.Outline.Segments.Select(s => s.GetType()),
                Is.EqualTo(new[] { typeof(ArcSegment), typeof(PolylineSegment), typeof(PolylineSegment) }));
            var (treble, _) = Halves(profile);
            var bounds = profile.Outline.Bounds;
            Assert.That(treble[0].Y, Is.EqualTo(profile.Outline.Segments[0].End.Y).Within(1e-9), "at the edge's top");
            // The shoulder curves in from the edge, so the underside is narrower than the fretboard.
            Assert.That(profile.FretboardUnderside.Length, Is.LessThan(44.954137));
            Assert.That(bounds.MaxX - bounds.MinX, Is.EqualTo(44.954137).Within(1e-6));
            Assert.That(bounds.MaxY - bounds.MinY, Is.EqualTo(21).Within(1e-6));
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
        public void SelectedFret_UsesTheFirstFretsSettings()
        {
            Func<Neck, Neck> own = n => n with
            {
                SquarenessAtFirstFret = new(Neck.Definitions.SquarenessAtFirstFret, 1),
                SquarenessAtSelectedFret = new(Neck.Definitions.SquarenessAtSelectedFret, 4),
            };
            var separate = Solve(TestGuitar(own)).SelectedFret;
            var copied = Solve(TestGuitar(n => own(n) with
            {
                UsesFirstFretAtSelectedFret = new(Neck.Definitions.UsesFirstFretAtSelectedFret, true),
            })).SelectedFret;

            Assert.That(copied.Outline.Bounds.MaxY - copied.Outline.Bounds.MinY, Is.EqualTo(23).Within(1e-6));
            // Squareness 1: straight shoulders. Squareness 4 is not.
            double Bulge(NeckProfileShape profile)
            {
                var (treble, _) = Halves(profile);
                var line = treble[^1] - treble[0];
                return treble.Max(p => Math.Abs(line.X * (p - treble[0]).Y - line.Y * (p - treble[0]).X) / line.Length);
            }
            Assert.That(Bulge(copied), Is.LessThan(1e-9));
            Assert.That(Bulge(separate), Is.GreaterThan(1));
        }

        [Test]
        public void HeelProfiles_WithNoStraightSides_StartTheirCurvesAtTheJoint()
        {
            var profiles = Solve(TestGuitar(n => n with
            {
                SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, 0),
                SideLengthAtHeelSection = new(Neck.Definitions.SideLengthAtHeelSection, 0),
            }));
            foreach (var profile in new[] { profiles.HeelEnd, profiles.HeelSection })
            {
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.Outline.IsClosed, Is.True);
                var joint = profile.Outline.Segments[0];
                var (treble, bass) = Halves(profile);
                AssertPoint(treble[0], joint.End.X, joint.End.Y);
                AssertPoint(bass[^1], joint.Start.X, joint.Start.Y);
                Assert.That(SidePoints(profile).All(p => double.IsFinite(p.X) && double.IsFinite(p.Y)), Is.True);
            }
            Assert.That(profiles.Messages.Where(m => m.Level == MessageLevel.Error), Is.Empty);
        }

        [Test]
        public void HeelEnd_IsAHalfEllipseBelowItsStraightSides()
        {
            // 50 wide and 50 thick, straight sides 5 long: the half ellipse is 25 across and 45 deep.
            var profiles = Solve(TestGuitar(n => n with
            {
                HeelEndWidth = new(Neck.Definitions.HeelEndWidth, 50),
                HeelEndThickness = new(Neck.Definitions.HeelEndThickness, 50),
                SideLengthAtHeelEnd = new(Neck.Definitions.SideLengthAtHeelEnd, 5),
            }));
            var segments = profiles.HeelEnd.Outline.Segments;

            // The joint along the top, 50 above the body's neck end.
            var joint = segments[0];
            var origin = new Point2((joint.Start.X + joint.End.X) / 2, joint.Start.Y);
            Assert.That(origin.Y, Is.EqualTo(650).Within(1e-6));
            AssertPoint(segments[1].End, origin.X + 25, origin.Y - 5);
            var (treble, bass) = Halves(profiles.HeelEnd);
            AssertPoint(treble[0], origin.X + 25, origin.Y - 5);
            AssertPoint(treble[^1], origin.X, origin.Y - 50);
            AssertPoint(bass[^1], origin.X - 25, origin.Y - 5);
            foreach (var p in SidePoints(profiles.HeelEnd))
            {
                double x = (p.X - origin.X) / 25, y = (p.Y - (origin.Y - 5)) / 45;
                Assert.That(x * x + y * y, Is.EqualTo(1).Within(1e-6));
            }
            Assert.That(profiles.HeelEnd.Outline.Bounds.MinY, Is.EqualTo(600).Within(1e-6), "the front of the heel");
            Assert.That(profiles.HeelEnd.Outline.IsClosed, Is.True);
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
            Assert.That(profiles.HeelEnd.Outline.Bounds.MaxY, Is.EqualTo(640).Within(1e-6));
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
        public void IncludesFretboard_WithASurfaceDroppingPastTheNeckThickness_IsError()
        {
            // A radius barely above the half-width drops the surface at the edge (about 21.5 mm) deeper than the 21 mm neck.
            var profiles = Solve(TestGuitar(n => n with
            {
                IncludesFretboardAtFirstFret = new(Neck.Definitions.IncludesFretboardAtFirstFret, true),
                Fretboard = n.Fretboard with { Radius = new(Fretboard.Definitions.Radius, 22.5) },
            }));

            Assert.That(profiles.FirstFret, Is.Null);
            Assert.That(profiles.Messages.Select(m => m.Text).Any(t => t.Contains("drop of the fretboard's surface")), Is.True);
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
                ThicknessAtFirstFret = new(Neck.Definitions.ThicknessAtFirstFret),
                HeelEndWidth = new(Neck.Definitions.HeelEndWidth),
            }));

            Assert.That(profiles.FirstFret, Is.Null);
            Assert.That(profiles.HeelEnd, Is.Null);
            Assert.That(profiles.SelectedFret, Is.Not.Null);
            Assert.That(profiles.Messages.Select(m => (m.Level, m.Text)), Is.EqualTo(new[]
            {
                (MessageLevel.Remark, "Set the Neck Thickness at First Fret (NKTF) to draw the neck profile at the first fret."),
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
