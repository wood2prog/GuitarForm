using System;
using System.Drawing;
using Grasshopper.Kernel;
using GuitarForm.Geometry;
using Rhino.Geometry;

namespace GuitarForm.Components
{
    // Grasshopper wrapper for CustomSoundholeGeometry, which places a custom-shaped soundhole.
    public class CustomSoundholeComponent : GuitarFormComponent
    {
        public CustomSoundholeComponent()
          : base("Custom Soundhole", "CSoundhole",
              "Custom-shaped soundhole. Moves a closed curve so its area centroid sits across from the body centreline and up from the tail end.",
              "Soundhole")
        {
        }

        // Input positions. These must match the registration order in RegisterInputParams.
        const int InShape = 0;
        const int InHorizontalOffset = 1;
        const int InTailOffset = 2;

        // Output positions. These must match the registration order in RegisterOutputParams.
        const int OutCurve = 0;
        const int OutCenter = 1;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Shape", "S", "Closed, planar curve parallel to the XY plane for the soundhole shape. Where it's drawn doesn't matter: it's moved so its area centroid sits at the offsets", GH_ParamAccess.item);
            pManager.AddNumberParameter("Horizontal Offset", "HO", "Distance of the soundhole's area centroid from the body centreline along X (+X is right). 0 = centred", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tail Offset", "TO", "Distance of the soundhole's area centroid up the Y axis from the tail end (the origin)", GH_ParamAccess.item);
            pManager[InHorizontalOffset].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "The soundhole outline, moved into place", GH_ParamAccess.item);
            pManager.AddPointParameter("Center", "Pt", "Area centroid of the soundhole", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve shape = null;
            double tailOffset = 0;
            if (!DA.GetData(InShape, ref shape)) return;
            if (!DA.GetData(InTailOffset, ref tailOffset)) return;

            var dimensions = new CustomSoundholeDimensions
            {
                Shape = shape,
                HorizontalOffset = GetOptionalNumber(DA, InHorizontalOffset),
                TailOffset = tailOffset,
            };

            var soundhole = CustomSoundholeGeometry.Solve(dimensions, DocumentTolerance());
            AddMessages(soundhole.Messages);
            if (soundhole.Failed) return;

            AddOutline(new[] { soundhole.Curve });

            DA.SetData(OutCurve, soundhole.Curve);
            DA.SetData(OutCenter, soundhole.Center);
        }

        static readonly Bitmap CustomSoundholeIcon = Icons.Load("CustomSoundhole.png");

        protected override Bitmap Icon => CustomSoundholeIcon;

        public override Guid ComponentGuid => new Guid("69b3b259-de60-4ed2-97ea-7eb3711cffb2");
    }
}
