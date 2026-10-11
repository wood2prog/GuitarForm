using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    public enum BraceProfileType
    {
        Scalloped,
        Tapered,
    }

    // The braces glued under the top plate: the X brace, graft, upper transverse bar, finger braces, tone bars,
    // soundhole braces and bridge patch. Like the neck and body, the bracing is saved on its own, so a bracing
    // pattern can be reused in another guitar.
    public sealed record Bracing : Part
    {
        public override bool IsSavedSeparately => true;

        public static class Definitions
        {
            public static readonly ParameterDefinition XBraceOffsetFromNeckEnd = ParameterDefinition.Distance("X Brace Offset from Neck End", "XBO");
            public static readonly ParameterDefinition XBraceAngle = ParameterDefinition.Angle("X Brace Angle", "XBA", 0, 90, exclusive: true);
            public static readonly ParameterDefinition XBraceEdgeOffset = ParameterDefinition.Distance("X Brace Edge Offset", "XBEO");
            public static readonly ParameterDefinition XBraceWidth = ParameterDefinition.Size("X Brace Width", "XBW");
            public static readonly ParameterDefinition XBraceHeight = ParameterDefinition.Size("X Brace Height", "XBH");
            public static readonly ParameterDefinition XBraceProfileType = ParameterDefinition.Choice("X Brace Profile Type", "XBPT");

            public static readonly ParameterDefinition Graft = ParameterDefinition.Flag("Graft", "GR");
            public static readonly ParameterDefinition GraftThickness = ParameterDefinition.Size("Graft Thickness", "GRT");
            public static readonly ParameterDefinition GraftWidth = ParameterDefinition.Size("Graft Width", "GRW");
            public static readonly ParameterDefinition GraftOffsetFromNeckEnd = ParameterDefinition.Distance("Graft Offset from Neck End", "GRO");
            public static readonly ParameterDefinition GraftEdgeOffset = ParameterDefinition.Distance("Graft Edge Offset", "GREO");

            public static readonly ParameterDefinition UpperTransverseBarOffsetFromNeckEnd = ParameterDefinition.Distance("Upper Transverse Bar Offset from Neck End", "UTO");
            public static readonly ParameterDefinition UpperTransverseBarWidth = ParameterDefinition.Size("Upper Transverse Bar Width", "UTW");
            public static readonly ParameterDefinition UpperTransverseBarHeight = ParameterDefinition.Size("Upper Transverse Bar Height", "UTH");
            public static readonly ParameterDefinition UpperTransverseBarEdgeOffset = ParameterDefinition.Distance("Upper Transverse Bar Edge Offset", "UTEO");

            public static readonly ParameterDefinition BassFingerBraceCount = ParameterDefinition.Count("Bass Finger Brace Count", "BFNC", 0);
            public static readonly ParameterDefinition BassFingerBraceAngle = ParameterDefinition.Angle("Bass Finger Brace Angle", "BFNA");
            public static readonly ParameterDefinition BassFingerBraceSpacing = ParameterDefinition.Size("Bass Finger Brace Spacing", "BFNS");
            public static readonly ParameterDefinition TrebleFingerBraceCount = ParameterDefinition.Count("Treble Finger Brace Count", "TFNC", 0);
            public static readonly ParameterDefinition TrebleFingerBraceAngle = ParameterDefinition.Angle("Treble Finger Brace Angle", "TFNA");
            public static readonly ParameterDefinition TrebleFingerBraceSpacing = ParameterDefinition.Size("Treble Finger Brace Spacing", "TFNS");
            public static readonly ParameterDefinition FingerBraceWidth = ParameterDefinition.Size("Finger Brace Width", "FNW");
            public static readonly ParameterDefinition FingerBraceHeight = ParameterDefinition.Size("Finger Brace Height", "FNH");
            public static readonly ParameterDefinition FingerBraceEdgeOffset = ParameterDefinition.Distance("Finger Brace Edge Offset", "FNEO");

            public static readonly ParameterDefinition ToneBarCount = ParameterDefinition.Count("Tone Bar Count", "TNC", 0);
            public static readonly ParameterDefinition ToneBarSpacing = ParameterDefinition.Size("Tone Bar Spacing", "TNS");
            public static readonly ParameterDefinition ToneBarAngle = ParameterDefinition.Angle("Tone Bar Angle", "TNA");
            public static readonly ParameterDefinition ToneBarWidth = ParameterDefinition.Size("Tone Bar Width", "TNW");
            public static readonly ParameterDefinition ToneBarHeight = ParameterDefinition.Size("Tone Bar Height", "TNH");
            public static readonly ParameterDefinition ToneBarEdgeOffset = ParameterDefinition.Distance("Tone Bar Edge Offset", "TNEO");

            public static readonly ParameterDefinition SoundholeBraces = ParameterDefinition.Flag("Soundhole Braces", "SHB");
            public static readonly ParameterDefinition SoundholeBraceOffset = ParameterDefinition.Distance("Soundhole Brace Offset", "SHBO");
            public static readonly ParameterDefinition SoundholeBraceWidth = ParameterDefinition.Size("Soundhole Brace Width", "SHBW");
            public static readonly ParameterDefinition SoundholeBraceHeight = ParameterDefinition.Size("Soundhole Brace Height", "SHBH");

            public static readonly ParameterDefinition BridgePatchThickness = ParameterDefinition.Size("Bridge Patch Thickness", "BPT");
            public static readonly ParameterDefinition BridgePatchWidthOffset = ParameterDefinition.Offset("Bridge Patch Width Offset", "BPWO");
            public static readonly ParameterDefinition BridgePatchOffset = ParameterDefinition.Offset("Bridge Patch Offset", "BPO");
        }

        public NumberParameter XBraceOffsetFromNeckEnd { get; init; } = new(Definitions.XBraceOffsetFromNeckEnd);
        public NumberParameter XBraceAngle { get; init; } = new(Definitions.XBraceAngle);
        public NumberParameter XBraceEdgeOffset { get; init; } = new(Definitions.XBraceEdgeOffset);
        public NumberParameter XBraceWidth { get; init; } = new(Definitions.XBraceWidth);
        public NumberParameter XBraceHeight { get; init; } = new(Definitions.XBraceHeight);
        public ChoiceParameter<BraceProfileType> XBraceProfileType { get; init; } = new(Definitions.XBraceProfileType);

        public FlagParameter Graft { get; init; } = new(Definitions.Graft);
        public NumberParameter GraftThickness { get; init; } = new(Definitions.GraftThickness);
        public NumberParameter GraftWidth { get; init; } = new(Definitions.GraftWidth);
        public NumberParameter GraftOffsetFromNeckEnd { get; init; } = new(Definitions.GraftOffsetFromNeckEnd);
        public NumberParameter GraftEdgeOffset { get; init; } = new(Definitions.GraftEdgeOffset);

        public NumberParameter UpperTransverseBarOffsetFromNeckEnd { get; init; } = new(Definitions.UpperTransverseBarOffsetFromNeckEnd);
        public NumberParameter UpperTransverseBarWidth { get; init; } = new(Definitions.UpperTransverseBarWidth);
        public NumberParameter UpperTransverseBarHeight { get; init; } = new(Definitions.UpperTransverseBarHeight);
        public NumberParameter UpperTransverseBarEdgeOffset { get; init; } = new(Definitions.UpperTransverseBarEdgeOffset);

        public CountParameter BassFingerBraceCount { get; init; } = new(Definitions.BassFingerBraceCount);
        public NumberParameter BassFingerBraceAngle { get; init; } = new(Definitions.BassFingerBraceAngle);
        public NumberParameter BassFingerBraceSpacing { get; init; } = new(Definitions.BassFingerBraceSpacing);
        public CountParameter TrebleFingerBraceCount { get; init; } = new(Definitions.TrebleFingerBraceCount);
        public NumberParameter TrebleFingerBraceAngle { get; init; } = new(Definitions.TrebleFingerBraceAngle);
        public NumberParameter TrebleFingerBraceSpacing { get; init; } = new(Definitions.TrebleFingerBraceSpacing);
        public NumberParameter FingerBraceWidth { get; init; } = new(Definitions.FingerBraceWidth);
        public NumberParameter FingerBraceHeight { get; init; } = new(Definitions.FingerBraceHeight);
        public NumberParameter FingerBraceEdgeOffset { get; init; } = new(Definitions.FingerBraceEdgeOffset);

        public CountParameter ToneBarCount { get; init; } = new(Definitions.ToneBarCount);
        public NumberParameter ToneBarSpacing { get; init; } = new(Definitions.ToneBarSpacing);
        public NumberParameter ToneBarAngle { get; init; } = new(Definitions.ToneBarAngle);
        public NumberParameter ToneBarWidth { get; init; } = new(Definitions.ToneBarWidth);
        public NumberParameter ToneBarHeight { get; init; } = new(Definitions.ToneBarHeight);
        public NumberParameter ToneBarEdgeOffset { get; init; } = new(Definitions.ToneBarEdgeOffset);

        public FlagParameter SoundholeBraces { get; init; } = new(Definitions.SoundholeBraces);
        public NumberParameter SoundholeBraceOffset { get; init; } = new(Definitions.SoundholeBraceOffset);
        public NumberParameter SoundholeBraceWidth { get; init; } = new(Definitions.SoundholeBraceWidth);
        public NumberParameter SoundholeBraceHeight { get; init; } = new(Definitions.SoundholeBraceHeight);

        public NumberParameter BridgePatchThickness { get; init; } = new(Definitions.BridgePatchThickness);
        public NumberParameter BridgePatchWidthOffset { get; init; } = new(Definitions.BridgePatchWidthOffset);
        public NumberParameter BridgePatchOffset { get; init; } = new(Definitions.BridgePatchOffset);

        protected override IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture)
        {
            var messages = Enumerable.Empty<ModelMessage>();
            if (BassFingerBraceCount.Value > 0)
                messages = messages.Concat(RequiredWhen(BassFingerBraceCount, unit, culture,
                    BassFingerBraceAngle, BassFingerBraceSpacing));
            if (TrebleFingerBraceCount.Value > 0)
                messages = messages.Concat(RequiredWhen(TrebleFingerBraceCount, unit, culture,
                    TrebleFingerBraceAngle, TrebleFingerBraceSpacing));
            // The width and height are shared by both sides, so they're asked for once.
            var fingerCount = BassFingerBraceCount.Value > 0 ? BassFingerBraceCount
                : TrebleFingerBraceCount.Value > 0 ? TrebleFingerBraceCount : null;
            if (fingerCount != null)
                messages = messages.Concat(RequiredWhen(fingerCount, unit, culture, FingerBraceWidth, FingerBraceHeight));
            if (ToneBarCount.Value > 0)
                messages = messages.Concat(RequiredWhen(ToneBarCount, unit, culture,
                    ToneBarAngle, ToneBarSpacing, ToneBarWidth, ToneBarHeight));
            if (Graft.Value)
                messages = messages.Concat(RequiredWhen(Graft, unit, culture,
                    GraftThickness, GraftWidth, GraftOffsetFromNeckEnd));
            if (SoundholeBraces.Value)
                messages = messages.Concat(RequiredWhen(SoundholeBraces, unit, culture,
                    SoundholeBraceOffset, SoundholeBraceWidth, SoundholeBraceHeight));
            return messages;
        }
    }
}
