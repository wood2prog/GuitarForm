using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    // A piece of the guitar: a set of parameters, plus the parts it's made of. Parts are immutable; change a value
    // with With, which returns a copy.
    public abstract record Part
    {
        static readonly ConcurrentDictionary<Type, PropertyInfo[]> ParameterPropertiesByType = new();
        static readonly ConcurrentDictionary<Type, PropertyInfo[]> PartPropertiesByType = new();

        // This part's parameters, in the order they're declared.
        public IEnumerable<Parameter> Parameters =>
            ParameterProperties(GetType()).Select(property => (Parameter)property.GetValue(this));

        // The parts this part holds directly, in the order they're declared. Lists of parts aren't included.
        public IEnumerable<Part> Subparts =>
            PartProperties(GetType()).Select(property => (Part)property.GetValue(this)).Where(part => part != null);

        // A copy with the parameter that has the same definition replaced by this one.
        public Part With(Parameter parameter)
        {
            PropertyInfo property = ParameterProperties(GetType())
                .FirstOrDefault(p => ((Parameter)p.GetValue(this)).Definition == parameter.Definition);
            if (property == null)
                throw new ArgumentException($"{GetType().Name} has no parameter {parameter.Label}.", nameof(parameter));
            Part copy = this with { };
            property.SetValue(copy, parameter);
            return copy;
        }

        // Problems with this part's values and with the parts it's made of.
        public IEnumerable<ModelMessage> Validate(LengthUnit unit, IFormatProvider culture = null) =>
            Parameters.SelectMany(parameter => parameter.Validate(unit, culture))
                .Concat(CheckValues(unit, culture))
                .Concat(Subparts.SelectMany(part => part.Validate(unit, culture)));

        // Problems between this part's values, and with any lists of parts it holds.
        protected virtual IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture) =>
            Enumerable.Empty<ModelMessage>();

        // An error for each of the parameters that isn't set, saying the condition's value requires it, e.g. "Soundhole
        // Diameter (SHD) is required when Soundhole (SH) is Yes."
        protected static IEnumerable<ModelMessage> RequiredWhen(Parameter condition, LengthUnit unit,
            IFormatProvider culture, params Parameter[] parameters) =>
            parameters.Where(parameter => !parameter.HasValue).Select(parameter => ModelMessage.Error(
                $"{parameter.Label} is required when {condition.Label} is {condition.FormatWithUnit(unit, culture)}."));

        // Checks each part in a list, with its place in the list in front of its messages, e.g. "String 3: ...".
        protected static IEnumerable<ModelMessage> ValidateList(IEnumerable<Part> parts, string itemName,
            LengthUnit unit, IFormatProvider culture) =>
            parts.SelectMany((part, index) => part.Validate(unit, culture)
                .Select(message => message with { Text = $"{itemName} {index + 1}: {message.Text}" }));

        static PropertyInfo[] ParameterProperties(Type type) =>
            ParameterPropertiesByType.GetOrAdd(type, t => DeclaredProperties(t, typeof(Parameter)));

        static PropertyInfo[] PartProperties(Type type) =>
            PartPropertiesByType.GetOrAdd(type, t => DeclaredProperties(t, typeof(Part)));

        static PropertyInfo[] DeclaredProperties(Type type, Type propertyType) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => propertyType.IsAssignableFrom(p.PropertyType) && p.CanWrite)
                .OrderBy(p => p.MetadataToken)
                .ToArray();
    }
}
