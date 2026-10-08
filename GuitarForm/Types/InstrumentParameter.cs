using System;
using Grasshopper.Kernel;

namespace GuitarForm.Types
{
    // Parameter for Instrument values, used for the Instrument component's output and the inputs of components that
    // read it. Hidden from the toolbar: an instrument only comes from the Instrument component.
    public class InstrumentParameter : GH_Param<GH_Instrument>
    {
        public InstrumentParameter()
          : base("Instrument", "I", "Values that apply to the whole instrument, from the Instrument component",
              "GuitarForm", "Instrument", GH_ParamAccess.item)
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("73d17053-d5f0-41e7-b1f0-bf21b41e244f");
    }
}
