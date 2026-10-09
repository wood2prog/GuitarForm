using GuitarForm.Geometry;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace GuitarForm.Tests
{
    // Checks the conversion between a design's millimetres and the document's units.
    [RhinoTestFixture]
    public class DesignUnitsTests
    {
        const double Tolerance = 1e-9;

        [TestCase(UnitSystem.Millimeters, 254.0)]
        [TestCase(UnitSystem.Centimeters, 25.4)]
        [TestCase(UnitSystem.Inches, 10.0)]
        public void ToDocument_ScalesMillimetres(UnitSystem units, double expectedX)
        {
            var point = new Point3d(254, 0, 0);
            point.Transform(DesignUnits.ToDocument(units));
            Assert.That(point.X, Is.EqualTo(expectedX).Within(Tolerance));
        }

        [TestCase(UnitSystem.Millimeters, 254.0, 254.0)]
        [TestCase(UnitSystem.Inches, 254.0, 10.0)]
        [TestCase(UnitSystem.Feet, 304.8, 1.0)]
        public void FromAndToMillimetres_AreInverses(UnitSystem units, double millimetres, double expected)
        {
            double shown = DesignUnits.FromMillimetres(millimetres, units);
            Assert.That(shown, Is.EqualTo(expected).Within(Tolerance));
            Assert.That(DesignUnits.ToMillimetres(shown, units), Is.EqualTo(millimetres).Within(Tolerance));
        }

        [TestCase(UnitSystem.Millimeters, 0.01, 0.01)]
        [TestCase(UnitSystem.Inches, 0.001, 0.0254)]
        public void ToleranceInMillimetres_ConvertsTheDocumentTolerance(UnitSystem units, double tolerance, double expected)
        {
            Assert.That(DesignUnits.ToleranceInMillimetres(tolerance, units), Is.EqualTo(expected).Within(Tolerance));
        }
    }
}
