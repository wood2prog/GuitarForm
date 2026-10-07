# Soundhole components

**GuitarForm › Soundhole.** Two components place the soundhole: **Soundhole** for a round hole and **Custom Soundhole** for any closed shape. Both take the same offsets: Horizontal Offset across from the body centreline and Tail Offset up from the tail end.

The guitar is drawn vertically from the origin up +Y, in the Rhino document's units; see [Using the components](../README.md#using-the-components) for that and the preview colours.

## Soundhole

A round soundhole. The circle is the final outline, so it uses the default preview colour.

| | Name | Type | Description |
|---|---|---|---|
| Input | Diameter (`D`) | Number | Diameter of the soundhole. Must be greater than 0. |
| Input | Horizontal Offset (`HO`) | Number | *Optional.* Distance of the soundhole centre from the body centreline along X (+X is right). Defaults to 0, which centres the hole. |
| Input | Tail Offset (`TO`) | Number | Distance of the soundhole centre up Y from the tail end (the origin). |
| Output | Circle (`C`) | Circle | The soundhole outline: radius D/2, centred at (HO, TO, 0). |
| Output | Center (`Pt`) | Point | Centre of the soundhole, (HO, TO, 0). |

**Quick test:** put a **Soundhole** component on the canvas and connect **Number Sliders** with these values:

| Input | Value | Result |
|---|---|---|
| D, TO | 100, 420 | Circle of radius 50 centred at (0, 420). |
| HO | −20 | The circle moves left, centred at (−20, 420). |
| D | change to 0 | Error: the diameter must be greater than zero. |

## Custom Soundhole

A soundhole of any shape. Draw the shape anywhere in Rhino (or build it in Grasshopper). The component moves it so its **area centroid** sits at the offsets. Only the shape's form is used, not where it was drawn. The curve is the final outline, so it uses the default preview colour.

| | Name | Type | Description |
|---|---|---|---|
| Input | Shape (`S`) | Curve | The soundhole shape. It must be closed and planar, in a plane parallel to the XY plane (a shape drawn above or below XY is moved onto it). |
| Input | Horizontal Offset (`HO`) | Number | *Optional.* Distance of the shape's area centroid from the body centreline along X (+X is right). Defaults to 0, which centres the hole. |
| Input | Tail Offset (`TO`) | Number | Distance of the shape's area centroid up Y from the tail end (the origin). |
| Output | Curve (`C`) | Curve | The soundhole outline, moved so its area centroid is at (HO, TO, 0). The input curve isn't changed. |
| Output | Center (`Pt`) | Point | Area centroid of the soundhole, (HO, TO, 0). |

The component fails with an error and outputs nothing if the shape is open, isn't planar, or isn't parallel to the XY plane.

**Quick test:** draw a closed **Polyline** in Rhino's Top view through (100, 50), (160, 50), (100, 110) and back to (100, 50). Set it in a **Curve** parameter, wire it to **S**, and connect **Number Sliders**:

| Input | Value | Result |
|---|---|---|
| TO | 420 | The triangle's area centroid, (120, 70) as drawn, moves to (0, 420). Its right-angle corner is at (−20, 400). |
| HO | −20 | The triangle moves left: centroid at (−20, 420), right-angle corner at (−40, 400). |
| S | a circle drawn in the Front view | Error: the shape isn't parallel to the XY plane. |
