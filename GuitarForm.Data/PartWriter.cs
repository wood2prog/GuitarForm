using System;
using System.Linq;
using GuitarForm.Model.Parts;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Data
{
    // Inserts a part, its values and the parts saved with it on their own, on a given connection and transaction, so
    // the library and a migration step can share it.
    internal sealed class PartWriter
    {
        readonly SqliteConnection _connection;
        readonly SqliteTransaction _transaction;

        public PartWriter(SqliteConnection connection, SqliteTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        // A new permanent uid: 32 lowercase hex characters, as the schema's backfill makes them.
        public static string NewUid() => Guid.NewGuid().ToString("N");

        // Inserts a part, its values and the parts saved with it on their own, and returns its id. A guitar gets the
        // uid given, or a new one. The name is stored in parts.name only.
        public long Insert(Part part, long? parentId, string name, string uid = null, long? legacyDesignId = null)
        {
            long? fretwireId = null, stringSetId = null;
            if (part is Neck neck)
            {
                fretwireId = LibraryItemId(neck.Fretwire, neck.Fretwire?.Id, "fretwire");
                stringSetId = LibraryItemId(neck.StringSet, neck.StringSet?.Id, "string_set");
            }
            if (part is Guitar)
                uid ??= NewUid();

            using var insert = Command(@"
                INSERT INTO parts (kind, parent_id, name, modified, fretwire_id, string_set_id, uid, legacy_design_id)
                VALUES ($kind, $parent, $name, $now, $fretwire, $stringSet, $uid, $legacy) RETURNING id");
            insert.Parameters.AddWithValue("$kind", KindOf(part.GetType()));
            insert.Parameters.AddWithValue("$parent", (object)parentId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$name", (object)name ?? DBNull.Value);
            insert.Parameters.AddWithValue("$now", Library.Now());
            insert.Parameters.AddWithValue("$fretwire", (object)fretwireId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$stringSet", (object)stringSetId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$uid", (object)uid ?? DBNull.Value);
            insert.Parameters.AddWithValue("$legacy", (object)legacyDesignId ?? DBNull.Value);
            long id = (long)insert.ExecuteScalar();

            InsertValues(id, part);
            InsertSeparatelySavedParts(id, part);
            return id;
        }

        // The neck, body and bracing reached from the part. Fretwire and string sets are referred to, not saved here.
        public void InsertSeparatelySavedParts(long id, Part part)
        {
            foreach (var child in part.SeparatelySavedParts.Where(p => p is not (Fretwire or StringSet)))
                Insert(child, id, null);
        }

        // A guitar's name isn't a value: it's stored once, in parts.name.
        public void InsertValues(long id, Part part)
        {
            using var insert = Command(
                "INSERT INTO part_values (part_id, abbreviation, item, value) VALUES ($id, $abbreviation, $item, $value)");
            var abbreviation = insert.Parameters.Add("$abbreviation", SqliteType.Text);
            var item = insert.Parameters.Add("$item", SqliteType.Integer);
            // The value's type (REAL, INTEGER or TEXT) comes from each value.
            var value = insert.Parameters.Add(new SqliteParameter { ParameterName = "$value" });
            insert.Parameters.AddWithValue("$id", id);
            foreach (var partValue in part.StoredValues)
            {
                if (part is Guitar && partValue.Abbreviation == "IN" && partValue.Item == null)
                    continue;
                abbreviation.Value = partValue.Abbreviation;
                item.Value = partValue.Item ?? -1;
                value.Value = partValue.Value;
                insert.ExecuteNonQuery();
            }
        }

        // The library id of a fretwire or string set a neck refers to. Null when none is chosen.
        long? LibraryItemId(Part item, long? id, string kind)
        {
            if (item == null)
                return null;
            if (id is not long itemId || KindOfRow(itemId) != kind)
                throw new InvalidOperationException(
                    $"The neck's {KindName(item.GetType())} isn't in the library. Save it to the library first.");
            return itemId;
        }

        string KindOfRow(long id)
        {
            using var query = Command("SELECT kind FROM parts WHERE id = $id");
            query.Parameters.AddWithValue("$id", id);
            return query.ExecuteScalar() as string;
        }

        SqliteCommand Command(string sql)
        {
            var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _transaction;
            return command;
        }

        public static string KindOf(Type type) =>
            type == typeof(Guitar) ? "guitar" :
            type == typeof(Neck) ? "neck" :
            type == typeof(Body) ? "body" :
            type == typeof(Bracing) ? "bracing" :
            type == typeof(Fretwire) ? "fretwire" :
            type == typeof(StringSet) ? "string_set" :
            throw new ArgumentException($"{type.Name} isn't saved on its own.");

        // A kind of part as it reads in a message, e.g. "string set".
        public static string KindName(Type type) => KindOf(type).Replace('_', ' ');
    }
}
