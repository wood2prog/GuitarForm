using System.Globalization;
using System.Linq;
using GuitarForm.Model.Parts;
using NUnit.Framework;

namespace GuitarForm.Tests
{
    public class DotMarkerTextTests
    {
        [Test]
        public void ParsesFretsAndDoubleDots()
        {
            var markers = DotMarkerText.Parse("3 5 7 9 12:2");
            Assert.That(markers.Select(m => m.Fret.Value), Is.EqualTo(new int?[] { 3, 5, 7, 9, 12 }));
            Assert.That(markers.Select(m => m.DotCount.Value), Is.EqualTo(new int?[] { 1, 1, 1, 1, 2 }));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void BlankTextGivesNoMarkers(string text) => Assert.That(DotMarkerText.Parse(text), Is.Empty);

        [TestCase("3 x")]
        [TestCase("3:2:1")]
        [TestCase("3:y")]
        [TestCase("1.5")]
        public void BadTextGivesNull(string text) => Assert.That(DotMarkerText.Parse(text), Is.Null);

        [Test]
        public void FormatRoundTrips()
        {
            const string text = "3 5 7 9 12:2";
            Assert.That(DotMarkerText.Format(DotMarkerText.Parse(text)), Is.EqualTo(text));
            Assert.That(DotMarkerText.Format(DotMarkerText.Parse("3, 5,12:2")), Is.EqualTo("3 5 12:2"));
        }

        [Test]
        public void IgnoresTheCurrentCulture()
        {
            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                var markers = DotMarkerText.Parse("1234 12:2");
                Assert.That(markers, Is.Not.Null);
                Assert.That(markers.Select(m => m.Fret.Value), Is.EqualTo(new int?[] { 1234, 12 }));
                Assert.That(DotMarkerText.Format(markers), Is.EqualTo("1234 12:2"));
            }
            finally { CultureInfo.CurrentCulture = saved; }
        }
    }
}
