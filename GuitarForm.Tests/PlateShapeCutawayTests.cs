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

        static Guitar GuitarOf(Body body) => new Guitar
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
    }
}
