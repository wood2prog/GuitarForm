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
    // drawn with GuitarForm.Shapes, and the values the old tabs don't have (cutaway, body skew, fretboard, neck, dot markers) are
    // kept per design in a JSON file in the library's folder, by glossary abbreviation, in mm.
    static class NewFields
    {
        public sealed record Field(Func<Guitar, Parameter> Get, Func<Guitar, Parameter, Guitar> Set);

        static Field BodyField(Func<Body, Parameter> get) =>
            new Field(g => get(g.Body), (g, p) => g with { Body = (Body)g.Body.With(p) });

        static Field FretboardField(Func<Fretboard, Parameter> get) =>
            new Field(g => get(g.Neck.Fretboard),
                (g, p) => g with { Neck = g.Neck with { Fretboard = (Fretboard)g.Neck.Fretboard.With(p) } });

        static Field NeckField(Func<Neck, Parameter> get) =>
            new Field(g => get(g.Neck), (g, p) => g with { Neck = (Neck)g.Neck.With(p) });

        public static readonly Field[] Fields =
        {
            BodyField(b => b.ProfileSkew),
            BodyField(b => b.CutawayType),
            BodyField(b => b.CutawayOffsetFromNeck),
            BodyField(b => b.CutawayWidth),
            BodyField(b => b.CutawayDepth),
            BodyField(b => b.CutawayValleyRadius),
            BodyField(b => b.CutawayPointRadius),
            FretboardField(f => f.NutWidth),
            FretboardField(f => f.StringSpacingAtBridge),
            FretboardField(f => f.BassStringSetback),
            FretboardField(f => f.TrebleStringSetback),
            FretboardField(f => f.FretCount),
            FretboardField(f => f.LastFretOverhang),
            FretboardField(f => f.ThicknessAtNut),
            FretboardField(f => f.ThicknessAtEnd),
            FretboardField(f => f.Radius),
            FretboardField(f => f.FaceDotDiameter),
            FretboardField(f => f.DoubleDotEdgeDistance),
            NeckField(n => n.Angle),
            NeckField(n => n.Overstand),
            NeckField(n => n.ThicknessAtFirstFret),
            NeckField(n => n.ThicknessAtSelectedFret),
            NeckField(n => n.ThicknessFret),
            NeckField(n => n.BottomRadiusAtFirstFret),
            NeckField(n => n.ShoulderAngleAtFirstFret),
            NeckField(n => n.MidshoulderRadiusAtFirstFret),
            NeckField(n => n.CenterOffsetAtFirstFret),
            NeckField(n => n.IncludesFretboardAtFirstFret),
            NeckField(n => n.UsesFirstFretAtSelectedFret),
            NeckField(n => n.BottomRadiusAtSelectedFret),
            NeckField(n => n.ShoulderAngleAtSelectedFret),
            NeckField(n => n.MidshoulderRadiusAtSelectedFret),
            NeckField(n => n.CenterOffsetAtSelectedFret),
            NeckField(n => n.IncludesFretboardAtSelectedFret),
            NeckField(n => n.HeelEndThickness),
            NeckField(n => n.HeelEndOffsetFromBack),
            NeckField(n => n.HeelEndWidth),
            NeckField(n => n.HeelCap),
            NeckField(n => n.HeelCapThickness),
            NeckField(n => n.UsesFirstFretAtHeelEnd),
            NeckField(n => n.BottomRadiusAtHeelEnd),
            NeckField(n => n.ShoulderAngleAtHeelEnd),
            NeckField(n => n.MidshoulderRadiusAtHeelEnd),
            NeckField(n => n.SideLengthAtHeelEnd),
            NeckField(n => n.HeelSectionThickness),
            NeckField(n => n.HeelSectionOffsetFromHeelEnd),
            NeckField(n => n.HeelOutlineStraightSides),
            NeckField(n => n.HeelSectionWidth),
            NeckField(n => n.UsesFirstFretAtHeelSection),
            NeckField(n => n.BottomRadiusAtHeelSection),
            NeckField(n => n.ShoulderAngleAtHeelSection),
            NeckField(n => n.MidshoulderRadiusAtHeelSection),
            NeckField(n => n.SideLengthAtHeelSection),
        };

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
            File.WriteAllText(path, JsonSerializer.Serialize(ToTexts(guitar), new JsonSerializerOptions { WriteIndented = true }));
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
