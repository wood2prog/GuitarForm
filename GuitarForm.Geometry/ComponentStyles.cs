using System;
using System.Collections.Generic;
using System.Drawing;
using GuitarForm.Shapes;
using Rhino.DocObjects;

namespace GuitarForm.Geometry
{
    // How a drawn component looks: the layer it's built on (root first), its display colour (the layer colour when
    // Build creates the layer, and the preview colour) and an optional print colour (the layer's PlotColor). This is
    // the ONE place for component names, layer paths and colours: adding, renaming or recolouring a component is a
    // change to the table below. A future Colors tab will override Display and Print.
    readonly record struct ComponentStyle(string[] LayerPath, Color Display, Color? Print = null)
    {
        // The group the component belongs to (Body, Neck or Fretboard): the layer path's second name.
        public string Group => LayerPath[1];
    }

    static class ComponentStyles
    {
        public const string Root = "Guitar";
        public const string ConstructionName = "Construction";

        // Print widths in mm: centerlines are thinner than outlines.
        public const double OutlinePlotWeight = 0.25;
        public const double CenterlinePlotWeight = 0.1;

        // Display thickness in the preview, in pixels.
        public const int OutlineThickness = 2;
        public const int CenterlineThickness = 1;

        // The construction geometry's colour, the same for every group.
        public static readonly Color ConstructionColor = Color.FromArgb(200, 200, 200);

        static ComponentStyle Style(string group, string name, int r, int g, int b, Color? print = null) =>
            new ComponentStyle(new[] { Root, group, name }, Color.FromArgb(r, g, b), print);

        // Reserved for the future Colors tab: component -> (display, print). Always empty until that exists.
        static readonly IReadOnlyDictionary<DrawingComponent, (Color Display, Color? Print)> Overrides =
            new Dictionary<DrawingComponent, (Color Display, Color? Print)>();

        static readonly Dictionary<DrawingComponent, ComponentStyle> Table = new()
        {
            // Body.
            [DrawingComponent.TopPlate] = Style("Body", "Top plate", 0, 110, 200),
            [DrawingComponent.Sides] = Style("Body", "Sides", 140, 80, 30),
            [DrawingComponent.Soundhole] = Style("Body", "Soundhole", 190, 30, 150),
            [DrawingComponent.Bracing] = Style("Body", "Bracing", 0, 150, 140),
            [DrawingComponent.Headblock] = Style("Body", "Headblock", 110, 70, 160),
            [DrawingComponent.Tailblock] = Style("Body", "Tailblock", 160, 120, 210),

            // Neck.
            [DrawingComponent.NeckSide] = Style("Neck", "Side view", 200, 30, 30),
            [DrawingComponent.Heel] = Style("Neck", "Heel", 150, 0, 60),
            [DrawingComponent.HeelCap] = Style("Neck", "Heel cap", 230, 90, 120),
            [DrawingComponent.Nut] = Style("Neck", "Nut", 110, 110, 110),
            [DrawingComponent.NeckProfiles] = Style("Neck", "Profiles", 0, 130, 90),

            // Fretboard (its plan outline and side profile share a layer).
            [DrawingComponent.Fretboard] = Style("Fretboard", "Outline", 20, 70, 170),
            [DrawingComponent.FretboardSide] = Style("Fretboard", "Outline", 20, 70, 170),
            [DrawingComponent.Frets] = Style("Fretboard", "Frets", 255, 140, 0),
            [DrawingComponent.FretSlots] = Style("Fretboard", "Fret slots", 150, 120, 0),
            [DrawingComponent.FaceDots] = Style("Fretboard", "Face dots", 220, 0, 120),
            [DrawingComponent.SideDots] = Style("Fretboard", "Side dots", 120, 0, 200),
            [DrawingComponent.Strings] = Style("Fretboard", "Strings", 0, 160, 0),
        };

        public static ComponentStyle Of(DrawingComponent component) => Of(component, Overrides);

        // As Of, with the given overrides (component -> display, print) in place of the built-in (empty) set.
        internal static ComponentStyle Of(DrawingComponent component,
            IReadOnlyDictionary<DrawingComponent, (Color Display, Color? Print)> overrides)
        {
            if (!Table.TryGetValue(component, out var style))
                throw new InvalidOperationException($"No style is defined for the component {component}.");
            if (overrides != null && overrides.TryGetValue(component, out var over))
                style = style with { Display = over.Display, Print = over.Print };
            return style;
        }

        // The layer path, layer colour and print colour for a drawn item: construction items go on their group's
        // Construction layer in light grey, the rest on their component's layer.
        public static ComponentStyle For(DrawingComponent component, DrawingRole role) => For(component, role, Overrides);

        internal static ComponentStyle For(DrawingComponent component, DrawingRole role,
            IReadOnlyDictionary<DrawingComponent, (Color Display, Color? Print)> overrides)
        {
            var style = Of(component, overrides);
            return role == DrawingRole.Construction
                ? new ComponentStyle(new[] { Root, style.Group, ConstructionName }, ConstructionColor)
                : style;
        }

        // The colour a drawn item is previewed in.
        public static Color DisplayColor(DrawingComponent component, DrawingRole role) => For(component, role).Display;

        public static double PlotWeight(DrawingRole role) =>
            role == DrawingRole.Centerline ? CenterlinePlotWeight : OutlinePlotWeight;

        public static int Thickness(DrawingRole role) =>
            role == DrawingRole.Outline ? OutlineThickness : CenterlineThickness;

        // Colours a layer that Build is creating: the display colour on the leaf (parents are black), and the print
        // colour (PlotColor) only when the style has one. Never called for a layer that already exists, so a layer
        // the user has recoloured keeps its colour.
        internal static void ApplyNewLayerStyle(Layer layer, ComponentStyle style, bool leaf = true)
        {
            layer.Color = leaf ? style.Display : Color.Black;
            if (leaf && style.Print is { } print)
                layer.PlotColor = print;
        }
    }
}
