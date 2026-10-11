using System.Linq;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;
using Body = GuitarForm.Model.Parts.Body;

namespace GuitarForm.Tests
{
    // The fretboard over the plate (outline, frets, slots, dots and strings) and its side profile in the side view.
    public class FretboardShapeTests
    {
        // Diameters of a light steel-string set, treble first, in inches.
        static readonly double[] SixStrings = { .012, .016, .024, .032, .042, .053 };
        static readonly double[] TwelveStrings = { .010, .010, .014, .014, .023, .008, .030, .012, .039, .018, .047, .027 };

        static StringSet Set(double[] inches) => new StringSet
        {
            Strings = inches.Select(d => new GuitarString { Diameter = new(GuitarString.Definitions.Diameter, d * 25.4) }).ToArray(),
        };

        // The 14th fret at the neck end of a 600 body, 645 scale: the nut is at 957.69 and the saddle at 312.69. The
        // fretboard is 44 wide at the nut and 61 wide at the saddle.
        static Guitar TestGuitar(StringSet strings = null, Fretwire fretwire = null) => new Guitar
        {
            ScaleLength = new(Guitar.Definitions.ScaleLength, 645),
            NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, 14),
            Neck = new Neck
            {
                Angle = new(Neck.Definitions.Angle, 1.5),
                Overstand = new(Neck.Definitions.Overstand, 2),
                StringSet = strings,
                Fretwire = fretwire,
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
                    FaceDotDiameter = new(Fretboard.Definitions.FaceDotDiameter, 6),
                    DoubleDotEdgeDistance = new(Fretboard.Definitions.DoubleDotEdgeDistance, 8),
                    Markers = new[] { (3, 1), (12, 2) }.Select(m => new DotMarker
                    {
                        Fret = new(DotMarker.Definitions.Fret, m.Item1),
                        DotCount = new(DotMarker.Definitions.DotCount, m.Item2),
                    }).ToArray(),
                },
            },
            Body = new Body { Length = new(Body.Definitions.Length, 600) },
        };

        static void AssertPoint(Point2 actual, double x, double y, double within = 1e-6)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(within), $"x of {actual}");
            Assert.That(actual.Y, Is.EqualTo(y).Within(within), $"y of {actual}");
        }

        [Test]
        public void Outline_TapersFromTheNutToASquareEndPastTheLastFret()
        {
            var fretboard = FretboardShape.Solve(TestGuitar());

            var corners = fretboard.Outline.Segments.Select(s => s.Start).ToArray();
            AssertPoint(corners[0], -22, 957.685163);
            AssertPoint(corners[1], 22, 957.685163);
            AssertPoint(corners[2], 27.888559, 510.847433);
            AssertPoint(corners[3], -27.888559, 510.847433);
            Assert.That(fretboard.Outline.IsClosed, Is.True);
            Assert.That(fretboard.SaddleY, Is.EqualTo(312.685163).Within(1e-6));
        }

        [Test]
        public void Frets_EdgeToEdgeAtEachFret()
        {
            var fretboard = FretboardShape.Solve(TestGuitar());

            Assert.That(fretboard.FretLines, Has.Count.EqualTo(20));
            AssertPoint(fretboard.FretLines[0].To, 22.477068, 921.484095);
            AssertPoint(fretboard.FretLines[13].From, -FretboardShape.TrebleEdgeAtNeckEnd(TestGuitar()).Value, 600);
            Assert.That(fretboard.Slots, Is.Empty, "no slots without a fretwire");
        }

        [Test]
        public void Slots_TangThicknessWide()
        {
            var fretwire = new Fretwire { TangThickness = new(Fretwire.Definitions.TangThickness, 0.53) };
            var fretboard = FretboardShape.Solve(TestGuitar(fretwire: fretwire));

            Assert.That(fretboard.Slots, Has.Count.EqualTo(20));
            var slot = fretboard.Slots[0].Bounds;
            Assert.That(slot.MaxY - slot.MinY, Is.EqualTo(0.53).Within(1e-9));
            Assert.That((slot.MaxY + slot.MinY) / 2, Is.EqualTo(921.484095).Within(1e-6));
        }

        [Test]
        public void Dots_HalfwayBetweenFrets()
        {
            var dots = FretboardShape.Solve(TestGuitar()).Dots;

            Assert.That(dots, Has.Count.EqualTo(3));
            AssertPoint(dots[0].Center, 0, 871.189094);
            Assert.That(dots[0].Radius, Is.EqualTo(3));
            // Each of the pair is the edge distance plus its radius in from the edge.
            AssertPoint(dots[1].Center, 15.123641, 644.773587);
            AssertPoint(dots[2].Center, -15.123641, 644.773587);
        }

        [Test]
        public void Strings_WithoutASet_OnlyTheOuterTwo()
        {
            var strings = FretboardShape.Solve(TestGuitar()).Strings;

            Assert.That(strings, Has.Count.EqualTo(2));
            AssertPoint(strings[0].From, 18.5, 957.685163);
            AssertPoint(strings[0].To, 27, 312.685163);
            AssertPoint(strings[1].From, -18.5, 957.685163);
            AssertPoint(strings[1].To, -27, 312.685163);
        }

        [Test]
        public void Strings_EqualGapsBetweenEdges()
        {
            var strings = FretboardShape.Solve(TestGuitar(Set(SixStrings))).Strings;

            Assert.That(strings.Select(s => s.From.X),
                Is.EqualTo(new[] { 18.5, 11.48862, 4.32484, -3.04214, -10.63772, -18.5 }).Within(1e-6));
            Assert.That(strings.Select(s => s.To.X),
                Is.EqualTo(new[] { 27, 16.58862, 6.02484, -4.74214, -15.73772, -27 }).Within(1e-6));
        }

        [Test]
        public void Strings_CoursesOfTwo_CourseSpacingWithinEqualGapsBetween()
        {
            var guitar = TestGuitar(Set(TwelveStrings));
            guitar = guitar with
            {
                Neck = guitar.Neck with
                {
                    Fretboard = guitar.Neck.Fretboard with
                    {
                        StringsPerCourse = new(Fretboard.Definitions.StringsPerCourse, 2),
                        CourseStringSpacingAtNut = new(Fretboard.Definitions.CourseStringSpacingAtNut, 2),
                        CourseStringSpacingAtBridge = new(Fretboard.Definitions.CourseStringSpacingAtBridge, 3),
                    },
                },
            };

            var strings = FretboardShape.Solve(guitar).Strings;

            Assert.That(strings.Select(s => s.From.X), Is.EqualTo(new[]
            {
                18.5, 16.5, 11.7413, 9.7413, 4.8175, 2.8175, -2.119, -4.119, -9.2206, -11.2206, -16.5, -18.5,
            }).Within(1e-6));
        }

        [Test]
        public void MissingValues_Remark()
        {
            var guitar = TestGuitar() with { Neck = new Neck() };

            var fretboard = FretboardShape.Solve(guitar);

            Assert.That(fretboard.Outline, Is.Null);
            Assert.That(fretboard.Messages.Single().Text, Is.EqualTo(
                "Set the Nut Width (NTW), String Spacing at Bridge (SSB), Bass String Setback (BSS) and Treble String " +
                "Setback (TSS) to draw the fretboard."));
        }

        [Test]
        public void SideProfile_OverstandAboveTheTopTippedTowardTheBack()
        {
            var guitar = TestGuitar() with
            {
                Body = new Body
                {
                    Length = new(Body.Definitions.Length, 600),
                    DepthAtTailEnd = new(Body.Definitions.DepthAtTailEnd, 120),
                    DepthAtNeckEnd = new(Body.Definitions.DepthAtNeckEnd, 95),
                },
            };
            // The side view placed as it is beside the Quick Test plate: its back's tail end at x = 240.
            var side = SideViewShape.Solve(guitar.Body, 240);

            var profile = FretboardSideShape.Solve(guitar, side);

            var corners = profile.Outline.Segments.Select(s => s.Start).ToArray();
            AssertPoint(corners[0], 352.636894, 957.562594);
            AssertPoint(corners[1], 359.634495, 957.745832);
            AssertPoint(corners[2], 370.331686, 511.035045);
            AssertPoint(corners[3], 364.333742, 510.877983);
        }

        [Test]
        public void SideProfile_WaitsForTheNeckValues()
        {
            var guitar = TestGuitar() with
            {
                Neck = TestGuitar().Neck with { Angle = new(Neck.Definitions.Angle) },
                Body = new Body
                {
                    Length = new(Body.Definitions.Length, 600),
                    DepthAtTailEnd = new(Body.Definitions.DepthAtTailEnd, 120),
                    DepthAtNeckEnd = new(Body.Definitions.DepthAtNeckEnd, 95),
                },
            };

            var profile = FretboardSideShape.Solve(guitar, SideViewShape.Solve(guitar.Body, 240));

            Assert.That(profile.Outline, Is.Null);
            Assert.That(profile.Messages.Single().Text, Is.EqualTo("Set the Neck Angle (NKA) to draw the fretboard in the side view."));
        }
    }
}
