using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    // The neck, one piece: its angle and thickness, its profile at the first fret and the selected fret, and the heel.
    // It holds its fretboard, nut and headshape, and refers to the fretwire and string set it uses, which are library
    // items shared between necks.
    public sealed record Neck : Part
    {
        public override bool IsSavedSeparately => true;

        // Used for a station's squareness, asymmetry or blend that isn't set. A blend of 50 and a squareness of 2 are
        // both a round C.
        public const double DefaultSquareness = 2;
        public const double DefaultAsymmetry = 0;
        public const double DefaultBlend = 50;
        public const double MinimumAsymmetry = -50;
        public const double MaximumAsymmetry = 50;

        public double SquarenessOrDefault(NumberParameter squareness) => squareness.Value ?? DefaultSquareness;
        public double AsymmetryOrDefault(NumberParameter asymmetry) => asymmetry.Value ?? DefaultAsymmetry;
        public double BlendOrDefault(NumberParameter blend) => blend.Value ?? DefaultBlend;

        public static class Definitions
        {
            public static readonly ParameterDefinition Angle = ParameterDefinition.Angle("Neck Angle", "NKA");
            public static readonly ParameterDefinition Overstand = ParameterDefinition.Distance("Overstand", "OVS");
            public static readonly ParameterDefinition ThicknessAtFirstFret = ParameterDefinition.Size("Neck Thickness at First Fret", "NKTF");
            public static readonly ParameterDefinition ThicknessAtSelectedFret = ParameterDefinition.Size("Neck Thickness at Selected Fret", "NKTS");
            public static readonly ParameterDefinition ThicknessFret = ParameterDefinition.Count("Neck Thickness Fret", "NKSF", 1);

            public static readonly ParameterDefinition IncludesFretboardAtFirstFret = ParameterDefinition.Flag("Profile Includes Fretboard at First Fret", "PIFF");

            public static readonly ParameterDefinition UsesFirstFretAtSelectedFret = ParameterDefinition.Flag("Profile Uses First Fret at Selected Fret", "PUFS");
            public static readonly ParameterDefinition IncludesFretboardAtSelectedFret = ParameterDefinition.Flag("Profile Includes Fretboard at Selected Fret", "PIFS");

            public static readonly ParameterDefinition HeelShaftRadius = ParameterDefinition.Size("Heel Shaft Radius", "HLSR");
            public static readonly ParameterDefinition HeelShaftOffset = ParameterDefinition.Distance("Heel Shaft Offset", "HLSO");

            public static readonly ParameterDefinition HeelEndThickness = ParameterDefinition.Size("Heel End Thickness", "HLET");
            public static readonly ParameterDefinition HeelEndOffsetFromBack = ParameterDefinition.Distance("Heel End Offset from Back", "HLEO");
            public static readonly ParameterDefinition HeelEndWidth = ParameterDefinition.Size("Heel End Width", "HLEW");
            public static readonly ParameterDefinition HeelCap = ParameterDefinition.Flag("Heel Cap", "HCP");
            public static readonly ParameterDefinition HeelCapThickness = ParameterDefinition.Size("Heel Cap Thickness", "HCPT");
            public static readonly ParameterDefinition UsesFirstFretAtHeelEnd = ParameterDefinition.Flag("Profile Uses First Fret at Heel End", "PUFE");
            public static readonly ParameterDefinition SideLengthAtHeelEnd = ParameterDefinition.Distance("Profile Side Length at Heel End", "PSLE");

            public static readonly ParameterDefinition HeelSectionThickness = ParameterDefinition.Size("Heel Section Thickness", "HLCT");
            public static readonly ParameterDefinition HeelSectionOffsetFromHeelEnd = ParameterDefinition.Distance("Heel Section Offset from Heel End", "HLCO");
            public static readonly ParameterDefinition HeelOutlineStraightSides = ParameterDefinition.Flag("Heel Outline Straight Sides", "HLOS");
            public static readonly ParameterDefinition HeelSectionWidth = ParameterDefinition.Size("Heel Section Width", "HLCW");
            public static readonly ParameterDefinition UsesFirstFretAtHeelSection = ParameterDefinition.Flag("Profile Uses First Fret at Heel Section", "PUFH");
            public static readonly ParameterDefinition SideLengthAtHeelSection = ParameterDefinition.Distance("Profile Side Length at Heel Section", "PSLH");

            // The advanced profile: squareness, asymmetry and blend at each station.
            static ParameterDefinition Squareness(string name, string abbreviation) =>
                ParameterDefinition.Number(name, abbreviation, "", 0.1) with { Minimum = 1, MinimumExclusive = false, Maximum = 10 };
            static ParameterDefinition Asymmetry(string name, string abbreviation) =>
                ParameterDefinition.Number(name, abbreviation, "%", 1) with { Minimum = MinimumAsymmetry, MinimumExclusive = false, Maximum = MaximumAsymmetry };
            static ParameterDefinition Blend(string name, string abbreviation) =>
                ParameterDefinition.Number(name, abbreviation, "%", 1) with { Minimum = 0, MinimumExclusive = false, Maximum = 100 };

            public static readonly ParameterDefinition ProfileAdvancedMode = ParameterDefinition.Flag("Profile Advanced Mode", "PADV");
            public static readonly ParameterDefinition SquarenessAtFirstFret = Squareness("Profile Squareness at First Fret", "PSQF");
            public static readonly ParameterDefinition AsymmetryAtFirstFret = Asymmetry("Profile Asymmetry at First Fret", "PASF");
            public static readonly ParameterDefinition BlendAtFirstFret = Blend("Profile Blend at First Fret", "PBLF");
            public static readonly ParameterDefinition SquarenessAtSelectedFret = Squareness("Profile Squareness at Selected Fret", "PSQS");
            public static readonly ParameterDefinition AsymmetryAtSelectedFret = Asymmetry("Profile Asymmetry at Selected Fret", "PASS");
            public static readonly ParameterDefinition BlendAtSelectedFret = Blend("Profile Blend at Selected Fret", "PBLS");
            public static readonly ParameterDefinition SquarenessAtHeelSection = Squareness("Profile Squareness at Heel Section", "PSQH");
            public static readonly ParameterDefinition AsymmetryAtHeelSection = Asymmetry("Profile Asymmetry at Heel Section", "PASH");
            public static readonly ParameterDefinition BlendAtHeelSection = Blend("Profile Blend at Heel Section", "PBLH");
            public static readonly ParameterDefinition SquarenessAtHeelEnd = Squareness("Profile Squareness at Heel End", "PSQE");
            public static readonly ParameterDefinition AsymmetryAtHeelEnd = Asymmetry("Profile Asymmetry at Heel End", "PASE");
            public static readonly ParameterDefinition BlendAtHeelEnd = Blend("Profile Blend at Heel End", "PBLE");
        }

        public NumberParameter Angle { get; init; } = new(Definitions.Angle);
        public NumberParameter Overstand { get; init; } = new(Definitions.Overstand);
        public NumberParameter ThicknessAtFirstFret { get; init; } = new(Definitions.ThicknessAtFirstFret);
        public NumberParameter ThicknessAtSelectedFret { get; init; } = new(Definitions.ThicknessAtSelectedFret);
        public CountParameter ThicknessFret { get; init; } = new(Definitions.ThicknessFret);

        public FlagParameter IncludesFretboardAtFirstFret { get; init; } = new(Definitions.IncludesFretboardAtFirstFret);

        public FlagParameter UsesFirstFretAtSelectedFret { get; init; } = new(Definitions.UsesFirstFretAtSelectedFret);
        public FlagParameter IncludesFretboardAtSelectedFret { get; init; } = new(Definitions.IncludesFretboardAtSelectedFret);

        public NumberParameter HeelShaftRadius { get; init; } = new(Definitions.HeelShaftRadius);
        public NumberParameter HeelShaftOffset { get; init; } = new(Definitions.HeelShaftOffset);

        public NumberParameter HeelEndThickness { get; init; } = new(Definitions.HeelEndThickness);
        public NumberParameter HeelEndOffsetFromBack { get; init; } = new(Definitions.HeelEndOffsetFromBack);
        public NumberParameter HeelEndWidth { get; init; } = new(Definitions.HeelEndWidth);
        public FlagParameter HeelCap { get; init; } = new(Definitions.HeelCap);
        public NumberParameter HeelCapThickness { get; init; } = new(Definitions.HeelCapThickness);
        public FlagParameter UsesFirstFretAtHeelEnd { get; init; } = new(Definitions.UsesFirstFretAtHeelEnd);
        public NumberParameter SideLengthAtHeelEnd { get; init; } = new(Definitions.SideLengthAtHeelEnd);

        public NumberParameter HeelSectionThickness { get; init; } = new(Definitions.HeelSectionThickness);
        public NumberParameter HeelSectionOffsetFromHeelEnd { get; init; } = new(Definitions.HeelSectionOffsetFromHeelEnd);
        public FlagParameter HeelOutlineStraightSides { get; init; } = new(Definitions.HeelOutlineStraightSides);
        public NumberParameter HeelSectionWidth { get; init; } = new(Definitions.HeelSectionWidth);
        public FlagParameter UsesFirstFretAtHeelSection { get; init; } = new(Definitions.UsesFirstFretAtHeelSection);
        public NumberParameter SideLengthAtHeelSection { get; init; } = new(Definitions.SideLengthAtHeelSection);

        public FlagParameter ProfileAdvancedMode { get; init; } = new(Definitions.ProfileAdvancedMode);
        public NumberParameter SquarenessAtFirstFret { get; init; } = new(Definitions.SquarenessAtFirstFret);
        public NumberParameter AsymmetryAtFirstFret { get; init; } = new(Definitions.AsymmetryAtFirstFret);
        public NumberParameter BlendAtFirstFret { get; init; } = new(Definitions.BlendAtFirstFret);
        public NumberParameter SquarenessAtSelectedFret { get; init; } = new(Definitions.SquarenessAtSelectedFret);
        public NumberParameter AsymmetryAtSelectedFret { get; init; } = new(Definitions.AsymmetryAtSelectedFret);
        public NumberParameter BlendAtSelectedFret { get; init; } = new(Definitions.BlendAtSelectedFret);
        public NumberParameter SquarenessAtHeelSection { get; init; } = new(Definitions.SquarenessAtHeelSection);
        public NumberParameter AsymmetryAtHeelSection { get; init; } = new(Definitions.AsymmetryAtHeelSection);
        public NumberParameter BlendAtHeelSection { get; init; } = new(Definitions.BlendAtHeelSection);
        public NumberParameter SquarenessAtHeelEnd { get; init; } = new(Definitions.SquarenessAtHeelEnd);
        public NumberParameter AsymmetryAtHeelEnd { get; init; } = new(Definitions.AsymmetryAtHeelEnd);
        public NumberParameter BlendAtHeelEnd { get; init; } = new(Definitions.BlendAtHeelEnd);

        public Fretboard Fretboard { get; init; } = new();
        public Nut Nut { get; init; } = new();
        public Headshape Headshape { get; init; } = new();

        // Library items the neck refers to. Null when none is chosen.
        public Fretwire Fretwire { get; init; }
        public StringSet StringSet { get; init; }

        // The nut's total height, from its base to its top: the fretboard's thickness at the nut plus the nut's
        // reveal. Null when either isn't set.
        public double? NutHeight => Fretboard.ThicknessAtNut.Value + Nut.Reveal.Value;

        // The narrowest the fretboard can be at the nut for its string set: the setbacks, plus the span between the
        // outer strings' centerlines with every string touching. Null when there's no string set, or a setback or
        // string diameter isn't set.
        public double? MinimumNutWidth
        {
            get
            {
                var strings = StringSet?.Strings;
                if (strings == null || strings.Count == 0 || strings.Any(s => !s.Diameter.HasValue))
                    return null;
                double span = strings.Sum(s => s.Diameter.Value.Value) -
                    (strings[0].Diameter.Value.Value + strings[^1].Diameter.Value.Value) / 2;
                return Fretboard.BassStringSetback.Value + Fretboard.TrebleStringSetback.Value + span;
            }
        }

        protected override IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture)
        {
            if (ThicknessFret.Value > Fretboard.FretCount.Value)
                yield return Fretboard.PastLastFret(ThicknessFret);

            if (SideLengthAtHeelEnd.Value >= HeelEndThickness.Value)
                yield return ModelMessage.Error($"{SideLengthAtHeelEnd.Label} must be less than {HeelEndThickness.Label}.");
            if (SideLengthAtHeelSection.Value >= HeelSectionThickness.Value)
                yield return ModelMessage.Error(
                    $"{SideLengthAtHeelSection.Label} must be less than {HeelSectionThickness.Label}.");
            // When the setbacks alone don't fit, the fretboard already says so.
            var nutWidth = Fretboard.NutWidth;
            if (Fretboard.SetbacksFitNutWidth && MinimumNutWidth is double minimum && nutWidth.Value < minimum)
                yield return ModelMessage.Error(
                    $"{nutWidth.Label} is too narrow for the string set: with the setbacks it needs at least " +
                    $"{(nutWidth with { Value = minimum }).FormatWithUnit(unit, culture)}.");
        }
    }
}
