using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using Body = GuitarForm.Model.Parts.Body;
using OldBody = GuitarForm.Model.Body;

namespace GuitarForm
{
    // A stopgap until the panel moves to the new model: the open design is turned into a new-model Guitar so it can be
    // drawn with GuitarForm.Shapes, and the values the old tabs don't have (cutaway, body skew, fretboard widths) are
    // kept per design in a JSON file in the library's folder, by glossary abbreviation, in mm.
    static class NewFields
    {
        public sealed record Field(Func<Guitar, Parameter> Get, Func<Guitar, Parameter, Guitar> Set);

        static Field BodyField(Func<Body, Parameter> get) =>
            new Field(g => get(g.Body), (g, p) => g with { Body = (Body)g.Body.With(p) });

        static Field FretboardField(Func<Fretboard, Parameter> get) =>
            new Field(g => get(g.Neck.Fretboard),
                (g, p) => g with { Neck = g.Neck with { Fretboard = (Fretboard)g.Neck.Fretboard.With(p) } });

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
        };

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
            Fields.Aggregate(to, (guitar, field) => field.Set(guitar, field.Get(from)));

        static string FilePath(long designId) =>
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(GuitarFormPlugin.Instance.GuitarFormSettings.LibraryPath)!,
                "NewFields", designId.ToString(CultureInfo.InvariantCulture) + ".json");

        // The design's saved new fields, on an otherwise empty guitar.
        public static Guitar Load(long designId)
        {
            var guitar = new Guitar();
            string path = FilePath(designId);
            if (!File.Exists(path))
                return guitar;
            var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new();
            foreach (var field in Fields)
            {
                var parameter = field.Get(guitar);
                if (texts.TryGetValue(parameter.Definition.Abbreviation, out string text) &&
                    parameter.TryParse(text, LengthUnit.Millimeters, CultureInfo.InvariantCulture, out var parsed, out _))
                    guitar = field.Set(guitar, parsed);
            }
            return guitar;
        }

        public static void Save(long designId, Guitar guitar)
        {
            var texts = Fields.Select(field => field.Get(guitar)).Where(p => p.HasValue).ToDictionary(
                p => p.Definition.Abbreviation, p => p.Format(LengthUnit.Millimeters, CultureInfo.InvariantCulture));
            string path = FilePath(designId);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(texts, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
