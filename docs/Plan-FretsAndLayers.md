# Plan: courses, side dots, Frets tab, colors and layers

Status: decisions approved by the product owner. The task plan is from the Senior Engineer and waits for the owner's "build this" and sign-off on the new public Shapes types.

## Decisions (product owner, approved)

1. **Side dots** are drawn in the side view only. They sit at the midpoint between frets, like face dots. A double dot is centered on that midpoint. SDDS is the distance between the two dots along the length; the group stays centered on the midpoint, so a larger SDDS only widens the pair. SDO is measured up from the bottom (underside) of the fretboard to the dot's center, so the dots line up along the bottom edge. A Warning is given if a dot would break through the edge (top). Fields: SDD (diameter), SDO (offset), SDDS (spacing). These already exist in the model and Glossary.
2. **Courses** keep their current behaviour. Strings are listed in set order (strings 1-2 form course 1, and so on). CSN and CSB are the center-to-center distance between the two strings of one course at the nut and at the bridge. Neighbouring strings of adjacent courses have equal gaps. Future note: other builders measure differently (e.g. between course centers); look into offering that as an option.
3. **Fretboard tab layout**: two groups. "Courses" holds SPC, CSN and CSB. The side-dot fields SDD, SDO and SDDS are added to the existing "Dot markers" group. CSN and CSB are disabled (not hidden) unless SPC = 2.
4. **Frets** are drawn orange as a closed rectangle at the fretwire bead width (FWBW), for visual and measuring only. The rectangle is centered on the fret position and runs flush from edge to edge of the fretboard. A center line (same orange, thinner) marks where the tang goes. If no fretwire is chosen, only the center line is drawn, with the Remark "Choose a fretwire on the Frets tab to draw the frets at their width."
   - Fret slots (FWTT wide) stay, on their own sublayer.
   - The side view also shows fret slot lines: at each fret, a line from the top of the board down to the bottom of the slot.
   - New fields: Fretwire Slot Depth (FWSD) on the Fretwire stock part (check `docs/Glossary.md` for clashes first), and Fretboard Slot Overage (FBSO) on the Fretboard tab, the extra depth when the taper is more than the wire's depth covers.
   - Effective slot depth = FWSD + FBSO.
   - The slot bottom is one flat level, that depth below the playing surface at the thickest part of the board (found from the side profile). Each slot line runs from the actual (tapered) top of the board down to that flat bottom.
   - A Warning is given if a slot would cut through the fretboard's underside.
   - Future feature: fretboard binding.
5. **Colors**: frets orange, RGB 255,140,0. Strings green, RGB 0,160,0. Strings are drawn as rectangles at the string's actual diameter, not as center lines. A "show strings" checkbox sits at the top of the Fretboard tab. It is a plug-in setting (global, a user preference, kept between sessions in the plug-in settings), not part of the design. Other components get a clearly visible default palette; the owner will change it later.
6. **Frets tab**: the fretwire is chosen only on the Frets tab ("Use for this guitar"), the same as string sets on the Strings tab. There is no picker on the Fretboard tab.
   - Fretwire fields: FWM (manufacturer; made optional if it is currently required), FWPC, FWBH, FWBW, FWTH, FWTT, FWBT, plus FWSD (slot depth), and a new optional price field (abbreviation to be chosen after checking the Glossary). The price is for future job costing.
   - Importing a design whose named fretwire isn't in the library opens it with no fretwire and a note.
   - Future improvement: when an item that is part of a design (fretwire, string set, etc.) is missing from the library, offer the user a pick-a-replacement choice.
7. **Layers**: root "Guitar". Tree:
   - Body: Top plate, Sides, Soundhole, Bracing, Headblock, Tailblock.
   - Neck: Side view, Heel, Heel cap, Nut, Profiles.
   - Fretboard: Outline, Frets, Fret slots, Face dots, Side dots, Strings.
   - The "heel" is the heel of the neck that attaches to the body. The headblock is the block inside the body that the heel attaches to.
   - One Construction sublayer per group.
   - Design requirement: all component names, layer paths and colors live in ONE table, so adding, renaming or recoloring a component is a one-place change.
   - Note: the neck-side shape now separates the heel from the shaft (`NeckSideShape.Heel` and `Shaft`), split at the end of the heel shaft fillet, square to the back, with the cut line in both.
8. **Preview** uses the component colors too, so it matches Build. The CLAUDE.md convention about the outline previewing in the feedback color is to be updated when the layers are built. The owner approved that CLAUDE.md change; CLAUDE.md is not edited in this task.
9. **Build** removes the old empty `GuitarForm::Outline` and `GuitarForm::Construction` layers, only when they are empty. It never deletes objects.
10. **An existing layer's color is left alone.** A color is set only when Build creates the layer.
11. **Future Colors tab**: colors are global (plug-in settings). An optional print color maps to Rhino's layer print color (PlotColor). Not built now. The style table must allow overriding colors later. `DrawingComponent` enum names are permanent keys: never rename them, only add.

## Task plan (from the Senior Engineer, awaiting 'build this' and API sign-off)

Each task lists its ID, files, dependencies and model.

- **T0**: Delete the legacy Geometry classes used only by tests (`PlateGeometry`, `SideViewGeometry`, `SoundholeGeometry`, `DesignDrawing.Draw`) and their tests. Files: `GuitarForm.Geometry`, `GuitarForm.Tests`. Depends on: none. Model: haiku.
- **T1**: Tag drawing in Shapes. New `GuitarForm.Shapes/DrawnItem.cs` with `DrawingComponent`, `DrawingRole` and `DrawnItem`. `GuitarDrawing.Items` replaces the four lists. Add the Heel and side-dot/slot components. Files: `GuitarForm.Shapes`. Depends on: none. Model: sonnet. **Public API change: needs owner sign-off.**
- **T2**: Carry the tags through Geometry: `DrawnCurve`, `DesignDrawing.Curves`. Files: `GuitarForm.Geometry`. Depends on: T0, T1. Model: sonnet.
- **T3**: `ComponentStyles` table (the one place for names, layer paths and colors), DesignBuilder layer tree, DesignPreview colors, removal of the empty old layers, and wiring of the "show strings" setting. Files: `GuitarForm` (DesignBuilder, DesignPreview). Depends on: T2. Model: sonnet.
- **T4**: Frets at bead width, strings as rectangles at actual diameter, and side-view fret slot lines (FWSD + FBSO, flat bottom). Files: `GuitarForm.Shapes` (`GuitarDrawing.cs`). Depends on: T1. Model: sonnet.
- **T4b**: Model additions: FWSD, FBSO, the optional fretwire price, and FWM made optional, in Model, Glossary and tests. Additive only. Files: `GuitarForm.Model`, `docs/Glossary.md`, tests. Depends on: none. Model: sonnet.
- **T5**: Fretboard tab fields: "Courses" group (SPC, CSN, CSB with the SPC = 2 rule), side-dot fields in "Dot markers", FBSO, and the "show strings" checkbox at the top. Files: `GuitarForm` (`NewFields.cs`, `GuitarFormPanel.cs`). Depends on: T8 (see the sequencing note below). Model: haiku or sonnet.
- **T6a**: Extract the shared library-item UI helpers from `StringsTab`. Files: `GuitarForm`. Depends on: none. Model: sonnet.
- **T6b**: `FretwireEditor`. Files: `GuitarForm`. Depends on: none stated; the FWSD and price fields come from T4b, so this needs confirming. Model: sonnet.
- **T6c**: `FretsTab`. Files: `GuitarForm`. Depends on: T6a, T6b. Model: sonnet.
- **T7**: Fretwire persistence (FretwireId and FretwireName in the `NewFields` JSON), panel wiring, and the Strings-tab discard prompt (see IMPROVEMENTS). Files: `GuitarForm` (`NewFields.cs`, `GuitarFormPanel.cs`). Depends on: T5, T6c. Model: sonnet.
- **T8**: Side dots shape and slot lines in `FretboardSideShape`, with tests and a Quick Test. Files: `GuitarForm.Shapes`, `GuitarForm.Tests`, `docs/`. Depends on: T4. Model: sonnet.
- **T9**: Docs: `docs/Frets.md`, `docs/Fretboard.md`, the README row, the CLAUDE.md layer and color convention update, and IMPROVEMENTS and NEXT. Files: `docs/`, `README.md`, `CLAUDE.md`, `IMPROVEMENTS.md`, `NEXT.md`. Depends on: all. Model: haiku. Runs last.

**Waves**

- Wave 1: T0, T1, T4b, T6a, T6b.
- Wave 2: T2, T4, T6c, T5.
- Wave 3: T3, T7, T8.
- Wave 4: T9.

**Sequencing and review**

- Shared hot files must be sequenced, not run in parallel:
  - `GuitarForm.Shapes/GuitarDrawing.cs`: T1, T4, T8.
  - `GuitarForm/NewFields.cs` and `GuitarForm/GuitarFormPanel.cs`: T5, T7.
- Independent review after T1/T2, after T3, and after T7.

**Open issues to settle before "build this"**

- T5 depends on T8, but the wave table puts T5 in wave 2 and T8 in wave 3. T5 must move to wave 3 or later, after T8.
- T6b's dependency on T4b (for FWSD and the price field) needs confirming.
- The Decisions made line in `NEXT.md` ("Frets are drawn orange, with a center line at the actual fret width") is superseded by decision 4 above.
- API sign-off is needed for the new public Shapes types: `DrawnItem`, `DrawingComponent` and `DrawingRole` (T1).
