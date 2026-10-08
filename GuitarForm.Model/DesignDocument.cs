using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuitarForm.Model
{
    // Reads and writes a design as a JSON document that other programs can read:
    //
    //   { "schemaVersion": 1, "units": "mm", "design": { "name": ..., "instrument": { ... }, "body": { ... } } }
    //
    // Values that aren't set are left out. Increase SchemaVersion whenever the shape of the document changes, and
    // teach FromJson to read the older versions.
    public static class DesignDocument
    {
        public const int SchemaVersion = 1;
        public const string Units = "mm";

        static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };

        sealed record Document
        {
            public int SchemaVersion { get; init; }
            public string Units { get; init; }
            public GuitarDesign Design { get; init; }
        }

        public static string ToJson(GuitarDesign design) =>
            JsonSerializer.Serialize(new Document { SchemaVersion = SchemaVersion, Units = Units, Design = design }, Options);

        // Throws FormatException if the text isn't a design document this version can read.
        public static GuitarDesign FromJson(string json)
        {
            Document document;
            try
            {
                document = JsonSerializer.Deserialize<Document>(json, Options);
            }
            catch (JsonException e)
            {
                throw new FormatException($"Not a valid design document: {e.Message}", e);
            }

            if (document == null || document.SchemaVersion < 1)
                throw new FormatException("Not a design document: schemaVersion is missing.");
            if (document.SchemaVersion > SchemaVersion)
                throw new FormatException(
                    $"The design document is version {document.SchemaVersion}, made by a newer GuitarForm. This version reads up to {SchemaVersion}.");
            if (document.Units != Units)
                throw new FormatException($"Design document units must be \"{Units}\", not \"{document.Units}\".");
            if (document.Design == null)
                throw new FormatException("The design document has no design.");
            if (document.Design.Instrument == null)
                throw new FormatException("The design has no instrument section.");
            if (document.Design.Body == null)
                throw new FormatException("The design has no body section.");
            return document.Design;
        }
    }
}
