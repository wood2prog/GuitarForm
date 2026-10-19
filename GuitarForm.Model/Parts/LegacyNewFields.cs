using System;
using System.Collections.Generic;
using System.Globalization;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    // Reads the values the plug-in keeps for a design beside the library (its "NewFields": glossary abbreviation to
    // text, lengths in mm in the invariant culture, plus "Markers", "StringSetId", "StringSetName", "FretwireId" and
    // "FretwireName"), onto a guitar. Nothing here throws for a bad text: it becomes a problem.
    public static class LegacyNewFields
    {
        public const string MarkersKey = "Markers";
        public const string StringSetIdKey = "StringSetId";
        public const string StringSetNameKey = "StringSetName";
        public const string FretwireIdKey = "FretwireId";
        public const string FretwireNameKey = "FretwireName";

        public sealed record Result(Guitar Guitar, long? StringSetId, long? FretwireId, string StringSetName,
            string FretwireName, IReadOnlyList<string> Problems);

        public static Result Apply(Guitar guitar, IReadOnlyDictionary<string, string> texts)
        {
            var problems = new List<string>();
            long? stringSetId = null, fretwireId = null;
            string stringSetName = null, fretwireName = null;

            foreach (var (key, text) in texts)
            {
                switch (key)
                {
                    case MarkersKey:
                        var markers = DotMarkerText.Parse(text);
                        if (markers == null)
                            problems.Add($"{key} was skipped: \"{text}\" isn't a list of frets such as \"3 5 12:2\".");
                        else
                            guitar = guitar with
                            {
                                Neck = guitar.Neck with { Fretboard = guitar.Neck.Fretboard with { Markers = markers } },
                            };
                        break;
                    case StringSetIdKey:
                        stringSetId = ReadId(key, text, problems);
                        break;
                    case FretwireIdKey:
                        fretwireId = ReadId(key, text, problems);
                        break;
                    case StringSetNameKey:
                        stringSetName = text;
                        break;
                    case FretwireNameKey:
                        fretwireName = text;
                        break;
                    default:
                        guitar = ApplyValue(guitar, key, text, problems);
                        break;
                }
            }
            return new Result(guitar, stringSetId, fretwireId, stringSetName, fretwireName, problems);
        }

        static long? ReadId(string key, string text, List<string> problems)
        {
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id))
                return id;
            problems.Add($"{key} was skipped: \"{text}\" isn't a whole number.");
            return null;
        }

        // Each part the abbreviation can be in: the guitar, and the neck, body and bracing, which are saved apart.
        static Guitar ApplyValue(Guitar guitar, string abbreviation, string text, List<string> problems)
        {
            Part[] holders = { guitar, guitar.Neck, guitar.Body, guitar.Body.TopPlate.Bracing };
            for (int level = 0; level < holders.Length; level++)
            {
                Parameter found = holders[level].FindParameter(abbreviation);
                if (found == null)
                    continue;
                if (!found.TryParse(text, LengthUnit.Millimeters, CultureInfo.InvariantCulture, out var parsed, out var error))
                {
                    problems.Add($"{abbreviation} was skipped: {error.Text}");
                    return guitar;
                }
                var value = new[] { new PartValue(abbreviation, null, parsed.StoredValue) };
                switch (level)
                {
                    case 0:
                        return (Guitar)guitar.WithStoredValues(value);
                    case 1:
                        return guitar with { Neck = (Neck)guitar.Neck.WithStoredValues(value) };
                    case 2:
                        return guitar with { Body = (Body)guitar.Body.WithStoredValues(value) };
                    default:
                        return guitar with
                        {
                            Body = guitar.Body with
                            {
                                TopPlate = guitar.Body.TopPlate with
                                {
                                    Bracing = (Bracing)guitar.Body.TopPlate.Bracing.WithStoredValues(value),
                                },
                            },
                        };
                }
            }
            problems.Add($"{abbreviation} was skipped: it isn't a known value.");
            return guitar;
        }
    }
}
