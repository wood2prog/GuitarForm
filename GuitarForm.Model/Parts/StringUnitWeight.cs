namespace GuitarForm.Model.Parts
{
    // Converts a string's unit weight between kg/m (stored) and lb/in (the usual way string sets are quoted).
    public static class StringUnitWeight
    {
        // One lb/in in kg/m: 0.45359237 kg per lb over 0.0254 m per inch.
        public const double KgPerMPerLbPerIn = 0.45359237 / 0.0254;

        public static double ToLbPerIn(double kgPerM) => kgPerM / KgPerMPerLbPerIn;

        public static double ToKgPerM(double lbPerIn) => lbPerIn * KgPerMPerLbPerIn;
    }
}
