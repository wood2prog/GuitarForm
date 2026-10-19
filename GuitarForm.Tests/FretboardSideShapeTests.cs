using System;
using System.Linq;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;
using Body = GuitarForm.Model.Parts.Body;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Tests
{
    // The side dots and the fret slot lines on the fretboard's side profile.
    public class FretboardSideShapeTests
    {
        static Fretwire Wire(double? slotDepth) => new Fretwire
        {
            SlotDepth = slotDepth is double d ? new(Fretwire.Definitions.SlotDepth, d) : new(Fretwire.Definitions.SlotDepth),
        };

        // The test guitar with the Body Quick Test depths, a fretboard radius of 241, side dots of 2 (offset 3, pair spacing
        // 6) and a fretwire with a slot depth of 2 unless given.
        static Guitar Make(double sideDotOffset = 3, double? slotDepth = 2, double? slotOverage = null, bool withWire = true)
        {
            var guitar = FretboardShapeTests.TestGuitar(fretwire: withWire ? Wire(slotDepth) : null);
            var fretboard = guitar.Neck.Fretboard with
            {
                Radius = new(Fretboard.Definitions.Radius, 241),
                SideDotDiameter = new(Fretboard.Definitions.SideDotDiameter, 2),
                SideDotOffset = new(Fretboard.Definitions.SideDotOffset, sideDotOffset),
                SideDoubleDotSpacing = new(Fretboard.Definitions.SideDoubleDotSpacing, 6),
                SlotOverage = slotOverage is double o ? new(Fretboard.Definitions.SlotOverage, o) : new(Fretboard.Definitions.SlotOverage),
            };
            return guitar with
            {
                Neck = guitar.Neck with { Fretboard = fretboard },
                Body = new Body
                {
                    Length = new(Body.Definitions.Length, 600),
                    DepthAtTailEnd = new(Body.Definitions.DepthAtTailEnd, 120),
                    DepthAtNeckEnd = new(Body.Definitions.DepthAtNeckEnd, 95),
                },
            };
        }

        static FretboardSideShape Solve(Guitar guitar) =>
            FretboardSideShape.Solve(guitar, SideViewShape.Solve(guitar.Body, 240));

        // How far above the underside (square to it) the point is.
        static double Height(FretboardSideShape shape, Point2 point)
        {
            double ux = shape.NutTop.X - shape.NutUnder.X, uy = shape.NutTop.Y - shape.NutUnder.Y;
            double length = Math.Sqrt(ux * ux + uy * uy);
            return ((point.X - shape.NutUnder.X) * ux + (point.Y - shape.NutUnder.Y) * uy) / length;
        }

        // How far along the underside from the nut the point is.
        static double Along(FretboardSideShape shape, Point2 point)
        {
            double dx = shape.EndUnder.X - shape.NutUnder.X, dy = shape.EndUnder.Y - shape.NutUnder.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            return ((point.X - shape.NutUnder.X) * dx + (point.Y - shape.NutUnder.Y) * dy) / length;
        }

        [Test]
        public void SideDots_AreAtTheMarkersMidpointsTheOffsetUpFromTheUnderside()
        {
            var guitar = Make();
            var shape = Solve(guitar);

            // Fret 3 has one dot; fret 12 a pair.
            Assert.That(shape.SideDots, Has.Count.EqualTo(3));
            Assert.That(shape.SideDots.All(d => d.Radius == 1), Is.True);
            double single = (guitar.FretDistance(2).Value + guitar.FretDistance(3).Value) / 2;
            double pair = (guitar.FretDistance(11).Value + guitar.FretDistance(12).Value) / 2;
            Assert.That(Along(shape, shape.SideDots[0].Center), Is.EqualTo(single).Within(1e-6));
            Assert.That(Height(shape, shape.SideDots[0].Center), Is.EqualTo(3).Within(1e-6));
            // The pair is centered on the midpoint, 6 apart along the fretboard, at the same height.
            Assert.That(Along(shape, shape.SideDots[1].Center), Is.EqualTo(pair - 3).Within(1e-6));
            Assert.That(Along(shape, shape.SideDots[2].Center), Is.EqualTo(pair + 3).Within(1e-6));
            Assert.That(Height(shape, shape.SideDots[1].Center), Is.EqualTo(3).Within(1e-6));
            Assert.That(Height(shape, shape.SideDots[2].Center), Is.EqualTo(3).Within(1e-6));
        }

        [Test]
        public void SideDots_ALargerSpacingOnlyWidensThePair()
        {
            var guitar = Make();
            guitar = guitar with
            {
                Neck = guitar.Neck with
                {
                    Fretboard = guitar.Neck.Fretboard with
                    {
                        SideDoubleDotSpacing = new(Fretboard.Definitions.SideDoubleDotSpacing, 10),
                    },
                },
            };
            var shape = Solve(guitar);

            double pair = (guitar.FretDistance(11).Value + guitar.FretDistance(12).Value) / 2;
            Assert.That(Along(shape, shape.SideDots[1].Center), Is.EqualTo(pair - 5).Within(1e-6));
            Assert.That(Along(shape, shape.SideDots[2].Center), Is.EqualTo(pair + 5).Within(1e-6));
        }

        [Test]
        public void SideDots_WithoutADiameterOrOffset_AreNotDrawnAndRemark()
        {
            var guitar = Make();
            guitar = guitar with
            {
                Neck = guitar.Neck with
                {
                    Fretboard = guitar.Neck.Fretboard with { SideDotOffset = new(Fretboard.Definitions.SideDotOffset) },
                },
            };
            var shape = Solve(guitar);

            Assert.That(shape.SideDots, Is.Empty);
            Assert.That(shape.Messages.Single(m => m.Text.Contains("side dots")).Text,
                Is.EqualTo("Set the Side Dot Diameter (SDD) and Offset (SDO) to draw the side dots."));
        }

        [Test]
        public void SideDots_ABlankDesign_DoesNotNag()
        {
            var guitar = Make();
            guitar = guitar with
            {
                Neck = guitar.Neck with
                {
                    Fretboard = guitar.Neck.Fretboard with
                    {
                        Markers = Array.Empty<DotMarker>(),
                        SideDotDiameter = new(Fretboard.Definitions.SideDotDiameter),
                        SideDotOffset = new(Fretboard.Definitions.SideDotOffset),
                        SideDoubleDotSpacing = new(Fretboard.Definitions.SideDoubleDotSpacing),
                    },
                },
            };

            Assert.That(Solve(guitar).Messages.Any(m => m.Text.Contains("side dots")), Is.False);
        }

        [Test]
        public void SideDots_ThatWouldBreakThroughTheTopAtTheEdge_WarnPerMarker()
        {
            // At fret 12 the edge is about 5.07 thick (6.30 on the centerline less the radius's drop of 1.23); the dot's top
            // is 4.5 + 1 = 5.5. Fret 3's edge is thicker (about 5.87), so only fret 12 warns.
            var shape = Solve(Make(sideDotOffset: 4.5));

            var warning = shape.Messages.Single(m => m.Level == MessageLevel.Warning);
            Assert.That(warning.Text, Does.Contain("fret 12"));
            Assert.That(shape.SideDots, Has.Count.EqualTo(3));
        }

        [Test]
        public void SideDots_ThatStayInsideTheEdge_DontWarn()
        {
            Assert.That(Solve(Make()).Messages.Any(m => m.Level == MessageLevel.Warning), Is.False);
        }

        [Test]
        public void SlotLines_AreAtEachFretFromThePlayingSurfaceDownToOneFlatLevel()
        {
            var guitar = Make(slotDepth: 2, slotOverage: 0.5);
            var shape = Solve(guitar);

            Assert.That(shape.SlotLines, Has.Count.EqualTo(20));
            for (int i = 0; i < 20; i++)
            {
                double distance = guitar.FretDistance(i + 1).Value;
                var line = shape.SlotLines[i];
                // The top is on the tapered playing surface, square above the bottom.
                Assert.That(line.From.X, Is.EqualTo(shape.PlayingSurfaceAt(distance).X).Within(1e-6));
                Assert.That(line.From.Y, Is.EqualTo(shape.PlayingSurfaceAt(distance).Y).Within(1e-6));
                Assert.That(Along(shape, line.From), Is.EqualTo(distance).Within(1e-6));
                Assert.That(Along(shape, line.To), Is.EqualTo(distance).Within(1e-6));
                // Thickest is 7 at the nut, the depth is 2 + 0.5, so the bottom is 4.5 above the underside everywhere.
                Assert.That(Height(shape, line.To), Is.EqualTo(4.5).Within(1e-6));
            }
        }

        [Test]
        public void SlotLines_AreShorterWhereTheBoardIsThinner()
        {
            var shape = Solve(Make());

            double Length(LineSegment line) => Math.Sqrt(Math.Pow(line.To.X - line.From.X, 2) + Math.Pow(line.To.Y - line.From.Y, 2));
            // Depth 2 below the 7 at the nut: the bottom is 5 up. Fret 1 is about 6.92 thick, fret 20 about 6.01.
            Assert.That(Length(shape.SlotLines[0]), Is.EqualTo(Height(shape, shape.SlotLines[0].From) - 5).Within(1e-6));
            Assert.That(Length(shape.SlotLines[0]), Is.EqualTo(1.919).Within(0.001));
            Assert.That(Length(shape.SlotLines[19]), Is.EqualTo(1.011).Within(0.001));
            Assert.That(Length(shape.SlotLines[19]), Is.LessThan(Length(shape.SlotLines[0])));
        }

        [Test]
        public void SlotLines_TheOverageAddsToTheDepth()
        {
            double Top(double? overage) => Height(Solve(Make(slotOverage: overage)), Solve(Make(slotOverage: overage)).SlotLines[0].To);

            Assert.That(Top(null), Is.EqualTo(5).Within(1e-6));
            Assert.That(Top(0.4), Is.EqualTo(4.6).Within(1e-6));
        }

        [Test]
        public void SlotLines_WithoutASlotDepth_AreNotDrawnAndRemark()
        {
            var shape = Solve(Make(slotDepth: null));

            Assert.That(shape.SlotLines, Is.Empty);
            Assert.That(shape.Messages.Single(m => m.Text.Contains("fret slots")).Text,
                Is.EqualTo("Set the fretwire's Slot Depth (FWSD) to draw the fret slots in the side view."));
        }

        [Test]
        public void SlotLines_WithoutAFretwire_AreNotDrawnAndSayNothingHere()
        {
            var shape = Solve(Make(withWire: false));

            Assert.That(shape.SlotLines, Is.Empty);
            Assert.That(shape.Messages.Any(m => m.Text.Contains("fret slots")), Is.False);
        }

        [Test]
        public void SlotLines_ADepthThatCutsThroughTheBoard_Warns()
        {
            // 6.5 + 0.5 is the thickest 7: the bottom is on the underside.
            var shape = Solve(Make(slotDepth: 6.5, slotOverage: 0.5));

            Assert.That(shape.Messages.Any(m => m.Level == MessageLevel.Warning && m.Text.Contains("cut through")), Is.True);
            Assert.That(shape.SlotLines, Is.Empty);
        }

        [Test]
        public void SlotLines_ADepthShallowerThanTheTaper_AreLeftOutWithAWarning()
        {
            // The bottom is 6.7 up; the board is thinner than that toward the end.
            var shape = Solve(Make(slotDepth: 0.3));

            Assert.That(shape.SlotLines.Count, Is.LessThan(20));
            Assert.That(shape.Messages.Any(m => m.Level == MessageLevel.Warning && m.Text.Contains("no slot line")), Is.True);
        }

        [Test]
        public void Drawing_EmitsTheSideDotsAndSlotLinesInTheSideView()
        {
            var drawing = GuitarDrawing.Draw(Make(), 0.001, 50);

            var dots = drawing.Items.Where(i => i.Component == DrawingComponent.SideDots).ToList();
            Assert.That(dots, Has.Count.EqualTo(3));
            Assert.That(dots.All(i => i.Circle.HasValue && i.Role == DrawingRole.Outline), Is.True);
            // The 20 face slots are rectangles; the side view adds 20 single lines.
            var slots = drawing.Items.Where(i => i.Component == DrawingComponent.FretSlots && i.Path.Segments.Count == 1).ToList();
            Assert.That(slots, Has.Count.EqualTo(20));
            Assert.That(slots.All(i => i.Role == DrawingRole.Outline), Is.True);
        }
    }
}
