using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using Body = GuitarForm.Model.Parts.Body;
using OldBody = GuitarForm.Model.Body;

namespace GuitarForm
{
    // A stopgap until the panel moves to the new model: the open design is turned into a new-model Guitar so it can be
    // drawn with GuitarForm.Shapes, and the values the old sections don't have (cutaway, body skew, blocks, fretboard,
    // neck, dot markers, bracing) are kept per design in a JSON file in the library's folder, by glossary abbreviation,
    // in mm. They're shown on the Body tab and on the Bracing, Fretboard and Neck tabs.
    static class NewFields
    {
        // Step is how much a slider changes the value per step, in the units it's shown in.
        // Visible, when set, says whether the field is shown for the working copy's values. Placeholder is shown on a
        // slider while the value is unset (e.g. the default it falls back to).
        public sealed record Field(Func<Guitar, Parameter> Get, Func<Guitar, Parameter, Guitar> Set, double Step = 0.1,
            Func<Guitar, bool> Visible = null, string Placeholder = null);

        static Field BodyField(Func<Body, Parameter> get) =>
            new Field(g => get(g.Body), (g, p) => g with { Body = (Body)g.Body.With(p) });

        static Field BracingField(Func<Bracing, Parameter> get) =>
            new Field(g => get(g.Body.TopPlate.Bracing), (g, p) => g with
            {
                Body = g.Body with
                {
                    TopPlate = g.Body.TopPlate with { Bracing = (Bracing)g.Body.TopPlate.Bracing.With(p) },
                },
            });

        static Field FretboardField(Func<Fretboard, Parameter> get) =>
            new Field(g => get(g.Neck.Fretboard),
                (g, p) => g with { Neck = g.Neck with { Fretboard = (Fretboard)g.Neck.Fretboard.With(p) } });

        static Field NeckField(Func<Neck, Parameter> get) =>
            new Field(g => get(g.Neck), (g, p) => g with { Neck = (Neck)g.Neck.With(p) });

        static Field NutField(Func<Nut, Parameter> get) =>
            new Field(g => get(g.Neck.Nut), (g, p) => g with { Neck = g.Neck with { Nut = (Nut)g.Neck.Nut.With(p) } });

        static bool SimpleProfile(Guitar g) => !g.Neck.ProfileAdvancedMode.Value;
        static bool AdvancedProfile(Guitar g) => g.Neck.ProfileAdvancedMode.Value;

        static string Show(double value) => value.ToString("0.#", CultureInfo.CurrentCulture);

        static Field SquarenessField(Func<Neck, Parameter> get) => NeckField(get) with
        {
            Visible = SimpleProfile,
            Placeholder = Show(Neck.DefaultSquareness),
        };

        static Field AsymmetryField(Func<Neck, Parameter> get) => NeckField(get) with
        {
            Step = 1,
            Placeholder = Show(Neck.DefaultAsymmetry),
        };

        static Field BlendField(Func<Neck, Parameter> get) => NeckField(get) with
        {
            Step = 1,
            Visible = AdvancedProfile,
            Placeholder = Show(Neck.DefaultBlend),
        };

        // The tabs the new fields are shown on.
        public const string BodyTab = "Body";
        public const string BracingTab = "Bracing";
        public const string FretboardTab = "Fretboard";
        public const string NeckTab = "Neck";

        // Related fields shown together in a box on a tab. A group with the title of one of the tab's old groups
        // (see Sections) is shown in that box, after the old fields.
        public sealed record Group(string Tab, string Title, params Field[] Fields);

        public static readonly Group[] Groups =
        {
            new(BodyTab, "Side view",
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

            new(BracingTab, "X brace",
                BracingField(b => b.XBraceOffsetFromNeckEnd),
                BracingField(b => b.XBraceAngle),
                BracingField(b => b.XBraceEdgeOffset),
                BracingField(b => b.XBraceWidth),
                BracingField(b => b.XBraceHeight)),
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

            new(FretboardTab, "Width and string spacing",
                FretboardField(f => f.NutWidth),
                FretboardField(f => f.StringSpacingAtBridge),
                FretboardField(f => f.BassStringSetback),
                FretboardField(f => f.TrebleStringSetback)),
            new(FretboardTab, "Frets",
                FretboardField(f => f.FretCount),
                FretboardField(f => f.LastFretOverhang)),
            new(FretboardTab, "Thickness and radius",
                FretboardField(f => f.ThicknessAtNut),
                FretboardField(f => f.ThicknessAtEnd),
                FretboardField(f => f.Radius)),
            // The panel adds the dot marker list to this group.
            new(FretboardTab, MarkersGroup,
                FretboardField(f => f.FaceDotDiameter),
                FretboardField(f => f.DoubleDotEdgeDistance)),

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
                NeckField(n => n.IncludesFretboardAtFirstFret)),
            new(NeckTab, "Profile at selected fret",
                NeckField(n => n.UsesFirstFretAtSelectedFret),
                SquarenessField(n => n.SquarenessAtSelectedFret),
                BlendField(n => n.BlendAtSelectedFret),
                AsymmetryField(n => n.AsymmetryAtSelectedFret),
                NeckField(n => n.IncludesFretboardAtSelectedFret)),
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
                NeckField(n => n.SideLengthAtHeelEnd)),
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
                NeckField(n => n.SideLengthAtHeelSection)),
        };

        public const string MarkersGroup = "Dot markers";
        public const string ModeGroup = "Profile mode";

        // The neck profile groups that get a "Starting shape" list.
        public static bool HasStartingShape(string tab, string title) =>
            tab == NeckTab && StationOf(title) != null;

        static string StationOf(string title) => title switch
        {
            "Profile at first fret" => "F",
            "Profile at selected fret" => "S",
            "Heel section" => "H",
            "Heel end" => "E",
            _ => null,
        };

        // The guitar with the preset's squareness, asymmetry and blend written to the station the group is for.
        public static Guitar WithStartingShape(Guitar guitar, string title, NeckProfilePresets.Preset preset)
        {
            Neck n = guitar.Neck;
            NumberParameter Number(NumberParameter p, double v) => p with { Value = v };
            n = StationOf(title) switch
            {
                "F" => n with
                {
                    SquarenessAtFirstFret = Number(n.SquarenessAtFirstFret, preset.Squareness),
                    AsymmetryAtFirstFret = Number(n.AsymmetryAtFirstFret, preset.Asymmetry),
                    BlendAtFirstFret = Number(n.BlendAtFirstFret, preset.Blend),
                },
                "S" => n with
                {
                    SquarenessAtSelectedFret = Number(n.SquarenessAtSelectedFret, preset.Squareness),
                    AsymmetryAtSelectedFret = Number(n.AsymmetryAtSelectedFret, preset.Asymmetry),
                    BlendAtSelectedFret = Number(n.BlendAtSelectedFret, preset.Blend),
                },
                "H" => n with
                {
                    SquarenessAtHeelSection = Number(n.SquarenessAtHeelSection, preset.Squareness),
                    AsymmetryAtHeelSection = Number(n.AsymmetryAtHeelSection, preset.Asymmetry),
                    BlendAtHeelSection = Number(n.BlendAtHeelSection, preset.Blend),
                },
                "E" => n with
                {
                    SquarenessAtHeelEnd = Number(n.SquarenessAtHeelEnd, preset.Squareness),
                    AsymmetryAtHeelEnd = Number(n.AsymmetryAtHeelEnd, preset.Asymmetry),
                    BlendAtHeelEnd = Number(n.BlendAtHeelEnd, preset.Blend),
                },
                _ => n,
            };
            return guitar with { Neck = n };
        }

        public static readonly Field[] Fields = Groups.SelectMany(g => g.Fields).ToArray();

        const string MarkersKey = "Markers";

        // The dot markers as text: each marker's fret, with ":2" for two dots, e.g. "3 5 7 9 12:2".
        public static string FormatMarkers(Guitar guitar) => string.Join(" ", guitar.Neck.Fretboard.Markers.Select(m =>
            m.DotCount.Value == 2 ? $"{m.Fret.Value}:2" : $"{m.Fret.Value}"));

        // The markers read from text, or null when it isn't understood.
        public static Guitar WithMarkers(Guitar guitar, string text)
        {
            var markers = new List<DotMarker>();
            foreach (string item in (text ?? "").Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = item.Split(':');
                if (!int.TryParse(parts[0], out int fret) || parts.Length > 2)
                    return null;
                int dots = 1;
                if (parts.Length == 2 && !int.TryParse(parts[1], out dots))
                    return null;
                markers.Add(new DotMarker
                {
                    Fret = new(DotMarker.Definitions.Fret, fret),
                    DotCount = new(DotMarker.Definitions.DotCount, dots),
                });
            }
            return guitar with { Neck = guitar.Neck with { Fretboard = guitar.Neck.Fretboard with { Markers = markers } } };
        }

        // The design's values as a Guitar, without the new fields.
        public static Guitar FromDesign(GuitarDesign design)
        {
            OldBody b = design.Body;
            var s = design.Soundhole;
            return new Guitar
            {
                ScaleLength = new(Guitar.Definitions.ScaleLength, design.Instrument.ScaleLength),
                NeckJoinFret = new(Guitar.Definitions.NeckJoinFret, design.Instrument.NeckJoinFret),
                Body = new Body
                {
                    Length = new(Body.Definitions.Length, b.BodyLength),
                    DepthAtTailEnd = new(Body.Definitions.DepthAtTailEnd, b.TailDepth),
                    DepthAtNeckEnd = new(Body.Definitions.DepthAtNeckEnd, b.NeckDepth),
                    UpperBoutWidth = new(Body.Definitions.UpperBoutWidth, b.UbWidth),
                    UpperBoutPrimaryRadius = new(Body.Definitions.UpperBoutPrimaryRadius, b.UbRadius),
                    UpperBoutOffsetFromNeckEnd = new(Body.Definitions.UpperBoutOffsetFromNeckEnd, b.UbOffset),
                    UpperBoutSecondaryOffset = new(Body.Definitions.UpperBoutSecondaryOffset, b.UbSecondaryOffset),
                    HeelFlatWidth = new(Body.Definitions.HeelFlatWidth, b.HeelWidth),
                    WaistWidth = new(Body.Definitions.WaistWidth, b.WaistWidth),
                    WaistRadius = new(Body.Definitions.WaistRadius, b.WaistRadius),
                    WaistOffsetFromTailEnd = new(Body.Definitions.WaistOffsetFromTailEnd, b.WaistOffset),
                    LowerBoutWidth = new(Body.Definitions.LowerBoutWidth, b.LbWidth),
                    LowerBoutPrimaryRadius = new(Body.Definitions.LowerBoutPrimaryRadius, b.LbRadius),
                    LowerBoutSecondaryOffset = new(Body.Definitions.LowerBoutSecondaryOffset, b.LbSecondaryOffset),
                    LowerBoutOffsetFromTailEnd = new(Body.Definitions.LowerBoutOffsetFromTailEnd, b.LbOffset),
                    TopPlate = new TopPlate
                    {
                        Soundhole = new(TopPlate.Definitions.Soundhole, s != null),
                        SoundholeDiameter = new(TopPlate.Definitions.SoundholeDiameter, s?.Diameter),
                        SoundholeOffsetFromTailEnd = new(TopPlate.Definitions.SoundholeOffsetFromTailEnd, s?.TailOffset),
                        SoundholeOffsetFromCenterline = new(TopPlate.Definitions.SoundholeOffsetFromCenterline, s?.HorizontalOffset),
                    },
                },
            };
        }

        // The new fields' values copied from one guitar into another.
        public static Guitar CopyFields(Guitar from, Guitar to) =>
            WithMarkers(Fields.Aggregate(to, (guitar, field) => field.Set(guitar, field.Get(from))), FormatMarkers(from));

        static string FilePath(long designId) =>
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(GuitarFormPlugin.Instance.GuitarFormSettings.LibraryPath)!,
                "NewFields", designId.ToString(CultureInfo.InvariantCulture) + ".json");

        // The design's saved new fields, on an otherwise empty guitar.
        public static Guitar Load(long designId)
        {
            string path = FilePath(designId);
            return File.Exists(path)
                ? FromTexts(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new())
                : new Guitar();
        }

        public static void Save(long designId, Guitar guitar)
        {
            string path = FilePath(designId);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            // Written to a temporary file and moved over the old one, so an interrupted save can't leave half a file.
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(ToTexts(guitar), new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, true);
        }

        // A design document (see DesignDocument) can carry the new fields in a "newFields" object beside the design,
        // which older readers ignore. Null when it has none.
        public static Guitar ReadFromDocument(string json)
        {
            var newFields = JsonNode.Parse(json)?["newFields"];
            return newFields == null ? null : FromTexts(newFields.Deserialize<Dictionary<string, string>>());
        }

        public static string AddToDocument(string json, Guitar guitar)
        {
            var document = JsonNode.Parse(json)!.AsObject();
            document["newFields"] = JsonSerializer.SerializeToNode(ToTexts(guitar));
            return document.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        // Values by glossary abbreviation, lengths in mm, plus the dot markers.
        static Guitar FromTexts(Dictionary<string, string> texts)
        {
            var guitar = new Guitar();
            foreach (var field in Fields)
            {
                var parameter = field.Get(guitar);
                if (texts.TryGetValue(parameter.Definition.Abbreviation, out string text) &&
                    parameter.TryParse(text, LengthUnit.Millimeters, CultureInfo.InvariantCulture, out var parsed, out _))
                    guitar = field.Set(guitar, parsed);
            }
            if (texts.TryGetValue(MarkersKey, out string markers))
                guitar = WithMarkers(guitar, markers) ?? guitar;
            return guitar;
        }

        static Dictionary<string, string> ToTexts(Guitar guitar)
        {
            var texts = Fields.Select(field => field.Get(guitar)).Where(p => p.HasValue).ToDictionary(
                p => p.Definition.Abbreviation, p => p.Format(LengthUnit.Millimeters, CultureInfo.InvariantCulture));
            texts[MarkersKey] = FormatMarkers(guitar);
            return texts;
        }
    }
}
