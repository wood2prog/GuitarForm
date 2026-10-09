# Soundhole tab

A round soundhole. Tick **This design has a soundhole** to add one; untick it to remove it (ticking it again brings the values back while the design is open). The circle is part of the final outline.

The guitar is drawn vertically from the origin up +Y. Lengths are shown and typed in the Rhino document's units and stored in millimetres; see [Units](../README.md#units).

| Field | Description |
|---|---|
| Diameter | *Required.* Diameter of the soundhole. Must be greater than 0. |
| Horizontal offset | *Optional.* Distance of the soundhole centre from the body centreline along X (+X is right). Blank centres the hole. |
| Tail offset | *Required.* Distance of the soundhole centre up Y from the tail end (the origin). |

The soundhole is a circle of radius Diameter / 2, centred at (Horizontal offset, Tail offset).

**Quick test:** in a document in millimetres, tick the soundhole box and type these values:

| Field | Value | Result |
|---|---|---|
| Diameter, Tail offset | 100, 420 | Circle of radius 50 centred at (0, 420). |
| Horizontal offset | −20 | The circle moves left, centred at (−20, 420). |
| Diameter | change to 0 | Error: the diameter must be greater than zero. |

## Custom shapes (not in the panel yet)

Custom-shaped soundholes are planned for later (see `IMPROVEMENTS.md`). Their geometry, `CustomSoundholeGeometry`, is already written and tested:

- The shape is any closed, planar curve in a plane parallel to the XY plane (a shape above or below XY is moved onto it). Where it was drawn doesn't matter; only its form is used.
- It's moved so its **area centroid** sits at (Horizontal offset, Tail offset).
- It fails with an error if the shape is open, isn't planar, or isn't parallel to the XY plane.

Its tests use a triangle through (100, 50), (160, 50) and (100, 110): with Tail offset 420, its centroid, (120, 70) as drawn, moves to (0, 420) and its right-angle corner to (−20, 400). With Horizontal offset −20 as well, the centroid is at (−20, 420) and the corner at (−40, 400). A circle standing upright, in the YZ plane, is rejected.
