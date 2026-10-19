using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    // One value a tab shows: which parameter of a Guitar it is and how to change it, plus how the panel presents it.
    // Pure data over the model (no UI), so tests can check the table. The label comes from the parameter's glossary
    // definition (e.g. "Body Length (BDL)"); Tooltip is the longer description. Lengths are in mm, so Step is too.
    public sealed record GuitarField(
        Func<Guitar, Parameter> Get,
        Func<Guitar, Parameter, Guitar> Set,
        string Tooltip = null,
        double Step = 0.1,
        double? PlaceholderValue = null,
        Func<Guitar, bool> Visible = null,
        Func<Guitar, bool> Enabled = null)
    {
        static readonly Guitar Template = new();

        public ParameterDefinition Definition => Get(Template).Definition;

        public string Label => Definition.Label;

        public string Abbreviation => Definition.Abbreviation;

        // A decimal number (length, angle or number) is shown on a slider.
        public bool IsDecimal => Definition.Kind is ParameterKind.Length or ParameterKind.Angle or ParameterKind.Number;

        // Choices and yes/no values are picked from a list; the other kinds are typed.
        public bool IsChoice => Get(Template).OptionTexts.Count > 0;

        // What a blank box shows: the default the value falls back to, else "required" for a required setting.
        public string PlaceholderText(IFormatProvider culture = null) =>
            PlaceholderValue is double value ? value.ToString("0.#", culture ?? CultureInfo.CurrentCulture)
            : Definition.Required ? "required" : "";

        public bool IsVisible(Guitar guitar) => Visible?.Invoke(guitar) ?? true;

        public bool IsEnabled(Guitar guitar) => Enabled?.Invoke(guitar) ?? true;
    }

    // Related fields shown together in a box on a tab, in order. HasStartingShape: the panel adds a "Starting shape"
    // list (neck profile stations). HasMarkers: the panel adds the dot marker text.
    public sealed record GuitarFieldGroup(string Tab, string Title, params GuitarField[] Fields)
    {
        public bool HasStartingShape { get; init; }
        public bool HasMarkers { get; init; }
    }

    // Every field the tabs show, grouped as the tabs lay them out. This one table is the only place that
    // declares them.
    public static class GuitarFields
    {
        public const string InstrumentTab = "Instrument";
        public const string BodyTab = "Body";
        public const string SoundholeTab = "Soundhole";
        public const string BracingTab = "Bracing";
        public const string FretboardTab = "Fretboard";
        public const string NeckTab = "Neck";

        public const string MarkersGroup = "Dot markers";
        public const string ModeGroup = "Profile mode";

        static GuitarField GuitarLevel(Func<Guitar, Parameter> get, string tooltip = null) =>
            new(get, (g, p) => (Guitar)g.With(p), tooltip);

        static GuitarField BodyField(Func<Body, Parameter> get, string tooltip = null) =>
            new(g => get(g.Body), (g, p) => g with { Body = (Body)g.Body.With(p) }, tooltip);

        static GuitarField TopPlateField(Func<TopPlate, Parameter> get, string tooltip = null) =>
            new(g => get(g.Body.TopPlate),
                (g, p) => g with { Body = g.Body with { TopPlate = (TopPlate)g.Body.TopPlate.With(p) } }, tooltip);

        static GuitarField BracingField(Func<Bracing, Parameter> get) =>
            new(g => get(g.Body.TopPlate.Bracing), (g, p) => g with
            {
                Body = g.Body with
                {
                    TopPlate = g.Body.TopPlate with { Bracing = (Bracing)g.Body.TopPlate.Bracing.With(p) },
                },
            });

        static GuitarField FretboardField(Func<Fretboard, Parameter> get) =>
            new(g => get(g.Neck.Fretboard),
                (g, p) => g with { Neck = g.Neck with { Fretboard = (Fretboard)g.Neck.Fretboard.With(p) } });

        static GuitarField NeckField(Func<Neck, Parameter> get) =>
            new(g => get(g.Neck), (g, p) => g with { Neck = (Neck)g.Neck.With(p) });

        static GuitarField NutField(Func<Nut, Parameter> get) =>
            new(g => get(g.Neck.Nut), (g, p) => g with { Neck = g.Neck with { Nut = (Nut)g.Neck.Nut.With(p) } });

        // The soundhole's values apply only when the guitar has one (they keep their values while it doesn't).
        static bool HasSoundhole(Guitar g) => g.Body.TopPlate.Soundhole.Value;

        // The course spacings apply only to courses of two strings.
        static bool TwoStringCourses(Guitar g) => g.Neck.Fretboard.StringsPerCourse.Value == 2;

        static bool SimpleProfile(Guitar g) => !g.Neck.ProfileAdvancedMode.Value;
        static bool AdvancedProfile(Guitar g) => g.Neck.ProfileAdvancedMode.Value;

        static GuitarField SquarenessField(Func<Neck, Parameter> get) => NeckField(get) with
        {
            Visible = SimpleProfile,
            PlaceholderValue = Neck.DefaultSquareness,
        };

        static GuitarField AsymmetryField(Func<Neck, Parameter> get) => NeckField(get) with
        {
            Step = 1,
            PlaceholderValue = Neck.DefaultAsymmetry,
        };

        static GuitarField BlendField(Func<Neck, Parameter> get) => NeckField(get) with
        {
            Step = 1,
            Visible = AdvancedProfile,
            PlaceholderValue = Neck.DefaultBlend,
        };

        public static readonly IReadOnlyList<GuitarFieldGroup> Groups = new GuitarFieldGroup[]
        {
            new(InstrumentTab, "Scale",
                GuitarLevel(g => g.ScaleLength, "Distance from the nut to the saddle"),
                GuitarLevel(g => g.NeckJoinFret, "The fret at which the neck joins the body")),

            new(BodyTab, "Outline",
                BodyField(b => b.Length, "Overall length of the guitar body"),
                BodyField(b => b.HeelFlatWidth, "Width of the flat at the top of the body where the heel of the neck attaches")),
            new(BodyTab, "Upper bout",
                BodyField(b => b.UpperBoutWidth, "Width of the upper bout"),
                BodyField(b => b.UpperBoutOffsetFromNeckEnd, "Offset of the upper bout line down from the upper bout primary radius center"),
                BodyField(b => b.UpperBoutPrimaryRadius, "Primary radius of the upper bout. Its center sits one radius below the top of the body"),
                BodyField(b => b.UpperBoutSecondaryOffset, "Moves the upper bout secondary radius center toward the body center and grows its radius by the same amount. 0 = same as the primary radius")),
            new(BodyTab, "Waist",
                BodyField(b => b.WaistWidth, "Width of the body at the waist"),
                BodyField(b => b.WaistRadius, "Radius of the waist curve. Its center sits on the waist center line, one radius outside the waist width"),
                BodyField(b => b.WaistOffsetFromTailEnd, "Distance of the waist center line up from the tail end")),
            new(BodyTab, "Lower bout",
                BodyField(b => b.LowerBoutWidth, "Width of the lower bout"),
                BodyField(b => b.LowerBoutOffsetFromTailEnd, "Offset of the lower bout center line up from the lower bout primary radius center"),
                BodyField(b => b.LowerBoutPrimaryRadius, "Primary radius of the lower bout. Its center sits one radius above the tail end"),
                BodyField(b => b.LowerBoutSecondaryOffset, "Moves the lower bout secondary radius center toward the body center and grows its radius by the same amount. 0 = same as the primary radius")),
            new(BodyTab, "Side view",
                BodyField(b => b.DepthAtTailEnd, "Depth of the body at the tail end (side view)"),
                BodyField(b => b.DepthAtNeckEnd, "Depth of the body at the neck end (side view)"),
                BodyField(b => b.ProfileSkew)),
            new(BodyTab, "Cutaway",
                BodyField(b => b.CutawayType),
                BodyField(b => b.CutawayOffsetFromNeck),
                BodyField(b => b.CutawayWidth),
                BodyField(b => b.CutawayDepth),
                BodyField(b => b.CutawayValleyRadius),
                BodyField(b => b.CutawayPointRadius)),
            new(BodyTab, "Blocks",
                BodyField(b => b.HeadblockWidth),
                BodyField(b => b.HeadblockDepth),
                BodyField(b => b.TailblockWidth),
                BodyField(b => b.TailblockDepth)),

            // The yes/no value sits in a group of its own; the old tab had a check box above the groups.
            new(SoundholeTab, "Soundhole",
                TopPlateField(t => t.Soundhole, "Whether the top plate has a soundhole")),
            new(SoundholeTab, "Size and position",
                TopPlateField(t => t.SoundholeDiameter, "Diameter of the soundhole") with { Enabled = HasSoundhole },
                TopPlateField(t => t.SoundholeOffsetFromCenterline, "Distance of the soundhole center from the body centerline (+ is right). Blank = centered") with { Enabled = HasSoundhole },
                TopPlateField(t => t.SoundholeOffsetFromTailEnd, "Distance of the soundhole center up from the tail end") with { Enabled = HasSoundhole }),

            new(BracingTab, "X brace",
                BracingField(b => b.XBraceOffsetFromNeckEnd),
                BracingField(b => b.XBraceAngle),
                BracingField(b => b.XBraceEdgeOffset),
                BracingField(b => b.XBraceWidth),
                BracingField(b => b.XBraceHeight),
                BracingField(b => b.XBraceProfileType)),
            new(BracingTab, "Graft",
                BracingField(b => b.Graft),
                BracingField(b => b.GraftThickness),
                BracingField(b => b.GraftWidth),
                BracingField(b => b.GraftOffsetFromNeckEnd),
                BracingField(b => b.GraftEdgeOffset)),
            new(BracingTab, "Upper transverse bar",
                BracingField(b => b.UpperTransverseBarOffsetFromNeckEnd),
                BracingField(b => b.UpperTransverseBarWidth),
                BracingField(b => b.UpperTransverseBarHeight),
                BracingField(b => b.UpperTransverseBarEdgeOffset)),
            new(BracingTab, "Finger braces",
                BracingField(b => b.BassFingerBraceCount),
                BracingField(b => b.BassFingerBraceAngle),
                BracingField(b => b.BassFingerBraceSpacing),
                BracingField(b => b.TrebleFingerBraceCount),
                BracingField(b => b.TrebleFingerBraceAngle),
                BracingField(b => b.TrebleFingerBraceSpacing),
                BracingField(b => b.FingerBraceWidth),
                BracingField(b => b.FingerBraceHeight),
                BracingField(b => b.FingerBraceEdgeOffset)),
            new(BracingTab, "Tone bars",
                BracingField(b => b.ToneBarCount),
                BracingField(b => b.ToneBarSpacing),
                BracingField(b => b.ToneBarAngle),
                BracingField(b => b.ToneBarWidth),
                BracingField(b => b.ToneBarHeight),
                BracingField(b => b.ToneBarEdgeOffset)),
            new(BracingTab, "Soundhole braces",
                BracingField(b => b.SoundholeBraces),
                BracingField(b => b.SoundholeBraceOffset),
                BracingField(b => b.SoundholeBraceWidth),
                BracingField(b => b.SoundholeBraceHeight)),
            // Not drawn until the bridge's footprint is defined.
            new(BracingTab, "Bridge patch",
                BracingField(b => b.BridgePatchThickness),
                BracingField(b => b.BridgePatchWidthOffset),
                BracingField(b => b.BridgePatchOffset)),

            new(FretboardTab, "Width and string spacing",
                FretboardField(f => f.NutWidth),
                FretboardField(f => f.StringSpacingAtBridge),
                FretboardField(f => f.BassStringSetback),
                FretboardField(f => f.TrebleStringSetback)),
            new(FretboardTab, "Courses",
                FretboardField(f => f.StringsPerCourse),
                FretboardField(f => f.CourseStringSpacingAtNut) with { Enabled = TwoStringCourses },
                FretboardField(f => f.CourseStringSpacingAtBridge) with { Enabled = TwoStringCourses }),
            new(FretboardTab, "Frets",
                FretboardField(f => f.FretCount),
                FretboardField(f => f.LastFretOverhang)),
            // Not drawn yet.
            new(FretboardTab, "Fret slots",
                FretboardField(f => f.SlotOverage)),
            new(FretboardTab, "Thickness and radius",
                FretboardField(f => f.ThicknessAtNut),
                FretboardField(f => f.ThicknessAtEnd),
                FretboardField(f => f.Radius)),
            new(FretboardTab, MarkersGroup,
                FretboardField(f => f.FaceDotDiameter),
                FretboardField(f => f.DoubleDotEdgeDistance),
                FretboardField(f => f.SideDotDiameter),
                FretboardField(f => f.SideDotOffset),
                FretboardField(f => f.SideDoubleDotSpacing)) { HasMarkers = true },
            // Information only: no shape uses these, so they never change the drawing.
            new(FretboardTab, "Setup (information only)",
                FretboardField(f => f.Relief),
                FretboardField(f => f.ReliefFret),
                FretboardField(f => f.TrebleStringAction),
                FretboardField(f => f.BassStringAction)),

            new(NeckTab, ModeGroup,
                NeckField(n => n.ProfileAdvancedMode)),
            new(NeckTab, "Angle",
                NeckField(n => n.Angle),
                NeckField(n => n.Overstand)),
            new(NeckTab, "Nut",
                NutField(n => n.Thickness),
                NutField(n => n.Reveal)),
            new(NeckTab, "Thickness",
                NeckField(n => n.ThicknessAtFirstFret),
                NeckField(n => n.ThicknessAtSelectedFret),
                NeckField(n => n.ThicknessFret)),
            new(NeckTab, "Profile at first fret",
                SquarenessField(n => n.SquarenessAtFirstFret),
                BlendField(n => n.BlendAtFirstFret),
                AsymmetryField(n => n.AsymmetryAtFirstFret),
                NeckField(n => n.IncludesFretboardAtFirstFret)) { HasStartingShape = true },
            new(NeckTab, "Profile at selected fret",
                NeckField(n => n.UsesFirstFretAtSelectedFret),
                SquarenessField(n => n.SquarenessAtSelectedFret),
                BlendField(n => n.BlendAtSelectedFret),
                AsymmetryField(n => n.AsymmetryAtSelectedFret),
                NeckField(n => n.IncludesFretboardAtSelectedFret)) { HasStartingShape = true },
            new(NeckTab, "Heel shaft",
                NeckField(n => n.HeelShaftRadius),
                NeckField(n => n.HeelShaftOffset)),
            new(NeckTab, "Heel end",
                NeckField(n => n.HeelEndThickness),
                NeckField(n => n.HeelEndOffsetFromBack),
                NeckField(n => n.HeelEndWidth),
                NeckField(n => n.UsesFirstFretAtHeelEnd),
                SquarenessField(n => n.SquarenessAtHeelEnd),
                BlendField(n => n.BlendAtHeelEnd),
                AsymmetryField(n => n.AsymmetryAtHeelEnd),
                NeckField(n => n.SideLengthAtHeelEnd)) { HasStartingShape = true },
            new(NeckTab, "Heel cap",
                NeckField(n => n.HeelCap),
                NeckField(n => n.HeelCapThickness)),
            new(NeckTab, "Heel section",
                NeckField(n => n.HeelSectionThickness),
                NeckField(n => n.HeelSectionOffsetFromHeelEnd),
                NeckField(n => n.HeelOutlineStraightSides),
                NeckField(n => n.HeelSectionWidth),
                NeckField(n => n.UsesFirstFretAtHeelSection),
                SquarenessField(n => n.SquarenessAtHeelSection),
                BlendField(n => n.BlendAtHeelSection),
                AsymmetryField(n => n.AsymmetryAtHeelSection),
                NeckField(n => n.SideLengthAtHeelSection)) { HasStartingShape = true },
        };

        public static readonly IReadOnlyList<GuitarField> All = Groups.SelectMany(g => g.Fields).ToArray();

        // The tabs that have fields, in order.
        public static IReadOnlyList<string> Tabs { get; } = Groups.Select(g => g.Tab).Distinct().ToArray();

        // The tab's groups, in the order the tab shows them.
        public static IEnumerable<GuitarFieldGroup> GroupsOf(string tab) => Groups.Where(g => g.Tab == tab);
    }
}
