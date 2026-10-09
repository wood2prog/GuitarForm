using System.Globalization;
using GuitarForm.Geometry;
using Rhino;

namespace GuitarForm.Plugin
{
    // Lengths are stored in millimetres and shown, and typed, in the active document's units.
    sealed record DisplayUnits(UnitSystem System)
    {
        public static DisplayUnits Current => new DisplayUnits(RhinoDoc.ActiveDoc?.ModelUnitSystem ?? UnitSystem.Millimeters);

        // The units' short name, e.g. "mm" or "in".
        public string Abbreviation => System switch
        {
            UnitSystem.Microns => "µm",
            UnitSystem.Millimeters => "mm",
            UnitSystem.Centimeters => "cm",
            UnitSystem.Decimeters => "dm",
            UnitSystem.Meters => "m",
            UnitSystem.Inches => "in",
            UnitSystem.Feet => "ft",
            UnitSystem.Yards => "yd",
            _ => System.ToString().ToLowerInvariant(),
        };

        public double FromMillimetres(double millimetres) => DesignUnits.FromMillimetres(millimetres, System);

        public double ToMillimetres(double length) => DesignUnits.ToMillimetres(length, System);

        // A stored length as a number for a text box.
        public string FormatNumber(double millimetres) =>
            FromMillimetres(millimetres).ToString("0.#####", CultureInfo.CurrentCulture);

        // A stored length with its unit, for messages.
        public string FormatLength(double millimetres) =>
            FromMillimetres(millimetres).ToString("0.###", CultureInfo.CurrentCulture) + " " + Abbreviation;
    }
}
