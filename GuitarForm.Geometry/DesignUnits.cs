using Rhino;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Designs are in millimetres; the Rhino document can be in any units. Geometry is built in millimetres, then
    // scaled about the origin into the document's units.
    public static class DesignUnits
    {
        // Scales geometry built in millimetres into the document's units.
        public static Transform ToDocument(UnitSystem documentUnits) =>
            Transform.Scale(Point3d.Origin, RhinoMath.UnitScale(UnitSystem.Millimeters, documentUnits));

        // The document's tolerance in millimetres, for building geometry.
        public static double ToleranceInMillimetres(double documentTolerance, UnitSystem documentUnits) =>
            documentTolerance * RhinoMath.UnitScale(documentUnits, UnitSystem.Millimeters);
    }
}
