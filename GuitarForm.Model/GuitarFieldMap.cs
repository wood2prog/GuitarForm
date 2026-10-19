using GuitarForm.Model.Parameters;
using GuitarForm.Model.Parts;
using NewBody = GuitarForm.Model.Parts.Body;
using OldBody = GuitarForm.Model.Body;

namespace GuitarForm.Model
{
    // TEMPORARY, until the field editors move to the new model (deleted with the old sections): the old section records
    // (Instrument, Body, Soundhole) that the editors still edit, as a view of a Guitar, and edits of that view written
    // back to the Guitar. The mapping is the frozen one of version 1 designs (see GuitarDocument's DesignFields).
    //
    // The old records are not nullable where the old tables weren't (scale length, neck join fret, body length, the
    // soundhole's diameter and tail offset); a guitar without one shows 0. Writing a view back changes only what differs
    // from the view of the guitar, so an unset value stays unset unless the edit touched it.
    public static class GuitarFieldMap
    {
        // The guitar as a design of the old model. The soundhole is there only if the guitar has one.
        public static GuitarDesign ToDesign(Guitar guitar) => new GuitarDesign
        {
            Name = guitar.Name.Value ?? "",
            Instrument = ToInstrument(guitar),
            Body = ToBody(guitar.Body),
            Soundhole = ToSoundhole(guitar.Body.TopPlate),
        };

        static Instrument ToInstrument(Guitar guitar) => new Instrument
        {
            ScaleLength = guitar.ScaleLength.Value ?? 0,
            NeckJoinFret = guitar.NeckJoinFret.Value ?? 0,
        };

        static OldBody ToBody(NewBody b) => new OldBody
        {
            BodyLength = b.Length.Value ?? 0,
            UbWidth = b.UpperBoutWidth.Value,
            UbOffset = b.UpperBoutOffsetFromNeckEnd.Value,
            UbRadius = b.UpperBoutPrimaryRadius.Value,
            UbSecondaryOffset = b.UpperBoutSecondaryOffset.Value,
            WaistRadius = b.WaistRadius.Value,
            WaistWidth = b.WaistWidth.Value,
            WaistOffset = b.WaistOffsetFromTailEnd.Value,
            LbWidth = b.LowerBoutWidth.Value,
            LbOffset = b.LowerBoutOffsetFromTailEnd.Value,
            LbRadius = b.LowerBoutPrimaryRadius.Value,
            LbSecondaryOffset = b.LowerBoutSecondaryOffset.Value,
            HeelWidth = b.HeelFlatWidth.Value,
            TailDepth = b.DepthAtTailEnd.Value,
            NeckDepth = b.DepthAtNeckEnd.Value,
        };

        static Soundhole ToSoundhole(TopPlate t) => t.Soundhole.Value
            ? new Soundhole
            {
                Diameter = t.SoundholeDiameter.Value ?? 0,
                HorizontalOffset = t.SoundholeOffsetFromCenterline.Value,
                TailOffset = t.SoundholeOffsetFromTailEnd.Value ?? 0,
            }
            : null;

        // The guitar with the design's name, instrument, body and soundhole written to it. The same guitar object if
        // nothing differs from its own view.
        public static Guitar WithDesign(Guitar guitar, GuitarDesign design)
        {
            var current = ToDesign(guitar);
            if (design.Name != current.Name)
                guitar = guitar with { Name = guitar.Name with { Value = design.Name } };
            if (design.Instrument != null && design.Instrument != current.Instrument)
                guitar = WithInstrument(guitar, design.Instrument, current.Instrument);
            if (design.Body != null && design.Body != current.Body)
                guitar = guitar with { Body = WithBody(guitar.Body, design.Body, current.Body) };
            if (design.Soundhole != current.Soundhole)
                guitar = guitar with { Body = guitar.Body with { TopPlate = WithSoundhole(guitar.Body.TopPlate, design.Soundhole, current.Soundhole) } };
            return guitar;
        }

        static NumberParameter Number(NumberParameter parameter, double? now, double? before) =>
            now == before ? parameter : parameter with { Value = now };

        static Guitar WithInstrument(Guitar g, Instrument now, Instrument before) => g with
        {
            ScaleLength = Number(g.ScaleLength, now.ScaleLength, before.ScaleLength),
            NeckJoinFret = now.NeckJoinFret == before.NeckJoinFret ? g.NeckJoinFret : g.NeckJoinFret with { Value = now.NeckJoinFret },
        };

        static NewBody WithBody(NewBody b, OldBody now, OldBody before) => b with
        {
            Length = Number(b.Length, now.BodyLength, before.BodyLength),
            UpperBoutWidth = Number(b.UpperBoutWidth, now.UbWidth, before.UbWidth),
            UpperBoutOffsetFromNeckEnd = Number(b.UpperBoutOffsetFromNeckEnd, now.UbOffset, before.UbOffset),
            UpperBoutPrimaryRadius = Number(b.UpperBoutPrimaryRadius, now.UbRadius, before.UbRadius),
            UpperBoutSecondaryOffset = Number(b.UpperBoutSecondaryOffset, now.UbSecondaryOffset, before.UbSecondaryOffset),
            WaistRadius = Number(b.WaistRadius, now.WaistRadius, before.WaistRadius),
            WaistWidth = Number(b.WaistWidth, now.WaistWidth, before.WaistWidth),
            WaistOffsetFromTailEnd = Number(b.WaistOffsetFromTailEnd, now.WaistOffset, before.WaistOffset),
            LowerBoutWidth = Number(b.LowerBoutWidth, now.LbWidth, before.LbWidth),
            LowerBoutOffsetFromTailEnd = Number(b.LowerBoutOffsetFromTailEnd, now.LbOffset, before.LbOffset),
            LowerBoutPrimaryRadius = Number(b.LowerBoutPrimaryRadius, now.LbRadius, before.LbRadius),
            LowerBoutSecondaryOffset = Number(b.LowerBoutSecondaryOffset, now.LbSecondaryOffset, before.LbSecondaryOffset),
            HeelFlatWidth = Number(b.HeelFlatWidth, now.HeelWidth, before.HeelWidth),
            DepthAtTailEnd = Number(b.DepthAtTailEnd, now.TailDepth, before.TailDepth),
            DepthAtNeckEnd = Number(b.DepthAtNeckEnd, now.NeckDepth, before.NeckDepth),
        };

        // No soundhole clears the flag and its values (as a design without a soundhole row had none); a soundhole that
        // is new writes all of its values.
        static TopPlate WithSoundhole(TopPlate t, Soundhole now, Soundhole before)
        {
            if (now == null)
                return t with
                {
                    Soundhole = t.Soundhole with { Value = false },
                    SoundholeDiameter = t.SoundholeDiameter with { Value = null },
                    SoundholeOffsetFromCenterline = t.SoundholeOffsetFromCenterline with { Value = null },
                    SoundholeOffsetFromTailEnd = t.SoundholeOffsetFromTailEnd with { Value = null },
                };
            if (before == null)
                before = new Soundhole { Diameter = double.NaN, HorizontalOffset = double.NaN, TailOffset = double.NaN };
            return t with
            {
                Soundhole = t.Soundhole with { Value = true },
                SoundholeDiameter = Number(t.SoundholeDiameter, now.Diameter, before.Diameter),
                SoundholeOffsetFromCenterline = Number(t.SoundholeOffsetFromCenterline, now.HorizontalOffset, before.HorizontalOffset),
                SoundholeOffsetFromTailEnd = Number(t.SoundholeOffsetFromTailEnd, now.TailOffset, before.TailOffset),
            };
        }
    }
}
