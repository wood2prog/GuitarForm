# Body tab

The guitar body: the plate (its construction lines, outline radii and the final body outline) and the side view.

The guitar is drawn vertically from the origin up +Y. Lengths are shown and typed in the Rhino document's units and stored in millimetres; see [Units](../README.md#units). Construction geometry (construction lines and outline radii circles) is drawn light grey; the final outline is drawn in Rhino's feedback colour in the preview, and on the `GuitarForm::Outline` layer when built.

The tables and formulas below use these short names:

| Field | Name | Description |
|---|---|---|
| Body length | L | *Required.* Overall length of the guitar body. Must be greater than 0. |
| Upper bout width | UbW | Width of the upper bout. Must be greater than 0. |
| Upper bout offset | UbO | Offset of the upper bout line down Y from the upper bout primary radius centre. A warning appears if the line falls outside 0–L. |
| Upper bout primary radius | UbR | Primary radius of the upper bout. Must be greater than 0. A warning appears if it's more than half of UbW. |
| Upper bout secondary offset | UbSO | Moves the upper bout secondary radius centre toward the body centre and grows its radius by the same amount. 0 = same as the primary radius. Can't be negative. |
| Waist radius | WR | Radius of the waist curve. Must be greater than 0. |
| Waist width | WW | Width of the body at the waist. Must be greater than 0. |
| Waist offset | WO | Distance of the waist center line up Y from the tail end (the origin). A warning appears if it's outside 0–L. |
| Lower bout width | LbW | Width of the lower bout. Must be greater than 0. |
| Lower bout offset | LbO | Offset of the lower bout center line up Y from the lower bout primary radius centre. A warning appears if the line falls outside 0–L. Can't be negative. |
| Lower bout primary radius | LbR | Primary radius of the lower bout. Must be greater than 0. A warning appears if it's more than half of LbW. |
| Lower bout secondary offset | LbSO | Same as UbSO, for the lower bout. |
| Heel width | HW | Width of the flat at the top of the body where the heel of the neck attaches. Must be greater than 0, and no more than the distance between the upper bout circle centres. |
| Tail depth | TD | Depth of the body at the tail end, for the side view. Must be greater than 0. |
| Neck depth | ND | Depth of the body at the neck end, for the side view. Must be greater than 0. |

Everything except the body length is optional: a part of the drawing appears once the fields it needs are set. If an error stops the plate, nothing of it is drawn.

## Plate

### Construction lines

1. **Centerline:** from (0, 0) to (0, L).
2. **Upper bout:** from (−UbW/2, L − UbR − UbO) to (UbW/2, L − UbR − UbO), a horizontal line centred on the Y axis. It's drawn only when UbW, UbO and UbR are all set.
3. **Waist center line:** from (−WW/2, WO) to (WW/2, WO). It's drawn only when both WW and WO are set.
4. **Lower bout center line:** from (−LbW/2, LbR + LbO) to (LbW/2, LbR + LbO). It's drawn only when LbW, LbO and LbR are all set.
5. **Heel width marks:** two vertical lines L/100 long (1% of the body length) at x = ±HW/2, centred on y = L.

### Heel flat and shoulder arcs

The heel flat and marks are drawn when HW is set. There's an error if the upper bout circle centres are closer together than the heel width. There's also an error if UbO is negative, which would put the circles above the top of the body, or if the shoulder arc would meet the circle below its centre (see below).

**Heel flat:** a horizontal line at the top of the body (y = L), from −HW/2 to HW/2. It's lengthened only when UbO is 0, which means the upper bout circle tops are level with the top of the body. It then runs from −(UbW/2 − UbR) to (UbW/2 − UbR), directly above the two upper bout circle centres.

When UbO is more than 0, the upper bout circle tops sit below the top of the body. The heel flat then stays HW long, and a **shoulder arc** joins each end of the flat to its upper bout circle:

- The arc starts at (±HW/2, L), heading straight outward, so it joins the flat smoothly (G1). Its centre is directly below that point.
- It touches the upper bout circle from outside, wrapping around it.
- Its radius is (dx² + t² − UbR²) / (2·UbO), where dx = UbW/2 − UbR − HW/2 and t = UbR + UbO.
- There's an error if dx is less than UbO. The arc would then meet the circle below its centre and swing wider than the upper bout width.

### Outline radii

The outline radii are drawn as full circles, a mirrored pair for each region, in light grey:

1. **Upper bout primary pair:** radius UbR. Each centre is on the upper bout line, one radius **inside** the width, at (±(UbW/2 − UbR), L − UbR − UbO). Each circle touches the end of the upper bout line. The pair is drawn whenever the upper bout line is drawn.
2. **Upper bout secondary pair:** radius UbR + UbSO. Each centre is on the upper bout line, UbSO closer to the body centre than the primary centre, at (±(UbW/2 − UbR − UbSO), L − UbR − UbO). Its outer edge stays locked to the primary circle's at ±UbW/2. At UbSO = 0 it's the same as the primary circle. The pair is drawn when the upper bout line is drawn and UbSO is set.
3. **Waist pair:** radius WR. Each centre is on the waist center line, one radius **outside** the width, at (±(WW/2 + WR), WO). Each circle touches the end of the waist line from outside the body. The pair is drawn when the waist line is drawn and WR is set.
4. **Lower bout primary pair:** radius LbR. Each centre is on the lower bout center line, one radius **inside** the width, at (±(LbW/2 − LbR), LbR + LbO). Each circle touches the end of the lower bout line. The pair is drawn whenever the lower bout line is drawn.
5. **Lower bout secondary pair:** radius LbR + LbSO. Each centre is on the lower bout center line, LbSO closer to the body centre than the primary centre, at (±(LbW/2 − LbR − LbSO), LbR + LbO). Its outer edge stays locked to the primary circle's at ±LbW/2. The pair is drawn when the lower bout line is drawn and LbSO is set.

A secondary offset can't be negative. A secondary circle also can't overlap the waist circle on the same side, because the bout curve has to meet the waist curve tangentially. They may touch, but there's an error if they overlap.

### Waist tangent lines

A straight line from each bout secondary circle to the waist circle on the same side. It touches the secondary circle on its **outer** side and the waist circle on its **inner** side (the side facing the body), so the two circles sit on opposite sides of the line.

- A bout's pair is drawn when its secondary offset and WR are both set.
- As the secondary circle grows toward the waist circle, the line shortens. It disappears once the circles touch, and there's an error once they overlap.

### Tail end

Joins the two lower bout primary circles across the end of the body (y = 0).

- **LbO = 0:** the circle bottoms sit on the end of the body, so the tail is a straight line from (−(LbW/2 − LbR), 0) to (LbW/2 − LbR, 0).
- **LbO > 0:** the tail is one arc through the origin, tangent to both lower bout primary circles.
  - Its centre is on the Y axis at (0, ρ), and it wraps each circle, touching it from outside.
  - Its radius is ρ = (cx² + cy² − LbR²) / (2·LbO), where cx = LbW/2 − LbR and cy = LbR + LbO.
- **Errors:** if LbO is negative, or if cx is less than LbO. In the second case, the arc would meet the circle above its centre and swing wider than the lower bout width.

### Outline segments of the circles

The parts of the bout and waist circles that form the outside of the body. Following the right side from top to bottom (each is mirrored on the left):

| Segment | Circle | From | To |
|---|---|---|---|
| Upper bout primary | UbR | the shoulder arc's touch point, or the circle top (x = UbW/2 − UbR, y = L) when UbO = 0 | the widest point (UbW/2, upper bout line), which it shares with the secondary circle |
| Upper bout secondary | UbR + UbSO | the widest point | the start of the upper waist tangent line |
| Waist | WR, around its inner side | the end of the upper waist tangent line | the end of the lower waist tangent line |
| Lower bout secondary | LbR + LbSO | the start of the lower waist tangent line | the widest point (LbW/2, lower bout line) |
| Lower bout primary | LbR | the widest point | the tail arc's touch point, or the circle bottom (x = LbW/2 − LbR, y = 0) when LbO = 0 |

A segment is drawn only when both of its ends exist:
- the upper bout primary segment needs HW
- the secondary segments need their secondary offset and WR
- the waist segment needs both secondary offsets

If the circles touch so that a waist tangent line shrinks to a point, the segments meet at that point.

Together with the heel flat, shoulder arcs, waist tangent lines and tail, these make one continuous outline.

## Side view

The side view is drawn once both TD and ND are set; until then a note says so. It's built the same way up as the plate, with the tail end at y = 0 and the body length up +Y, then placed to the right of the plate: its bottom line sits the drawing gap (see [Settings](Settings.md#drawing), 50 mm by default) right of the plate's widest point. Before it's moved right, its outline is:

1. **Tail depth line:** from (0, 0) to (TD, 0).
2. **Neck depth line:** from (0, L) to (ND, L).
3. **Bottom line:** from (0, 0) to (0, L).
4. **Top line:** from (TD, 0) to (ND, L).

## Quick test

In a document in millimetres, make a design with body length 600 and type these values on the Body tab:

| Field | Value | Result |
|---|---|---|
| Body length | 600 | Centerline from the origin up to y = 600. |
| UbW, UbO, UbR | 280, 5, 100 | Upper bout line 280 wide at y = 495. Circles of radius 100 centred at x = ±40. |
| WW, WO, WR | 240, 300, 60 | Waist line 240 wide at y = 300. Circles of radius 60 centred at x = ±180. |
| LbW, LbO, LbR | 380, 10, 120 | Lower bout line 380 wide at y = 130. Circles of radius 120 centred at x = ±70. |
| (tail) | with LbO = 10 | Tail arc of radius 370, centred at (0, 370), through the origin to about (±103.6, 14.8) on the lower bout primary circles. |
| HW | 56 | Heel flat from x = −28 to 28 at y = 600, with marks at x = ±28. Shoulder arcs of radius 116.9 run from (±28, 600) to about (±111.0, 565.4) on the upper bout circles. |
| UbO | change to 0 | The shoulder arcs disappear and the heel flat lengthens to run from x = −40 to 40. |
| UbSO | 30 | Upper bout secondary circles of radius 130 centred at x = ±10, with outer edges still at x = ±140. A waist tangent line runs from about (±139.2, 481.0) on each upper secondary circle to (±120.3, 306.5) on the waist circle. |
| LbSO | 40 | Lower bout secondary circles of radius 160 centred at x = ±30, with outer edges still at x = ±190. A waist tangent line runs from about (±161.7, 220.8) on each lower secondary circle to (±130.6, 265.9) on the waist circle. |
| LbSO | change to 70 | Error: the lower bout secondary circle overlaps the waist circle. With these values, the most LbSO can be is about 61.4. Just below that, the lower waist tangent lines become very short. |
| LbSO, LbO | change LbSO back to 40, and LbO to 0 | The tail arc disappears and the tail becomes a straight line from (−70, 0) to (70, 0). |
| TD, ND | 120, 95 (with UbO 5 and LbO 10 again) | The side view appears 50 right of the lower bout's widest point (x = 190): its bottom line runs from (240, 0) to (240, 600), and its top line from (360, 0) to (335, 600). |
| TD | change to 0 | Error: the tail depth must be greater than zero. |
