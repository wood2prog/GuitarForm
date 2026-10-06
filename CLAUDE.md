# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

GuitarForm is a guitar designer implemented as a set of Grasshopper (Rhino) components written in C#. Each component models one part of the guitar: body shape, strings, fretboard, neck, and so on.

## Working agreement

- The user gives the design direction for each component's C# code. Implement what they describe, and ask instead of guessing at guitar geometry or component inputs and outputs that haven't been specified.
- `Spec.txt` holds the original project brief.

## Build

```
dotnet build GuitarForm/GuitarForm.csproj
```

- This is a Rhino 8–only project. It uses the `Grasshopper` NuGet package (8.0.x, `ExcludeAssets="runtime"`), targets only `net7.0-windows` (Rhino 8's default .NET runtime), and outputs `bin/Debug/net7.0-windows/GuitarForm.gha`.
- To load it in Grasshopper, run `GrasshopperDeveloperSettings` in Rhino and add the `bin/Debug/net7.0-windows` folder, or copy the `.gha` into `%APPDATA%\Grasshopper\Libraries`.
- There are no automated tests. Check behaviour in Grasshopper.

## Component conventions

- The guitar is drawn vertically: the origin is the starting point and the body length runs along +Y. Lengths are in the Rhino document's units, with no conversion.
- `SolveInstance` reads inputs by index (`DA.GetData(n, ...)`). The user sometimes reorders inputs in `RegisterInputParams` by hand, so whenever the input order changes, re-check every `GetData` index and the README input table.
- Components live in `GuitarForm/Components/`, use category `GuitarForm`, and each needs its own permanent `ComponentGuid`. Never change an existing GUID, because saved .gh files reference it.
- Geometry is output as plain geometry, so its display style isn't part of it. Each component draws its geometry in `DrawViewportWires` and applies colour and linetype when baking by overriding `BakeGeometry`. Construction geometry (construction lines, outline radii circles) is solid red. Final outline geometry (Outline Lines and Outline Arcs) uses the default Grasshopper preview colour (`args.WireColour`) and bakes with the default attributes. See `PlateComponent` for the pattern.
