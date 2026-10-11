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

        public static class Definitions
        {
            // Above 0°, a shoulder line is flat across; at 90° it runs straight down.
            static ParameterDefinition ShoulderAngle(string name, string abbreviation) =>
                ParameterDefinition.Angle(name, abbreviation, 0, 90) with { MinimumExclusive = true };

            public static readonly ParameterDefinition Angle = ParameterDefinition.Angle("Neck Angle", "NKA");
            public static readonly ParameterDefinition Overstand = ParameterDefinition.Distance("Overstand", "OVS");
            public static readonly ParameterDefinition ThicknessAtFirstFret = ParameterDefinition.Size("Neck Thickness at First Fret", "NKTF");
            public static readonly ParameterDefinition ThicknessAtSelectedFret = ParameterDefinition.Size("Neck Thickness at Selected Fret", "NKTS");
            public static readonly ParameterDefinition ThicknessFret = ParameterDefinition.Count("Neck Thickness Fret", "NKSF", 1);

            public static readonly ParameterDefinition BottomRadiusAtFirstFret = ParameterDefinition.Size("Profile Bottom Radius at First Fret", "PBRF");
            public static readonly ParameterDefinition ShoulderAngleAtFirstFret = ShoulderAngle("Profile Shoulder Angle at First Fret", "PSAF");
            public static readonly ParameterDefinition MidshoulderRadiusAtFirstFret = ParameterDefinition.Size("Profile Midshoulder Radius at First Fret", "PMRF");
            public static readonly ParameterDefinition CenterOffsetAtFirstFret = ParameterDefinition.Offset("Profile Center Offset at First Fret", "PCOF");
            public static readonly ParameterDefinition IncludesFretboardAtFirstFret = ParameterDefinition.Flag("Profile Includes Fretboard at First Fret", "PIFF");

            public static readonly ParameterDefinition UsesFirstFretAtSelectedFret = ParameterDefinition.Flag("Profile Uses First Fret at Selected Fret", "PUFS");
            public static readonly ParameterDefinition BottomRadiusAtSelectedFret = ParameterDefinition.Size("Profile Bottom Radius at Selected Fret", "PBRS");
            public static readonly ParameterDefinition ShoulderAngleAtSelectedFret = ShoulderAngle("Profile Shoulder Angle at Selected Fret", "PSAS");
            public static readonly ParameterDefinition MidshoulderRadiusAtSelectedFret = ParameterDefinition.Size("Profile Midshoulder Radius at Selected Fret", "PMRS");
            public static readonly ParameterDefinition CenterOffsetAtSelectedFret = ParameterDefinition.Offset("Profile Center Offset at Selected Fret", "PCOS");
            public static readonly ParameterDefinition IncludesFretboardAtSelectedFret = ParameterDefinition.Flag("Profile Includes Fretboard at Selected Fret", "PIFS");

            public static readonly ParameterDefinition HeelShaftRadius = ParameterDefinition.Size("Heel Shaft Radius", "HLSR");
            public static readonly ParameterDefinition HeelShaftOffset = ParameterDefinition.Distance("Heel Shaft Offset", "HLSO");

            public static readonly ParameterDefinition HeelEndThickness = ParameterDefinition.Size("Heel End Thickness", "HLET");
            public static readonly ParameterDefinition HeelEndOffsetFromBack = ParameterDefinition.Distance("Heel End Offset from Back", "HLEO");
            public static readonly ParameterDefinition HeelEndWidth = ParameterDefinition.Size("Heel End Width", "HLEW");
            public static readonly ParameterDefinition HeelCap = ParameterDefinition.Flag("Heel Cap", "HCP");
            public static readonly ParameterDefinition HeelCapThickness = ParameterDefinition.Size("Heel Cap Thickness", "HCPT");
            public static readonly ParameterDefinition UsesFirstFretAtHeelEnd = ParameterDefinition.Flag("Profile Uses First Fret at Heel End", "PUFE");
            public static readonly ParameterDefinition BottomRadiusAtHeelEnd = ParameterDefinition.Size("Profile Bottom Radius at Heel End", "PBRE");
            public static readonly ParameterDefinition ShoulderAngleAtHeelEnd = ShoulderAngle("Profile Shoulder Angle at Heel End", "PSAE");
            public static readonly ParameterDefinition MidshoulderRadiusAtHeelEnd = ParameterDefinition.Size("Profile Midshoulder Radius at Heel End", "PMRE");
            public static readonly ParameterDefinition SideLengthAtHeelEnd = ParameterDefinition.Distance("Profile Side Length at Heel End", "PSLE");

            public static readonly ParameterDefinition HeelSectionThickness = ParameterDefinition.Size("Heel Section Thickness", "HLCT");
            public static readonly ParameterDefinition HeelSectionOffsetFromHeelEnd = ParameterDefinition.Distance("Heel Section Offset from Heel End", "HLCO");
            public static readonly ParameterDefinition HeelOutlineStraightSides = ParameterDefinition.Flag("Heel Outline Straight Sides", "HLOS");
            public static readonly ParameterDefinition HeelSectionWidth = ParameterDefinition.Size("Heel Section Width", "HLCW");
            public static readonly ParameterDefinition UsesFirstFretAtHeelSection = ParameterDefinition.Flag("Profile Uses First Fret at Heel Section", "PUFH");
            public static readonly ParameterDefinition BottomRadiusAtHeelSection = ParameterDefinition.Size("Profile Bottom Radius at Heel Section", "PBRH");
            public static readonly ParameterDefinition ShoulderAngleAtHeelSection = ShoulderAngle("Profile Shoulder Angle at Heel Section", "PSAH");
            public static readonly ParameterDefinition MidshoulderRadiusAtHeelSection = ParameterDefinition.Size("Profile Midshoulder Radius at Heel Section", "PMRH");
            public static readonly ParameterDefinition SideLengthAtHeelSection = ParameterDefinition.Distance("Profile Side Length at Heel Section", "PSLH");
        }

        public NumberParameter Angle { get; init; } = new(Definitions.Angle);
        public NumberParameter Overstand { get; init; } = new(Definitions.Overstand);
        public NumberParameter ThicknessAtFirstFret { get; init; } = new(Definitions.ThicknessAtFirstFret);
        public NumberParameter ThicknessAtSelectedFret { get; init; } = new(Definitions.ThicknessAtSelectedFret);
        public CountParameter ThicknessFret { get; init; } = new(Definitions.ThicknessFret);

        public NumberParameter BottomRadiusAtFirstFret { get; init; } = new(Definitions.BottomRadiusAtFirstFret);
        public NumberParameter ShoulderAngleAtFirstFret { get; init; } = new(Definitions.ShoulderAngleAtFirstFret);
        public NumberParameter MidshoulderRadiusAtFirstFret { get; init; } = new(Definitions.MidshoulderRadiusAtFirstFret);
        public NumberParameter CenterOffsetAtFirstFret { get; init; } = new(Definitions.CenterOffsetAtFirstFret);
        public FlagParameter IncludesFretboardAtFirstFret { get; init; } = new(Definitions.IncludesFretboardAtFirstFret);

        public FlagParameter UsesFirstFretAtSelectedFret { get; init; } = new(Definitions.UsesFirstFretAtSelectedFret);
        public NumberParameter BottomRadiusAtSelectedFret { get; init; } = new(Definitions.BottomRadiusAtSelectedFret);
        public NumberParameter ShoulderAngleAtSelectedFret { get; init; } = new(Definitions.ShoulderAngleAtSelectedFret);
        public NumberParameter MidshoulderRadiusAtSelectedFret { get; init; } = new(Definitions.MidshoulderRadiusAtSelectedFret);
        public NumberParameter CenterOffsetAtSelectedFret { get; init; } = new(Definitions.CenterOffsetAtSelectedFret);
        public FlagParameter IncludesFretboardAtSelectedFret { get; init; } = new(Definitions.IncludesFretboardAtSelectedFret);

        public NumberParameter HeelShaftRadius { get; init; } = new(Definitions.HeelShaftRadius);
        public NumberParameter HeelShaftOffset { get; init; } = new(Definitions.HeelShaftOffset);

        public NumberParameter HeelEndThickness { get; init; } = new(Definitions.HeelEndThickness);
        public NumberParameter HeelEndOffsetFromBack { get; init; } = new(Definitions.HeelEndOffsetFromBack);
        public NumberParameter HeelEndWidth { get; init; } = new(Definitions.HeelEndWidth);
        public FlagParameter HeelCap { get; init; } = new(Definitions.HeelCap);
        public NumberParameter HeelCapThickness { get; init; } = new(Definitions.HeelCapThickness);
        public FlagParameter UsesFirstFretAtHeelEnd { get; init; } = new(Definitions.UsesFirstFretAtHeelEnd);
        public NumberParameter BottomRadiusAtHeelEnd { get; init; } = new(Definitions.BottomRadiusAtHeelEnd);
        public NumberParameter ShoulderAngleAtHeelEnd { get; init; } = new(Definitions.ShoulderAngleAtHeelEnd);
        public NumberParameter MidshoulderRadiusAtHeelEnd { get; init; } = new(Definitions.MidshoulderRadiusAtHeelEnd);
        public NumberParameter SideLengthAtHeelEnd { get; init; } = new(Definitions.SideLengthAtHeelEnd);

        public NumberParameter HeelSectionThickness { get; init; } = new(Definitions.HeelSectionThickness);
        public NumberParameter HeelSectionOffsetFromHeelEnd { get; init; } = new(Definitions.HeelSectionOffsetFromHeelEnd);
        public FlagParameter HeelOutlineStraightSides { get; init; } = new(Definitions.HeelOutlineStraightSides);
        public NumberParameter HeelSectionWidth { get; init; } = new(Definitions.HeelSectionWidth);
        public FlagParameter UsesFirstFretAtHeelSection { get; init; } = new(Definitions.UsesFirstFretAtHeelSection);
        public NumberParameter BottomRadiusAtHeelSection { get; init; } = new(Definitions.BottomRadiusAtHeelSection);
        public NumberParameter ShoulderAngleAtHeelSection { get; init; } = new(Definitions.ShoulderAngleAtHeelSection);
        public NumberParameter MidshoulderRadiusAtHeelSection { get; init; } = new(Definitions.MidshoulderRadiusAtHeelSection);
        public NumberParameter SideLengthAtHeelSection { get; init; } = new(Definitions.SideLengthAtHeelSection);

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

            // Each profile's midshoulder fillet sits inside its bottom arc. A profile that uses the first fret's
            // settings is checked with the first fret.
            var profiles = new (bool UsesFirstFret, NumberParameter Midshoulder, NumberParameter Bottom)[]
            {
                (false, MidshoulderRadiusAtFirstFret, BottomRadiusAtFirstFret),
                (UsesFirstFretAtSelectedFret.Value, MidshoulderRadiusAtSelectedFret, BottomRadiusAtSelectedFret),
                (UsesFirstFretAtHeelEnd.Value, MidshoulderRadiusAtHeelEnd, BottomRadiusAtHeelEnd),
                (UsesFirstFretAtHeelSection.Value, MidshoulderRadiusAtHeelSection, BottomRadiusAtHeelSection),
            };
            foreach (var (usesFirstFret, midshoulder, bottom) in profiles)
                if (!usesFirstFret && midshoulder.Value >= bottom.Value)
                    yield return ModelMessage.Error($"{midshoulder.Label} must be less than {bottom.Label}.");

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
