using System;
using Grasshopper.Kernel;
using GuitarForm.Geometry;
using GuitarForm.Types;

namespace GuitarForm.Components
{
    // Collects the values that apply to the whole instrument into one Instrument object for other components to read.
    public class InstrumentComponent : GuitarFormComponent
    {
        public InstrumentComponent()
          : base("Instrument", "Instrument",
              "Values that apply to the whole instrument, such as the scale length, passed to other components as one Instrument object.",
              "Instrument")
        {
        }

        // Input positions. These must match the registration order in RegisterInputParams.
        const int InScaleLength = 0;
        const int InNeckJoinFret = 1;

        // Output positions. These must match the registration order in RegisterOutputParams.
        const int OutInstrument = 0;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Scale Length", "SL", "Distance from the nut to the saddle", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Neck Join Fret", "NJ", "The fret at which the neck joins the body", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new InstrumentParameter(), "Instrument", "I", "The instrument values, for other GuitarForm components", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double scaleLength = 0;
            int neckJoinFret = 0;
            if (!DA.GetData(InScaleLength, ref scaleLength)) return;
            if (!DA.GetData(InNeckJoinFret, ref neckJoinFret)) return;

            var instrument = new Instrument
            {
                ScaleLength = scaleLength,
                NeckJoinFret = neckJoinFret,
            };

            var messages = instrument.Validate();
            AddMessages(messages);
            if (messages.Exists(m => m.Level == MessageLevel.Error)) return;

            DA.SetData(OutInstrument, new GH_Instrument(instrument));
        }

        public override Guid ComponentGuid => new Guid("84bc17a4-657b-45dd-94ac-eab4e35e5839");
    }
}
