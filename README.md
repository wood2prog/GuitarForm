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

Construction lines are previewed as **solid red lines**. When you bake them, they stay red with the Continuous (solid) linetype.

### Plate (GuitarForm › Body)

| | Name | Type | Description |
|---|---|---|---|
| Input | Body Length (`L`) | Number | Overall length of the guitar body. Must be greater than 0. |
| Input | Upper Bout Width (`UbW`) | Number | *Optional.* Width of the upper bout. Must be greater than 0. |
| Input | Upper Bout Offset (`UbO`) | Number | *Optional.* Offset of the upper bout line down Y from the upper bout primary radius centre. A warning appears if the line falls outside 0–L. |
| Input | Upper Bout Primary Radius (`UbR`) | Number | *Optional.* Primary radius of the upper bout. Must be greater than 0. A warning appears if it's more than half of UbW. |
| Input | Waist Radius (`WR`) | Number | *Optional.* Radius of the waist curve. Must be greater than 0. |
| Input | Waist Width (`WW`) | Number | *Optional.* Width of the body at the waist. Must be greater than 0. |
| Input | Waist Offset (`WO`) | Number | *Optional.* Distance of the waist center line up Y from the tail end (the origin). A warning appears if it's outside 0–L. |
| Input | Lower Bout Width (`LbW`) | Number | *Optional.* Width of the lower bout. Must be greater than 0. |
| Input | Lower Bout Offset (`LbO`) | Number | *Optional.* Offset of the lower bout center line up Y from the lower bout primary radius centre. A warning appears if the line falls outside 0–L. |
| Input | Lower Bout Primary Radius (`LbR`) | Number | *Optional.* Primary radius of the lower bout. Must be greater than 0. A warning appears if it's more than half of LbW. |
| Output | Construction Lines (`CL`) | Lines (list) | Construction lines for the plate (see below). |
| Output | Outline Radii (`OR`) | Circles (list) | Circles for the body outline radii (see below). |

The construction lines are output in this order:

1. **Centerline:** from (0, 0, 0) to (0, L, 0).
2. **Upper bout:** from (−UbW/2, L − UbR − UbO, 0) to (UbW/2, L − UbR − UbO, 0), a horizontal line centred on the Y axis. It's drawn only when Upper Bout Width, Upper Bout Offset and Upper Bout Primary Radius are all connected.
3. **Waist center line:** from (−WW/2, WO, 0) to (WW/2, WO, 0), a horizontal line centred on the Y axis. It's drawn only when both Waist Width and Waist Offset are connected.
4. **Lower bout center line:** from (−LbW/2, LbR + LbO, 0) to (LbW/2, LbR + LbO, 0), a horizontal line centred on the Y axis. It's drawn only when Lower Bout Width, Lower Bout Offset and Lower Bout Primary Radius are all connected.

The outline radii are drawn as full circles, a mirrored pair for each region. They're output in this order, right side (+X) then left side (−X) for each pair:

1. **Upper bout pair:** radius UbR. Each centre is on the upper bout line, one radius **inside** the width, at (±(UbW/2 − UbR), L − UbR − UbO). Each circle touches the end of the upper bout line. The pair is drawn whenever the upper bout line is drawn.
2. **Waist pair:** radius WR. Each centre is on the waist center line, one radius **outside** the width, at (±(WW/2 + WR), WO). Each circle touches the end of the waist line from outside the body. The pair is drawn when the waist line is drawn and Waist Radius is connected.
3. **Lower bout pair:** radius LbR. Each centre is on the lower bout center line, one radius **inside** the width, at (±(LbW/2 − LbR), LbR + LbO). Each circle touches the end of the lower bout line. The pair is drawn whenever the lower bout line is drawn.

Lines and circles are both previewed and baked in solid red.

**Quick test:** put a **Plate** component on the canvas and connect **Number Sliders** with these values:

| Input | Value | Result |
|---|---|---|
| L | 600 | Centerline from the origin up to y = 600. |
| UbW, UbO, UbR | 280, 20, 100 | Upper bout line 280 wide at y = 480. Circles of radius 100 centred at x = ±40. |
| WW, WO, WR | 240, 300, 60 | Waist line 240 wide at y = 300. Circles of radius 60 centred at x = ±180. |
| LbW, LbO, LbR | 380, 10, 120 | Lower bout line 380 wide at y = 130. Circles of radius 120 centred at x = ±70. |

## Troubleshooting

- **Component doesn't appear:** check that the `.gha` is in the folder you added (or in `Libraries`) and that you restarted Rhino. In Grasshopper, open **File › Special Folders › Components Folder** to confirm the location.
- **Build fails because the file is in use:** close Rhino, then rebuild.
- **No preview:** make sure the component isn't hidden or disabled, and that **L** has a value greater than 0.
