using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuitarForm.Model.Parts;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Data
{
    // A saved guitar in the list of guitars.
    // Uid is the guitar's permanent identity (kept when it's saved or renamed); LegacyDesignId is the id of the design it
    // was converted from, if any.
    public sealed record GuitarInfo(long Id, string Name, DateTime Modified, string Uid, long? LegacyDesignId);

    // A neck, body or bracing that can be loaded into a guitar: a copy saved under a name, or the one belonging to a
    // saved guitar (GuitarName), or the bracing of a named body (BodyName).
    public sealed record AssemblyInfo(long Id, string Name, string GuitarName, string BodyName, DateTime Modified);

    // A fretwire or string set in the library, named by its manufacturer and product code or set name.
    public sealed record LibraryItemInfo(long Id, string Name);

    // Guitars and their parts, for the model in GuitarForm.Model.Parts (see Schema migration 1 -> 2).
    //
    // A guitar owns its neck, body and bracing. Loading another guitar's (or a named copy's) neck, body or bracing
    // copies its values: the guitar gets its own copy when it's saved, and the source never changes. Loading a body
    // brings its bracing. Fretwire and string sets are shared library items that necks refer to.
    public sealed partial class Library
    {
        // --- Guitars ---

        public IReadOnlyList<GuitarInfo> ListGuitars()
        {
            using var query = Command(
                "SELECT id, name, modified, uid, legacy_design_id FROM parts WHERE kind = 'guitar' ORDER BY name COLLATE NOCASE");
            using var reader = query.ExecuteReader();
            var guitars = new List<GuitarInfo>();
            while (reader.Read())
                guitars.Add(new GuitarInfo(reader.GetInt64(0), reader.GetString(1), ParseTime(reader.GetString(2)),
                    NullableString(reader, 3), reader.IsDBNull(4) ? null : reader.GetInt64(4)));
            return guitars;
        }

        public bool GuitarExists(string name) => TopLevelId("guitar", name) != null;

        // True if a guitar other than exceptId has the name (trimmed, ignoring case): what a rename or Save as must check.
        public bool GuitarExists(string name, long? exceptId)
        {
            long? existing = TopLevelId("guitar", name?.Trim() ?? "");
            return existing != null && existing != exceptId;
        }

        // Saves a new guitar, with its neck, body and bracing, and returns its id. Its name is its Instrument Name.
        // Throws InvalidOperationException if the name is empty or taken (ignoring case), or the neck refers to a
        // fretwire or string set that isn't in the library.
        public long CreateGuitar(Guitar guitar)
        {
            string name = CheckGuitarName(guitar, null);
            return InTransaction(() => InsertPart(guitar, null, name));
        }

        // Throws KeyNotFoundException if there's no guitar with the id. When problems is given, a stored value that
        // can't be read is skipped and described there instead of throwing.
        public Guitar LoadGuitar(long id, ICollection<string> problems = null) => (Guitar)ReadPart(id, "guitar", problems);

        // Saves a copy of a saved guitar under a new name ("X copy", "X copy 2", ...) and a new uid, and returns its id.
        // Throws KeyNotFoundException if there's no guitar with the id.
        public long CopyGuitar(long id)
        {
            var guitar = LoadGuitar(id);
            string name = UnusedGuitarName($"{guitar.Name.Value?.Trim()} copy");
            return InTransaction(() => InsertPart(guitar, null, name));
        }

        // The name if no guitar has it (ignoring case), otherwise the name with " 2", " 3" and so on added.
        public string UnusedGuitarName(string name)
        {
            name = name?.Trim() ?? "";
            string candidate = name;
            for (int n = 2; GuitarExists(candidate); n++)
                candidate = $"{name} {n}";
            return candidate;
        }

        // Overwrites a saved guitar, including its name, neck, body and bracing.
        public void SaveGuitar(long id, Guitar guitar)
        {
            string name = CheckGuitarName(guitar, id);
            InTransaction(() =>
            {
                using var update = Command(
                    "UPDATE parts SET name = $name, modified = $now WHERE id = $id AND kind = 'guitar'");
                update.Parameters.AddWithValue("$name", name);
                update.Parameters.AddWithValue("$now", Now());
                update.Parameters.AddWithValue("$id", id);
                if (update.ExecuteNonQuery() == 0)
                    throw new KeyNotFoundException($"There's no guitar with id {id}.");

                Execute("DELETE FROM part_values WHERE part_id = $id", id);
                Execute("DELETE FROM parts WHERE parent_id = $id", id);
                InsertValues(id, guitar);
                InsertSeparatelySavedParts(id, guitar);
                return 0;
            });
        }

        public void DeleteGuitar(long id) => Execute("DELETE FROM parts WHERE id = $id AND kind = 'guitar'", id);

        string CheckGuitarName(Guitar guitar, long? id)
        {
            string name = guitar.Name.Value?.Trim();
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException($"A guitar needs an {guitar.Name.Label} to be saved.");
            long? existing = TopLevelId("guitar", name);
            if (existing != null && existing != id)
                throw new InvalidOperationException($"There's already a guitar called \"{name}\".");
            return name;
        }

        // --- Necks, bodies and bracing ---
        // T is Neck, Body or Bracing.

        // Every neck, body or bracing that can be loaded: the named copies first, then those of the saved guitars.
        public IReadOnlyList<AssemblyInfo> ListAssemblies<T>() where T : Part
        {
            CheckAssemblyType(typeof(T));
            return QueryAssemblies("p.kind = $kind", query => query.Parameters.AddWithValue("$kind", KindOf(typeof(T))));
        }

        // Loads a neck, body (with its bracing) or bracing, to replace a guitar's own. Throws KeyNotFoundException if
        // there's no such part with the id.
        public T LoadAssembly<T>(long id, ICollection<string> problems = null) where T : Part
        {
            CheckAssemblyType(typeof(T));
            return (T)ReadPart(id, KindOf(typeof(T)), problems);
        }

        // Saves a copy of a neck, body (with its bracing) or bracing under a name, replacing any copy of the same kind
        // with that name (ignoring case). Returns its id.
        public long SaveAssembly(string name, Part assembly)
        {
            CheckAssemblyType(assembly.GetType());
            name = name?.Trim();
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException($"A saved {KindName(assembly.GetType())} needs a name.");
            string kind = KindOf(assembly.GetType());
            return InTransaction(() =>
            {
                if (TopLevelId(kind, name) is long existing)
                    Execute("DELETE FROM parts WHERE id = $id", existing);
                return InsertPart(assembly, null, name);
            });
        }

        // Deletes a named copy. A guitar's own neck, body and bracing go only with the guitar.
        public void DeleteAssembly(long id)
        {
            using var delete = Command(
                "DELETE FROM parts WHERE id = $id AND parent_id IS NULL AND kind IN ('neck', 'body', 'bracing')");
            delete.Parameters.AddWithValue("$id", id);
            if (delete.ExecuteNonQuery() == 0)
                throw new InvalidOperationException(
                    "Only a named neck, body or bracing can be deleted. One that belongs to a guitar goes with it.");
        }

        static void CheckAssemblyType(Type type)
        {
            if (type != typeof(Neck) && type != typeof(Body) && type != typeof(Bracing))
                throw new ArgumentException($"{type.Name} isn't a neck, body or bracing.");
        }

        IReadOnlyList<AssemblyInfo> QueryAssemblies(string condition, Action<SqliteCommand> bind)
        {
            // A guitar's neck or body has the guitar as its parent; its bracing has the guitar's body. The bracing of
            // a named body has that body as its parent.
            using var query = Command($@"
                SELECT p.id, p.name, g.name, b.name, p.modified
                FROM parts p
                LEFT JOIN parts a ON a.id = p.parent_id
                LEFT JOIN parts g ON g.kind = 'guitar' AND g.id = (CASE a.kind WHEN 'guitar' THEN a.id ELSE a.parent_id END)
                LEFT JOIN parts b ON b.kind = 'body' AND b.parent_id IS NULL AND b.id = p.parent_id
                WHERE {condition}
                ORDER BY p.name IS NULL, coalesce(p.name, g.name, b.name) COLLATE NOCASE");
            bind(query);
            using var reader = query.ExecuteReader();
            var assemblies = new List<AssemblyInfo>();
            while (reader.Read())
                assemblies.Add(new AssemblyInfo(reader.GetInt64(0), NullableString(reader, 1),
                    NullableString(reader, 2), NullableString(reader, 3), ParseTime(reader.GetString(4))));
            return assemblies;
        }

        // --- Fretwire and string sets ---
        // T is Fretwire or StringSet.

        public IReadOnlyList<LibraryItemInfo> ListLibraryItems<T>() where T : Part
        {
            CheckLibraryItemType(typeof(T));
            using var query = Command("SELECT id, name FROM parts WHERE kind = $kind ORDER BY name COLLATE NOCASE");
            query.Parameters.AddWithValue("$kind", KindOf(typeof(T)));
            using var reader = query.ExecuteReader();
            var items = new List<LibraryItemInfo>();
            while (reader.Read())
                items.Add(new LibraryItemInfo(reader.GetInt64(0), reader.GetString(1)));
            return items;
        }

        // Throws KeyNotFoundException if there's no such item with the id. The item's Id is set.
        public T LoadLibraryItem<T>(long id, ICollection<string> problems = null) where T : Part
        {
            CheckLibraryItemType(typeof(T));
            return (T)ReadPart(id, KindOf(typeof(T)), problems);
        }

        // Saves a fretwire or string set: a new one when its Id isn't set, otherwise over the saved one. Returns its
        // id. Throws InvalidOperationException if it has no manufacturer and product code (or set name), or another
        // item of its kind already has them (ignoring case).
        public long SaveLibraryItem(Part item)
        {
            CheckLibraryItemType(item.GetType());
            string kind = KindOf(item.GetType());
            string name = LibraryItemName(item);
            long? id = item switch { Fretwire f => f.Id, StringSet s => s.Id, _ => null };
            long? existing = TopLevelId(kind, name);
            if (existing != null && existing != id)
                throw new InvalidOperationException($"There's already a {KindName(item.GetType())} called \"{name}\".");

            return InTransaction(() =>
            {
                if (id is not long savedId)
                    return InsertPart(item, null, name);
                using var update = Command("UPDATE parts SET name = $name, modified = $now WHERE id = $id AND kind = $kind");
                update.Parameters.AddWithValue("$name", name);
                update.Parameters.AddWithValue("$now", Now());
                update.Parameters.AddWithValue("$id", savedId);
                update.Parameters.AddWithValue("$kind", kind);
                if (update.ExecuteNonQuery() == 0)
                    throw new KeyNotFoundException($"There's no {KindName(item.GetType())} with id {savedId}.");
                Execute("DELETE FROM part_values WHERE part_id = $id", savedId);
                InsertValues(savedId, item);
                return savedId;
            });
        }

        // The necks that use a fretwire or string set: those of saved guitars and named copies.
        public IReadOnlyList<AssemblyInfo> NecksUsing(long itemId) =>
            QueryAssemblies("p.kind = 'neck' AND (p.fretwire_id = $item OR p.string_set_id = $item)",
                query => query.Parameters.AddWithValue("$item", itemId));

        // Deletes a fretwire or string set. When necks use it, a replacement of the same kind must be given: they're
        // all switched to it first. Throws InvalidOperationException if it's in use and there's no replacement.
        public void DeleteLibraryItem(long id, long? replacementId = null)
        {
            InTransaction(() =>
            {
                string kind = KindOfRow(id);
                if (kind != "fretwire" && kind != "string_set")
                    throw new KeyNotFoundException($"There's no fretwire or string set with id {id}.");
                string column = kind == "fretwire" ? "fretwire_id" : "string_set_id";

                var users = NecksUsing(id);
                if (users.Count > 0)
                {
                    if (replacementId is not long replacement)
                        throw new InvalidOperationException(
                            $"It's used by {users.Count} neck{(users.Count == 1 ? "" : "s")}: " +
                            string.Join(", ", users.Select(u => u.Name ?? u.GuitarName)) + ". Choose a replacement.");
                    if (replacement == id || KindOfRow(replacement) != kind)
                        throw new InvalidOperationException("The replacement must be another item of the same kind.");
                    using var update = Command($"UPDATE parts SET {column} = $replacement WHERE {column} = $id");
                    update.Parameters.AddWithValue("$replacement", replacement);
                    update.Parameters.AddWithValue("$id", id);
                    update.ExecuteNonQuery();
                }
                Execute("DELETE FROM parts WHERE id = $id", id);
                return 0;
            });
        }

        static void CheckLibraryItemType(Type type)
        {
            if (type != typeof(Fretwire) && type != typeof(StringSet))
                throw new ArgumentException($"{type.Name} isn't a fretwire or string set.");
        }

        static string LibraryItemName(Part item)
        {
            var (manufacturer, product) = item switch
            {
                Fretwire f => (f.Manufacturer, f.ProductCode),
                StringSet s => (s.Manufacturer, s.Name),
                _ => throw new ArgumentException($"{item.GetType().Name} isn't a fretwire or string set."),
            };
            // A fretwire's manufacturer is optional: without one, its name is just the product code.
            bool manufacturerOptional = item is Fretwire;
            if ((!manufacturerOptional && string.IsNullOrWhiteSpace(manufacturer.Value)) || string.IsNullOrWhiteSpace(product.Value))
                throw new InvalidOperationException(manufacturerOptional
                    ? $"A {KindName(item.GetType())} needs a {product.Label} to be saved in the library."
                    : $"A {KindName(item.GetType())} needs a {manufacturer.Label} and {product.Label} to be saved in the library.");
            return item switch { Fretwire f => f.DisplayName, StringSet s => s.DisplayName, _ => null };
        }

        // --- Saving and reading parts ---

        PartWriter Writer => new PartWriter(_connection, _transaction);

        long InsertPart(Part part, long? parentId, string name) => Writer.Insert(part, parentId, name);

        void InsertValues(long id, Part part) => Writer.InsertValues(id, part);

        void InsertSeparatelySavedParts(long id, Part part) => Writer.InsertSeparatelySavedParts(id, part);

        // Reads a part of the given kind, its values, the parts saved on their own beneath it, and the library items
        // a neck refers to.
        Part ReadPart(long id, string kind, ICollection<string> problems = null)
        {
            using var query = Command("SELECT kind, fretwire_id, string_set_id FROM parts WHERE id = $id");
            query.Parameters.AddWithValue("$id", id);
            long? fretwireId, stringSetId;
            using (var reader = query.ExecuteReader())
            {
                if (!reader.Read() || reader.GetString(0) != kind)
                    throw new KeyNotFoundException($"There's no {kind.Replace('_', ' ')} with id {id}.");
                fretwireId = reader.IsDBNull(1) ? null : reader.GetInt64(1);
                stringSetId = reader.IsDBNull(2) ? null : reader.GetInt64(2);
            }

            var values = ReadValues(id);
            // A guitar's name is in parts.name only; an older library may also hold it as an IN value, which is ignored.
            if (kind == "guitar")
                values.RemoveAll(v => v.Abbreviation == "IN" && v.Item == null);
            Part part = NewPart(kind).WithStoredValues(values, problems);
            if (part is Guitar named)
                part = named with { Name = new(Guitar.Definitions.Name, NameOfRow(id)) };
            part = part switch
            {
                Neck neck => neck with
                {
                    Fretwire = fretwireId is long f ? (Fretwire)ReadPart(f, "fretwire", problems) : null,
                    StringSet = stringSetId is long s ? (StringSet)ReadPart(s, "string_set", problems) : null,
                },
                Fretwire fretwire => fretwire with { Id = id },
                StringSet stringSet => stringSet with { Id = id },
                _ => part,
            };

            using var children = Command("SELECT id, kind FROM parts WHERE parent_id = $id ORDER BY id");
            children.Parameters.AddWithValue("$id", id);
            var childRows = new List<(long Id, string Kind)>();
            using (var reader = children.ExecuteReader())
                while (reader.Read())
                    childRows.Add((reader.GetInt64(0), reader.GetString(1)));
            foreach (var (childId, childKind) in childRows)
                part = Attach(part, ReadPart(childId, childKind, problems));
            return part;
        }

        string NameOfRow(long id)
        {
            using var query = Command("SELECT name FROM parts WHERE id = $id");
            query.Parameters.AddWithValue("$id", id);
            return query.ExecuteScalar() as string;
        }

        List<PartValue> ReadValues(long id)
        {
            using var query = Command("SELECT abbreviation, item, value FROM part_values WHERE part_id = $id");
            query.Parameters.AddWithValue("$id", id);
            using var reader = query.ExecuteReader();
            var values = new List<PartValue>();
            while (reader.Read())
            {
                long item = reader.GetInt64(1);
                values.Add(new PartValue(reader.GetString(0), item < 0 ? null : (int)item, reader.GetValue(2)));
            }
            return values;
        }

        static Part Attach(Part parent, Part child) => (parent, child) switch
        {
            (Guitar guitar, Neck neck) => guitar with { Neck = neck },
            (Guitar guitar, Body body) => guitar with { Body = body },
            (Body body, Bracing bracing) => body with { TopPlate = body.TopPlate with { Bracing = bracing } },
            _ => throw new InvalidDataException(
                $"A {child.GetType().Name} can't belong to a {parent.GetType().Name}."),
        };

        static Part NewPart(string kind) => kind switch
        {
            "guitar" => new Guitar(),
            "neck" => new Neck(),
            "body" => new Body(),
            "bracing" => new Bracing(),
            "fretwire" => new Fretwire(),
            "string_set" => new StringSet(),
            _ => throw new InvalidDataException($"Unknown kind of part \"{kind}\"."),
        };

        static string KindOf(Type type) => PartWriter.KindOf(type);

        // A kind of part as it reads in a message, e.g. "string set".
        static string KindName(Type type) => PartWriter.KindName(type);

        // The kind of the row with the id, or null when there's none.
        string KindOfRow(long id)
        {
            using var query = Command("SELECT kind FROM parts WHERE id = $id");
            query.Parameters.AddWithValue("$id", id);
            return query.ExecuteScalar() as string;
        }

        // The id of the part of the kind with the name and no parent, or null when there's none. Names are compared
        // ignoring case.
        long? TopLevelId(string kind, string name)
        {
            using var query = Command("SELECT id FROM parts WHERE kind = $kind AND parent_id IS NULL AND name = $name");
            query.Parameters.AddWithValue("$kind", kind);
            query.Parameters.AddWithValue("$name", name);
            return query.ExecuteScalar() as long?;
        }

        void Execute(string sql, long id)
        {
            using var command = Command(sql);
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        static string NullableString(SqliteDataReader reader, int column) =>
            reader.IsDBNull(column) ? null : reader.GetString(column);
    }
}
