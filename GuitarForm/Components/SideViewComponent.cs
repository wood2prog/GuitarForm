using System;
using System.Linq;
using Grasshopper.Kernel;
using GuitarForm.Geometry;
using Rhino.Geometry;

namespace GuitarForm.Components
{
    // Grasshopper wrapper for SideViewGeometry, which builds the side view of the body.
    public class SideViewComponent : GuitarFormComponent
    {
        public SideViewComponent()
          : base("Side View", "Side",
              "Side view of the guitar body. Drawn from the tail end at the origin up the Y axis, then moved right by the drawing offset.",
              "Body")
        {
        }

        // Input positions. These must match the registration order in RegisterInputParams.
        const int InBodyLength = 0;
        const int InDrawingOffset = 1;

        // Output positions. These must match the registration order in RegisterOutputParams.
        const int OutConstructionLines = 0;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Body Length", "L", "Overall length of the guitar body", GH_ParamAccess.item);
            pManager.AddNumberParameter("Drawing Offset", "DO", "Distance the side view is moved to the right (+X) from the origin, so it can sit beside the plate. 0 = drawn at the origin", GH_ParamAccess.item);
            pManager[InDrawingOffset].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Construction Lines", "CL", "Construction lines for the side view", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double length = 0;
            if (!DA.GetData(InBodyLength, ref length)) return;

            var dimensions = new SideViewDimensions
            {
                BodyLength = length,
                DrawingOffset = GetOptionalNumber(DA, InDrawingOffset),
            };

            var side = SideViewGeometry.Solve(dimensions);
            AddMessages(side.Messages);
            if (side.Failed) return;

            AddConstruction(side.ConstructionLines.Select(line => new LineCurve(line)));

            DA.SetDataList(OutConstructionLines, side.ConstructionLines);
        }

        public override Guid ComponentGuid => new Guid("60d85f4b-5914-404c-8352-4876eb9c0728");
    }
}
