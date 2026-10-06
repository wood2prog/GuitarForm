using System;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using GuitarForm.Geometry;
using Rhino.Geometry;

namespace GuitarForm.Components
{
    // Grasshopper wrapper for PlateGeometry, which builds the outline.
    public class PlateComponent : GuitarFormComponent
    {
        public PlateComponent()
          : base("Plate", "Plate",
              "Guitar body plate. Outputs construction lines driven by the body dimensions.",
              "Body")
        {
        }

        // Input positions. These must match the registration order in RegisterInputParams.
        const int InBodyLength = 0;
        const int InUbWidth = 1;
        const int InUbOffset = 2;
        const int InUbRadius = 3;
        const int InUbSecondaryOffset = 4;
        const int InWaistRadius = 5;
        const int InWaistWidth = 6;
        const int InWaistOffset = 7;
        const int InLbWidth = 8;
        const int InLbOffset = 9;
        const int InLbRadius = 10;
        const int InLbSecondaryOffset = 11;
        const int InHeelWidth = 12;

        // Output positions. These must match the registration order in RegisterOutputParams.
        const int OutConstructionLines = 0;
        const int OutOutlineRadii = 1;
        const int OutOutlineArcs = 2;
        const int OutOutlineLines = 3;
        const int OutOutline = 4;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Body Length", "L", "Overall length of the guitar body", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Width", "UbW", "Width of the upper bout", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Offset", "UbO", "Offset of the upper bout line down the Y axis from the upper bout primary radius centre", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Primary Radius", "UbR", "Primary radius of the upper bout. Its centre sits one radius below the top of the body length line", GH_ParamAccess.item);
            pManager.AddNumberParameter("Upper Bout Secondary Offset", "UbSO", "Moves the upper bout secondary radius centre toward the body centre and grows its radius by the same amount, keeping its outer edge on the primary radius. 0 = same as the primary radius", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Radius", "WR", "Radius of the waist curve. Its centre sits on the waist center line, one radius outside the waist width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Width", "WW", "Width of the body at the waist", GH_ParamAccess.item);
            pManager.AddNumberParameter("Waist Offset", "WO", "Distance of the waist center line up the Y axis from the tail end (the origin)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Width", "LbW", "Width of the lower bout", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Offset", "LbO", "Offset of the lower bout center line up the Y axis from the lower bout primary radius centre", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Primary Radius", "LbR", "Primary radius of the lower bout. Its centre sits one radius above the tail end (the origin)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lower Bout Secondary Offset", "LbSO", "Moves the lower bout secondary radius centre toward the body centre and grows its radius by the same amount, keeping its outer edge on the primary radius. 0 = same as the primary radius", GH_ParamAccess.item);
            pManager.AddNumberParameter("Heel Width", "HW", "Width of the flat at the top of the body where the heel of the neck attaches", GH_ParamAccess.item);
            for (int i = 1; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Construction Lines", "CL", "Construction lines for the plate", GH_ParamAccess.list);
            pManager.AddCircleParameter("Outline Radii", "OR", "Circles for the upper bout primary and secondary, waist, and lower bout primary and secondary radii, right side then left side for each", GH_ParamAccess.list);
            pManager.AddArcParameter("Outline Arcs", "OA", "Final outline arcs, right then left for each: the shoulder arcs, the upper bout primary and secondary, waist, and lower bout secondary and primary circle segments, then the tail arc", GH_ParamAccess.list);
            pManager.AddLineParameter("Outline Lines", "OL", "Straight parts of the final body outline: the heel flat, the waist tangent lines (upper bout right, left, lower bout right, left), then the straight tail", GH_ParamAccess.list);
            pManager.AddCurveParameter("Outline", "O", "The whole body outline as one closed curve: the outline arcs and lines joined. Empty until every input needed for a complete outline is connected", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double length = 0;
            if (!DA.GetData(InBodyLength, ref length)) return;

            var dimensions = new PlateDimensions
            {
                BodyLength = length,
                UbWidth = GetOptionalNumber(DA, InUbWidth),
                UbOffset = GetOptionalNumber(DA, InUbOffset),
                UbRadius = GetOptionalNumber(DA, InUbRadius),
                UbSecondaryOffset = GetOptionalNumber(DA, InUbSecondaryOffset),
                WaistRadius = GetOptionalNumber(DA, InWaistRadius),
                WaistWidth = GetOptionalNumber(DA, InWaistWidth),
                WaistOffset = GetOptionalNumber(DA, InWaistOffset),
                LbWidth = GetOptionalNumber(DA, InLbWidth),
                LbOffset = GetOptionalNumber(DA, InLbOffset),
                LbRadius = GetOptionalNumber(DA, InLbRadius),
                LbSecondaryOffset = GetOptionalNumber(DA, InLbSecondaryOffset),
                HeelWidth = GetOptionalNumber(DA, InHeelWidth),
            };

            var plate = PlateGeometry.Solve(dimensions, DocumentTolerance());
            AddMessages(plate.Messages);
            if (plate.Failed) return;

            // Construction lines and outline radii preview light grey; the outline arcs and lines are the final outline.
            AddConstruction(plate.ConstructionLines.Select(line => new LineCurve(line)));
            AddConstruction(plate.OutlineRadii.Select(circle => new ArcCurve(circle)));
            AddOutline(plate.OutlineArcs.Select(arc => new ArcCurve(arc)));
            AddOutline(plate.OutlineLines.Select(line => new LineCurve(line)));

            DA.SetDataList(OutConstructionLines, plate.ConstructionLines);
            DA.SetDataList(OutOutlineRadii, plate.OutlineRadii);
            DA.SetDataList(OutOutlineArcs, plate.OutlineArcs);
            DA.SetDataList(OutOutlineLines, plate.OutlineLines);
            // Not previewed or baked separately: it's the same curves as the outline arcs and lines.
            if (plate.Outline != null)
                DA.SetData(OutOutline, plate.Outline);
        }

        static readonly Bitmap PlateIcon = Icons.Load("Plate.png");

        protected override Bitmap Icon => PlateIcon;

        public override Guid ComponentGuid => new Guid("0b87450f-df46-4399-bedc-2321bb4d748c");
    }
}
