using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GuitarForm.Model.Parts
{
    // Reads and writes a guitar as a JSON document that other programs can read:
    //
    //   { "schemaVersion": 3, "units": "mm",
    //     "guitar": { "values": { "IN": "OM", "SL": 645, ... },
    //                 "neck": { "values": { ... }, "markers": [ { "MKF": 3, "MKDC": 1 }, ... ],
    //                           "fretwire": { "values": { ... } },
    //                           "stringSet": { "values": { ... }, "strings": [ { ... }, ... ] } },
    //                 "body": { "values": { ... }, "bracing": { "values": { ... } } } } }
    //
    // Each part saved on its own has "values", keyed by glossary abbreviation (docs/Glossary.md): the parts saved with
    // it (e.g. the neck's fretboard and nut) are in its values. Its lists of parts (e.g. the dot markers) are arrays
    // named after the list, and its separately saved parts are objects named after their kind. Values that aren't
    // set are left out. Lengths are in mm, angles in degrees and string unit weight in kg/m. Library ids aren't
    // written, so an imported fretwire or string set has no Id: match it to the library by its name, or save it.
    //
    // Increase SchemaVersion whenever the shape of the document changes, and teach FromJson to read the older
    // versions. Version 1 is the first design document (instrument, body and soundhole), which is converted.
    public static class GuitarDocument
    {
        public const int SchemaVersion = 3;
        public const string Units = "mm";

        static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        public static string ToJson(Guitar guitar) =>
            new JsonObject
            {
                ["schemaVersion"] = SchemaVersion,
                ["units"] = Units,
                ["guitar"] = PartObject(guitar),
            }.ToJsonString(Options);

        // Throws FormatException if the text isn't a guitar or design document this version can read, or a value
        // isn't one its setting can take.
        public static Guitar FromJson(string json) => Read(json).Guitar;

        // A library item (string set or fretwire) a document refers to: its library id and name, either may be missing.
        public sealed record ItemReference(long? Id, string Name);

        // A document read: the guitar, the string set and fretwire it refers to (null when none), and problems with
        // values that were skipped.
        public sealed record ReadResult(Guitar Guitar, ItemReference StringSet, ItemReference Fretwire,
            IReadOnlyList<string> Problems);

        // As FromJson, plus the references. A version 1 document's "newFields" object (see LegacyNewFields) is applied
        // to the converted guitar, and gives the references; a version 3 guitar's neck gives its own.
        public static ReadResult Read(string json)
        {
            JsonObject document;
            try
            {
                document = JsonNode.Parse(json) as JsonObject;
            }
            catch (JsonException e)
            {
                throw new FormatException($"Not a valid guitar document: {e.Message}", e);
            }
            if (document == null)
                throw new FormatException("Not a guitar document: it isn't a JSON object.");

            int version = document["schemaVersion"] is JsonValue v && v.TryGetValue(out int number) ? number : 0;
            if (version < 1)
                throw new FormatException("Not a guitar document: schemaVersion is missing.");
            if (version > SchemaVersion)
                throw new FormatException(
                    $"The guitar document is version {version}, made by a newer GuitarForm. This version reads up to {SchemaVersion}.");
            string units = document["units"] is JsonValue u && u.TryGetValue(out string text) ? text : null;
            if (units != Units)
                throw new FormatException($"Guitar document units must be \"{Units}\", not \"{units}\".");

            if (version == 1)
                return FromVersion1(document);
            var guitar = (Guitar)ReadPart(new Guitar(), document["guitar"] as JsonObject
                ?? throw new FormatException("The guitar document has no guitar."));
            return new ReadResult(guitar,
                guitar.Neck.StringSet is { } set ? new ItemReference(set.Id, set.DisplayName) : null,
                guitar.Neck.Fretwire is { } wire ? new ItemReference(wire.Id, wire.DisplayName) : null,
                Array.Empty<string>());
        }

        static ReadResult FromVersion1(JsonObject document)
        {
            var guitar = FromDesign(document["design"] as JsonObject
                ?? throw new FormatException("The design document has no design."));
            if (document["newFields"] is not JsonObject newFields)
                return new ReadResult(guitar, null, null, Array.Empty<string>());

            var texts = new Dictionary<string, string>();
            foreach (var (key, node) in newFields)
                if (node is JsonValue value)
                    texts[key] = value.TryGetValue(out string text) ? text : value.ToJsonString();
            var applied = LegacyNewFields.Apply(guitar, texts);
            return new ReadResult(applied.Guitar,
                Reference(applied.StringSetId, applied.StringSetName),
                Reference(applied.FretwireId, applied.FretwireName),
                applied.Problems);
        }

        static ItemReference Reference(long? id, string name) =>
            id == null && string.IsNullOrEmpty(name) ? null : new ItemReference(id, name);

        // --- Writing ---

        static JsonObject PartObject(Part part)
        {
            var values = new JsonObject();
            foreach (var value in part.StoredValues.Where(v => v.Item == null))
                values[value.Abbreviation] = ToNode(value.Value);
            var node = new JsonObject { ["values"] = values };

            foreach (var (name, items) in part.PartLists)
            {
                var array = new JsonArray();
                foreach (var item in items)
                {
                    var itemValues = new JsonObject();
                    foreach (var value in item.StoredValues)
                        itemValues[value.Abbreviation] = ToNode(value.Value);
                    array.Add(itemValues);
                }
                node[JsonNamingPolicy.CamelCase.ConvertName(name)] = array;
            }

            foreach (var child in part.SeparatelySavedParts)
                node[KindName(child)] = PartObject(child);
            return node;
        }

        static JsonNode ToNode(object value) => value switch
        {
            double d => JsonValue.Create(d),
            int i => JsonValue.Create(i),
            bool b => JsonValue.Create(b),
            string s => JsonValue.Create(s),
            _ => throw new ArgumentException($"Can't write a {value.GetType().Name} value."),
        };

        // --- Reading ---

        static Part ReadPart(Part empty, JsonObject node)
        {
            var values = new List<PartValue>();
            if (node["values"] is JsonObject own)
                values.AddRange(own.Select(pair => new PartValue(pair.Key, null, FromNode(pair.Key, pair.Value))));

            // A list's values are matched to the list by their abbreviations, so the array's name isn't needed here.
            foreach (var (name, _) in empty.PartLists)
            {
                if (node[JsonNamingPolicy.CamelCase.ConvertName(name)] is not JsonArray array)
                    continue;
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is not JsonObject item)
                        throw new FormatException($"Item {i + 1} of \"{name}\" isn't an object.");
                    // The marker keeps a blank item ({}) in the list.
                    values.Add(new PartValue(PartValue.ListItemKey(name), i, 1));
                    values.AddRange(item.Select(pair => new PartValue(pair.Key, i, FromNode(pair.Key, pair.Value))));
                }
            }

            Part part = empty.WithStoredValues(values);
            return part switch
            {
                Guitar guitar => guitar with
                {
                    Neck = Child(node, guitar.Neck),
                    Body = Child(node, guitar.Body),
                },
                Body body => body with
                {
                    TopPlate = body.TopPlate with { Bracing = Child(node, body.TopPlate.Bracing) },
                },
                Neck neck => neck with
                {
                    Fretwire = Child(node, new Fretwire(), optional: true),
                    StringSet = Child(node, new StringSet(), optional: true),
                },
                _ => part,
            };
        }

        // The child part named after the kind of empty, read from the node. When it isn't there: the empty part, or
        // null when it's optional.
        static T Child<T>(JsonObject node, T empty, bool optional = false) where T : Part
        {
            if (node[KindName(empty)] is JsonObject child)
                return (T)ReadPart(empty, child);
            return optional ? null : empty;
        }

        static object FromNode(string abbreviation, JsonNode node)
        {
            if (node is JsonValue value && value.TryGetValue(out JsonElement element))
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Number:
                        return element.TryGetInt64(out long whole) ? whole : element.GetDouble();
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                }
            }
            throw new FormatException($"{abbreviation}'s value must be a number, text, true or false.");
        }

        static string KindName(Part part) => part switch
        {
            Neck => "neck",
            Body => "body",
            Bracing => "bracing",
            Fretwire => "fretwire",
            StringSet => "stringSet",
            _ => throw new ArgumentException($"{part.GetType().Name} isn't saved on its own."),
        };

        // --- Version 1 ---

        // The version 1 document's fields, with the abbreviations they became. This mapping is frozen: version 1 documents
        // only ever had these fields, so don't extend it for new sections or fields.
        static readonly (string Section, string Field, string Abbreviation)[] DesignFields =
        {
            ("instrument", "scaleLength", "SL"), ("instrument", "neckJoinFret", "NJF"),
            ("body", "bodyLength", "BDL"), ("body", "ubWidth", "UBW"), ("body", "ubOffset", "UBO"),
            ("body", "ubRadius", "UBPR"), ("body", "ubSecondaryOffset", "UBSO"), ("body", "waistRadius", "WSR"),
            ("body", "waistWidth", "WSW"), ("body", "waistOffset", "WSO"), ("body", "lbWidth", "LBW"),
            ("body", "lbOffset", "LBO"), ("body", "lbRadius", "LBPR"), ("body", "lbSecondaryOffset", "LBSO"),
            ("body", "heelWidth", "HFW"), ("body", "tailDepth", "BDDT"), ("body", "neckDepth", "BDDN"),
            ("soundhole", "diameter", "SHD"), ("soundhole", "horizontalOffset", "SHOC"),
            ("soundhole", "tailOffset", "SHOT"),
        };

        // Converts a version 1 design: its name, instrument, body and soundhole (the document's "newFields" is applied
        // by FromVersion1).
        static Guitar FromDesign(JsonObject design)
        {
            if (design["instrument"] is not JsonObject)
                throw new FormatException("The design has no instrument section.");
            if (design["body"] is not JsonObject)
                throw new FormatException("The design has no body section.");

            var guitarValues = new List<PartValue>();
            var bodyValues = new List<PartValue>();
            if (design["name"] is JsonNode name)
                guitarValues.Add(new PartValue("IN", null, FromNode("IN", name)));
            if (design["soundhole"] is JsonObject)
                bodyValues.Add(new PartValue("SH", null, true));
            foreach (var (section, field, abbreviation) in DesignFields)
            {
                if (design[section]?[field] is not JsonNode node)
                    continue;
                var value = new PartValue(abbreviation, null, FromNode(abbreviation, node));
                (section == "instrument" ? guitarValues : bodyValues).Add(value);
            }

            var guitar = (Guitar)new Guitar().WithStoredValues(guitarValues);
            return guitar with { Body = (Body)guitar.Body.WithStoredValues(bodyValues) };
        }
    }
}
