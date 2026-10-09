using System;
using System.Drawing;
using System.Linq;
using GuitarForm.Geometry;
using Rhino;
using Rhino.ApplicationSettings;
using Rhino.Display;
using Rhino.Geometry;

namespace GuitarForm
{
    // Draws the open design in the viewports without adding it to the document: construction geometry light grey,
    // the outline in Rhino's feedback colour so it doesn't look like a built object.
    sealed class DesignPreview : DisplayConduit
    {
        public static readonly Color ConstructionColor = Color.FromArgb(200, 200, 200);

        Curve[] _construction = Array.Empty<Curve>();
        Curve[] _outline = Array.Empty<Curve>();
        BoundingBox _box = BoundingBox.Empty;

        // Shows the drawing (in mm), scaled into the document's units.
        public void Show(DesignDrawing drawing, RhinoDoc doc)
        {
            var toDocument = DesignUnits.ToDocument(doc.ModelUnitSystem);
            _construction = Transformed(drawing.Construction, toDocument);
            _outline = Transformed(drawing.Outline, toDocument);
            _box = BoundingBox.Empty;
            foreach (var curve in _construction.Concat(_outline))
                _box.Union(curve.GetBoundingBox(false));
            Enabled = true;
            doc.Views.Redraw();
        }

        public void Hide(RhinoDoc doc)
        {
            Enabled = false;
            doc?.Views.Redraw();
        }

        static Curve[] Transformed(System.Collections.Generic.IEnumerable<Curve> curves, Transform transform) =>
            curves.Select(curve =>
            {
                var copy = curve.DuplicateCurve();
                copy.Transform(transform);
                return copy;
            }).ToArray();

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            if (_box.IsValid)
                e.IncludeBoundingBox(_box);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            foreach (var curve in _construction)
                e.Display.DrawCurve(curve, ConstructionColor, 1);
            foreach (var curve in _outline)
                e.Display.DrawCurve(curve, AppearanceSettings.FeedbackColor, 2);
        }
    }
}
