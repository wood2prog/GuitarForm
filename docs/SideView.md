# Side View

**GuitarForm › Body.** The side view of the guitar body. It's built the same way up as [Plate](Plate.md), with the tail end at the origin and the body length up +Y, then moved right by the Drawing Offset so it can sit beside the plate.

The guitar is drawn vertically from the origin up +Y, in the Rhino document's units; see [Using the components](../README.md#using-the-components) for that and the preview colours.

| | Name | Type | Description |
|---|---|---|---|
| Input | Body Length (`L`) | Number | Overall length of the guitar body, the same value as Plate's. Must be greater than 0. |
| Input | Drawing Offset (`DO`) | Number | *Optional.* Distance the side view is moved right (+X) from the origin. Defaults to 0, which draws it at the origin. |
| Output | Construction Lines (`CL`) | Lines (list) | Construction lines for the side view (see below). |

The construction lines are output in this order:

1. **Centerline:** from (DO, 0, 0) to (DO, L, 0).

**Quick test:** put a **Side View** component on the canvas and connect **Number Sliders** with these values:

| Input | Value | Result |
|---|---|---|
| L | 500 | A light grey centerline from (0, 0) to (0, 500). |
| DO | 600 | The centerline moves right, from (600, 0) to (600, 500). |
| L | change to 0 | Error: the body length must be greater than zero. |
