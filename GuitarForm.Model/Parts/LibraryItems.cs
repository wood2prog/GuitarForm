using System;
using System.Collections.Generic;
using System.Linq;
using GuitarForm.Model.Parameters;

namespace GuitarForm.Model.Parts
{
    public enum StringConstruction
    {
        Plain,
        Wound,
    }

    public enum StringCoreMaterial
    {
        NotSpecified,
        SolidNylon,
        NylonFloss,
        Steel,
        Gut,
    }

    // A fretwire from the library. Necks refer to one rather than owning a copy, since a builder doesn't change it.
    public sealed record Fretwire : Part
    {
        public override bool IsSavedSeparately => true;

        public static class Definitions
        {
            public static readonly ParameterDefinition Manufacturer = ParameterDefinition.Text("Fretwire Manufacturer", "FWM");
            public static readonly ParameterDefinition ProductCode = ParameterDefinition.Text("Fretwire Product Code", "FWPC");
            public static readonly ParameterDefinition BeadHeight = ParameterDefinition.Size("Fretwire Bead Height", "FWBH");
            public static readonly ParameterDefinition BeadWidth = ParameterDefinition.Size("Fretwire Bead Width", "FWBW");
            public static readonly ParameterDefinition TangHeight = ParameterDefinition.Size("Fretwire Tang Height", "FWTH");
            public static readonly ParameterDefinition TangThickness = ParameterDefinition.Size("Fretwire Tang Thickness", "FWTT");
            public static readonly ParameterDefinition BarbThickness = ParameterDefinition.Size("Fretwire Barb Thickness", "FWBT");
            public static readonly ParameterDefinition SlotDepth = ParameterDefinition.Size("Fretwire Slot Depth", "FWSD");
            // A plain number (no currency), zero or more.
            public static readonly ParameterDefinition Price = ParameterDefinition.Number("Fretwire Price", "FWPR", "", 0.01) with { MinimumExclusive = false };
        }

        // The library's id for this fretwire. Null until it's saved.
        public long? Id { get; init; }

        public TextParameter Manufacturer { get; init; } = new(Definitions.Manufacturer);
        public TextParameter ProductCode { get; init; } = new(Definitions.ProductCode);
        public NumberParameter BeadHeight { get; init; } = new(Definitions.BeadHeight);
        public NumberParameter BeadWidth { get; init; } = new(Definitions.BeadWidth);
        public NumberParameter TangHeight { get; init; } = new(Definitions.TangHeight);
        public NumberParameter TangThickness { get; init; } = new(Definitions.TangThickness);
        public NumberParameter BarbThickness { get; init; } = new(Definitions.BarbThickness);
        public NumberParameter SlotDepth { get; init; } = new(Definitions.SlotDepth);
        public NumberParameter Price { get; init; } = new(Definitions.Price);

        protected override IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture)
        {
            if (BarbThickness.Value <= TangThickness.Value)
                yield return ModelMessage.Error($"{BarbThickness.Label} must be more than {TangThickness.Label}.");
            if (TangThickness.Value >= BeadWidth.Value)
                yield return ModelMessage.Error($"{TangThickness.Label} must be less than {BeadWidth.Label}.");
        }
    }

    // A manufacturer's string set from the library. Necks refer to one rather than owning a copy.
    public sealed record StringSet : Part
    {
        public override bool IsSavedSeparately => true;

        public static class Definitions
        {
            public static readonly ParameterDefinition Manufacturer = ParameterDefinition.Text("String Set Manufacturer", "SSM");
            public static readonly ParameterDefinition Name = ParameterDefinition.Text("String Set Name", "SSN");
            public static readonly ParameterDefinition StringCount = ParameterDefinition.Count("String Count", "SC", 1);
        }

        // The library's id for this string set. Null until it's saved.
        public long? Id { get; init; }

        public TextParameter Manufacturer { get; init; } = new(Definitions.Manufacturer);
        public TextParameter Name { get; init; } = new(Definitions.Name);
        public CountParameter StringCount { get; init; } = new(Definitions.StringCount);

        // The strings, from the first (treble) to the last (bass).
        public IReadOnlyList<GuitarString> Strings { get; init; } = Array.Empty<GuitarString>();

        // The set with these strings, and its string count set to match.
        public StringSet WithStrings(IReadOnlyList<GuitarString> strings) =>
            this with { Strings = strings, StringCount = StringCount with { Value = strings.Count } };

        // The set with a plain string added at the end (the bass).
        public StringSet WithAddedString() =>
            WithStrings(Strings.Concat(new[]
            {
                new GuitarString { Construction = new(GuitarString.Definitions.Construction, StringConstruction.Plain) },
            }).ToList());

        // The set without the string at index (counted from the treble).
        public StringSet WithRemovedString(int index)
        {
            if (index < 0 || index >= Strings.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "There's no string at that index.");
            return WithStrings(Strings.Where((_, i) => i != index).ToList());
        }

        protected override IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture)
        {
            if (StringCount.Value is int count && count != Strings.Count)
                yield return ModelMessage.Error($"{StringCount.Label} is {count}, but the set has {Strings.Count} strings.");
            foreach (var message in ValidateList(Strings, "String", unit, culture))
                yield return message;
        }
    }

    // One string of a string set. The core settings apply to wound strings, and the modulus and strength only when
    // the core material isn't specified; otherwise they're ignored. The unit weight is stored in kg/m.
    public sealed record GuitarString : Part
    {
        public static class Definitions
        {
            public static readonly ParameterDefinition Description = ParameterDefinition.Text("String Description", "STD");
            public static readonly ParameterDefinition Diameter = ParameterDefinition.Size("String Diameter", "STDI", required: true);
            public static readonly ParameterDefinition Construction = ParameterDefinition.Choice("String Construction", "STC");
            public static readonly ParameterDefinition UnitWeight = ParameterDefinition.Number("String Unit Weight", "STUW", "kg/m", 1e-7) with { Required = true };
            public static readonly ParameterDefinition HexCore = ParameterDefinition.Flag("String Hex Core", "STHC");
            public static readonly ParameterDefinition CoreMaterial = ParameterDefinition.Choice("String Core Material", "STCM");
            public static readonly ParameterDefinition CoreModulus = ParameterDefinition.Number("String Core Modulus", "STCE", "GPa", 0.1);
            public static readonly ParameterDefinition CoreStrength = ParameterDefinition.Number("String Core Strength", "STCS", "MPa", 1);
            public static readonly ParameterDefinition UsedInSaddleCompensation = ParameterDefinition.Flag("String Used in Saddle Compensation", "STSC");
        }

        public TextParameter Description { get; init; } = new(Definitions.Description);
        public NumberParameter Diameter { get; init; } = new(Definitions.Diameter);
        public ChoiceParameter<StringConstruction> Construction { get; init; } = new(Definitions.Construction);
        public NumberParameter UnitWeight { get; init; } = new(Definitions.UnitWeight);
        public FlagParameter HexCore { get; init; } = new(Definitions.HexCore);
        public ChoiceParameter<StringCoreMaterial> CoreMaterial { get; init; } = new(Definitions.CoreMaterial);
        public NumberParameter CoreModulus { get; init; } = new(Definitions.CoreModulus);
        public NumberParameter CoreStrength { get; init; } = new(Definitions.CoreStrength);
        public FlagParameter UsedInSaddleCompensation { get; init; } = new(Definitions.UsedInSaddleCompensation);

        // Whether the string is wound. An unset construction counts as plain.
        public bool IsWound => Construction.Value == StringConstruction.Wound;

        // Which settings apply to this string: the core settings to wound strings, and the modulus and strength
        // only when the core material isn't specified.
        public bool UsesCoreMaterial => IsWound;
        public bool UsesHexCore => IsWound;
        public bool UsesCoreProperties => IsWound && CoreMaterial.Value == StringCoreMaterial.NotSpecified;

        protected override IEnumerable<ModelMessage> CheckValues(LengthUnit unit, IFormatProvider culture) =>
            Construction.Value == StringConstruction.Wound && CoreMaterial.Value == StringCoreMaterial.NotSpecified
                ? RequiredWhen(CoreMaterial, unit, culture, CoreModulus, CoreStrength)
                : Enumerable.Empty<ModelMessage>();
    }
}
