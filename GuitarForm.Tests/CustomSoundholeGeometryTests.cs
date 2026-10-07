using GuitarForm.Geometry;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace GuitarForm.Tests
{
    // Checks CustomSoundholeGeometry against the README Quick Test values.
    [RhinoTestFixture]
    public class CustomSoundholeGeometryTests
    {
        const double Tolerance = 0.001;

        // Right triangle drawn away from the origin: corners (100, 50), (160, 50), (100, 110). Its area centroid,
        // (120, 70), isn't the centre of its bounding box, (130, 80).
        static Curve Triangle(double z = 0) => new PolylineCurve(new[]
        {
            new Point3d(100, 50, z), new Point3d(160, 50, z), new Point3d(100, 110, z), new Point3d(100, 50, z),
        });

        static CustomSoundholeGeometry Solve(Curve shape, double? horizontalOffset = null) =>
            CustomSoundholeGeometry.Solve(new CustomSoundholeDimensions
            {
                Shape = shape,
                HorizontalOffset = horizontalOffset,
                TailOffset = 420,
            }, Tolerance);

        static void AssertPoint(Point3d actual, double x, double y)
        {
            Assert.That(actual.X, Is.EqualTo(x).Within(Tolerance), $"X of {actual}");
            Assert.That(actual.Y, Is.EqualTo(y).Within(Tolerance), $"Y of {actual}");
            Assert.That(actual.Z, Is.EqualTo(0).Within(Tolerance), $"Z of {actual}");
        }

        static void AssertError(CustomSoundholeGeometry soundhole)
        {
            Assert.That(soundhole.Failed, Is.True);
            Assert.That(soundhole.Messages, Has.Count.EqualTo(1));
            Assert.That(soundhole.Messages[0].Level, Is.EqualTo(MessageLevel.Error));
        }

        [Test]
        public void QuickTest_AreaCentroidMovesToOffsets()
        {
            var soundhole = Solve(Triangle());

            Assert.That(soundhole.Failed, Is.False);
            Assert.That(soundhole.Messages, Is.Empty);
            AssertPoint(soundhole.Center, 0, 420);
            AssertPoint(AreaMassProperties.Compute(soundhole.Curve).Centroid, 0, 420);
            // The triangle's corners keep their places relative to the centroid.
            AssertPoint(soundhole.Curve.PointAtStart, -20, 400);
        }

        [Test]
        public void HorizontalOffset_MovesCentroidAlongX()
        {
            var soundhole = Solve(Triangle(), -20);

            Assert.That(soundhole.Failed, Is.False);
            AssertPoint(soundhole.Center, -20, 420);
            AssertPoint(soundhole.Curve.PointAtStart, -40, 400);
        }

        [Test]
        public void ShapeAboveXYPlane_MovesOntoIt()
        {
            var soundhole = Solve(Triangle(z: 15));

            Assert.That(soundhole.Failed, Is.False);
            AssertPoint(soundhole.Curve.PointAtStart, -20, 400);
        }

        [Test]
        public void InputCurve_IsNotModified()
        {
            var shape = Triangle();
            Solve(shape);

            AssertPoint(shape.PointAtStart, 100, 50);
        }

        [Test]
        public void OpenCurve_Fails()
        {
            AssertError(Solve(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0))));
        }

        [Test]
        public void ShapeNotParallelToXY_Fails()
        {
            AssertError(Solve(new ArcCurve(new Circle(Plane.WorldYZ, 20))));
        }

        [Test]
        public void NonPlanarShape_Fails()
        {
            AssertError(Solve(new PolylineCurve(new[]
            {
                new Point3d(0, 0, 0), new Point3d(10, 0, 0), new Point3d(10, 10, 5), new Point3d(0, 10, 0),
                new Point3d(0, 0, 0),
            })));
        }
    }
}
