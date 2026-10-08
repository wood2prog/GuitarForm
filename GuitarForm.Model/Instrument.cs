using System;
using System.Collections.Generic;

namespace GuitarForm.Model
{
    // Values that apply to the whole instrument. Lengths are in millimetres in a design. The Grasshopper Instrument
    // component also uses this record, with lengths in the Rhino document's units, until it's removed.
    public sealed record Instrument
    {
        // Distance from the nut to the saddle (the vibrating string length).
        public double ScaleLength { get; init; }

        // The fret at which the neck joins the body, e.g. 14 for a typical steel-string acoustic.
        public int NeckJoinFret { get; init; }

        // Distance from the nut to the given fret, with equal-tempered fret spacing: L - L / 2^(n/12).
        public double FretDistance(int fret) => ScaleLength - ScaleLength / Math.Pow(2, fret / 12.0);

        // Distance from the nut to the neck join fret.
        public double NeckJoinDistance => FretDistance(NeckJoinFret);

        // Problems with the values. The instrument shouldn't be used if any of them is an error.
        public List<GeometryMessage> Validate()
        {
            var messages = new List<GeometryMessage>();
            if (!(ScaleLength > 0))
                messages.Add(new GeometryMessage(MessageLevel.Error, "Scale length must be greater than zero."));
            if (NeckJoinFret < 1)
                messages.Add(new GeometryMessage(MessageLevel.Error, "Neck join fret must be 1 or more."));
            return messages;
        }
    }
}
