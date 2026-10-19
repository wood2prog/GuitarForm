# Improvements

Project-organisation work, in the order we plan to do it. Tick items off as they're done. Items marked **(decision)** need direction from the user before any code is written.

## Where we left off

- The Strings tab is built and committed (f1297f3, 40747d1, cc76023), and the user checked it in Rhino: it looks good. See [docs/Strings.md](docs/Strings.md).
- Plan followed: library editor and picker; "Use for this guitar" stores the StringSetId in `NewFields/<id>.json`; unit weight in lb/in by default, with a remembered toggle; no crash recovery; no tension calculation.
- Known limitations and follow-ups:
  - Deleting a string set can't see designs that use it through `NewFields`, so they show none, with a note.
  - Add/Remove String drops held invalid input in other boxes, and Remove has no confirmation.
  - Collapsed string groups can re-expand under the wrong title after Add/Remove, because state is keyed by title.
  - Imports match a string set by name only.
  - When the panel moves to the new model, move the chosen set from `NewFields` onto library guitars' necks.
- The Frets tab, courses, side dots, colors and layers (see [docs/Plan-FretsAndLayers.md](docs/Plan-FretsAndLayers.md)) are built but uncommitted and not yet checked in Rhino; see `NEXT.md`. The tab is documented in [docs/Frets.md](docs/Frets.md).
- Workflow: the manager session delegates all edits to workers. All new work needs the user's explicit "build it" before implementation starts.

## Decided: plug-in and design database

The decisions behind the plug-in. The work they led to is done and in git history.

- **Grasshopper is dropped.** Keeping track of many wired values, and saving a copy of them, was the problem. In the plug-in every part reads shared values from the one design.
- **SQLite holds everything.** Opening a previous design rebuilds the model from the database.
- **A design owns copies of its sections.** Each area (Instrument, Body, Soundhole, String set, ...) can also be saved as a named library preset. Loading a preset copies its values into the design, so editing a design never changes the library or other designs.
- **Edits change a working copy.** Save, Save as and Revert write it to the library; switching or closing with unsaved changes asks first. `RecoveryStore` keeps unsaved work outside the library, so it can be offered back.
- **Lengths are stored in millimetres** (database, presets, JSON, geometry) **and shown and typed in the Rhino document's units** everywhere in GuitarForm: the tab fields, dialogs, settings and messages. They're converted when drawn, too.
- **Live preview while editing, plus a Build command.** The preview is drawn, not added to the document. Build writes real objects to GuitarForm layers and replaces those from the previous build of that design.
- **One library database, kept locally, with backups to a synced folder.** The main copy is `%LOCALAPPDATA%\GuitarForm\GuitarForm.db` by default, which isn't synced, so OneDrive never sees a half-written file. Backups are written with SQLite's backup API to `Documents\GuitarForm\Backups` by default, which OneDrive syncs. Both locations are set on the Settings page, and kept in Rhino's plug-in settings rather than the database, since they're needed before it's opened.
- **A design can be exported** as a structured JSON document that other programs can read.
- **Round soundholes only** for now; custom shapes are under Planned features.

## 1. Next

- [ ] Name every setting in the user's notes and define it in `docs/Glossary.md` (name, abbreviation, short definition), one at a time with the user. Then delete `variables and settings.txt`. Implementation details that come up along the way:
  - Scale Length (SL): the field offers a list of common scale lengths to pick from, and also takes any value.
  - Compound fretboard radius (FBRN, FBRE) is planned for later; until then the fretboard uses the single Fretboard Radius (FBR).
  - String action (TSA, BSA): beside each value, also show the action measured from the fretboard surface, calculated from the chosen fretwire's height.
  - String Unit Weight (STUW): stored in kg/m; shown and typed in lb/in by default, with a lb/in | kg/m drop-down at the top of the Strings tab, remembered in settings.
  - String core (STCE, STCS): entered by hand when the core material is not specified. The known materials have no built-in values for now; they may get them later.
  - Neck Angle (NKA): typed in by hand, with a button that matches it to the top's angle.
  - Neck profile: the shoulder line and the bottom arc are fixed, and the midshoulder arc is a fillet between them, so its radius is free within a range. Report an error when it's too large (a tangent point runs past the fretboard edge or the bottom of the neck), or when the shoulder line doesn't cross the bottom arc. With a center offset, each side gets its own fillet of the same radius.
  - Heel shaft (HLSR, HLSO): at HLSO = 0 the front of the heel is a straight line through the fronts of the heel end and heel section. Above 0 it's an arc through those two points and the point where that line meets the back of the neck, moved away from the body along the back of the neck by HLSO. The heel shaft radius is tangent to the front of the heel and the back of the neck. The geometry reports an error if the radius's center would pass the Neck Thickness Fret, or it would touch the front of the heel past the heel section.
  - Replacing a guitar's neck, body or bracing with another's warns that the current one will be overwritten (it's gone unless it was saved as a named copy), and can be cancelled. Replacing a body brings its bracing.
  - Deleting a fretwire or string set that necks use lists them and asks for a replacement, which every neck is switched to before the item is deleted. Done for string sets and fretwires (checking saved necks in the library only).
  - Importing a guitar document: its fretwire and string set have no library id. Match each to the library by manufacturer and product code (or set name), or offer to save it there.
  - When the panel moves to the new model, drop the old design tables (designs, instruments, bodies, soundholes) in a new migration; existing designs don't need converting.
  - Fields that don't apply are disabled rather than hidden or checked: e.g. String Hex Core and Core Material for a plain string, and the core modulus and strength unless the core material is not specified. The model ignores their values. Done for the string fields.
- [ ] Work the user's notes (`variables and settings.txt`, untracked, don't commit) into the plan: the sections, their fields, which have their own tables of choices (string sets, fretwire, string action, dot marker layouts), and the order to build them in. String sets are already settled.
- [ ] Move the panel, preview and Build onto the new model and `GuitarForm.Shapes`, with `GuitarForm.Geometry` reduced to turning shapes into Rhino curves; then retire `PlateGeometry`, `SideViewGeometry`, `SoundholeGeometry` and `DesignDrawing`, and rewrite `docs/Body.md` and `docs/Soundhole.md` for the new fields.
- [ ] **(decision)** The bridge's plan settings (its width along the centerline, where the saddle sits in it), so the bridge patch can be drawn.
- [ ] The Florentine cutaway's radius doesn't work (reported by the user while trying the panel). Find out from the user what goes wrong, then fix it in `PlateShape` (the Florentine arc, around `PlateShape.cs` line 360).
- [ ] UI polish: alignment and positioning in the panel. The user will describe or screenshot what looks off.
- [ ] **(decision)** Placing parts with planes. The plate stays fixed at the origin, since it's the starting point. A part that attaches to another (e.g. the neck at the heel) would be built in simple local coordinates, with the origin at its attachment point, then moved onto a plane that the layout works out from the part it attaches to (e.g. a heel plane at (0, L) on the body). The neck then follows the body when L changes, and the plane can carry a neck angle (tilt out of XY). Separate views (like the side view) are placed by the layout. Not decided yet; settle it when designing the section that needs it (probably the neck).

### Slider refinements

`NumberSlider` (a fader: the value is on a lever dragged along a track that springs back to the middle; each step is the field's `Step`, a tenth of it with Shift; click the lever to type) is the box for every decimal field: lengths, angles and numbers. Choices and yes/no values are drop-downs; counts and the dot marker list keep plain boxes.

- [ ] The user tries the slider and says whether it feels natural; adjust the feel (reach per drag, pixels per step, lever and track size).
- [ ] **(decision)** Each field's step. A tenth is right for some values and too much for others. `NumberField.Step` and `NewFields.Field.Step` (default 0.1) are in place; the user will say which fields need another step as they use it. Also decide how steps behave in inches, since they're in the units shown (0.1 in is much coarser than 0.1 mm).
- [ ] Check mouse capture during a drag in Rhino (called by reflection, since the Eto package RhinoCommon 8.0 compiles against lacks it), and that a slider looks disabled when no design is open.

### Limits on values

- [ ] **(decision)** Limits on many fields, so values that don't make sense can't be entered at all rather than only reported as errors. Decide with the user, field by field, the range that makes sense (some come from the glossary definitions' minimum and maximum, others from other values, e.g. a waist narrower than the bouts). Then stop sliders at the limits and refuse typed values outside them. `NumberField.Minimum` and `NumberSlider.Minimum` are in place for this, as the upper and lower bout secondary offsets (which can't go below 0) already use.

## 2. Tech debt (from the code review)

### Quick fixes (done this session; owner: manager)

- [x] Fix `launchSettings.json` escaping.
- [x] Clean up the `NewFields` file on design delete.
- [x] Warn on Save and Build while input errors exist.
- [x] Convert slider steps to document units on the main tabs. Open follow-up: see "Slider refinements" and the Pre-migration group (steps in inches).
- [x] Unsubscribe static Rhino events on panel Dispose.
- [x] Make Open load into locals before assigning.
- [x] Clear stale new-field errors in `ShowDesign`.
- [x] Guard throw sites in `NeckSideShape`, `FretboardShape` and `ShapeCurves`.
- [x] Add missing messages where shapes fail silently.
- [x] Partial drawing: only the failed parts go blank, and the rest draw with a warning.
- [x] Tighten `WithStoredValue` coercion; count overflow throws `FormatException`.
- [x] Backup: clean up `.tmp` files; give the restore safety copy a unique name.
- [x] SQLite `busy_timeout`.
- [x] Map `SqliteException` constraint errors to `InvalidOperationException` (narrowed to name conflicts).
- [x] Document the Bracing and Fretboard tabs in `docs/` and the README tabs table.

### Pre-migration cleanup

- [ ] Delete the legacy Geometry classes used only by tests (`PlateGeometry`, `SideViewGeometry`, `SoundholeGeometry`, `DesignDrawing.Draw`) and their tests.
- [ ] Extract the shared shape helpers (Format, Error/Remark/Warning, Intersect, Join, Rectangle, tolerances) from the shapes.
- [ ] Split `BracingShape` (move the outline offset and point-in-polygon maths out), `PlateShape.AddCutaway`, `NeckSideShape.Build` and `NeckProfilesShape` sampling.
- [x] Tag `GuitarDrawing` output by role and layer, so `DesignBuilder` can use separate layers (`GuitarDrawing.Items` of `DrawnItem`; `DesignBuilder` uses the `ComponentStyles` layer tree).
- [ ] Split the neck heel from the `NeckSideShape` outline: the Heel layer exists, but nothing is tagged Heel yet.
- [ ] Add tests for plug-in code and for `ComponentStyles` (needs Rhino-free code moved out of the .rhp, as above).
- [ ] The T0 deletion of the legacy Geometry classes is still pending, blocked on the owner's action (`git rm` of the listed files). `CLAUDE.md` still names `PlateGeometry`.
- [ ] **(decision)** The `docs/Body.md` side-view Quick Test row disagrees with the Shapes drawing; the owner decides which is right.
- [ ] **(question)** When the strings overlap, the fallback draws the two outer strings as center lines. Should it draw rectangles anyway?
- [ ] **(question)** Cross-checks between the Fretwire Slot Depth (FWSD) and the Tang Height (FWTH) or the fretboard thickness: which, if any, should be errors or warnings?
- [ ] **(question)** The fretwire price's unit or currency, when job costing is built.
- [ ] **(question)** Construction lines print at the outline weight. Confirm that is right.
- [ ] The print colour path (a component's optional print colour to the layer's PlotColor) is untested.
- [ ] Add a library upgrade test (a v1 library with data migrates to v2).
- [ ] Back up the library before a schema migration.
- [ ] Add non-invariant culture tests (de-DE).
- [ ] Add tests for `Segment`, `FretboardSideShape`, `SideViewShape`, `SoundholeShape`, `ShapeCurves`, the Rhino-free parts of `NewFields`, and FK/cascade behaviour.
- [ ] Centralise the csproj settings (`Directory.Build.props`).
- [ ] Extract the duplicated UI helpers (Run, ActionButton, the discard prompt). Partly done: `LibraryItemUi` now holds the library-item helpers shared by the Strings and Frets tabs; `GuitarFormPanel` and `SettingsPage` still have their own.
- [ ] Remove the `UiMetrics` bypasses and dead code (`ScreenScale`, the `Scale` no-op).
- [ ] Pick one spelling (American) across code and docs, including the UI text and CLAUDE.md. Rhino API names such as `DisplayColor` aren't affected.
- [x] Fix the Strings tab's missing unsaved-changes prompt on switch. There is still no prompt on hide or close, by design, since Rhino's shutdown must not be blocked.
- [ ] Make `CopyDesign`, `ImportDesign` and `SaveDesign` atomic with `NewFields` (resolved by the migration).
- [ ] Slider steps converted from mm look odd in inches (0.1 mm = 0.003937 in); snap to a sensible grid in non-mm units.
- [ ] Tests for plug-in code (`NewFields.Delete`, `RequireNoInputErrors`, slider conversion) need Rhino-free code moved out of the .rhp.
- [ ] Add tests for `NeckSideShape` parallel-line/near-parallel guards and the `BracingShape` unsupported segment (currently unreachable via the public API); add `InternalsVisibleTo` or a public seam for `Library.IsNameConflict`.
- [ ] Tidy: a failed plate makes the side view jump horizontally while editing.
- [ ] Only an unset `Body.Length` is treated as unfinished (Remark); other missing plate values rely on the plate's own Remarks.

### Migration (needs product-owner sign-off and a senior-engineer plan first)

- [ ] Move `NewFields` values into the library via `part_values` (fixes backup, restore, move and id reuse, with AUTOINCREMENT or equivalent).
- [ ] Retire `GuitarDesign`, the `Sections.cs` vs `NewFields.Groups` duplication, and same-named legacy/new records.
- [ ] Split `GuitarFormPanel` (a `DesignSession` class, and a `NewFieldBinding` per field replacing the parallel arrays).
- [ ] Recovery ids need a kind prefix once guitars replace designs.
- [ ] Save blank list items (`DotMarker`) so they round-trip.
- [ ] Store the guitar name once (trimmed vs untrimmed IN), and make library item names collision-safe.
- [ ] `SaveGuitar` and parse tolerate one bad `part_values` row.
- [ ] The `NewFields` folder is shared by all library files in the same directory, and design ids are per library, so orphan cleanup was removed. Resolved by moving `NewFields` into the library.

## 3. Planned features

- [ ] **Custom soundhole** (planned). The legacy `CustomSoundholeGeometry` has no `Shapes` replacement, and the plug-in doesn't use it. It needs a `Shapes` version when the product owner specifies the shape; keep the legacy class until then. Still to decide: how the shape is provided (picked from the Rhino document, or imported) and stored (e.g. the curve serialised into the database).
- [ ] GitHub Actions workflow that builds the plug-in on each push.
- [ ] Yak package for distribution through Rhino's Package Manager.
- [ ] Use Relief, Relief Fret and String Action in the drawing (future): today these are information-only fields on the Fretboard tab and don't modify any geometry.
- [ ] Colors tab (future, global: stored in the plug-in settings, not the design): the user picks a display color and an optional print color for each component. The print color maps to Rhino's layer print color (PlotColor). The layer tree (one main grouping layer per component with a sublayer per part, e.g. Guitar > sides, top plate, bracing, heel block) is designed so each sublayer's color can come from this later. The component style table must allow this override.
- [ ] Fretboard binding (future). See [docs/Plan-FretsAndLayers.md](docs/Plan-FretsAndLayers.md).
- [ ] Other ways of measuring course spacing (future), e.g. between course centers, offered as an option next to the current string-to-string measure. See [docs/Plan-FretsAndLayers.md](docs/Plan-FretsAndLayers.md).
- [ ] Job costing (future): use the optional fretwire price (and other part prices) for a cost total per guitar.
- [ ] When a design's library item (fretwire or string set) is missing from the library, offer a pick-a-replacement choice, rather than opening it with nothing selected.

## 4. Open product questions

These need the product owner's direction before any code is written.

- [x] **(decision)** Bracing tab: the bridge patch fields (BPT, BPWO, BPO) and the X-brace profile (XBPT) are in the model but not on the tab. Should they be?
  - Decided: show BPT, BPWO, BPO and XBPT on the Bracing tab. Being added.
- [x] **(decision)** Fretboard tab: Relief, Relief Fret, String Action, Strings per Course, the course spacings and the side dots exist in the model and Glossary but not on the tab. Should they be added, and where?
  - Decided: Relief, Relief Fret and String Action are added as an information-only group. They don't change any geometry yet (see Planned features).
  - Decided: Strings per Course, the course spacings and the side dots will add or modify geometry. Pending a plan from the senior engineer.
- [x] **(decision)** Fretboard tab: where should the fretwire be chosen? There's no tab control for it, so fret slots only appear in tests.
  - Decided: fretwire gets its own Frets tab, and is chosen only there. It is a stock part, saved separately in the library and added to a job from the db. Built: see [docs/Frets.md](docs/Frets.md).
  - Decided: frets are drawn as an orange bead-width rectangle with a thinner center line marking the tang. Strings are drawn green, as rectangles at their diameter.
  - Decided: each component is on its own sublayer under a main grouping layer.
- [x] **(decision)** Is the split right between construction lines (frets, strings) and outline objects (slots, dots)?
  - Decided: superseded by the layer tree. Each component has its own layer and colour (`ComponentStyles`), and each group has a light grey Construction sublayer.

## Review follow-ups (layers, library tabs)

- [ ] `ComponentStyles` completeness (every `DrawingComponent` has a style) needs a test. Plug-in internals aren't visible to the tests: use `InternalsVisibleTo`, or move the table's component list into a testable place.
- [ ] Unsaved Frets and Strings library edits have no crash recovery.
- [ ] `DesignDrawing.Construction` and `DesignDrawing.Outline` recompute on every access: cache them in callers that loop.
