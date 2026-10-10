using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    public enum ArchingType
    {
        Flat,
        Spherical,
        Custom,
    }

    // The top plate: its thickness, arching and soundhole. It holds the bracing glued under it.
    public sealed record TopPlate : Part
    {
        public static class Definitions
        {
            public static readonly ParameterDefinition Thickness = ParameterDefinition.Size("Top Plate Thickness", "TPT");
            public static readonly ParameterDefinition ArchingType = ParameterDefinition.Choice("Top Arching Type", "TPAT");
            public static readonly ParameterDefinition SphericalRadius = ParameterDefinition.Size("Top Spherical Radius", "TPSR");
            public static readonly ParameterDefinition ArchingHeightAtBridge = ParameterDefinition.Distance("Top Arching Height at Bridge", "TPAB");
            public static readonly ParameterDefinition ArchingOffsetFromEdge = ParameterDefinition.Distance("Top Arching Offset from Edge", "TPAO");

            public static readonly ParameterDefinition Soundhole = ParameterDefinition.Flag("Soundhole", "SH");
            public static readonly ParameterDefinition SoundholeDiameter = ParameterDefinition.Size("Soundhole Diameter", "SHD");
            public static readonly ParameterDefinition SoundholeOffsetFromCenterline = ParameterDefinition.Offset("Soundhole Offset from Centerline", "SHOC");
            public static readonly ParameterDefinition SoundholeOffsetFromTailEnd = ParameterDefinition.Distance("Soundhole Offset from Tail End", "SHOT");
        }

        public NumberParameter Thickness { get; init; } = new(Definitions.Thickness);
        public ChoiceParameter<ArchingType> ArchingType { get; init; } = new(Definitions.ArchingType);
        public NumberParameter SphericalRadius { get; init; } = new(Definitions.SphericalRadius);
        public NumberParameter ArchingHeightAtBridge { get; init; } = new(Definitions.ArchingHeightAtBridge);
        public NumberParameter ArchingOffsetFromEdge { get; init; } = new(Definitions.ArchingOffsetFromEdge);

        public FlagParameter Soundhole { get; init; } = new(Definitions.Soundhole);
        public NumberParameter SoundholeDiameter { get; init; } = new(Definitions.SoundholeDiameter);
        public NumberParameter SoundholeOffsetFromCenterline { get; init; } = new(Definitions.SoundholeOffsetFromCenterline);
        public NumberParameter SoundholeOffsetFromTailEnd { get; init; } = new(Definitions.SoundholeOffsetFromTailEnd);

        public Bracing Bracing { get; init; } = new();
    }

    // The back plate: its thickness and arching.
    public sealed record BackPlate : Part
    {
        public static class Definitions
        {
            public static readonly ParameterDefinition Thickness = ParameterDefinition.Size("Back Plate Thickness", "BKT");
            public static readonly ParameterDefinition ArchingType = ParameterDefinition.Choice("Back Arching Type", "BKAT");
            public static readonly ParameterDefinition SphericalRadius = ParameterDefinition.Size("Back Spherical Radius", "BKSR");
            public static readonly ParameterDefinition ArchingHeightAtCenter = ParameterDefinition.Distance("Back Arching Height at Center", "BKAC");
            public static readonly ParameterDefinition ArchingOffsetFromEdge = ParameterDefinition.Distance("Back Arching Offset from Edge", "BKAO");
        }

        public NumberParameter Thickness { get; init; } = new(Definitions.Thickness);
        public ChoiceParameter<ArchingType> ArchingType { get; init; } = new(Definitions.ArchingType);
        public NumberParameter SphericalRadius { get; init; } = new(Definitions.SphericalRadius);
        public NumberParameter ArchingHeightAtCenter { get; init; } = new(Definitions.ArchingHeightAtCenter);
        public NumberParameter ArchingOffsetFromEdge { get; init; } = new(Definitions.ArchingOffsetFromEdge);
    }
}
