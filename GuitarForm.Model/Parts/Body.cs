using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    public enum CutawayType
    {
        None,
        Florentine,
        Venetian,
    }

    // The body: its length and depths, the plate outline, the cutaway, the bridge heights and the head and tail
    // blocks. It holds the top and back plates.
    public sealed record Body : Part
    {
        public override bool IsSavedSeparately => true;

        public static class Definitions
        {
            public static readonly ParameterDefinition Length = ParameterDefinition.Size("Body Length", "BDL", required: true);
            public static readonly ParameterDefinition DepthAtNeckEnd = ParameterDefinition.Size("Body Depth at Neck End", "BDDN");
            public static readonly ParameterDefinition DepthAtTailEnd = ParameterDefinition.Size("Body Depth at Tail End", "BDDT");
            public static readonly ParameterDefinition ProfileSkew = ParameterDefinition.Offset("Body Profile Skew", "BDPS");

            public static readonly ParameterDefinition UpperBoutWidth = ParameterDefinition.Size("Upper Bout Width", "UBW");
            public static readonly ParameterDefinition UpperBoutPrimaryRadius = ParameterDefinition.Size("Upper Bout Primary Radius", "UBPR");
            public static readonly ParameterDefinition UpperBoutOffsetFromNeckEnd = ParameterDefinition.Distance("Upper Bout Offset from Neck End", "UBO");
            public static readonly ParameterDefinition UpperBoutSecondaryOffset = ParameterDefinition.Distance("Upper Bout Secondary Offset", "UBSO");
            public static readonly ParameterDefinition HeelFlatWidth = ParameterDefinition.Distance("Heel Flat Width", "HFW");
            public static readonly ParameterDefinition WaistWidth = ParameterDefinition.Size("Waist Width", "WSW");
            public static readonly ParameterDefinition WaistRadius = ParameterDefinition.Size("Waist Radius", "WSR");
            public static readonly ParameterDefinition WaistOffsetFromTailEnd = ParameterDefinition.Distance("Waist Offset from Tail End", "WSO");
            public static readonly ParameterDefinition LowerBoutWidth = ParameterDefinition.Size("Lower Bout Width", "LBW");
            public static readonly ParameterDefinition LowerBoutPrimaryRadius = ParameterDefinition.Size("Lower Bout Primary Radius", "LBPR");
            public static readonly ParameterDefinition LowerBoutSecondaryOffset = ParameterDefinition.Distance("Lower Bout Secondary Offset", "LBSO");
            public static readonly ParameterDefinition LowerBoutOffsetFromTailEnd = ParameterDefinition.Distance("Lower Bout Offset from Tail End", "LBO");

            public static readonly ParameterDefinition CutawayType = ParameterDefinition.Choice("Cutaway Type", "CTT");
            public static readonly ParameterDefinition CutawayOffsetFromNeck = ParameterDefinition.Distance("Cutaway Offset from Neck", "CTO");
            public static readonly ParameterDefinition CutawayWidth = ParameterDefinition.Size("Cutaway Width", "CTW");
            public static readonly ParameterDefinition CutawayDepth = ParameterDefinition.Distance("Cutaway Depth", "CTD");
            public static readonly ParameterDefinition CutawayValleyRadius = ParameterDefinition.Size("Cutaway Valley Radius", "CTVR");
            public static readonly ParameterDefinition CutawayPointRadius = ParameterDefinition.Size("Cutaway Point Radius", "CTPR");

            public static readonly ParameterDefinition BridgeHeight = ParameterDefinition.Size("Bridge Height", "BRH");
            public static readonly ParameterDefinition SaddleExposure = ParameterDefinition.Distance("Saddle Exposure", "SDLE");

            public static readonly ParameterDefinition HeadblockWidth = ParameterDefinition.Size("Headblock Width", "HBW");
            public static readonly ParameterDefinition HeadblockDepth = ParameterDefinition.Size("Headblock Depth", "HBD");
            public static readonly ParameterDefinition TailblockWidth = ParameterDefinition.Size("Tailblock Width", "TBW");
            public static readonly ParameterDefinition TailblockDepth = ParameterDefinition.Size("Tailblock Depth", "TBD");
        }

        public NumberParameter Length { get; init; } = new(Definitions.Length);
        public NumberParameter DepthAtNeckEnd { get; init; } = new(Definitions.DepthAtNeckEnd);
        public NumberParameter DepthAtTailEnd { get; init; } = new(Definitions.DepthAtTailEnd);
        public NumberParameter ProfileSkew { get; init; } = new(Definitions.ProfileSkew);

        public NumberParameter UpperBoutWidth { get; init; } = new(Definitions.UpperBoutWidth);
        public NumberParameter UpperBoutPrimaryRadius { get; init; } = new(Definitions.UpperBoutPrimaryRadius);
        public NumberParameter UpperBoutOffsetFromNeckEnd { get; init; } = new(Definitions.UpperBoutOffsetFromNeckEnd);
        public NumberParameter UpperBoutSecondaryOffset { get; init; } = new(Definitions.UpperBoutSecondaryOffset);
        public NumberParameter HeelFlatWidth { get; init; } = new(Definitions.HeelFlatWidth);
        public NumberParameter WaistWidth { get; init; } = new(Definitions.WaistWidth);
        public NumberParameter WaistRadius { get; init; } = new(Definitions.WaistRadius);
        public NumberParameter WaistOffsetFromTailEnd { get; init; } = new(Definitions.WaistOffsetFromTailEnd);
        public NumberParameter LowerBoutWidth { get; init; } = new(Definitions.LowerBoutWidth);
        public NumberParameter LowerBoutPrimaryRadius { get; init; } = new(Definitions.LowerBoutPrimaryRadius);
        public NumberParameter LowerBoutSecondaryOffset { get; init; } = new(Definitions.LowerBoutSecondaryOffset);
        public NumberParameter LowerBoutOffsetFromTailEnd { get; init; } = new(Definitions.LowerBoutOffsetFromTailEnd);

        public ChoiceParameter<CutawayType> CutawayType { get; init; } = new(Definitions.CutawayType);
        public NumberParameter CutawayOffsetFromNeck { get; init; } = new(Definitions.CutawayOffsetFromNeck);
        public NumberParameter CutawayWidth { get; init; } = new(Definitions.CutawayWidth);
        public NumberParameter CutawayDepth { get; init; } = new(Definitions.CutawayDepth);
        public NumberParameter CutawayValleyRadius { get; init; } = new(Definitions.CutawayValleyRadius);
        public NumberParameter CutawayPointRadius { get; init; } = new(Definitions.CutawayPointRadius);

        public NumberParameter BridgeHeight { get; init; } = new(Definitions.BridgeHeight);
        public NumberParameter SaddleExposure { get; init; } = new(Definitions.SaddleExposure);

        public NumberParameter HeadblockWidth { get; init; } = new(Definitions.HeadblockWidth);
        public NumberParameter HeadblockDepth { get; init; } = new(Definitions.HeadblockDepth);
        public NumberParameter TailblockWidth { get; init; } = new(Definitions.TailblockWidth);
        public NumberParameter TailblockDepth { get; init; } = new(Definitions.TailblockDepth);

        public TopPlate TopPlate { get; init; } = new();
        public BackPlate BackPlate { get; init; } = new();

        // Distances from the tail end, along the centerline, to the widest points of the bouts. Null when a value
        // they need isn't set.
        public double? LowerBoutWidestOffset => LowerBoutOffsetFromTailEnd.Value + LowerBoutPrimaryRadius.Value;
        public double? UpperBoutWidestOffset =>
            Length.Value - UpperBoutOffsetFromNeckEnd.Value - UpperBoutPrimaryRadius.Value;

        // The widest the heel flat can be: its ends can't pass the centers of the upper bout primary radius, since a
        // radius tangent to both joins them.
        public double? MaximumHeelFlatWidth => UpperBoutWidth.Value - 2 * UpperBoutPrimaryRadius.Value;

        // How far a bout's secondary radius overlaps the waist radius on the same side; zero or less when they don't
        // overlap. The bouts and the waist meet by a line tangent to both, so they can't overlap. A secondary offset
        // that isn't set counts as 0, where the secondary radius is the primary. Null when a value it needs isn't set.
        public double? UpperBoutWaistOverlap =>
            WaistOverlap(UpperBoutWidth, UpperBoutPrimaryRadius, UpperBoutSecondaryOffset, UpperBoutWidestOffset);

        public double? LowerBoutWaistOverlap =>
            WaistOverlap(LowerBoutWidth, LowerBoutPrimaryRadius, LowerBoutSecondaryOffset, LowerBoutWidestOffset);

        double? WaistOverlap(NumberParameter width, NumberParameter primaryRadius, NumberParameter secondaryOffset,
            double? centerOffset)
        {
            if (!(width.Value is double boutWidth && primaryRadius.Value is double primary &&
                  centerOffset is double boutY && WaistWidth.Value is double waistWidth &&
                  WaistRadius.Value is double waistRadius && WaistOffsetFromTailEnd.Value is double waistY))
                return null;
            double offset = secondaryOffset.Value ?? 0;
            double boutX = boutWidth / 2 - primary - offset;
            double waistX = waistWidth / 2 + waistRadius;
            double distance = Math.Sqrt((waistX - boutX) * (waistX - boutX) + (waistY - boutY) * (waistY - boutY));
            return primary + offset + waistRadius - distance;
        }

        // The space along the centerline between the headblock and the upper transverse bar, where the graft goes.
        // Null when either isn't set.
        public double? GraftSpace =>
            TopPlate.Bracing.UpperTransverseBarOffsetFromNeckEnd.Value - HeadblockDepth.Value;

        // Whether the graft fits in its space. Null when that can't be told yet. A graft that doesn't fit is left out.
        public bool? GraftFits =>
            GraftSpace is double space && TopPlate.Bracing.GraftWidth.Value is double width ? width <= space : null;

        // Whether the graft is built: it's chosen and isn't known not to fit.
        public bool IncludesGraft => TopPlate.Bracing.Graft.Value && GraftFits != false;

        protected override IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture)
        {
            string Format(double millimeters) => (Length with { Value = millimeters }).FormatWithUnit(unit, culture);

            foreach (var bout in new[] { UpperBoutWidth, LowerBoutWidth })
                if (WaistWidth.Value > bout.Value)
                    yield return ModelMessage.Error($"{WaistWidth.Label} can't be more than {bout.Label}.");

            if (WaistOffsetFromTailEnd.Value is double waist)
            {
                if (LowerBoutWidestOffset is double lower && lower >= waist)
                    yield return ModelMessage.Error(
                        $"The lower bout's widest point ({Format(lower)} from the tail end) must be closer to the tail " +
                        $"end than the waist ({WaistOffsetFromTailEnd.Label}).");
                if (UpperBoutWidestOffset is double upper && upper <= waist)
                    yield return ModelMessage.Error(
                        $"The upper bout's widest point ({Format(upper)} from the tail end) must be farther from the " +
                        $"tail end than the waist ({WaistOffsetFromTailEnd.Label}).");
            }

            if (HeelFlatWidth.Value > MaximumHeelFlatWidth)
                yield return ModelMessage.Error(MaximumHeelFlatWidth < 0
                    ? $"{UpperBoutPrimaryRadius.Label} is more than half the {UpperBoutWidth.Label}, so its center " +
                      $"is past the centerline and no heel flat fits."
                    : $"{HeelFlatWidth.Label} can't reach past the centers of the {UpperBoutPrimaryRadius.Label}: " +
                      $"it can be at most {Format(MaximumHeelFlatWidth.Value)}.");

            // Shoulder arc: from the heel flat's end to the upper bout primary radius, when the radius sits below the
            // neck end. It meets the radius above its center only while the gap to the center is at least the offset;
            // otherwise it runs wider than the upper bout. At no offset this is the heel flat check above.
            if (HeelFlatWidth.Value <= MaximumHeelFlatWidth && UpperBoutOffsetFromNeckEnd.Value is double ubOffset &&
                ubOffset > 0 && (MaximumHeelFlatWidth - HeelFlatWidth.Value) / 2 is double shoulderGap &&
                shoulderGap < ubOffset)
                yield return ModelMessage.Error(
                    $"The shoulder arc would run wider than the {UpperBoutWidth.Label}: the gap from the heel flat's " +
                    $"end to the center of the {UpperBoutPrimaryRadius.Label} ({Format(shoulderGap)}) must be at " +
                    $"least the {UpperBoutOffsetFromNeckEnd.Label}.");

            // Tail arc: through the tail end to the lower bout primary radius, when the radius sits above the tail end.
            // It meets the radius below its center only while the center is at least the offset from the centerline.
            if (LowerBoutOffsetFromTailEnd.Value is double lbOffset && lbOffset > 0 &&
                LowerBoutWidth.Value / 2 - LowerBoutPrimaryRadius.Value is double tailCenter && tailCenter < lbOffset)
                yield return ModelMessage.Error(
                    $"The tail arc would run wider than the {LowerBoutWidth.Label}: the center of the " +
                    $"{LowerBoutPrimaryRadius.Label} ({Format(tailCenter)} from the centerline) must be at least the " +
                    $"{LowerBoutOffsetFromTailEnd.Label} from the centerline.");

            if (UpperBoutWaistOverlap > 0)
                yield return ModelMessage.Error(
                    $"The upper bout's secondary radius overlaps the {WaistRadius.Label} by {Format(UpperBoutWaistOverlap.Value)}.");
            if (LowerBoutWaistOverlap > 0)
                yield return ModelMessage.Error(
                    $"The lower bout's secondary radius overlaps the {WaistRadius.Label} by {Format(LowerBoutWaistOverlap.Value)}.");

            if (CutawayType.Value != Parts.CutawayType.None)
                foreach (var message in RequiredWhen(CutawayType, unit, culture,
                             CutawayOffsetFromNeck, CutawayWidth, CutawayDepth))
                    yield return message;
            if (CutawayType.Value == Parts.CutawayType.Venetian)
                foreach (var message in RequiredWhen(CutawayType, unit, culture, CutawayValleyRadius, CutawayPointRadius))
                    yield return message;

            if (HeadblockDepth.Value + TailblockDepth.Value >= Length.Value)
                yield return ModelMessage.Error(
                    $"{HeadblockDepth.Label} and {TailblockDepth.Label} together must be less than {Length.Label}.");

            var bracing = TopPlate.Bracing;
            var graft = bracing.GraftOffsetFromNeckEnd;
            var offsets = new[] { bracing.XBraceOffsetFromNeckEnd, graft, bracing.UpperTransverseBarOffsetFromNeckEnd };
            foreach (var offset in offsets.Where(offset => offset != graft || bracing.Graft.Value))
                if (offset.Value >= Length.Value)
                    yield return ModelMessage.Error($"{offset.Label} must be less than {Length.Label}.");

            // The graft sits between the headblock and the upper transverse bar. When there isn't room for it there,
            // it's left out, with a warning. When there is, it must be placed in that space.
            if (bracing.Graft.Value && GraftFits == false)
                yield return ModelMessage.Warning(
                    $"The graft is left out: the space between the headblock and the upper transverse bar " +
                    $"({Format(GraftSpace.Value)}) is narrower than the {bracing.GraftWidth.Label}.");
            else if (bracing.Graft.Value)
            {
                if (graft.Value < HeadblockDepth.Value)
                    yield return ModelMessage.Error(
                        $"The graft must sit past the headblock: {graft.Label} must be at least {HeadblockDepth.Label}.");
                if (graft.Value + bracing.GraftWidth.Value > bracing.UpperTransverseBarOffsetFromNeckEnd.Value)
                    yield return ModelMessage.Error(
                        $"The graft must end before the upper transverse bar: {graft.Label} plus " +
                        $"{bracing.GraftWidth.Label} can't be more than {bracing.UpperTransverseBarOffsetFromNeckEnd.Label}.");
            }

            // The soundhole must sit at least a diameter from the neck end. Without a diameter, it must be on the body.
            var soundhole = TopPlate.SoundholeOffsetFromTailEnd;
            double? soundholeLimit = Length.Value - (TopPlate.SoundholeDiameter.Value ?? 0);
            if (TopPlate.Soundhole.Value && soundhole.Value >= soundholeLimit)
                yield return ModelMessage.Error(TopPlate.SoundholeDiameter.HasValue
                    ? $"{soundhole.Label} must be less than {Length.Label} minus {TopPlate.SoundholeDiameter.Label} " +
                      $"({Format(soundholeLimit.Value)})."
                    : $"{soundhole.Label} must be less than {Length.Label}.");
        }
    }
}
