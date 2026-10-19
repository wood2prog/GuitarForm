using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuitarForm.Model;
using GuitarForm.Model.Parameters;
using GuitarForm.Shapes;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    // The helpers the shapes share: Join, Rectangle and the message helper.
    public class ShapeSupportTests
    {
        [TestCase(new string[0], "")]
        [TestCase(new[] { "a" }, "a")]
        [TestCase(new[] { "a", "b" }, "a and b")]
        [TestCase(new[] { "a", "b", "c" }, "a, b and c")]
        public void Join_NamesTheItems(string[] items, string expected) =>
            Assert.That(ShapeSupport.Join(items), Is.EqualTo(expected));

        [Test]
        public void Rectangle_IsAClosedPathThroughTheCornersInOrder()
        {
            var corners = new[] { new Point2(0, 0), new Point2(0, 10), new Point2(5, 10), new Point2(5, 0) };
            var path = ShapeSupport.Rectangle(corners);

            Assert.That(path.IsClosed, Is.True);
            Assert.That(path.Segments.Count, Is.EqualTo(4));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(path.Segments[i].Start, Is.EqualTo(corners[i]));
                Assert.That(path.Segments[i].End, Is.EqualTo(corners[(i + 1) % 4]));
            }
        }

        [Test]
        public void Rectangle_PiecesThatMeetEndToEndFormOnePath()
        {
            var path = ShapeSupport.Rectangle(new Point2(0, 0), new Point2(1, 0), new Point2(1, 1));
            Assert.That(path.Segments.Count, Is.EqualTo(3));
            Assert.That(path.Start, Is.EqualTo(path.End));
        }

        [Test]
        public void Messages_AreAddedToTheListInOrderWithTheirLevels()
        {
            var list = new List<GeometryMessage>();
            var messages = new ShapeMessages(list, LengthUnit.Millimeters, CultureInfo.InvariantCulture);
            messages.Remark("r");
            messages.Warning("w");
            messages.Error("e");

            Assert.That(list.Select(m => (m.Level, m.Text)),
                Is.EqualTo(new[] { (MessageLevel.Remark, "r"), (MessageLevel.Warning, "w"), (MessageLevel.Error, "e") }));
        }

        [Test]
        public void Format_UsesTheCultureAndUnit()
        {
            var list = new List<GeometryMessage>();
            var us = new ShapeMessages(list, LengthUnit.Millimeters, new CultureInfo("en-US"));
            var de = new ShapeMessages(list, LengthUnit.Millimeters, new CultureInfo("de-DE"));
            var cm = new ShapeMessages(list, LengthUnit.Centimeters, new CultureInfo("en-US"));

            Assert.That(us.Format(12.5), Is.EqualTo("12.5 mm"));
            Assert.That(de.Format(12.5), Is.EqualTo("12,5 mm"));
            Assert.That(cm.Format(12.5), Is.EqualTo("1.25 cm"));
        }
    }
}
