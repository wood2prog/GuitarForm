# Side View

**GuitarForm › Body.** The side view of the guitar body. It's built the same way up as [Plate](Plate.md), with the tail end at the origin and the body length up +Y, then moved right by the Drawing Offset so it can sit beside the plate.

The guitar is drawn vertically from the origin up +Y, in the Rhino document's units; see [Using the components](../README.md#using-the-components) for that and the preview colours.

| | Name | Type | Description |
|---|---|---|---|
| Input | Body Length (`L`) | Number | Overall length of the guitar body, the same value as Plate's. Must be greater than 0. |
| Input | Tail Depth (`TD`) | Number | Depth of the body at the tail end, from the bottom line to the top line. Must be greater than 0. |
| Input | Neck Depth (`ND`) | Number | Depth of the body at the neck end, from the bottom line to the top line. Must be greater than 0. |
| Input | Drawing Offset (`DO`) | Number | *Optional.* Distance the side view is moved right (+X) from the origin. Defaults to 0, which draws it at the origin. |
| Output | Outline Lines (`OL`) | Lines (list) | The side view outline (see below). |

The outline is built at the origin, then moved DO to the right. Before the move, the outline lines are output in this order:

1. **Tail depth line:** from (0, 0, 0) to (TD, 0, 0).
2. **Neck depth line:** from (0, L, 0) to (ND, L, 0).
3. **Bottom line:** from (0, 0, 0) to (0, L, 0).
4. **Top line:** from (TD, 0, 0) to (ND, L, 0).

**Quick test:** put a **Side View** component on the canvas and connect **Number Sliders** with these values:

| Input | Value | Result |
|---|---|---|
| L, TD, ND | 500, 120, 95 | A four-sided outline: the bottom line from (0, 0) to (0, 500), and the top line from (120, 0) to (95, 500). |
| DO | 600 | The outline moves right: the bottom line runs from (600, 0) to (600, 500), and the top line from (720, 0) to (695, 500). |
| L | change to 0 | Error: the body length must be greater than zero. |
| TD | change to 0 | Error: the tail depth must be greater than zero. |
