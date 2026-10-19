using System;
using System.Linq;
using GuitarForm.Model;
using GuitarForm.Model.Parts;
using GuitarForm.Shapes;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Characterization of the guard branches of NeckSideShape's stages that NeckSideShapeTests doesn't reach. They pin
    // the messages and what is left drawn when each guard trips.
    public class NeckSideShapeBranchTests
    {
        const double Tolerance = 0.001;

        static NeckSideShape Solve(Func<Neck, Neck> change)
        {
            var guitar = NeckProfilesShapeTests.TestGuitar(change);
            var side = SideViewShape.Solve(guitar.Body, 0);
            var fretboard = FretboardSideShape.Solve(guitar, side);
            return NeckSideShape.Solve(guitar, side, fretboard, Tolerance);
        }

        static void AssertOnlyError(NeckSideShape neck, string text, int constructionLines)
        {
            Assert.That(neck.Outline, Is.Null);
            Assert.That(neck.Heel, Is.Null);
            Assert.That(neck.Shaft, Is.Null);
            Assert.That(neck.HeelCap, Is.Null);
            Assert.That(neck.Nut, Is.Null);
            Assert.That(neck.ConstructionLines, Has.Count.EqualTo(constructionLines));
            Assert.That(neck.Messages.Select(m => (m.Level, m.Text)), Is.EqualTo(new[] { (MessageLevel.Error, text) }));
        }

        const string RangeText = "The neck can't be drawn in the side view: a thickness, the nut thickness or the heel shaft radius " +
            "is zero or less, or the selected fret is past the last.";

        const string NoMeetText = "The front of the heel, through the fronts of the heel end and the heel section, doesn't meet " +
            "the back of the neck past the heel section and clear of the body.";

        const string PastSectionText = "The Heel Shaft Radius (HLSR) can't touch the front of the heel between the heel section and the heel end.";

        [Test]
        public void ThicknessZero_IsErrorBeforeAnyConstruction()
        {
            var neck = Solve(n => n with { ThicknessAtFirstFret = new(Neck.Definitions.ThicknessAtFirstFret, 0) });

            AssertOnlyError(neck, RangeText, 0);
        }

        [Test]
        public void SelectedFretPastTheLast_IsError()
        {
            var neck = Solve(n => n with { ThicknessFret = new(Neck.Definitions.ThicknessFret, 25) });

            AssertOnlyError(neck, RangeText, 0);
        }

        [Test]
        public void HeelCapThicknessMissing_IsRemarkOnlyWithAHeelCap()
        {
            var neck = Solve(n => n with { HeelCapThickness = new(Neck.Definitions.HeelCapThickness) });

            Assert.That(neck.Outline, Is.Null);
            Assert.That(neck.ConstructionLines, Is.Empty);
            Assert.That(neck.Messages.Select(m => (m.Level, m.Text)), Is.EqualTo(new[]
            {
                (MessageLevel.Remark, "Set the Heel Cap Thickness (HCPT) to draw the neck in the side view."),
            }));

            var noCap = Solve(n => n with
            {
                HeelCap = new(Neck.Definitions.HeelCap, false),
                HeelCapThickness = new(Neck.Definitions.HeelCapThickness),
            });
            Assert.That(noCap.Outline, Is.Not.Null);
            Assert.That(noCap.Messages, Is.Empty);
        }

        [Test]
        public void FrontMeetingNoWhere_IsErrorAfterTheMeasuringLines()
        {
            var neck = Solve(n => n with { HeelEndOffsetFromBack = new(Neck.Definitions.HeelEndOffsetFromBack, 100) });

            AssertOnlyError(neck, NoMeetText, 3);
        }

        [Test]
        public void FrontNearlyParallelToTheBack_IsTooShallowForTheFillet()
        {
            var neck = Solve(n => n with
            {
                HeelSectionOffsetFromHeelEnd = new(Neck.Definitions.HeelSectionOffsetFromHeelEnd, -0.21),
            });

            AssertOnlyError(neck,
                "The front of the heel and the back of the neck meet at too shallow an angle to fit the Heel Shaft Radius (HLSR).", 3);
        }

        [Test]
        public void ArcedFront_FilletTouchingBeforeTheHeelSection_IsError()
        {
            var neck = Solve(n => n with
            {
                HeelShaftOffset = new(Neck.Definitions.HeelShaftOffset, 10),
                HeelShaftRadius = new(Neck.Definitions.HeelShaftRadius, 100),
            });

            AssertOnlyError(neck, PastSectionText, 4);
        }

        [Test]
        public void ArcedFront_RadiusNotLessThanTheArc_IsErrorWithItsRadius()
        {
            var neck = Solve(n => n with
            {
                HeelShaftOffset = new(Neck.Definitions.HeelShaftOffset, 10),
                HeelShaftRadius = new(Neck.Definitions.HeelShaftRadius, 500),
            });

            AssertOnlyError(neck,
                "The Heel Shaft Radius (HLSR) must be less than the radius of the front of the heel (231.71 mm).", 4);
        }

        [Test]
        public void ArcedFront_RadiusTooLargeToFit_IsError()
        {
            var neck = Solve(n => n with
            {
                HeelShaftOffset = new(Neck.Definitions.HeelShaftOffset, 10),
                HeelShaftRadius = new(Neck.Definitions.HeelShaftRadius, 200),
            });

            AssertOnlyError(neck,
                "The Heel Shaft Radius (HLSR) is too large to fit between the front of the heel and the back of the neck.", 4);
        }

        [Test]
        public void ArcedFront_FilletCenterPastTheNeckThicknessFret_IsError()
        {
            var neck = Solve(n => n with { HeelShaftOffset = new(Neck.Definitions.HeelShaftOffset, 50) });

            AssertOnlyError(neck,
                "The Heel Shaft Radius (HLSR)'s center can't be past the Neck Thickness Fret (NKSF).", 4);
        }

        [Test]
        public void SeamOffTheJointLine_KeepsTheOutlineNutAndCapButNotTheSplit()
        {
            var neck = Solve(n => n with { ThicknessAtFirstFret = new(Neck.Definitions.ThicknessAtFirstFret, 80) });

            Assert.That(neck.Outline, Is.Not.Null);
            Assert.That(neck.Heel, Is.Null);
            Assert.That(neck.Shaft, Is.Null);
            Assert.That(neck.HeelCap, Is.Not.Null);
            Assert.That(neck.Nut, Is.Not.Null);
            Assert.That(neck.Messages.Select(m => (m.Level, m.Text)), Is.EqualTo(new[]
            {
                (MessageLevel.Error,
                    "The back of the neck, carried on to the joint line, doesn't land between the heel end and the top of " +
                    "the joint, so the neck can't be split into its shaft and heel: check the neck thicknesses and the " +
                    "heel end offset."),
            }));
        }

        [Test]
        public void NutRevealMissing_DrawsTheNeckWithoutTheNutAndRemarks()
        {
            var neck = Solve(n => n with { Nut = n.Nut with { Reveal = new(Nut.Definitions.Reveal) } });

            Assert.That(neck.Outline, Is.Not.Null);
            Assert.That(neck.Heel, Is.Not.Null);
            Assert.That(neck.HeelCap, Is.Not.Null);
            Assert.That(neck.Nut, Is.Null);
            Assert.That(neck.Messages.Select(m => (m.Level, m.Text)), Is.EqualTo(new[]
            {
                (MessageLevel.Remark, "Set the Nut Reveal (NTR) to draw the nut in the side view."),
            }));
        }
    }
}
