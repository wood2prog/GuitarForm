using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GuitarForm.Geometry;
using GuitarForm.Shapes;
using NUnit.Framework;
using Rhino.DocObjects;
using Rhino.Testing.Fixtures;

namespace GuitarForm.Tests
{
    // Checks the component style table (layer paths, names, colors), the construction layers and how a new layer is
    // colored. The expected values are written out literally, from the table before it moved to Geometry.
    [RhinoTestFixture]
    public class ComponentStylesTests
    {
        static readonly (DrawingComponent Component, string Group, string Name, int R, int G, int B)[] Expected =
        {
            (DrawingComponent.TopPlate, "Body", "Top plate", 0, 110, 200),
            (DrawingComponent.Sides, "Body", "Sides", 140, 80, 30),
            (DrawingComponent.Soundhole, "Body", "Soundhole", 190, 30, 150),
            (DrawingComponent.Bracing, "Body", "Bracing", 0, 150, 140),
            (DrawingComponent.Headblock, "Body", "Headblock", 110, 70, 160),
            (DrawingComponent.Tailblock, "Body", "Tailblock", 160, 120, 210),
            (DrawingComponent.NeckSide, "Neck", "Side view", 200, 30, 30),
            (DrawingComponent.Heel, "Neck", "Heel", 150, 0, 60),
            (DrawingComponent.HeelCap, "Neck", "Heel cap", 230, 90, 120),
            (DrawingComponent.Nut, "Neck", "Nut", 110, 110, 110),
            (DrawingComponent.NeckProfiles, "Neck", "Profiles", 0, 130, 90),
            (DrawingComponent.Fretboard, "Fretboard", "Outline", 20, 70, 170),
            (DrawingComponent.FretboardSide, "Fretboard", "Outline", 20, 70, 170),
            (DrawingComponent.Frets, "Fretboard", "Frets", 255, 140, 0),
            (DrawingComponent.FretSlots, "Fretboard", "Fret slots", 150, 120, 0),
            (DrawingComponent.FaceDots, "Fretboard", "Face dots", 220, 0, 120),
            (DrawingComponent.SideDots, "Fretboard", "Side dots", 120, 0, 200),
            (DrawingComponent.Strings, "Fretboard", "Strings", 0, 160, 0),
        };

        static DrawingComponent[] AllComponents =>
            Enum.GetValues(typeof(DrawingComponent)).Cast<DrawingComponent>().ToArray();

        static Dictionary<DrawingComponent, (Color Display, Color? Print)> NutOverride() =>
            new Dictionary<DrawingComponent, (Color Display, Color? Print)>
            {
                [DrawingComponent.Nut] = (Color.FromArgb(1, 2, 3), Color.FromArgb(4, 5, 6)),
            };

        [Test]
        public void EveryComponentHasAStyleInARootedKnownGroup()
        {
            foreach (var component in AllComponents)
            {
                var style = ComponentStyles.Of(component);
                Assert.That(style.LayerPath, Has.Length.EqualTo(3), component.ToString());
                Assert.That(style.LayerPath[0], Is.EqualTo("Guitar"), component.ToString());
                Assert.That(new[] { "Body", "Neck", "Fretboard" }, Does.Contain(style.Group), component.ToString());
                Assert.That(style.LayerPath[2], Is.Not.Empty, component.ToString());
            }
        }

        [Test]
        public void TheExpectedListCoversEveryComponent()
        {
            Assert.That(Expected.Select(e => e.Component), Is.EquivalentTo(AllComponents));
        }

        [Test]
        public void NamesPathsAndColorsMatchThePalette()
        {
            foreach (var e in Expected)
            {
                var style = ComponentStyles.Of(e.Component);
                Assert.That(style.LayerPath, Is.EqualTo(new[] { "Guitar", e.Group, e.Name }), e.Component.ToString());
                Assert.That(style.Display, Is.EqualTo(Color.FromArgb(e.R, e.G, e.B)), e.Component.ToString());
                Assert.That(style.Print, Is.Null, e.Component.ToString());
            }
        }

        [Test]
        public void ConstructionItemsGoOnTheGroupsGreyConstructionLayer()
        {
            var gray = Color.FromArgb(200, 200, 200);
            foreach (var e in Expected)
            {
                var style = ComponentStyles.For(e.Component, DrawingRole.Construction);
                Assert.That(style.LayerPath, Is.EqualTo(new[] { "Guitar", e.Group, "Construction" }), e.Component.ToString());
                Assert.That(style.Display, Is.EqualTo(gray), e.Component.ToString());
                Assert.That(ComponentStyles.DisplayColor(e.Component, DrawingRole.Construction), Is.EqualTo(gray));
            }
        }

        [Test]
        public void NonConstructionRolesUseTheComponentStyle()
        {
            foreach (var role in new[] { DrawingRole.Outline, DrawingRole.Centerline })
                foreach (var component in AllComponents)
                    Assert.That(ComponentStyles.For(component, role), Is.EqualTo(ComponentStyles.Of(component)));
        }

        [Test]
        public void PlotWeightsAndThicknesses()
        {
            Assert.That(ComponentStyles.PlotWeight(DrawingRole.Outline), Is.EqualTo(0.25));
            Assert.That(ComponentStyles.PlotWeight(DrawingRole.Construction), Is.EqualTo(0.25));
            Assert.That(ComponentStyles.PlotWeight(DrawingRole.Centerline), Is.EqualTo(0.1));
            Assert.That(ComponentStyles.Thickness(DrawingRole.Outline), Is.EqualTo(2));
            Assert.That(ComponentStyles.Thickness(DrawingRole.Centerline), Is.EqualTo(1));
        }

        [Test]
        public void AnOverrideChangesTheResultAndTheDefaultDoesNot()
        {
            var overrides = NutOverride();
            var over = ComponentStyles.Of(DrawingComponent.Nut, overrides);
            Assert.That(over.Display, Is.EqualTo(Color.FromArgb(1, 2, 3)));
            Assert.That(over.Print, Is.EqualTo(Color.FromArgb(4, 5, 6)));
            Assert.That(over.LayerPath, Is.EqualTo(new[] { "Guitar", "Neck", "Nut" }));
            Assert.That(ComponentStyles.For(DrawingComponent.Nut, DrawingRole.Outline, overrides).Display,
                Is.EqualTo(Color.FromArgb(1, 2, 3)));
            // Construction stays gray whatever the override.
            Assert.That(ComponentStyles.For(DrawingComponent.Nut, DrawingRole.Construction, overrides).Display,
                Is.EqualTo(Color.FromArgb(200, 200, 200)));
            // Other components and the default lookup are unchanged.
            Assert.That(ComponentStyles.Of(DrawingComponent.Sides, overrides).Display,
                Is.EqualTo(Color.FromArgb(140, 80, 30)));
            Assert.That(ComponentStyles.Of(DrawingComponent.Nut).Display, Is.EqualTo(Color.FromArgb(110, 110, 110)));
            Assert.That(ComponentStyles.Of(DrawingComponent.Nut).Print, Is.Null);
        }

        [Test]
        public void ANewLayerGetsTheDisplayColorAndSetsNoPlotColorOfItsOwnWithoutAPrintColor()
        {
            // Rhino's PlotColor follows the layer color until one is set, so it matches the display color here.
            var layer = new Layer { Name = "Nut" };
            ComponentStyles.ApplyNewLayerStyle(layer, ComponentStyles.Of(DrawingComponent.Nut));
            Assert.That(layer.Color, Is.EqualTo(Color.FromArgb(110, 110, 110)));
            Assert.That(layer.PlotColor.ToArgb(), Is.EqualTo(layer.Color.ToArgb()));
        }

        [Test]
        public void APrintColorReachesThePlotColor()
        {
            var layer = new Layer { Name = "Nut" };
            ComponentStyles.ApplyNewLayerStyle(layer, ComponentStyles.Of(DrawingComponent.Nut, NutOverride()));
            Assert.That(layer.Color, Is.EqualTo(Color.FromArgb(1, 2, 3)));
            Assert.That(layer.PlotColor, Is.EqualTo(Color.FromArgb(4, 5, 6)));
        }

        [Test]
        public void AParentLayerIsBlackWithNoPlotColor()
        {
            var layer = new Layer { Name = "Guitar" };
            ComponentStyles.ApplyNewLayerStyle(layer, ComponentStyles.Of(DrawingComponent.Nut, NutOverride()), leaf: false);
            Assert.That(layer.Color.ToArgb(), Is.EqualTo(Color.Black.ToArgb()));
            Assert.That(layer.PlotColor.ToArgb(), Is.EqualTo(Color.Black.ToArgb()));
        }
    }
}
