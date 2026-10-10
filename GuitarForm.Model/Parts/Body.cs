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
    }
}
