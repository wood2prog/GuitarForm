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
    // Characterization of the cutaway branches PlateShapeTests and the golden snapshots don't reach directly.
    public class PlateShapeCutawayTests
    {
        const double Tolerance = 0.001;

        static Body Quick() => new Body
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

        static Guitar GuitarOf(Body body, double nut = 44, double spacing = 37) => new Guitar
        {
            ScaleLength = new(Guitar.Definitions.ScaleLength, 645),
            NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, 14),
            Neck = new Neck
            {
                Fretboard = new Fretboard
                {
                    NutWidth = new(Fretboard.Definitions.NutWidth, nut),
                    StringSpacingAtBridge = new(Fretboard.Definitions.StringSpacingAtBridge, spacing),
                    BassStringSetback = new(Fretboard.Definitions.BassStringSetback, 3.5),
                    TrebleStringSetback = new(Fretboard.Definitions.TrebleStringSetback, 3.5),
                },
            },
            Body = body,
        };

        static Body With(Body body, ParameterDefinition definition, double? value) =>
            (Body)body.With(new NumberParameter(definition, value));

        static Body Cutaway(CutawayType type, double offset, double width, double depth)
        {
            var body = Quick() with { CutawayType = new(Body.Definitions.CutawayType, type) };
            body = With(body, Body.Definitions.CutawayOffsetFromNeck, offset);
            body = With(body, Body.Definitions.CutawayWidth, width);
            body = With(body, Body.Definitions.CutawayDepth, depth);
            return body;
        }

        static PlateShape Solve(Body body) => PlateShape.Solve(GuitarOf(body), Tolerance);

        [Test]
        public void InvalidValues_FailWithoutAMessage()
        {
            var plate = Solve(Cutaway(CutawayType.Florentine, 8, 0, 20));

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages, Is.Empty);
        }

        [Test]
        public void InvalidVenetianRadii_FailWithoutAMessage()
        {
            var body = Cutaway(CutawayType.Venetian, 8, 90, 20);
            body = With(body, Body.Definitions.CutawayValleyRadius, 0);
            body = With(body, Body.Definitions.CutawayPointRadius, 10);
            var plate = Solve(body);

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages, Is.Empty);
        }

        [Test]
        public void IncompleteOutline_RemarkAndNoCutaway()
        {
            var body = With(Cutaway(CutawayType.Florentine, 8, 90, 20), Body.Definitions.LowerBoutWidth, null);
            var plate = Solve(body);

            Assert.That(plate.Failed, Is.False);
            Assert.That(plate.Messages.Select(m => m.Text), Does.Contain("The cutaway is drawn once the outline is complete."));
        }

        [Test]
        public void WallMissesTheOutline_Error()
        {
            var plate = Solve(Cutaway(CutawayType.Florentine, 200, 10, 20));

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages.Single().Text, Does.StartWith("The cutaway's offset line, 222 mm from the centerline"));
        }

        [Test]
        public void FlorentineArcStartsBelowTheEndPoint_Error()
        {
            var plate = Solve(Cutaway(CutawayType.Florentine, 8, 90, 80));

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages.Single().Text, Does.StartWith("The cutaway's arc must start above its end point"));
        }

        static Body Full(CutawayType type, double offset)
        {
            var body = Cutaway(type, offset, 90, 30);
            body = With(body, Body.Definitions.CutawayValleyRadius, 10);
            return With(body, Body.Definitions.CutawayPointRadius, 10);
        }

        static (LineSegment wall, ArcSegment arc) WallAndArc(PlateShape plate, Guitar guitar)
        {
            double top = guitar.Body.Length.Value.Value;
            var segments = plate.ClosedOutline.Segments;
            var arc = segments.OfType<ArcSegment>().First(a => a.Radius > 0 && a.IsClockwise == false &&
                Math.Abs(a.Start.Y - (top - 30)) < 1e-6);
            var wall = segments.OfType<LineSegment>().First(l => l.To.DistanceTo(arc.Start) < 1e-6);
            return (wall, arc);
        }

        [TestCase(CutawayType.Florentine, 8)]
        [TestCase(CutawayType.Venetian, 8)]
        [TestCase(CutawayType.Florentine, 0)]
        [TestCase(CutawayType.Venetian, 0)]
        public void WallFollowsTheFretboardTrebleEdge(CutawayType type, double offset)
        {
            var body = Full(type, offset);
            var guitar = GuitarOf(body, 44, 54);
            var plate = PlateShape.Solve(guitar, Tolerance);
            Assert.That(plate.Failed, Is.False, string.Join("; ", plate.Messages.Select(m => m.Text)));

            var (wall, arc) = WallAndArc(plate, guitar);
            foreach (var p in new[] { wall.From, wall.To })
                Assert.That(p.X - FretboardShape.TrebleEdgeAtY(guitar, p.Y).Value, Is.EqualTo(offset).Within(Tolerance));

            // Tangent to the wall at its end: the radius is square to the wall.
            var radial = arc.Start - arc.Center;
            Assert.That(radial.Dot((wall.To - wall.From).Unit), Is.EqualTo(0).Within(Tolerance));
            if (type == CutawayType.Florentine)
            {
                var endPoint = arc.End;
                Assert.That(endPoint.X, Is.EqualTo(FretboardShape.TrebleEdgeAtNeckEnd(guitar).Value + offset + 90).Within(Tolerance));
                Assert.That(arc.Center.DistanceTo(endPoint), Is.EqualTo(arc.Radius).Within(Tolerance));
            }
        }

        [Test]
        public void NegativeTaper_FlorentineWallStaysTheOffsetFromTheEdge()
        {
            var guitar = GuitarOf(Full(CutawayType.Florentine, 8), 60, 37);
            var plate = PlateShape.Solve(guitar, Tolerance);
            Assert.That(plate.Failed, Is.False, string.Join("; ", plate.Messages.Select(m => m.Text)));

            var (wall, _) = WallAndArc(plate, guitar);
            Assert.That(FretboardShape.TrebleEdgeAtY(guitar, 0), Is.LessThan(FretboardShape.TrebleEdgeAtY(guitar, 600)));
            foreach (var p in new[] { wall.From, wall.To })
                Assert.That(p.X - FretboardShape.TrebleEdgeAtY(guitar, p.Y).Value, Is.EqualTo(8).Within(Tolerance));
        }

        [Test]
        public void SteepSlantedWall_ArcWouldSweepPastHalfATurn_Error()
        {
            var body = Cutaway(CutawayType.Florentine, 8, 90, 52);
            var plate = PlateShape.Solve(GuitarOf(body, 90, 20), Tolerance);

            Assert.That(plate.Failed, Is.True);
            Assert.That(plate.Messages.Select(m => m.Text), Has.Some.StartWith("The cutaway's arc must start above its end point"));
        }
    }
}
