using System;
using System.Drawing;
using Grasshopper.Kernel;
using GuitarForm.Geometry;
using Rhino.Geometry;

namespace GuitarForm.Components
{
    // Grasshopper wrapper for SoundholeGeometry, which builds a round soundhole.
    public class SoundholeComponent : GuitarFormComponent
    {
        public SoundholeComponent()
          : base("Soundhole", "Soundhole",
              "Round soundhole. A circle placed across from the body centreline and up from the tail end.",
              "Soundhole")
        {
        }

        // Input positions. These must match the registration order in RegisterInputParams.
        const int InDiameter = 0;
        const int InHorizontalOffset = 1;
        const int InTailOffset = 2;

        // Output positions. These must match the registration order in RegisterOutputParams.
        const int OutCircle = 0;
        const int OutCenter = 1;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Diameter", "D", "Diameter of the soundhole", GH_ParamAccess.item);
            pManager.AddNumberParameter("Horizontal Offset", "HO", "Distance of the soundhole centre from the body centreline along X (+X is right). 0 = centred", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tail Offset", "TO", "Distance of the soundhole centre up the Y axis from the tail end (the origin)", GH_ParamAccess.item);
            pManager[InHorizontalOffset].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCircleParameter("Circle", "C", "The soundhole outline", GH_ParamAccess.item);
            pManager.AddPointParameter("Center", "Pt", "Centre of the soundhole", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double diameter = 0, tailOffset = 0;
            if (!DA.GetData(InDiameter, ref diameter)) return;
            if (!DA.GetData(InTailOffset, ref tailOffset)) return;

            var dimensions = new SoundholeDimensions
            {
                Diameter = diameter,
                HorizontalOffset = GetOptionalNumber(DA, InHorizontalOffset),
                TailOffset = tailOffset,
            };

            var soundhole = SoundholeGeometry.Solve(dimensions);
            AddMessages(soundhole.Messages);
            if (soundhole.Failed) return;

            AddOutline(new[] { new ArcCurve(soundhole.Circle) });

            DA.SetData(OutCircle, soundhole.Circle);
            DA.SetData(OutCenter, soundhole.Center);
        }

        static readonly Bitmap SoundholeIcon = Icons.Load("Soundhole.png");

        protected override Bitmap Icon => SoundholeIcon;

        public override Guid ComponentGuid => new Guid("b1065328-8bfb-4ca9-b2ae-bf428bda37cb");
    }
}
