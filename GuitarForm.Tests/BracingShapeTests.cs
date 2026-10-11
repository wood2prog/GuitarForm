using System;
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
    // The top's bracing in plan view: the blocks, bars, X brace, finger braces, tone bars and soundhole braces, their
    // ends against the outline and each other, and its copy seen from inside, left of the plate.
    public class BracingShapeTests
    {
        const double Tolerance = 0.001;
        const double Gap = 50;

        // The Quick Test body (docs/Body.md), with a 100 soundhole centered 380 from the tail end. The X crosses at
        // y = 300, its legs 50° from the centerline; the upper transverse bar spans y = 450 to 470, above the soundhole.
        static Guitar TestGuitar(Func<Bracing, Bracing> change = null)
        {
            var bracing = new Bracing
            {
                XBraceOffsetFromNeckEnd = new(Bracing.Definitions.XBraceOffsetFromNeckEnd, 300),
                XBraceAngle = new(Bracing.Definitions.XBraceAngle, 50),
                XBraceEdgeOffset = new(Bracing.Definitions.XBraceEdgeOffset, 5),
                XBraceWidth = new(Bracing.Definitions.XBraceWidth, 8),
                XBraceHeight = new(Bracing.Definitions.XBraceHeight, 15),
                Graft = new(Bracing.Definitions.Graft, true),
                GraftThickness = new(Bracing.Definitions.GraftThickness, 2),
                GraftWidth = new(Bracing.Definitions.GraftWidth, 60),
                GraftOffsetFromNeckEnd = new(Bracing.Definitions.GraftOffsetFromNeckEnd, 30),
                GraftEdgeOffset = new(Bracing.Definitions.GraftEdgeOffset, 10),
                UpperTransverseBarOffsetFromNeckEnd = new(Bracing.Definitions.UpperTransverseBarOffsetFromNeckEnd, 130),
                UpperTransverseBarWidth = new(Bracing.Definitions.UpperTransverseBarWidth, 20),
                UpperTransverseBarHeight = new(Bracing.Definitions.UpperTransverseBarHeight, 12),
                UpperTransverseBarEdgeOffset = new(Bracing.Definitions.UpperTransverseBarEdgeOffset, 2),
                BassFingerBraceCount = new(Bracing.Definitions.BassFingerBraceCount, 3),
                BassFingerBraceAngle = new(Bracing.Definitions.BassFingerBraceAngle, 10),
                BassFingerBraceSpacing = new(Bracing.Definitions.BassFingerBraceSpacing, 50),
                TrebleFingerBraceCount = new(Bracing.Definitions.TrebleFingerBraceCount, 2),
                TrebleFingerBraceAngle = new(Bracing.Definitions.TrebleFingerBraceAngle, 0),
                TrebleFingerBraceSpacing = new(Bracing.Definitions.TrebleFingerBraceSpacing, 60),
                FingerBraceWidth = new(Bracing.Definitions.FingerBraceWidth, 6),
                FingerBraceHeight = new(Bracing.Definitions.FingerBraceHeight, 8),
                FingerBraceEdgeOffset = new(Bracing.Definitions.FingerBraceEdgeOffset, 3),
                ToneBarCount = new(Bracing.Definitions.ToneBarCount, 2),
                ToneBarSpacing = new(Bracing.Definitions.ToneBarSpacing, 80),
                ToneBarAngle = new(Bracing.Definitions.ToneBarAngle, -20),
                ToneBarWidth = new(Bracing.Definitions.ToneBarWidth, 7),
                ToneBarHeight = new(Bracing.Definitions.ToneBarHeight, 9),
                ToneBarEdgeOffset = new(Bracing.Definitions.ToneBarEdgeOffset, 4),
                SoundholeBraces = new(Bracing.Definitions.SoundholeBraces, true),
                SoundholeBraceOffset = new(Bracing.Definitions.SoundholeBraceOffset, 3),
                SoundholeBraceWidth = new(Bracing.Definitions.SoundholeBraceWidth, 6),
                SoundholeBraceHeight = new(Bracing.Definitions.SoundholeBraceHeight, 6),
            };
            return new Guitar
            {
                Body = new Body
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
                    HeadblockWidth = new(Body.Definitions.HeadblockWidth, 70),
                    HeadblockDepth = new(Body.Definitions.HeadblockDepth, 20),
                    TailblockWidth = new(Body.Definitions.TailblockWidth, 90),
                    TailblockDepth = new(Body.Definitions.TailblockDepth, 15),
                    TopPlate = new TopPlate
                    {
                        Soundhole = new(TopPlate.Definitions.Soundhole, true),
                        SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, 100),
                        SoundholeOffsetFromTailEnd = new(TopPlate.Definitions.SoundholeOffsetFromTailEnd, 380),
                        Bracing = change == null ? bracing : change(bracing),
                    },
                },
            };
        }

        static Bracing With(Bracing bracing, ParameterDefinition definition, double? value) =>
            (Bracing)bracing.With(new NumberParameter(definition, value));

        static (BracingShape Bracing, PlateShape Plate) Solve(Guitar guitar)
        {
            var plate = PlateShape.Solve(guitar, Tolerance);
            return (BracingShape.Solve(guitar, plate, SoundholeShape.Solve(guitar.Body.TopPlate)), plate);
        }

        static Path Brace(BracingShape bracing, string name) =>
            bracing.Braces.Single(brace => brace.Name == name).Outline;

        static Point2[] Corners(Path path) => path.Segments.Select(segment => segment.Start).ToArray();

        // The shortest distance from a point to the outline, from points close together along it.
        static double DistanceToOutline(Point2 point, Path outline) =>
            outline.Segments.SelectMany(segment => Enumerable.Range(0, 20001).Select(i => segment.PointAt(i / 20000.0)))
                .Min(p => p.DistanceTo(point));

        // The distance from a point to the line through a point in a direction.
        static double DistanceToLine(Point2 point, Point2 through, Vector2 direction) =>
            Math.Abs(direction.Unit.Cross(point - through));

        static readonly double Angle = 50 * Math.PI / 180;
        static readonly Point2 Crossing = new Point2(0, 300);
        static readonly Vector2 TrebleUp = new Vector2(Math.Sin(Angle), Math.Cos(Angle));
        static readonly Vector2 TrebleDown = new Vector2(Math.Sin(Angle), -Math.Cos(Angle));

        [Test]
        public void Braces_EveryOneDrawnAsAJoinedClosedOutline()
        {
            var (bracing, _) = Solve(TestGuitar());

            Assert.That(bracing.Braces.Select(brace => brace.Name), Is.EqualTo(new[]
            {
                "The headblock", "The tailblock", "The upper transverse bar", "The graft", "The X brace",
                "The X brace's bass side upper leg", "The X brace's treble side lower leg",
                "Treble finger brace 1", "Treble finger brace 2",
                "Bass finger brace 1", "Bass finger brace 2", "Bass finger brace 3",
                "Tone bar 1", "Tone bar 2",
                "The bottom soundhole brace", "The treble soundhole brace", "The bass soundhole brace",
            }));
            foreach (var brace in bracing.Braces)
            {
                var segments = brace.Outline.Segments;
                Assert.That(brace.Outline.IsClosed, Is.True, brace.Name);
                for (int i = 0; i < segments.Count; i++)
                    Assert.That(segments[i].End.DistanceTo(segments[(i + 1) % segments.Count].Start),
                        Is.EqualTo(0).Within(1e-6), $"{brace.Name}, after segment {i}");
            }
            Assert.That(bracing.Messages, Is.Empty);
        }

        [Test]
        public void Bars_RunAcrossWithTheirEndsTheEdgeOffsetInsideTheOutline()
        {
            var (bracing, plate) = Solve(TestGuitar());

            var bar = Brace(bracing, "The upper transverse bar");
            var bounds = bar.Bounds;
            Assert.That(bounds.MinY, Is.EqualTo(450).Within(1e-6));
            Assert.That(bounds.MaxY, Is.EqualTo(470).Within(1e-6));
            // Its ends follow the outline, here the upper waist tangent line: every point on them is the edge offset
            // inside it.
            foreach (var end in new[] { bar.Segments[1], bar.Segments[3] })
                foreach (double t in new[] { 0, 0.5, 1 })
                    Assert.That(DistanceToOutline(end.PointAt(t), plate.ClosedOutline), Is.EqualTo(2).Within(0.01));

            // The graft's ends follow the upper bout's arcs.
            var graft = Brace(bracing, "The graft");
            Assert.That(graft.Bounds.MinY, Is.EqualTo(510).Within(1e-6));
            Assert.That(graft.Bounds.MaxY, Is.EqualTo(570).Within(1e-6));
            var graftEnds = graft.Segments.Where(segment => segment is ArcSegment).ToList();
            Assert.That(graftEnds, Has.Count.GreaterThanOrEqualTo(2));
            foreach (var end in graftEnds)
                foreach (double t in new[] { 0, 0.5, 1 })
                    Assert.That(DistanceToOutline(end.PointAt(t), plate.ClosedOutline), Is.EqualTo(10).Within(0.01));
        }

        [Test]
        public void Blocks_FromTheEndsWithTheirOuterSideOnTheOutline()
        {
            var (bracing, plate) = Solve(TestGuitar());

            var head = Brace(bracing, "The headblock");
            Assert.That(head.Bounds.MinY, Is.EqualTo(580).Within(1e-6));
            Assert.That(head.Bounds.MinX, Is.EqualTo(-35).Within(1e-6));
            Assert.That(head.Bounds.MaxX, Is.EqualTo(35).Within(1e-6));
            Assert.That(head.Bounds.MaxY, Is.EqualTo(600).Within(1e-6));

            var tail = Brace(bracing, "The tailblock");
            Assert.That(tail.Bounds.MaxY, Is.EqualTo(15).Within(1e-6));
            Assert.That(tail.Bounds.MinY, Is.EqualTo(0).Within(1e-6));
            // The corners against the tail end are on the curved outline.
            foreach (var corner in Corners(tail).Where(corner => corner.Y < 15 - 1e-6))
                Assert.That(DistanceToOutline(corner, plate.ClosedOutline), Is.EqualTo(0).Within(0.01));
        }

        [Test]
        public void XBrace_OneBarThroughTheCrossingAndTheOtherCutAgainstItsSides()
        {
            var (bracing, plate) = Solve(TestGuitar());

            var through = Brace(bracing, "The X brace");
            Assert.That(Corners(through).Select(c => DistanceToLine(c, Crossing, TrebleUp)), Is.All.EqualTo(4).Within(1e-6));
            Assert.That(through.Bounds.MinY, Is.LessThan(300));
            Assert.That(through.Bounds.MaxY, Is.GreaterThan(300));

            // Each cut leg starts against the through bar's side, 4 from its centerline.
            foreach (string name in new[] { "The X brace's bass side upper leg", "The X brace's treble side lower leg" })
            {
                var leg = Brace(bracing, name);
                var start = leg.Segments[^1];
                Assert.That(start, Is.TypeOf<LineSegment>(), name);
                Assert.That(DistanceToLine(start.Start, Crossing, TrebleUp), Is.EqualTo(4).Within(1e-6), name);
                Assert.That(DistanceToLine(start.End, Crossing, TrebleUp), Is.EqualTo(4).Within(1e-6), name);
                // Its other end is the edge offset inside the outline.
                var end = leg.Segments[1];
                Assert.That(DistanceToOutline(end.PointAt(0.5), plate.ClosedOutline), Is.EqualTo(5).Within(0.01), name);
            }
        }

        [Test]
        public void XBrace_UpperLegsStopAgainstTheUpperTransverseBarWhenTheyReachItFirst()
        {
            // The bar spans y = 380 to 400.
            var (bracing, _) = Solve(TestGuitar(b => (Bracing)With(b, Bracing.Definitions.UpperTransverseBarOffsetFromNeckEnd, 200)
                .With(new FlagParameter(Bracing.Definitions.SoundholeBraces, false))
                .With(new FlagParameter(Bracing.Definitions.Graft, false))));

            Assert.That(Brace(bracing, "The X brace's bass side upper leg").Bounds.MaxY, Is.EqualTo(380).Within(1e-6));
            Assert.That(Brace(bracing, "The X brace").Bounds.MaxY, Is.EqualTo(380).Within(1e-6));
        }

        [Test]
        public void FingerBraces_FromTheLowerLegsOuterSideSpacedAlongIt()
        {
            var (bracing, plate) = Solve(TestGuitar());

            // At an angle of 0 the treble ones run square to the leg, from its outer side, toward the treble side.
            for (int k = 1; k <= 2; k++)
            {
                var brace = Brace(bracing, $"Treble finger brace {k}");
                var start = brace.Segments[^1];
                var outerSide = Crossing + TrebleDown.Left * 4;
                Assert.That(DistanceToLine(start.Start, outerSide, TrebleDown), Is.EqualTo(0).Within(1e-6));
                Assert.That(DistanceToLine(start.End, outerSide, TrebleDown), Is.EqualTo(0).Within(1e-6));
                var side = brace.Segments[0];
                Assert.That((side.End - side.Start).Unit.Dot(TrebleDown), Is.EqualTo(0).Within(1e-9));
                // Centered 60 apart along the leg's centerline.
                var middle = start.PointAt(0.5);
                Assert.That((middle - Crossing).Dot(TrebleDown), Is.EqualTo(60 * k).Within(1e-6));
                Assert.That(DistanceToOutline(brace.Segments[1].PointAt(0.5), plate.ClosedOutline), Is.EqualTo(3).Within(0.01));
                Assert.That(brace.Bounds.MinX, Is.GreaterThan(0));
            }
            // The bass ones are on the bass side, turned 10° toward the neck from square to the leg.
            var bass = Brace(bracing, "Bass finger brace 1");
            Assert.That(bass.Bounds.MaxX, Is.LessThan(0));
            var bassDown = new Vector2(-TrebleDown.X, TrebleDown.Y);
            var bassSide = bass.Segments[0];
            double turned = Math.Acos((bassSide.End - bassSide.Start).Unit.Dot(bassDown.Right)) * 180 / Math.PI;
            Assert.That(turned, Is.EqualTo(10).Within(1e-6));
            Assert.That((bassSide.End - bassSide.Start).Y, Is.GreaterThan(bassDown.Right.Y * bassSide.Length));
        }

        [Test]
        public void ToneBars_FromTheTrebleLegsInnerSideTowardTheTail()
        {
            var (bracing, _) = Solve(TestGuitar());

            var bar = Brace(bracing, "Tone bar 1");
            var start = bar.Segments[^1];
            var innerSide = Crossing + TrebleDown.Right * 4;
            Assert.That(DistanceToLine(start.Start, innerSide, TrebleDown), Is.EqualTo(0).Within(1e-6));
            // Turned 20° toward the tail from square to the leg.
            var side = bar.Segments[0];
            var direction = (side.End - side.Start).Unit;
            Assert.That(Math.Acos(direction.Dot(TrebleDown.Right)) * 180 / Math.PI, Is.EqualTo(20).Within(1e-6));
            Assert.That(direction.Y, Is.LessThan(TrebleDown.Right.Y));
        }

        [Test]
        public void ToneBars_ThatWouldCrossTheBassLowerLeg_AreLeftOutWithAnError()
        {
            var (bracing, _) = Solve(TestGuitar(b => With(b, Bracing.Definitions.ToneBarAngle, 60)));

            Assert.That(bracing.Braces.Select(brace => brace.Name), Has.None.StartsWith("Tone bar"));
            Assert.That(bracing.Messages.Select(m => m.Level), Is.All.EqualTo(MessageLevel.Error));
            Assert.That(bracing.Messages[0].Text, Does.StartWith("Tone bar 1 runs into the X brace's bass side lower leg"));
        }

        [Test]
        public void FingerBraces_PastTheEndOfTheLeg_AreAnError()
        {
            var (bracing, _) = Solve(TestGuitar(b => With(b, Bracing.Definitions.TrebleFingerBraceSpacing, 200)));

            Assert.That(bracing.Braces.Select(brace => brace.Name), Has.Member("Treble finger brace 1"));
            Assert.That(bracing.Braces.Select(brace => brace.Name), Has.No.Member("Treble finger brace 2"));
            Assert.That(bracing.Messages.Single().Text, Does.StartWith(
                "Treble finger brace 2 is past the end of the X brace's treble side lower leg"));
        }

        [Test]
        public void SoundholeBraces_RunToTheNearestBrace()
        {
            var (bracing, _) = Solve(TestGuitar());

            // The bottom one sits 3 below the hole (y = 330), 6 wide, and ends against the X's upper legs.
            var bottom = Brace(bracing, "The bottom soundhole brace");
            Assert.That(bottom.Bounds.MaxY, Is.EqualTo(327).Within(1e-6));
            Assert.That(bottom.Bounds.MinY, Is.EqualTo(321).Within(1e-6));
            // Its ends' corners are on the facing sides of the upper legs: the through bar on the treble side, the cut
            // bar's leg on the bass side.
            var corners = Corners(bottom);
            foreach (var corner in corners.Where(c => c.X > 0))
                Assert.That(DistanceToLine(corner, Crossing + TrebleUp.Left * 4, TrebleUp), Is.EqualTo(0).Within(1e-6));
            var bassUp = new Vector2(-TrebleUp.X, TrebleUp.Y);
            foreach (var corner in corners.Where(c => c.X < 0))
                Assert.That(DistanceToLine(corner, Crossing - bassUp.Left * 4, bassUp), Is.EqualTo(0).Within(1e-6));

            // The side ones reach up to the upper transverse bar and down to the X's upper legs.
            foreach (string name in new[] { "The treble soundhole brace", "The bass soundhole brace" })
                Assert.That(Brace(bracing, name).Bounds.MaxY, Is.EqualTo(450).Within(1e-6), name);
            var treble = Brace(bracing, "The treble soundhole brace");
            var lowest = Corners(treble).OrderBy(c => c.Y).First();
            Assert.That(DistanceToLine(lowest, Crossing + TrebleUp.Left * 4, TrebleUp), Is.EqualTo(0).Within(1e-6));
        }

        [Test]
        public void Braces_WithTheQuickTestsVenetianCutaway_EndAlongIt()
        {
            var guitar = TestGuitar();
            var body = (Body)guitar.Body
                .With(new ChoiceParameter<CutawayType>(Body.Definitions.CutawayType, CutawayType.Venetian))
                .With(new NumberParameter(Body.Definitions.CutawayOffsetFromNeck, 8))
                .With(new NumberParameter(Body.Definitions.CutawayWidth, 90))
                .With(new NumberParameter(Body.Definitions.CutawayDepth, 20))
                .With(new NumberParameter(Body.Definitions.CutawayValleyRadius, 10))
                .With(new NumberParameter(Body.Definitions.CutawayPointRadius, 10));
            var (bracing, plate) = Solve(guitar with { Body = body });

            Assert.That(bracing.Braces, Has.Count.EqualTo(17));
            Assert.That(bracing.Messages, Is.Empty);
            var through = Brace(bracing, "The X brace");
            Assert.That(DistanceToOutline(through.Segments[1].PointAt(0.5), plate.ClosedOutline), Is.EqualTo(5).Within(0.01));
        }

        [Test]
        public void PartlySetBrace_IsLeftOutWithARemark()
        {
            var (bracing, _) = Solve(TestGuitar(b => With(b, Bracing.Definitions.XBraceWidth, null)));

            Assert.That(bracing.Braces.Select(brace => brace.Name), Has.None.Contains("X brace"));
            Assert.That(bracing.Messages.Select(m => m.Text), Is.EqualTo(new[]
            {
                "Set the X Brace Width (XBW) to draw the X brace.",
                "The finger braces and tone bars are placed on the X brace: set it to draw them.",
            }));
            Assert.That(bracing.Messages.Select(m => m.Level), Is.All.EqualTo(MessageLevel.Remark));
        }

        [Test]
        public void Drawing_PlacesTheBracingMirroredTheGapLeftOfThePlate()
        {
            var guitar = TestGuitar();
            var drawing = GuitarDrawing.Draw(guitar, Tolerance, Gap);
            var plate = drawing.Plate.ClosedOutline.Bounds;

            var copy = drawing.Bracing;
            Assert.That(copy.Plate.Bounds.MaxX, Is.EqualTo(plate.MinX - Gap).Within(1e-6));
            double centerline = copy.ConstructionLines[0].From.X;
            Assert.That(centerline, Is.EqualTo(2 * plate.MinX - Gap).Within(1e-6));
            // Seen from inside, the treble side is on the left.
            Assert.That(copy.Braces.Single(b => b.Name == "Treble finger brace 1").Outline.Bounds.MaxX, Is.LessThan(centerline));
            Assert.That(copy.Soundhole.Value.Center.X, Is.EqualTo(centerline).Within(1e-9));
            Assert.That(drawing.OutlinePaths, Has.Member(copy.Plate));
            Assert.That(drawing.BracingMessages, Is.Empty);
        }

        [Test]
        public void Drawing_WithoutBracing_HasNoCopyOfThePlate()
        {
            var drawing = GuitarDrawing.Draw(TestGuitar(_ => new Bracing()) with
            {
                Body = TestGuitar().Body with
                {
                    HeadblockWidth = new(Body.Definitions.HeadblockWidth, null),
                    TailblockWidth = new(Body.Definitions.TailblockWidth, null),
                    TopPlate = TestGuitar().Body.TopPlate with { Bracing = new Bracing() },
                },
            }, Tolerance, Gap);

            Assert.That(drawing.Bracing, Is.Null);
            Assert.That(drawing.BracingMessages.Select(m => m.Text), Is.EqualTo(new[]
            {
                "Set the Headblock Width (HBW) to draw the headblock.",
                "Set the Tailblock Width (TBW) to draw the tailblock.",
            }));
        }
    }
}
