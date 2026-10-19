using System.Linq;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;
using Body = GuitarForm.Model.Parts.Body;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Tests
{
    // SideViewShape and SoundholeShape on their own, in local coordinates (GuitarDrawingTests covers them placed in a
    // whole drawing). Values are the Body tab's Quick Test (docs/Body.md): body length 600, depth at tail end 120, at
    // neck end 95, so the top is x = left + 120 and the back slants from left to left + 25 at the neck end.
    public class SideViewSoundholeShapeTests
    {
        static Body Make(double? length = 600, double? tailDepth = 120, double? neckDepth = 95, double? skew = null) => new Body
        {
            Length = new(Body.Definitions.Length, length),
            DepthAtTailEnd = new(Body.Definitions.DepthAtTailEnd, tailDepth),
            DepthAtNeckEnd = new(Body.Definitions.DepthAtNeckEnd, neckDepth),
            ProfileSkew = new(Body.Definitions.ProfileSkew, skew),
        };

        static Point2[] Corners(SideViewShape side) => side.Outline.Segments.Select(s => s.Start).ToArray();

        // --- Side view ---

        [Test]
        public void SideView_QuickTestValues_InLocalCoordinates()
        {
            var side = SideViewShape.Solve(Make(), 0);

            // Back at the tail end, top at the tail end, top at the neck end, back at the neck end (120 - 95 = 25).
            Assert.That(Corners(side), Is.EqualTo(new[] { new Point2(0, 0), new Point2(120, 0), new Point2(120, 600), new Point2(25, 600) }));
            Assert.That(side.TopAtTailEnd, Is.EqualTo(new Point2(120, 0)));
            Assert.That(side.TopAtNeckEnd, Is.EqualTo(new Point2(120, 600)));
            Assert.That(side.BackAtNeckEnd, Is.EqualTo(new Point2(25, 600)));
            Assert.That(side.Outline.IsClosed, Is.True);
            Assert.That(side.Outline.Segments, Has.Count.EqualTo(4));
            Assert.That(side.Messages, Is.Empty);
        }

        [Test]
        public void SideView_PlacedFiftyRightOfTheWidestPoint_MatchesTheDocsCoordinates()
        {
            // docs/Body.md Quick Test: with the plate's widest point at x = 190 and the 50 gap, left = 240, giving the
            // top from (360,0) to (360,600) and the back from (240,0) to (265,600).
            var side = SideViewShape.Solve(Make(), 190 + 50);

            Assert.That(side.TopAtTailEnd, Is.EqualTo(new Point2(360, 0)));
            Assert.That(side.TopAtNeckEnd, Is.EqualTo(new Point2(360, 600)));
            Assert.That(side.Outline.Start, Is.EqualTo(new Point2(240, 0)));
            Assert.That(side.BackAtNeckEnd, Is.EqualTo(new Point2(265, 600)));
        }

        [Test]
        public void SideView_ANegativeLeft_ShiftsEverything()
        {
            var side = SideViewShape.Solve(Make(), -10);

            Assert.That(side.Outline.Start, Is.EqualTo(new Point2(-10, 0)));
            Assert.That(side.TopAtTailEnd, Is.EqualTo(new Point2(110, 0)));
            Assert.That(side.Outline.Bounds.MinX, Is.EqualTo(-10));
        }

        [Test]
        public void SideView_ASkewedBack_PutsTheLeftmostPointAtLeft()
        {
            // Skew -30: the neck end's top is at 90 and its back at 90 - 95 = -5, left of the tail end's back, so
            // everything shifts 5 right to keep the leftmost point at 0.
            var side = SideViewShape.Solve(Make(skew: -30), 0);

            Assert.That(Corners(side), Is.EqualTo(new[] { new Point2(5, 0), new Point2(125, 0), new Point2(95, 600), new Point2(0, 600) }));
            Assert.That(side.Outline.Bounds.MinX, Is.EqualTo(0));
        }

        [Test]
        public void SideView_AZeroSkew_IsTheSameAsNone()
        {
            Assert.That(Corners(SideViewShape.Solve(Make(skew: 0), 0)), Is.EqualTo(Corners(SideViewShape.Solve(Make(), 0))));
        }

        [Test]
        public void SideView_MissingDepths_RemarkNamingBoth()
        {
            var body = Make(tailDepth: null, neckDepth: null);

            var side = SideViewShape.Solve(body, 0);

            Assert.That(side.Outline, Is.Null);
            var message = side.Messages.Single();
            Assert.That(message.Level, Is.EqualTo(MessageLevel.Remark));
            Assert.That(message.Text, Is.EqualTo(
                $"Set the {body.DepthAtTailEnd.Label} and {body.DepthAtNeckEnd.Label} to draw the side view."));
        }

        [Test]
        public void SideView_OneDepthMissing_IsStillARemark()
        {
            foreach (var body in new[] { Make(tailDepth: null), Make(neckDepth: null) })
            {
                var side = SideViewShape.Solve(body, 0);
                Assert.That(side.Outline, Is.Null);
                Assert.That(side.Messages.Single().Level, Is.EqualTo(MessageLevel.Remark));
            }
        }

        [Test]
        public void SideView_NonPositiveValues_AreAnError()
        {
            var cases = new[] { Make(tailDepth: 0), Make(neckDepth: 0), Make(tailDepth: -5), Make(neckDepth: -5), Make(length: 0), Make(length: -600) };
            foreach (var body in cases)
            {
                var side = SideViewShape.Solve(body, 0);

                Assert.That(side.Outline, Is.Null);
                var message = side.Messages.Single();
                Assert.That(message.Level, Is.EqualTo(MessageLevel.Error));
                Assert.That(message.Text, Does.Contain("must be greater than zero to draw the side view"));
            }
        }

        [Test]
        public void SideView_WithoutALength_IsAnErrorOnceTheDepthsAreSet()
        {
            // The length isn't one of the "set" checks, so with both depths set a missing length reports as an error.
            var side = SideViewShape.Solve(Make(length: null), 0);

            Assert.That(side.Outline, Is.Null);
            Assert.That(side.Messages.Single().Level, Is.EqualTo(MessageLevel.Error));
        }

        // --- Soundhole ---

        static TopPlate Top(bool on = true, double? diameter = 100, double? fromTail = 420, double? fromCenterline = null) => new TopPlate
        {
            Soundhole = new(TopPlate.Definitions.Soundhole, on),
            SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, diameter),
            SoundholeOffsetFromTailEnd = new(TopPlate.Definitions.SoundholeOffsetFromTailEnd, fromTail),
            SoundholeOffsetFromCenterline = new(TopPlate.Definitions.SoundholeOffsetFromCenterline, fromCenterline),
        };

        [Test]
        public void Soundhole_RadiusIsHalfTheDiameter_AndACenterlineOffsetMovesItSideways()
        {
            // docs/Soundhole.md Quick Test: diameter 100 at 420 from the tail end is radius 50 about (0, 420).
            Assert.That(SoundholeShape.Solve(Top()), Is.EqualTo(new Circle(new Point2(0, 420), 50)));
            Assert.That(SoundholeShape.Solve(Top(fromCenterline: 15)), Is.EqualTo(new Circle(new Point2(15, 420), 50)));
            Assert.That(SoundholeShape.Solve(Top(diameter: 0.1, fromTail: 0)), Is.EqualTo(new Circle(new Point2(0, 0), 0.05)));
        }

        [Test]
        public void Soundhole_SwitchedOff_IsNone()
        {
            Assert.That(SoundholeShape.Solve(Top(on: false)), Is.Null);
        }

        [Test]
        public void Soundhole_MissingOrInvalidValues_AreNone()
        {
            Assert.That(SoundholeShape.Solve(Top(diameter: null)), Is.Null);
            Assert.That(SoundholeShape.Solve(Top(diameter: -100)), Is.Null);
            Assert.That(SoundholeShape.Solve(Top(fromTail: null)), Is.Null);
        }

        [Test]
        public void Soundhole_OutsideThePlate_IsNotCheckedByTheShape()
        {
            // The shape doesn't know the plate: a soundhole far off it is still a circle (the model validates ranges).
            Assert.That(SoundholeShape.Solve(Top(fromTail: 5000, fromCenterline: 900)),
                Is.EqualTo(new Circle(new Point2(900, 5000), 50)));
        }
    }
}
