# Soundhole tab

A round soundhole. Tick **This design has a soundhole** to add one. Unticking it changes only the Soundhole (SH) setting: the soundhole's fields stay filled but are grayed out, nothing is drawn or built for it, and ticking it again brings the soundhole back as it was. The circle is part of the final outline.

The guitar is drawn vertically from the origin up +Y. Lengths are shown and typed in the Rhino document's units and stored in millimeters; see [Units](../README.md#units).

| Field | Description |
|---|---|
| Soundhole Diameter (SHD) | *Required when SH is Yes.* Diameter of the soundhole. Must be greater than 0. |
| Soundhole Offset from Centerline (SHOC) | *Optional.* Distance of the soundhole center from the body centerline along X (+X is right). Blank centers the hole. |
| Soundhole Offset from Tail End (SHOT) | *Required when SH is Yes.* Distance of the soundhole center up Y from the tail end (the origin). |

The soundhole is a circle of radius Diameter / 2, centerd at (Horizontal offset, Tail offset).

**Quick test:** in a document in millimeters, tick the soundhole box and type these values. They change the working copy until you press **Save**:

| Field | Value | Result |
|---|---|---|
| Diameter, Tail offset | 100, 420 | Circle of radius 50 centerd at (0, 420). |
| Horizontal offset | −20 | The circle moves left, centerd at (−20, 420). |
| Diameter | change to 0 | Error: Soundhole Diameter (SHD) must be greater than 0 mm. |

## Custom shapes (not in the panel yet)

Custom-shaped soundholes are planned for later (see `IMPROVEMENTS.md`). Their geometry, `CustomSoundholeGeometry`, is already written and tested:

- The shape is any closed, planar curve in a plane parallel to the XY plane (a shape above or below XY is moved onto it). Where it was drawn doesn't matter; only its form is used.
- It's moved so its **area centroid** sits at (Horizontal offset, Tail offset).
- It fails with an error if the shape is open, isn't planar, or isn't parallel to the XY plane.

Its tests use a triangle through (100, 50), (160, 50) and (100, 110): with Tail offset 420, its centroid, (120, 70) as drawn, moves to (0, 420) and its right-angle corner to (−20, 400). With Horizontal offset −20 as well, the centroid is at (−20, 420) and the corner at (−40, 400). A circle standing upright, in the YZ plane, is rejected.
