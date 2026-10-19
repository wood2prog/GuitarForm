using System;
using System.Collections.Generic;
using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using GeometryMessage = GuitarForm.Model.GeometryMessage;
using MessageLevel = GuitarForm.Model.MessageLevel;

namespace GuitarForm.Shapes
{
    // Adds messages to a shape's list and formats the lengths in them in the unit and culture the caller asked for.
    internal sealed class ShapeMessages
    {
        readonly List<GeometryMessage> _list;
        readonly LengthUnit _unit;
        readonly IFormatProvider _culture;

        internal ShapeMessages(List<GeometryMessage> list, LengthUnit unit, IFormatProvider culture)
        {
            _list = list;
            _unit = unit;
            _culture = culture;
        }

        internal void Error(string text) => _list.Add(new GeometryMessage(MessageLevel.Error, text));

        internal void Remark(string text) => _list.Add(new GeometryMessage(MessageLevel.Remark, text));

        internal void Warning(string text) => _list.Add(new GeometryMessage(MessageLevel.Warning, text));

        internal string Format(double millimeters) =>
            new NumberParameter(Body.Definitions.Length, millimeters).FormatWithUnit(_unit, _culture);
    }
}
