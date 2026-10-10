using System;
using System.Collections.Generic;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    // A whole guitar: the values that belong to the whole instrument, its neck and its body. The neck and body are
    // sub-assemblies, each saved on its own so other guitars can use them.
    public sealed record Guitar : Part
    {
        public static class Definitions
        {
            public static readonly ParameterDefinition Name = ParameterDefinition.Text("Instrument Name", "IN", required: true);
            public static readonly ParameterDefinition Designer = ParameterDefinition.Text("Designer", "DSG");
            public static readonly ParameterDefinition ScaleLength = ParameterDefinition.Size("Scale Length", "SL", required: true);
            public static readonly ParameterDefinition NeckJoinFret = ParameterDefinition.Count("Neck Join Fret", "NJF", 1, required: true);
        }

        public TextParameter Name { get; init; } = new(Definitions.Name);
        public TextParameter Designer { get; init; } = new(Definitions.Designer);
        public NumberParameter ScaleLength { get; init; } = new(Definitions.ScaleLength);
        public CountParameter NeckJoinFret { get; init; } = new(Definitions.NeckJoinFret);

        public Neck Neck { get; init; } = new();
        public Body Body { get; init; } = new();

        // Distance from the nut to the given fret, with equal-tempered fret spacing: L - L / 2^(n/12). Null when the
        // scale length isn't set.
        public double? FretDistance(int fret) =>
            ScaleLength.Value is double scale ? scale - scale / Math.Pow(2, fret / 12.0) : null;

        // Distance from the nut to the neck join fret. Null when either isn't set.
        public double? NeckJoinDistance => NeckJoinFret.Value is int fret ? FretDistance(fret) : null;

        protected override IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture)
        {
            if (NeckJoinFret.Value is int join && Neck.Fretboard.FretCount.Value is int count && join > count)
                yield return ModelMessage.Error(
                    $"{NeckJoinFret.Label} can't be past the last fret ({Neck.Fretboard.FretCount.Label} is {count}).");
        }
    }
}
