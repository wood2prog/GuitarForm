using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GuitarForm.Model.Parts
{
    // The dot markers as text: each marker's fret, with ":2" for two dots, e.g. "3 5 7 9 12:2". Markers are
    // separated by spaces or commas, and the numbers are plain whole numbers (no culture).
    public static class DotMarkerText
    {
        public static string Format(IEnumerable<DotMarker> markers) => string.Join(" ", markers.Select(m =>
            m.DotCount.Value == 2
                ? $"{m.Fret.Value?.ToString(CultureInfo.InvariantCulture)}:2"
                : $"{m.Fret.Value?.ToString(CultureInfo.InvariantCulture)}"));

        // The markers read from text, or null when it isn't understood. Blank text gives no markers.
        public static IReadOnlyList<DotMarker> Parse(string text)
        {
            var markers = new List<DotMarker>();
            foreach (string item in (text ?? "").Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = item.Split(':');
                if (parts.Length > 2 ||
                    !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fret))
                    return null;
                int dots = 1;
                if (parts.Length == 2 &&
                    !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out dots))
                    return null;
                markers.Add(new DotMarker
                {
                    Fret = new(DotMarker.Definitions.Fret, fret),
                    DotCount = new(DotMarker.Definitions.DotCount, dots),
                });
            }
            return markers;
        }
    }
}
