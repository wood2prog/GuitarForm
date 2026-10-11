using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    // The nut. Its width is the fretboard's Nut Width, and its height is calculated by the neck (Neck.NutHeight).
    public sealed record Nut : Part
    {
        public static class Definitions
        {
            public static readonly ParameterDefinition Thickness = ParameterDefinition.Size("Nut Thickness", "NTT");
            public static readonly ParameterDefinition Reveal = ParameterDefinition.Distance("Nut Reveal", "NTR");
        }

        public NumberParameter Thickness { get; init; } = new(Definitions.Thickness);
        public NumberParameter Reveal { get; init; } = new(Definitions.Reveal);
    }

    // The headstock's shape. It has no settings yet: it will be filled in by importing a shape.
    public sealed record Headshape : Part;
}
