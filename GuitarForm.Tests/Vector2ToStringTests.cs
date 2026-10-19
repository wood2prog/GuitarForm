using System.Globalization;
using GuitarForm.Shapes;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // Vector2.ToString used to recurse forever (the generated record printing included Unit, Left and Right).
    public class Vector2ToStringTests
    {
        CultureInfo _culture;

        [SetUp]
        public void SetUp() => _culture = CultureInfo.CurrentCulture;

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = _culture;

        [Test]
        public void Vector2PrintsOnlyXAndY()
        {
            Assert.That(new Vector2(1.5, 2).ToString(), Is.EqualTo("Vector2 { X = 1.5, Y = 2 }"));
        }

        [Test]
        public void Vector2PrintsInvariantUnderGerman()
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.That(new Vector2(1.5, 2).ToString(), Is.EqualTo("Vector2 { X = 1.5, Y = 2 }"));
        }

        [Test]
        public void ZeroVectorPrints()
        {
            Assert.That(new Vector2(0, 0).ToString(), Is.EqualTo("Vector2 { X = 0, Y = 0 }"));
        }

        [Test]
        public void Point2HasNoSelfReferenceAndPrints()
        {
            Assert.That(new Point2(1, 2).ToString(), Is.EqualTo("Point2 { X = 1, Y = 2 }"));
        }
    }
}
