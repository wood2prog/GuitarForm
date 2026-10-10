using System;

namespace GuitarForm.Model.Parameters
{
    // A unit lengths can be shown and typed in. Lengths are always stored in millimeters.
    public enum LengthUnit
    {
        Microns,
        Millimeters,
        Centimeters,
        Decimeters,
        Meters,
        Inches,
        Feet,
        Yards,
    }

    public static class LengthUnits
    {
        public static double MillimetersPer(this LengthUnit unit) => unit switch
        {
            LengthUnit.Microns => 0.001,
            LengthUnit.Millimeters => 1,
            LengthUnit.Centimeters => 10,
            LengthUnit.Decimeters => 100,
            LengthUnit.Meters => 1000,
            LengthUnit.Inches => 25.4,
            LengthUnit.Feet => 304.8,
            LengthUnit.Yards => 914.4,
            _ => throw new ArgumentOutOfRangeException(nameof(unit)),
        };

        // The unit's short name, e.g. "mm" or "in".
        public static string Symbol(this LengthUnit unit) => unit switch
        {
            LengthUnit.Microns => "µm",
            LengthUnit.Millimeters => "mm",
            LengthUnit.Centimeters => "cm",
            LengthUnit.Decimeters => "dm",
            LengthUnit.Meters => "m",
            LengthUnit.Inches => "in",
            LengthUnit.Feet => "ft",
            LengthUnit.Yards => "yd",
            _ => throw new ArgumentOutOfRangeException(nameof(unit)),
        };

        public static double FromMillimeters(this LengthUnit unit, double millimeters) => millimeters / unit.MillimetersPer();

        public static double ToMillimeters(this LengthUnit unit, double length) => length * unit.MillimetersPer();
    }
}
