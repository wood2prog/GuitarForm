using GuitarForm.Geometry;
using GuitarForm.Model;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace GuitarForm.Tests
{
    // Checks how a whole design is laid out and how its messages are grouped by section.
    [RhinoTestFixture]
    public class DesignDrawingTests
    {
        const double Tolerance = 0.001;
        const double Gap = 50;

        // The docs/Plate.md Quick Test body, with the docs/SideView.md depths. Its widest point is the lower bout, 380
        // wide, so 190 right of the centreline.
        static readonly GuitarDesign Design = new GuitarDesign
        {
            Name = "Test",
            Instrument = new Instrument { ScaleLength = 650, NeckJoinFret = 14 },
            Body = new Body
            {
                BodyLength = 600,
                UbWidth = 280,
                UbOffset = 5,
                UbRadius = 100,
                UbSecondaryOffset = 30,
                WaistRadius = 60,
                WaistWidth = 240,
                WaistOffset = 300,
                LbWidth = 380,
                LbOffset = 10,
                LbRadius = 120,
                LbSecondaryOffset = 40,
                HeelWidth = 56,
                TailDepth = 120,
                NeckDepth = 95,
            },
        };

        static int PlateOutlineCount(Body body)
        {
            var plate = PlateGeometry.Solve(body, Tolerance);
            return plate.OutlineArcs.Count + plate.OutlineLines.Count;
        }

        static int CountVerticalLinesAt(DesignDrawing drawing, double x)
        {
            int count = 0;
            foreach (var curve in drawing.Outline)
            {
                if (curve is LineCurve line &&
                    System.Math.Abs(line.Line.From.X - x) < Tolerance && System.Math.Abs(line.Line.To.X - x) < Tolerance)
                    count++;
            }
            return count;
        }

        [Test]
        public void SideView_SitsTheGapRightOfThePlatesWidestPoint()
        {
            var drawing = DesignDrawing.Draw(Design, Tolerance, Gap);

            Assert.That(drawing.SideViewOffset, Is.EqualTo(240).Within(Tolerance));
            Assert.That(CountVerticalLinesAt(drawing, 240), Is.EqualTo(1), "side view bottom line");
            Assert.That(drawing.BodyMessages, Is.Empty);
        }

        [Test]
        public void ConstructionAndOutline_AreKeptApart()
        {
            var drawing = DesignDrawing.Draw(Design, Tolerance, Gap);

            var plate = PlateGeometry.Solve(Design.Body, Tolerance);
            Assert.That(drawing.Construction, Has.Count.EqualTo(plate.ConstructionLines.Count + plate.OutlineRadii.Count));
            Assert.That(drawing.Outline, Has.Count.EqualTo(PlateOutlineCount(Design.Body) + 4), "plate outline and side view");
        }

        [Test]
        public void SideView_WithoutDepths_IsLeftOutWithARemark()
        {
            var drawing = DesignDrawing.Draw(Design with { Body = Design.Body with { TailDepth = null } }, Tolerance, Gap);

            Assert.That(drawing.SideViewOffset, Is.Null);
            Assert.That(drawing.Outline, Has.Count.EqualTo(PlateOutlineCount(Design.Body)));
            Assert.That(drawing.BodyMessages, Has.Count.EqualTo(1));
            Assert.That(drawing.BodyMessages[0].Level, Is.EqualTo(MessageLevel.Remark));
        }

        [Test]
        public void InvalidBodyLength_GivesOneBodyError()
        {
            var drawing = DesignDrawing.Draw(Design with { Body = Design.Body with { BodyLength = 0 } }, Tolerance, Gap);

            Assert.That(drawing.BodyMessages, Has.Count.EqualTo(1));
            Assert.That(drawing.BodyMessages[0].Level, Is.EqualTo(MessageLevel.Error));
            Assert.That(drawing.Construction, Is.Empty);
            Assert.That(drawing.Outline, Is.Empty);
        }

        [Test]
        public void Soundhole_IsDrawnWithTheOutline()
        {
            var design = Design with { Soundhole = new Soundhole { Diameter = 100, TailOffset = 330 } };

            var drawing = DesignDrawing.Draw(design, Tolerance, Gap);

            Assert.That(drawing.Outline, Has.Count.EqualTo(PlateOutlineCount(Design.Body) + 5));
            Assert.That(drawing.SoundholeMessages, Is.Empty);
        }

        [Test]
        public void InvalidSoundhole_GivesASoundholeErrorOnly()
        {
            var design = Design with { Soundhole = new Soundhole { Diameter = 0, TailOffset = 330 } };

            var drawing = DesignDrawing.Draw(design, Tolerance, Gap);

            Assert.That(drawing.SoundholeMessages, Has.Count.EqualTo(1));
            Assert.That(drawing.BodyMessages, Is.Empty);
            Assert.That(drawing.Outline, Has.Count.EqualTo(PlateOutlineCount(Design.Body) + 4));
        }

        [Test]
        public void InvalidInstrument_GivesInstrumentErrors()
        {
            var design = Design with { Instrument = new Instrument { ScaleLength = 0, NeckJoinFret = 0 } };

            var drawing = DesignDrawing.Draw(design, Tolerance, Gap);

            Assert.That(drawing.InstrumentMessages, Has.Count.EqualTo(2));
        }
    }
}
