using System;
using System.Text.Json.Nodes;

namespace GuitarForm.Tests
{
    // Builds version 1 design documents as the retired DesignDocument wrote them (camelCase fields, values not set left
    // out, the instrument's calculated neckJoinDistance included), for the tests of reading old files.
    static class V1Documents
    {
        public static JsonObject Instrument(double scaleLength, int neckJoinFret) => new JsonObject
        {
            ["scaleLength"] = scaleLength,
            ["neckJoinFret"] = neckJoinFret,
            ["neckJoinDistance"] = scaleLength - scaleLength / Math.Pow(2, neckJoinFret / 12.0),
        };

        public static JsonObject Obj(params (string Key, double Value)[] fields)
        {
            var o = new JsonObject();
            foreach (var (key, value) in fields)
                o[key] = value;
            return o;
        }

        // A document; name and soundhole may be null (left out).
        public static JsonObject Document(string name, JsonObject instrument, JsonObject body, JsonObject soundhole = null)
        {
            var design = new JsonObject();
            if (name != null)
                design["name"] = name;
            design["instrument"] = instrument;
            design["body"] = body;
            if (soundhole != null)
                design["soundhole"] = soundhole;
            return new JsonObject { ["schemaVersion"] = 1, ["units"] = "mm", ["design"] = design };
        }

        public static string Json(string name, double scaleLength, int neckJoinFret, double bodyLength) =>
            Document(name, Instrument(scaleLength, neckJoinFret), Obj(("bodyLength", bodyLength))).ToJsonString();
    }
}
