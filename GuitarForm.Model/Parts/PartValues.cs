using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    // One saved value of a part: its glossary abbreviation, its place in a list of parts (null when it isn't in one,
    // e.g. a dot marker's fret is in the fretboard's list of markers), and the value (see Parameter.StoredValue).
    // Abbreviations are unique across the model, so they identify a value anywhere in a part.
    //
    // A part in a list also has a marker value, so a part with no values of its own (a blank dot marker or string)
    // isn't lost: its abbreviation is ListItemKey of the list's property name, its item is the part's place, and its
    // value is 1. Readers that don't know the marker ignore it.
    public readonly record struct PartValue(string Abbreviation, int? Item, object Value)
    {
        // The abbreviation of the marker value for a part in the list with this property name, e.g. "#Markers".
        public static string ListItemKey(string listName) => "#" + listName;
    }

    // Saving and loading a part's values by abbreviation.
    //
    // A part is saved together with the parts it's made of, except those saved on their own: the guitar's neck and
    // body and the top plate's bracing are sub-assemblies another guitar can reuse, and a fretwire or string set is a
    // library item a neck refers to.
    public abstract partial record Part
    {
        static readonly ConcurrentDictionary<Type, PropertyInfo[]> ListPropertiesByType = new();
        static readonly ConcurrentDictionary<Type, HashSet<string>> AbbreviationsByType = new();

        // Whether this part is saved on its own rather than with the part that holds it.
        public virtual bool IsSavedSeparately => false;

        // The values that are set in this part and the parts saved with it.
        public IEnumerable<PartValue> StoredValues => StoredValuesOf(this, null);

        // A copy with the saved values set. A value whose abbreviation isn't in the part (e.g. a setting since
        // removed) is ignored. Lists of parts are rebuilt from the values that have an item number.
        // Throws FormatException for a value its setting can't take.
        public Part WithStoredValues(IEnumerable<PartValue> values) => WithStoredValues(values, null);

        // As above, but a value its setting can't take is skipped, with a message added to problems.
        public Part WithStoredValues(IEnumerable<PartValue> values, ICollection<string> problems)
        {
            var all = values.ToList();
            Part result = this;
            foreach (var value in all.Where(v => v.Item == null))
            {
                try
                {
                    result = result.WithStoredValue(value.Abbreviation, value.Value) ?? result;
                }
                catch (FormatException e) when (problems != null)
                {
                    problems.Add($"{value.Abbreviation} was skipped: {e.Message}");
                }
            }
            return result.WithListItems(all.Where(v => v.Item != null).ToList(), problems);
        }

        // The parameter with this abbreviation, in this part or a part saved with it (not in a list, nor in a part
        // saved on its own). Null when there's no such parameter.
        public Parameter FindParameter(string abbreviation) =>
            Parameters.FirstOrDefault(p => p.Definition.Abbreviation == abbreviation) ??
            Subparts.Where(p => !p.IsSavedSeparately).Select(p => p.FindParameter(abbreviation))
                .FirstOrDefault(p => p != null);

        // The parts reached through the parts saved with this one that are saved on their own, e.g. a guitar's neck
        // and body, or a body's bracing.
        public IEnumerable<Part> SeparatelySavedParts =>
            Subparts.SelectMany(part => part.IsSavedSeparately ? new[] { part } : part.SeparatelySavedParts);

        // The lists of parts held by this part and the parts saved with it, by property name, e.g. the fretboard's
        // "Markers" for a neck.
        public IEnumerable<(string Name, IReadOnlyList<Part> Items)> PartLists =>
            ListProperties(GetType())
                .Select(p => (p.Name, (IReadOnlyList<Part>)((IEnumerable<Part>)p.GetValue(this)).ToList()))
                .Concat(Subparts.Where(p => !p.IsSavedSeparately).SelectMany(p => p.PartLists));

        static IEnumerable<PartValue> StoredValuesOf(Part part, int? item)
        {
            foreach (var parameter in part.Parameters)
                if (parameter.StoredValue != null)
                    yield return new PartValue(parameter.Definition.Abbreviation, item, parameter.StoredValue);
            foreach (var subpart in part.Subparts.Where(p => !p.IsSavedSeparately))
                foreach (var value in StoredValuesOf(subpart, item))
                    yield return value;
            foreach (var list in ListProperties(part.GetType()))
            {
                int index = 0;
                foreach (Part listItem in (IEnumerable<Part>)list.GetValue(part))
                {
                    if (item != null)
                        throw new NotSupportedException("A part in a list can't hold a list of its own.");
                    yield return new PartValue(PartValue.ListItemKey(list.Name), index, 1);
                    foreach (var value in StoredValuesOf(listItem, index))
                        yield return value;
                    index++;
                }
            }
        }

        // A copy with the value set, here or in a part saved with this one. Null when there's no such parameter.
        Part WithStoredValue(string abbreviation, object value)
        {
            foreach (var property in ParameterProperties(GetType()))
            {
                var parameter = (Parameter)property.GetValue(this);
                if (parameter.Definition.Abbreviation != abbreviation)
                    continue;
                Part copy = this with { };
                property.SetValue(copy, parameter.WithStoredValue(value));
                return copy;
            }
            foreach (var property in PartProperties(GetType()))
            {
                var subpart = (Part)property.GetValue(this);
                if (subpart == null || subpart.IsSavedSeparately)
                    continue;
                var changed = subpart.WithStoredValue(abbreviation, value);
                if (changed == null)
                    continue;
                Part copy = this with { };
                property.SetValue(copy, changed);
                return copy;
            }
            return null;
        }

        // A copy with each list of parts, here or in a part saved with this one, rebuilt from the values whose
        // abbreviations belong to the list's parts. A list with no values is left as it is.
        Part WithListItems(IReadOnlyList<PartValue> values, ICollection<string> problems)
        {
            if (values.Count == 0)
                return this;
            Part result = this;
            foreach (var property in ListProperties(GetType()))
            {
                Type itemType = property.PropertyType.GetGenericArguments()[0];
                var abbreviations = AbbreviationsOf(itemType);
                string key = PartValue.ListItemKey(property.Name);
                var mine = values.Where(v => abbreviations.Contains(v.Abbreviation) || v.Abbreviation == key).ToList();
                if (mine.Count == 0)
                    continue;
                var items = Array.CreateInstance(itemType, mine.Max(v => v.Item.Value) + 1);
                for (int i = 0; i < items.Length; i++)
                    items.SetValue(((Part)Activator.CreateInstance(itemType))
                        .WithStoredValues(mine.Where(v => v.Item == i && v.Abbreviation != key)
                            .Select(v => v with { Item = null }), problems), i);
                if (ReferenceEquals(result, this))
                    result = this with { };
                property.SetValue(result, items);
            }
            foreach (var property in PartProperties(GetType()))
            {
                var subpart = (Part)property.GetValue(result);
                if (subpart == null || subpart.IsSavedSeparately)
                    continue;
                var changed = subpart.WithListItems(values, problems);
                if (ReferenceEquals(changed, subpart))
                    continue;
                if (ReferenceEquals(result, this))
                    result = this with { };
                property.SetValue(result, changed);
            }
            return result;
        }

        // The abbreviations of a kind of part's own parameters.
        static HashSet<string> AbbreviationsOf(Type type) =>
            AbbreviationsByType.GetOrAdd(type, t =>
                ((Part)Activator.CreateInstance(t)).Parameters.Select(p => p.Definition.Abbreviation).ToHashSet());

        // Properties holding a list of parts (IReadOnlyList<T> where T is a part), in the order they're declared.
        static PropertyInfo[] ListProperties(Type type) =>
            ListPropertiesByType.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) &&
                            typeof(Part).IsAssignableFrom(p.PropertyType.GetGenericArguments()[0]))
                .OrderBy(p => p.MetadataToken)
                .ToArray());
    }
}
