using System;
using System.Drawing;
using System.Linq;
using GuitarForm.Geometry;
using GuitarForm.Shapes;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

namespace GuitarForm
{
    // Draws the open design in the viewports without adding it to the document, each curve in its component's
    // display color (construction geometry light gray), so the preview matches what Build makes.
    sealed class DesignPreview : DisplayConduit
    {
        public static readonly Color ConstructionColor = ComponentStyles.ConstructionColor;

        sealed record Shown(Curve Curve, Color Color, int Thickness);

        Shown[] _curves = Array.Empty<Shown>();
        BoundingBox _box = BoundingBox.Empty;

        // Shows the drawing (in mm), scaled into the document's units. Strings are left out unless showStrings.
        public void Show(DesignDrawing drawing, RhinoDoc doc, bool showStrings = true)
        {
            var toDocument = DesignUnits.ToDocument(doc.ModelUnitSystem);
            _curves = drawing.Curves
                .Where(c => showStrings || c.Component != DrawingComponent.Strings)
                .Select(c =>
                {
                    var copy = c.Curve.DuplicateCurve();
                    copy.Transform(toDocument);
                    return new Shown(copy, ComponentStyles.DisplayColor(c.Component, c.Role), ComponentStyles.Thickness(c.Role));
                }).ToArray();
            _box = BoundingBox.Empty;
            foreach (var shown in _curves)
                _box.Union(shown.Curve.GetBoundingBox(false));
            Enabled = true;
            doc.Views.Redraw();
        }

        public void Hide(RhinoDoc doc)
        {
            Enabled = false;
            doc?.Views.Redraw();
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            if (_box.IsValid)
                e.IncludeBoundingBox(_box);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            foreach (var shown in _curves)
                e.Display.DrawCurve(shown.Curve, shown.Color, shown.Thickness);
        }
    }
}
