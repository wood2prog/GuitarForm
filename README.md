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
| Input | Upper Bout Primary Radius (`UbR`) | Number | *Optional.* Primary radius of the upper bout. Must be greater than 0. Its centre sits one radius below the top of the body length line. For now it's used only to place the upper bout line. |
| Input | Waist Width (`WW`) | Number | *Optional.* Width of the body at the waist. Must be greater than 0. |
| Input | Waist Offset (`WO`) | Number | *Optional.* Distance of the waist center line up Y from the tail end (the origin). A warning appears if it's outside 0–L. |
| Input | Lower Bout Width (`LbW`) | Number | *Optional.* Width of the lower bout. Must be greater than 0. |
| Input | Lower Bout Offset (`LbO`) | Number | *Optional.* Offset of the lower bout center line up Y from the lower bout primary radius centre. A warning appears if the line falls outside 0–L. |
| Input | Lower Bout Primary Radius (`LbR`) | Number | *Optional.* Primary radius of the lower bout. Must be greater than 0. Its centre sits one radius above the tail end (the origin). For now it's used only to place the lower bout center line. |
| Output | Construction Lines (`CL`) | Lines (list) | Construction lines for the plate (see below). |

The construction lines are output in this order:

1. **Centerline:** from (0, 0, 0) to (0, L, 0).
2. **Upper bout:** from (−UbW/2, L − UbR − UbO, 0) to (UbW/2, L − UbR − UbO, 0), a horizontal line centred on the Y axis. It's drawn only when Upper Bout Width, Upper Bout Offset and Upper Bout Primary Radius are all connected.
3. **Waist center line:** from (−WW/2, WO, 0) to (WW/2, WO, 0), a horizontal line centred on the Y axis. It's drawn only when both Waist Width and Waist Offset are connected.
4. **Lower bout center line:** from (−LbW/2, LbR + LbO, 0) to (LbW/2, LbR + LbO, 0), a horizontal line centred on the Y axis. It's drawn only when Lower Bout Width, Lower Bout Offset and Lower Bout Primary Radius are all connected.

**Quick test:** put a **Plate** component on the canvas and connect a **Number Slider** to **L**, for example 0–600. A solid red line should appear in the Rhino viewport, running up from the origin. Then connect sliders to **UbW** (e.g. 280), **UbO** (e.g. 20) and **UbR** (e.g. 150). A horizontal red line 280 wide should cross the centerline 170 below its top end (radius 150 + offset 20). Finally connect **WW** (e.g. 240) and **WO** (e.g. 300). A 240-wide line should cross the centerline 300 up from the origin. Then connect **LbW** (e.g. 380), **LbO** (e.g. 10) and **LbR** (e.g. 190). A 380-wide line should cross the centerline 200 up from the origin (radius 190 + offset 10).

## Troubleshooting

- **Component doesn't appear:** check that the `.gha` is in the folder you added (or in `Libraries`) and that you restarted Rhino. In Grasshopper, open **File › Special Folders › Components Folder** to confirm the location.
- **Build fails because the file is in use:** close Rhino, then rebuild.
- **No preview:** make sure the component isn't hidden or disabled, and that **L** has a value greater than 0.
