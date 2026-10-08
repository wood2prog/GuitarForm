using Grasshopper.Kernel.Types;
using GuitarForm.Geometry;

namespace GuitarForm.Types
{
    // Grasshopper wrapper for Instrument, so it can travel along wires from the Instrument component.
    public class GH_Instrument : GH_Goo<Instrument>
    {
        public GH_Instrument()
        {
        }

        public GH_Instrument(Instrument instrument)
          : base(instrument)
        {
        }

        public override bool IsValid => Value != null;

        public override string TypeName => "Instrument";

        public override string TypeDescription => "Values that apply to the whole instrument, such as the scale length";

        // Instrument is an immutable record, so the copy can share it.
        public override IGH_Goo Duplicate() => new GH_Instrument(Value);

        public override string ToString() =>
            Value == null ? "Null Instrument" : $"Instrument (Scale {Value.ScaleLength}, Neck Join Fret {Value.NeckJoinFret})";

        public override bool CastFrom(object source)
        {
            if (source is Instrument instrument)
            {
                Value = instrument;
                return true;
            }
            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(Instrument)))
            {
                target = (Q)(object)Value;
                return true;
            }
            return false;
        }
    }
}
