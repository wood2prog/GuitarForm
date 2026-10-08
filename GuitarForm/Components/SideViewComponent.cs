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
        const int InTailDepth = 1;
        const int InNeckDepth = 2;
        const int InDrawingOffset = 3;

        // Output positions. These must match the registration order in RegisterOutputParams.
        const int OutOutlineLines = 0;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Body Length", "L", "Overall length of the guitar body", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tail Depth", "TD", "Depth of the body at the tail end, from the bottom line to the top line", GH_ParamAccess.item);
            pManager.AddNumberParameter("Neck Depth", "ND", "Depth of the body at the neck end, from the bottom line to the top line", GH_ParamAccess.item);
            pManager.AddNumberParameter("Drawing Offset", "DO", "Distance the side view is moved to the right (+X) from the origin, so it can sit beside the plate. 0 = drawn at the origin", GH_ParamAccess.item);
            pManager[InDrawingOffset].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Outline Lines", "OL", "The side view outline: the tail depth line, the neck depth line, the bottom line, then the top line", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double length = 0, tailDepth = 0, neckDepth = 0;
            if (!DA.GetData(InBodyLength, ref length)) return;
            if (!DA.GetData(InTailDepth, ref tailDepth)) return;
            if (!DA.GetData(InNeckDepth, ref neckDepth)) return;

            var dimensions = new SideViewDimensions
            {
                BodyLength = length,
                TailDepth = tailDepth,
                NeckDepth = neckDepth,
                DrawingOffset = GetOptionalNumber(DA, InDrawingOffset),
            };

            var side = SideViewGeometry.Solve(dimensions);
            AddMessages(side.Messages);
            if (side.Failed) return;

            AddOutline(side.OutlineLines.Select(line => new LineCurve(line)));

            DA.SetDataList(OutOutlineLines, side.OutlineLines);
        }

        public override Guid ComponentGuid => new Guid("60d85f4b-5914-404c-8352-4876eb9c0728");
    }
}
