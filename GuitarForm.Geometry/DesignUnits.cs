using Rhino;
using Rhino.Geometry;

namespace GuitarForm.Geometry
{
    // Designs are stored in millimeters; the Rhino document can be in any units. Geometry is built in millimeters, then
    // scaled about the origin into the document's units, and lengths are shown and typed in the document's units.
    public static class DesignUnits
    {
        // Scales geometry built in millimeters into the document's units.
        public static Transform ToDocument(UnitSystem documentUnits) =>
            Transform.Scale(Point3d.Origin, RhinoMath.UnitScale(UnitSystem.Millimeters, documentUnits));

        // A length in millimeters, in the document's units: for showing a stored length.
        public static double FromMillimetres(double millimetres, UnitSystem documentUnits) =>
            millimetres * RhinoMath.UnitScale(UnitSystem.Millimeters, documentUnits);

        // A length in the document's units, in millimeters: for storing a length typed in.
        public static double ToMillimetres(double length, UnitSystem documentUnits) =>
            length * RhinoMath.UnitScale(documentUnits, UnitSystem.Millimeters);

        // The document's tolerance in millimeters, for building geometry.
        public static double ToleranceInMillimetres(double documentTolerance, UnitSystem documentUnits) =>
            documentTolerance * RhinoMath.UnitScale(documentUnits, UnitSystem.Millimeters);
    }
}
