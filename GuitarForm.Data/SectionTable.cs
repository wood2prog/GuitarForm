using System;
using System.Linq;
using GuitarForm.Model;
using Microsoft.Data.Sqlite;

namespace GuitarForm.Data
{
    // Maps one section record to its table (see Schema). The columns must match the table's columns in Schema.
    sealed class SectionTable<T>
    {
        public string Table { get; }

        readonly (string Name, Func<T, object> Get)[] _columns;
        readonly Func<SqliteDataReader, T> _read;

        public SectionTable(string table, (string, Func<T, object>)[] columns, Func<SqliteDataReader, T> read)
        {
            Table = table;
            _columns = columns;
            _read = read;
        }

        public string SelectColumns => string.Join(", ", _columns.Select(c => c.Name));

        // Adds the section as a row owned by the design or the preset; the other owner is null.
        public void Insert(Func<string, SqliteCommand> command, long? designId, string presetName, T section)
        {
            string names = string.Join(", ", _columns.Select(c => c.Name));
            string values = string.Join(", ", _columns.Select((_, i) => "$c" + i));
            using var insert = command(
                $"INSERT INTO {Table} (design_id, preset_name, {names}) VALUES ($design, $preset, {values})");
            insert.Parameters.AddWithValue("$design", (object)designId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$preset", (object)presetName ?? DBNull.Value);
            for (int i = 0; i < _columns.Length; i++)
                insert.Parameters.AddWithValue("$c" + i, _columns[i].Get(section) ?? DBNull.Value);
            insert.ExecuteNonQuery();
        }

        // Reads the section from the first row of a query that selected SelectColumns, or default if there's no row.
        public T ReadSingle(SqliteCommand query)
        {
            using var reader = query.ExecuteReader();
            return reader.Read() ? _read(reader) : default;
        }
    }

    // The table for each section.
    static class SectionTables
    {
        static double Number(SqliteDataReader r, string column) => r.GetDouble(r.GetOrdinal(column));

        static double? OptionalNumber(SqliteDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDouble(i);
        }

        public static readonly SectionTable<Instrument> Instruments = new SectionTable<Instrument>(
            "instruments",
            new (string, Func<Instrument, object>)[]
            {
                ("scale_length", s => s.ScaleLength),
                ("neck_join_fret", s => s.NeckJoinFret),
            },
            r => new Instrument
            {
                ScaleLength = Number(r, "scale_length"),
                NeckJoinFret = r.GetInt32(r.GetOrdinal("neck_join_fret")),
            });

        public static readonly SectionTable<Body> Bodies = new SectionTable<Body>(
            "bodies",
            new (string, Func<Body, object>)[]
            {
                ("body_length", s => s.BodyLength),
                ("ub_width", s => s.UbWidth),
                ("ub_offset", s => s.UbOffset),
                ("ub_radius", s => s.UbRadius),
                ("ub_secondary_offset", s => s.UbSecondaryOffset),
                ("waist_radius", s => s.WaistRadius),
                ("waist_width", s => s.WaistWidth),
                ("waist_offset", s => s.WaistOffset),
                ("lb_width", s => s.LbWidth),
                ("lb_offset", s => s.LbOffset),
                ("lb_radius", s => s.LbRadius),
                ("lb_secondary_offset", s => s.LbSecondaryOffset),
                ("heel_width", s => s.HeelWidth),
                ("tail_depth", s => s.TailDepth),
                ("neck_depth", s => s.NeckDepth),
            },
            r => new Body
            {
                BodyLength = Number(r, "body_length"),
                UbWidth = OptionalNumber(r, "ub_width"),
                UbOffset = OptionalNumber(r, "ub_offset"),
                UbRadius = OptionalNumber(r, "ub_radius"),
                UbSecondaryOffset = OptionalNumber(r, "ub_secondary_offset"),
                WaistRadius = OptionalNumber(r, "waist_radius"),
                WaistWidth = OptionalNumber(r, "waist_width"),
                WaistOffset = OptionalNumber(r, "waist_offset"),
                LbWidth = OptionalNumber(r, "lb_width"),
                LbOffset = OptionalNumber(r, "lb_offset"),
                LbRadius = OptionalNumber(r, "lb_radius"),
                LbSecondaryOffset = OptionalNumber(r, "lb_secondary_offset"),
                HeelWidth = OptionalNumber(r, "heel_width"),
                TailDepth = OptionalNumber(r, "tail_depth"),
                NeckDepth = OptionalNumber(r, "neck_depth"),
            });

        public static readonly SectionTable<Soundhole> Soundholes = new SectionTable<Soundhole>(
            "soundholes",
            new (string, Func<Soundhole, object>)[]
            {
                ("diameter", s => s.Diameter),
                ("horizontal_offset", s => s.HorizontalOffset),
                ("tail_offset", s => s.TailOffset),
            },
            r => new Soundhole
            {
                Diameter = Number(r, "diameter"),
                HorizontalOffset = OptionalNumber(r, "horizontal_offset"),
                TailOffset = Number(r, "tail_offset"),
            });
    }
}
