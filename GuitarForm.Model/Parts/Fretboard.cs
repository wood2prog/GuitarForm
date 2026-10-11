using System;
using System.Collections.Generic;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    // The fretboard: its frets, shape and widths, the string spacing and action it's set up for, and its position
    // dots.
    public sealed record Fretboard : Part
    {
        public static class Definitions
        {
            public static readonly ParameterDefinition FretCount = ParameterDefinition.Count("Fret Count", "FC", 1);
            public static readonly ParameterDefinition LastFretOverhang = ParameterDefinition.Distance("Last Fret Overhang", "LFO");
            public static readonly ParameterDefinition Radius = ParameterDefinition.Size("Fretboard Radius", "FBR");
            public static readonly ParameterDefinition ThicknessAtNut = ParameterDefinition.Size("Fretboard Thickness at Nut", "FBTN");
            public static readonly ParameterDefinition ThicknessAtEnd = ParameterDefinition.Size("Fretboard Thickness at End", "FBTE");
            public static readonly ParameterDefinition NutWidth = ParameterDefinition.Size("Nut Width", "NTW");
            public static readonly ParameterDefinition StringSpacingAtBridge = ParameterDefinition.Size("String Spacing at Bridge", "SSB");
            public static readonly ParameterDefinition Relief = ParameterDefinition.Distance("Fretboard Relief", "FBRL");
            public static readonly ParameterDefinition ReliefFret = ParameterDefinition.Count("Fretboard Relief Fret", "FBRF", 1);
            public static readonly ParameterDefinition BassStringSetback = ParameterDefinition.Distance("Bass String Setback", "BSS");
            public static readonly ParameterDefinition TrebleStringSetback = ParameterDefinition.Distance("Treble String Setback", "TSS");
            public static readonly ParameterDefinition StringsPerCourse = ParameterDefinition.Count("Strings per Course", "SPC", 1, 2);
            public static readonly ParameterDefinition CourseStringSpacingAtNut = ParameterDefinition.Size("Course String Spacing at Nut", "CSN");
            public static readonly ParameterDefinition CourseStringSpacingAtBridge = ParameterDefinition.Size("Course String Spacing at Bridge", "CSB");

            public static readonly ParameterDefinition TrebleStringAction = ParameterDefinition.Size("Treble String Action", "TSA");
            public static readonly ParameterDefinition BassStringAction = ParameterDefinition.Size("Bass String Action", "BSA");

            public static readonly ParameterDefinition FaceDotDiameter = ParameterDefinition.Size("Face Dot Diameter", "FDD");
            public static readonly ParameterDefinition DoubleDotEdgeDistance = ParameterDefinition.Distance("Double Dot Edge Distance", "DDED");
            public static readonly ParameterDefinition SideDotDiameter = ParameterDefinition.Size("Side Dot Diameter", "SDD");
            public static readonly ParameterDefinition SideDotOffset = ParameterDefinition.Distance("Side Dot Offset", "SDO");
            public static readonly ParameterDefinition SideDoubleDotSpacing = ParameterDefinition.Size("Side Double Dot Spacing", "SDDS");
        }

        public CountParameter FretCount { get; init; } = new(Definitions.FretCount);
        public NumberParameter LastFretOverhang { get; init; } = new(Definitions.LastFretOverhang);
        public NumberParameter Radius { get; init; } = new(Definitions.Radius);
        public NumberParameter ThicknessAtNut { get; init; } = new(Definitions.ThicknessAtNut);
        public NumberParameter ThicknessAtEnd { get; init; } = new(Definitions.ThicknessAtEnd);
        public NumberParameter NutWidth { get; init; } = new(Definitions.NutWidth);
        public NumberParameter StringSpacingAtBridge { get; init; } = new(Definitions.StringSpacingAtBridge);
        public NumberParameter Relief { get; init; } = new(Definitions.Relief);
        public CountParameter ReliefFret { get; init; } = new(Definitions.ReliefFret);
        public NumberParameter BassStringSetback { get; init; } = new(Definitions.BassStringSetback);
        public NumberParameter TrebleStringSetback { get; init; } = new(Definitions.TrebleStringSetback);
        public CountParameter StringsPerCourse { get; init; } = new(Definitions.StringsPerCourse);
        public NumberParameter CourseStringSpacingAtNut { get; init; } = new(Definitions.CourseStringSpacingAtNut);
        public NumberParameter CourseStringSpacingAtBridge { get; init; } = new(Definitions.CourseStringSpacingAtBridge);

        public NumberParameter TrebleStringAction { get; init; } = new(Definitions.TrebleStringAction);
        public NumberParameter BassStringAction { get; init; } = new(Definitions.BassStringAction);

        public NumberParameter FaceDotDiameter { get; init; } = new(Definitions.FaceDotDiameter);
        public NumberParameter DoubleDotEdgeDistance { get; init; } = new(Definitions.DoubleDotEdgeDistance);
        public NumberParameter SideDotDiameter { get; init; } = new(Definitions.SideDotDiameter);
        public NumberParameter SideDotOffset { get; init; } = new(Definitions.SideDotOffset);
        public NumberParameter SideDoubleDotSpacing { get; init; } = new(Definitions.SideDoubleDotSpacing);

        // The frets that have position dots, and how many.
        public IReadOnlyList<DotMarker> Markers { get; init; } = Array.Empty<DotMarker>();

        // Whether the setbacks leave room for the strings at the nut. True when any of them isn't set.
        public bool SetbacksFitNutWidth =>
            !(BassStringSetback.Value + TrebleStringSetback.Value >= NutWidth.Value);

        protected override IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture)
        {
            if (ReliefFret.Value > FretCount.Value)
                yield return PastLastFret(ReliefFret);
            if (!SetbacksFitNutWidth)
                yield return ModelMessage.Error(
                    $"{BassStringSetback.Label} and {TrebleStringSetback.Label} together must be less than {NutWidth.Label}.");
            if (StringsPerCourse.Value == 2)
            {
                foreach (var message in RequiredWhen(StringsPerCourse, unit, culture,
                             CourseStringSpacingAtNut, CourseStringSpacingAtBridge))
                    yield return message;
            }

            foreach (var message in ValidateList(Markers, "Marker", unit, culture))
                yield return message;
            for (int i = 0; i < Markers.Count; i++)
            {
                var fret = Markers[i].Fret;
                if (fret.Value > FretCount.Value)
                    yield return ModelMessage.Error($"Marker {i + 1}: {PastLastFret(fret).Text}");
                int first = FirstMarkerAt(fret.Value);
                if (fret.HasValue && first < i)
                    yield return ModelMessage.Error($"Marker {i + 1}: fret {fret.Value} already has marker {first + 1}.");
            }
        }

        // The place in the list of the first marker at the given fret, or -1 when there's none.
        int FirstMarkerAt(int? fret)
        {
            for (int i = 0; i < Markers.Count; i++)
                if (Markers[i].Fret.Value == fret)
                    return i;
            return -1;
        }

        // A fret that's past the last fret, as an error.
        public ModelMessage PastLastFret(CountParameter fret) =>
            ModelMessage.Error($"{fret.Label} can't be past the last fret ({FretCount.Label} is {FretCount.Value}).");
    }

    // One fret's position dots, on both the face and the bass edge.
    public sealed record DotMarker : Part
    {
        public static class Definitions
        {
            public static readonly ParameterDefinition Fret = ParameterDefinition.Count("Marker Fret", "MKF", 1, required: true);
            public static readonly ParameterDefinition DotCount = ParameterDefinition.Count("Marker Dot Count", "MKDC", 1, 2, required: true);
        }

        public CountParameter Fret { get; init; } = new(Definitions.Fret);
        public CountParameter DotCount { get; init; } = new(Definitions.DotCount);
    }
}
