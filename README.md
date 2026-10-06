# GuitarForm

A guitar designer built as Grasshopper components for **Rhino 8**. Each component draws one part of the guitar, such as the body plate, neck, fretboard or strings.

## Requirements

- Rhino 8 (Windows), running on its default .NET runtime. The plug-in won't load if Rhino has been switched to .NET Framework with `SetDotNetRuntime`.
- .NET SDK 8 or later, to build.

## Build

From the repository folder:

```
dotnet build GuitarForm/GuitarForm.csproj
```

The plug-in is written to:

```
GuitarForm/bin/Debug/net7.0-windows/GuitarForm.gha
```

## Load in Grasshopper

**Option A: point Grasshopper at the build folder (best while developing)**

1. In Rhino, run the command `GrasshopperDeveloperSettings`.
2. Add the full path of `GuitarForm\bin\Debug\net7.0-windows`.
3. Make sure **Memory load *.GHA assemblies using COFF byte arrays** is unchecked.
4. Restart Rhino and open Grasshopper.

After this, each rebuild is picked up the next time you start Rhino.

**Option B: copy the plug-in**

1. Copy `GuitarForm.gha` into `%APPDATA%\Grasshopper\Libraries`.
2. If Windows marks the file as blocked, right-click it, open **Properties** and tick **Unblock**.
3. Restart Rhino and open Grasshopper.

Grasshopper locks the `.gha` while Rhino is running, so close Rhino before you rebuild.

## Using the components

The components are on the **GuitarForm** tab of the Grasshopper toolbar.

The guitar is drawn **vertically**: it starts at the origin and the body length runs up the **+Y** axis. All lengths are in the Rhino document's units.

Construction geometry (construction lines and outline radii) is previewed **solid red**, and bakes red with the Continuous (solid) linetype. The **final outline** (heel flat and shoulder arcs) uses the default Grasshopper preview colour, and bakes with the default attributes so it takes its layer's colour.

### Plate (GuitarForm › Body)

| | Name | Type | Description |
|---|---|---|---|
| Input | Body Length (`L`) | Number | Overall length of the guitar body. Must be greater than 0. |
| Input | Upper Bout Width (`UbW`) | Number | *Optional.* Width of the upper bout. Must be greater than 0. |
| Input | Upper Bout Offset (`UbO`) | Number | *Optional.* Offset of the upper bout line down Y from the upper bout primary radius centre. A warning appears if the line falls outside 0–L. |
| Input | Upper Bout Primary Radius (`UbR`) | Number | *Optional.* Primary radius of the upper bout. Must be greater than 0. A warning appears if it's more than half of UbW. |
| Input | Upper Bout Secondary Offset (`UbSO`) | Number | *Optional.* Moves the upper bout secondary radius centre toward the body centre and grows its radius by the same amount. 0 = same as the primary radius. Can't be negative. |
| Input | Waist Radius (`WR`) | Number | *Optional.* Radius of the waist curve. Must be greater than 0. |
| Input | Waist Width (`WW`) | Number | *Optional.* Width of the body at the waist. Must be greater than 0. |
| Input | Waist Offset (`WO`) | Number | *Optional.* Distance of the waist center line up Y from the tail end (the origin). A warning appears if it's outside 0–L. |
| Input | Lower Bout Width (`LbW`) | Number | *Optional.* Width of the lower bout. Must be greater than 0. |
| Input | Lower Bout Offset (`LbO`) | Number | *Optional.* Offset of the lower bout center line up Y from the lower bout primary radius centre. A warning appears if the line falls outside 0–L. |
| Input | Lower Bout Primary Radius (`LbR`) | Number | *Optional.* Primary radius of the lower bout. Must be greater than 0. A warning appears if it's more than half of LbW. |
| Input | Lower Bout Secondary Offset (`LbSO`) | Number | *Optional.* Same as UbSO, for the lower bout. |
| Input | Heel Width (`HW`) | Number | *Optional.* Width of the flat at the top of the body where the heel of the neck attaches. Must be greater than 0, and no more than the distance between the upper bout circle centres. |
| Output | Construction Lines (`CL`) | Lines (list) | Construction lines for the plate (see below). |
| Output | Outline Radii (`OR`) | Circles (list) | Circles for the body outline radii (see below). |
| Output | Outline Arcs (`OA`) | Arcs (list) | Shoulder arcs from the heel flat to the upper bout radii (see below). |
| Output | Outline Lines (`OL`) | Lines (list) | Straight parts of the final outline. Currently the heel flat (see below). |

The construction lines are output in this order:

1. **Centerline:** from (0, 0, 0) to (0, L, 0).
2. **Upper bout:** from (−UbW/2, L − UbR − UbO, 0) to (UbW/2, L − UbR − UbO, 0), a horizontal line centred on the Y axis. It's drawn only when Upper Bout Width, Upper Bout Offset and Upper Bout Primary Radius are all connected.
3. **Waist center line:** from (−WW/2, WO, 0) to (WW/2, WO, 0), a horizontal line centred on the Y axis. It's drawn only when both Waist Width and Waist Offset are connected.
4. **Lower bout center line:** from (−LbW/2, LbR + LbO, 0) to (LbW/2, LbR + LbO, 0), a horizontal line centred on the Y axis. It's drawn only when Lower Bout Width, Lower Bout Offset and Lower Bout Primary Radius are all connected.
5. **Heel width marks:** two vertical lines 0.25 long (in document units) at x = ±HW/2, centred on y = L. Right mark first, then left.

The heel flat and marks are drawn when Heel Width is connected. The component fails with an error and outputs nothing if the upper bout circle centres are closer together than the heel width. It also fails if Upper Bout Offset is negative, which would put the circles above the top of the body, or if the shoulder arc would meet the circle below its centre (see below).

**Heel flat** (on Outline Lines): a horizontal line at the top of the body (y = L), from −HW/2 to HW/2. It's lengthened only when Upper Bout Offset is 0, which means the upper bout circle tops are level with the top of the body. It then runs from −(UbW/2 − UbR) to (UbW/2 − UbR), directly above the two upper bout circle centres.

When Upper Bout Offset is more than 0, the upper bout circle tops sit below the top of the body. The heel flat then stays HW long, and a **shoulder arc** joins each end of the flat to its upper bout circle. The arcs are output on **Outline Arcs**, right side then left side:

- The arc starts at (±HW/2, L), heading straight outward, so it joins the flat smoothly (G1). Its centre is directly below that point.
- It touches the upper bout circle from outside, wrapping around it.
- Its radius is (dx² + t² − UbR²) / (2·UbO), where dx = UbW/2 − UbR − HW/2 and t = UbR + UbO.
- The component fails with an error and outputs nothing if dx is less than UbO. The arc would then meet the circle below its centre and swing wider than the upper bout width.

The outline radii are drawn as full circles, a mirrored pair for each region. They're output in this order, right side (+X) then left side (−X) for each pair:

1. **Upper bout primary pair:** radius UbR. Each centre is on the upper bout line, one radius **inside** the width, at (±(UbW/2 − UbR), L − UbR − UbO). Each circle touches the end of the upper bout line. The pair is drawn whenever the upper bout line is drawn.
2. **Upper bout secondary pair:** radius UbR + UbSO. Each centre is on the upper bout line, UbSO closer to the body centre than the primary centre, at (±(UbW/2 − UbR − UbSO), L − UbR − UbO). Its outer edge stays locked to the primary circle's at ±UbW/2. At UbSO = 0 it's the same as the primary circle. The pair is drawn when the upper bout line is drawn and Upper Bout Secondary Offset is connected.
3. **Waist pair:** radius WR. Each centre is on the waist center line, one radius **outside** the width, at (±(WW/2 + WR), WO). Each circle touches the end of the waist line from outside the body. The pair is drawn when the waist line is drawn and Waist Radius is connected.
4. **Lower bout primary pair:** radius LbR. Each centre is on the lower bout center line, one radius **inside** the width, at (±(LbW/2 − LbR), LbR + LbO). Each circle touches the end of the lower bout line. The pair is drawn whenever the lower bout line is drawn.
5. **Lower bout secondary pair:** radius LbR + LbSO. Each centre is on the lower bout center line, LbSO closer to the body centre than the primary centre, at (±(LbW/2 − LbR − LbSO), LbR + LbO). Its outer edge stays locked to the primary circle's at ±LbW/2. The pair is drawn when the lower bout line is drawn and Lower Bout Secondary Offset is connected.

A secondary offset can't be negative. A secondary circle also can't overlap the waist circle on the same side, because the bout curve has to meet the waist curve tangentially. They may touch, but if they overlap, the component fails with an error and outputs nothing.

The outline radii circles are construction geometry and are drawn red.

**Quick test:** put a **Plate** component on the canvas and connect **Number Sliders** with these values:

| Input | Value | Result |
|---|---|---|
| L | 600 | Centerline from the origin up to y = 600. |
| UbW, UbO, UbR | 280, 5, 100 | Upper bout line 280 wide at y = 495. Circles of radius 100 centred at x = ±40. |
| WW, WO, WR | 240, 300, 60 | Waist line 240 wide at y = 300. Circles of radius 60 centred at x = ±180. |
| LbW, LbO, LbR | 380, 10, 120 | Lower bout line 380 wide at y = 130. Circles of radius 120 centred at x = ±70. |
| HW | 56 | Heel flat from x = −28 to 28 at y = 600, with marks at x = ±28. Shoulder arcs of radius 116.9 run from (±28, 600) to about (±111.0, 565.4) on the upper bout circles. |
| UbO | change to 0 | The shoulder arcs disappear and the heel flat lengthens to run from x = −40 to 40. |
| UbSO | 30 | Upper bout secondary circles of radius 130 centred at x = ±10, with outer edges still at x = ±140. |
| LbSO | 40 | Lower bout secondary circles of radius 160 centred at x = ±30, with outer edges still at x = ±190. |
| LbSO | change to 70 | Error: the lower bout secondary circle overlaps the waist circle. With these values, the most LbSO can be is about 61.4. |

## Troubleshooting

- **Component doesn't appear:** check that the `.gha` is in the folder you added (or in `Libraries`) and that you restarted Rhino. In Grasshopper, open **File › Special Folders › Components Folder** to confirm the location.
- **Build fails because the file is in use:** close Rhino, then rebuild.
- **No preview:** make sure the component isn't hidden or disabled, and that **L** has a value greater than 0.
