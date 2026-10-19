using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;

namespace GuitarForm
{
    // The "Starting shape" list on the neck profile groups: a named shape written to one profile station's squareness,
    // asymmetry and blend.
    static class StartingShape
    {
        // The station a neck profile group is for ("F", "S", "H" or "E"), or null for any other group.
        static string StationOf(string title) => title switch
        {
            "Profile at first fret" => "F",
            "Profile at selected fret" => "S",
            "Heel section" => "H",
            "Heel end" => "E",
            _ => null,
        };

        // The guitar with the preset's squareness, asymmetry and blend written to the station the group is for.
        public static Guitar With(Guitar guitar, string title, NeckProfilePresets.Preset preset)
        {
            Neck n = guitar.Neck;
            NumberParameter Number(NumberParameter p, double v) => p with { Value = v };
            n = StationOf(title) switch
            {
                "F" => n with
                {
                    SquarenessAtFirstFret = Number(n.SquarenessAtFirstFret, preset.Squareness),
                    AsymmetryAtFirstFret = Number(n.AsymmetryAtFirstFret, preset.Asymmetry),
                    BlendAtFirstFret = Number(n.BlendAtFirstFret, preset.Blend),
                },
                "S" => n with
                {
                    SquarenessAtSelectedFret = Number(n.SquarenessAtSelectedFret, preset.Squareness),
                    AsymmetryAtSelectedFret = Number(n.AsymmetryAtSelectedFret, preset.Asymmetry),
                    BlendAtSelectedFret = Number(n.BlendAtSelectedFret, preset.Blend),
                },
                "H" => n with
                {
                    SquarenessAtHeelSection = Number(n.SquarenessAtHeelSection, preset.Squareness),
                    AsymmetryAtHeelSection = Number(n.AsymmetryAtHeelSection, preset.Asymmetry),
                    BlendAtHeelSection = Number(n.BlendAtHeelSection, preset.Blend),
                },
                "E" => n with
                {
                    SquarenessAtHeelEnd = Number(n.SquarenessAtHeelEnd, preset.Squareness),
                    AsymmetryAtHeelEnd = Number(n.AsymmetryAtHeelEnd, preset.Asymmetry),
                    BlendAtHeelEnd = Number(n.BlendAtHeelEnd, preset.Blend),
                },
                _ => n,
            };
            return guitar with { Neck = n };
        }
    }
}
