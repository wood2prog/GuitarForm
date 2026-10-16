using System.Collections.Generic;

namespace GuitarForm.Model.Parts
{
    // Named starting points for the advanced neck profile: squareness, asymmetry (%) and blend (%).
    public static class NeckProfilePresets
    {
        public sealed record Preset(string Name, double Squareness, double Asymmetry, double Blend);

        public static readonly IReadOnlyList<Preset> All = new[]
        {
            new Preset("Modern C", 2.0, 0, 50),
            new Preset("Soft V", 1.5, 0, 25),
            new Preset("Hard V", 1.1, 0, 5),
            new Preset("D", 3.2, 0, 100),
            new Preset("Asymmetric", 2.0, 15, 50),
        };
    }
}
