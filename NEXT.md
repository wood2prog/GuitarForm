# Next

Running record of where the project stands. Update it at the end of each session.

## State

- Branch: `main`. This session's work is committed.
- Build: 0 warnings. Tests: 673 passing (405 before wave A).
- An independent review of this batch was done and its findings were fixed (T10). 
- A code review of the whole codebase was done; the quick fixes from it are in.
- Bracing, Fretboard and Frets docs are written.
- `IMPROVEMENTS.md` is cleaned up and holds the tech-debt checklist, with open product questions in section 4.

## Done this session

- Reviewed the whole codebase and turned the findings into the quick-fix list in `IMPROVEMENTS.md`.
- Fixed the quick fixes: `launchSettings.json` escaping, `NewFields` cleanup on delete, warn/block on input errors, slider step conversion, static event unsubscribe, Open loading into locals, stale-error clearing, throw-site guards, messages for silent shape failures, partial drawing, stricter coercion, backup and restore cleanup, SQLite `busy_timeout`, and constraint mapping.
- Wrote `docs/Bracing.md` and `docs/Fretboard.md`.
- Cleaned up `IMPROVEMENTS.md`: ticked the finished quick fixes and added the new open items.
- Built the plan in `docs/Plan-FretsAndLayers.md`:
  - Fretboard and Bracing tab fields, including the Courses group (SPC, CSN, CSB), side dots and fret slot lines in the side view.
  - The Frets tab (`FretsTab`, `FretwireEditor`, shared `LibraryItemUi`) and fretwire persistence with the design, its export and import.
  - The `Guitar` layer tree with `ComponentStyles` (names, paths and colors), colors ByLayer, and the preview in the same colors.
  - Bead-width frets with a thinner center line, strings as rectangles at their diameter, and the "Show strings" plug-in setting.
  - Model fields FWSD, FWPR and FBSO (FWM is now optional).
- Wrote `docs/Frets.md` and updated `docs/Fretboard.md`, the README, `CLAUDE.md` and `IMPROVEMENTS.md`.

## Pre-migration cleanup progress

- Wave A is done and committed: golden test (en-US and de-DE), Segment/side view/soundhole/ShapeCurves/FK-cascade tests, library v1 upgrade test, culture tests. It also found and fixed the `Vector2.ToString` infinite recursion.
- Wave B is done: shared shape helpers (`ShapeSupport`, `ShapeMessages`, `PolygonMath`), the splits of `BracingShape`, `PlateShape.AddCutaway`, `NeckSideShape` and `NeckProfilesShape` sampling, and `InternalsVisibleTo` for the tests. An independent review passed.
- Remaining waves:
  - C is done: backup before migration (T8), `ComponentStyles` moved to Geometry with `ComponentStylesTests` (T10), and `DesignDrawing` caching and the `UiMetrics` tidy (T11).
  - D: `Directory.Build.props` is done (T9: LangVersion and NETSDK1138 centralized; output and 596 tests unchanged). American spelling is done (T12); wave D and the pre-migration cleanup are complete.
- Owner decisions: the upgrade backup goes next to the library (e.g. `GuitarForm.before-v1.bak`), and a failed copy blocks opening. `Directory.Build.props`, the `ComponentStyles` move and Shapes `InternalsVisibleTo` are approved. UI helper duplicates are deferred.
- Follow-ups found by the tests are in `IMPROVEMENTS.md` ("Follow-ups from the test work").

## Rhino checks

The owner confirmed the Rhino checks all passed.

(Old checklist, kept for reference.)

- Type junk such as `abc` into a field, then try Save, Save as, Build, Export and switching design. Each should refuse with a message, and work again once the field is fixed. WORKS
- Change the document units mm to inches and back. Confirm length slider steps follow, while angle sliders don't. WORKS
- Delete a saved design and confirm its `NewFields\<id>.json` goes. NOT SURE WHERE THIS IS
- Open a design with corrupt `NewFields` JSON. The previous design should stay open, and the drop-down should snap back. NOT SURE HOW TO TEST THIS
- Build a design with an error. A Yes/No prompt should appear, and No should build nothing. WORKS
- Clear a neck-profile field with a placeholder and arrow-step it. It should start from the placeholder. WORKS IF I AM UNDERSTANDING
- Close and reopen the panel. The preview should hide, and unit changes shouldn't throw. WORKS
- Drag a slider off the control. The drag should end cleanly. WORKS
- A design with a failed plate should still preview the other parts, with a warning. YES
- An unfinished design (no body length) should show a Remark, not an Error, and no Build prompt. YES
- F5 / `launchSettings` starts Rhino. DIDN'T TEST
- The last build was blocked by a running Rhino locking the `.rhp`. Close Rhino and rebuild to confirm 0 warnings after the Bracing and Fretboard field additions.

- Rhino check for the `ComponentStyles` move: Build still creates the `Guitar` layer tree with the same colors, and a layer recolored by hand keeps its color after building again.

### New this round

Frets tab (see `docs/Frets.md`):

- Create, edit, Save, Save as, Revert and Delete a fretwire. Delete one that a saved neck uses, and pick the replacement.
- Save is refused with no Product Code, and while a field holds junk text.
- **Use for this guitar** and **Use no fretwire**: the design shows Unsaved changes. Frets then draw as beads, with slots, after Use.
- Change the document units mm to inches and back: the fields follow.
- The fretwire carries through Save, reopen, Save as (a design copy), Export and Import. A deleted fretwire shows the note "isn't in the library" and the design opens with none. An imported name that isn't in the library does the same.
- The Strings tab behaves exactly as before after the refactor, including the new prompt when switching with unsaved changes.

Fretboard tab:

- Courses: SPC 1 grays CSN and CSB. SPC 2 enables them and 12 strings draw.
- Side dots and fret slot lines draw in the side view. Check the warnings: a side dot breaking through the top, and a slot that cuts through or is cut off at thin frets.
- **Show strings** hides and shows the strings in the preview and in Build, and persists across a Rhino restart.

Layers and colors:

- Build makes the layer tree with sublayers and a Construction sublayer per group. Objects are colored ByLayer.
- Rebuilding replaces only that design's objects. Two designs coexist. One Undo removes a build.
- A layer you recolor keeps its color after a rebuild.
- The old `GuitarForm::Outline` and `GuitarForm::Construction` layers are removed when empty, and kept when they have objects.
- Centerlines are thinner. The preview colors match the built colors.

Unsaved library edits, prompts and builds:

- Edit a string set or fretwire without saving, then switch design, New or Import. There should be no Strings or Frets prompt, and the edit should still be there on return.
- Change the library in Settings with a dirty design and dirty tabs. It should ask about the design, then each dirty tab. "No" on a tab shows the saved item with no repeat prompt. Cancel aborts.
- Build twice. Only that design's objects are replaced, and one Undo reverts the whole build.
- Old empty `GuitarForm` layers (and the parent) are removed in the same build. A layer with an object, the current layer, or one used by a block stays.
- **Show strings** redraws the preview, persists across a Rhino restart, and doesn't reset the backup timer.
- A fretboard slot deeper than the board's thickness shows the cut-through Warning and draws no slot lines.
- Heel layer now has objects: Build puts the shaft on `Guitar::Neck::Side view` and the heel on `Guitar::Neck::Heel`. They share the seam (the back of the neck carried on to the joint line), both ending flush on the joint line, and the Heel layer's color shows in the preview.

## Decisions made

- T0 is done: `PlateGeometry`, `SideViewGeometry`, `SoundholeGeometry`, `DesignDrawing.Draw` and their tests are deleted. `CustomSoundholeGeometry` stays.
- Body.md side view is resolved: the top line is straight and the back line slopes by TD - ND.
- Partial drawing: only the failed parts go blank. The rest draws with a warning.
- Custom soundhole is a planned feature. Keep the legacy `CustomSoundholeGeometry` until then.
- `NEXT.md` is the running state record. Update it at the end of each session.
- The Bracing tab shows BPT, BPWO, BPO and XBPT.
- Relief, Relief Fret and String Action are information-only. They don't change geometry yet.
- Fretwire gets its own Frets tab and is a stock part, saved separately in the library.
- Frets are drawn as an orange bead-width rectangle with a thinner center line marking the tang. Strings are drawn green, as rectangles at their diameter.
- Side dots sit at the same place as face dots, up from the underside by SDO. Slot lines run from the playing surface to one flat bottom, the slot depth (FWSD + FBSO) below the thickest part, where thickest = max(FBTN, FBTE). Both give Warnings when they would break through or cut through.
- Each component gets its own sublayer under a main grouping layer.
- A Colors tab (display and print color per component) is a future improvement.
- Fret/courses/side dots/layers/colors decisions are recorded in `docs/Plan-FretsAndLayers.md`.
- Unsaved Strings/Frets library edits stay in memory across design switches. Only a library change (Settings switch/restore) prompts for them. There is no prompt on hide/close, so Rhino shutdown is never blocked.
- Build creates all layers before deleting a design's old objects.

## Next up

1. The NewFields migration is complete (stages 1 to 4, including R1/R2 below). Next: tests for the untested shapes and for plug-in code, and the Rhino checks listed under P2, P4 and P5.
2. A later migration (v5) can drop the old tables (`designs`, `instruments`, `bodies`, `soundholes`) once the owner is happy with the converted libraries. `LegacyDesignMigration` reads them with raw SQL, so its tests must stay green until then.

### NewFields → library migration (complete)

The senior-engineer plan is approved. It runs in four stages:

1. Model and Data foundations.
2. `DesignSession`, recovery keys and the build key, not wired yet.
3. Conversion migration v4, the panel switch, the field table, and presets as named copies.
4. Retire the old model.

**Stage 1 is done:**

- Schema v3: `parts.uid` and `parts.legacy_design_id` columns; the guitar name is stored once in `parts.name`; list-item marker rows `#<ListName>`.
- Tolerant `part_values` loading.
- `CopyGuitar` and `UnusedGuitarName`.
- `PartWriter`.
- `LegacyNewFields`, `DotMarkerText` and `DisplayName`.
- `GuitarDocument.Read`.
- Tests: 625 passing.

**Stage 2 done** (DesignSession in GuitarForm.Data with import/export/recovery/legacy recovery, RecoveryStore string keys `guitar-<uid>`, DesignBuilder buildKey + alsoReplace; not wired into the panel yet). Tests 673. Independent review (Opus) passed; its defects were fixed.

Stage 3 must:

- Pass `DesignSession.BuildKey` with `LegacyBuildKeys` to Build.
- Remove or replace the panel's old long-id `RecoveryStore.DeleteStale` call (GuitarFormPanel ~line 619) with `DeleteStaleRecovery`.
- Supply the panel-only behaviors: dirty label, Save state and debounce timer; flush on hide, close and Rhino shutdown; `RequireNoInputErrors`; the delete warning; the export dialog; `ConfirmLibraryChange`; validating an import file with `GuitarDocument.Read` before asking about discard; catching and logging `FlushRecovery`/`ClearRecovery` failures; and `ResolveLibraryItems` error swallowing.
- Use Open, CreateNew, Copy and `Import(discardPrevious: true)` after a user-confirmed discard.
- Tabs treat `note == null` as no note.
- `DesignBuilder`'s null-key guard has no automated test (it needs Rhino). Check in Rhino that Build still replaces only its own objects.

**Stage 4 (R1/R2) done: the old model is retired.** Deleted: `GuitarDesign`, `Instrument`, the old `Body` and `Soundhole`, `DesignDocument`, `SectionTable`/`SectionTables`, and `Library`'s design and preset API (`ListDesigns`, `CreateDesign`, `LoadDesign`, `SaveDesign`, `CopyDesign`, `DeleteDesign`, presets). `GuitarDocument` reads version 1 through its own frozen field map. `RecoveryStore` lost its long-id `Write`/`TryRead`/`Exists`/`Delete`/`DeleteStale`; `TryReadLegacy`/`DeleteLegacy` stay for numeric-key files (tests write one with the internal `WriteLegacy`). Tests build version 1 JSON with `V1Documents`. Tests: 690.

**Owner decisions:**

- Convert existing designs on upgrade.
- Body presets become named bodies. Instrument and soundhole presets stay in the old tables and are listed in the upgrade notes.
- Keep the old tables for now; drop them in a later migration.
- Leave the `NewFields` folder in place.
- Shared folder: convert, with a note.
- Export becomes GuitarDocument v3. v1 stays readable.
- The `uid` and `legacy_design_id` columns are approved. The Build tag and recovery key are `guitar-<uid>`.
- Labels become glossary labels, with the old descriptions as tooltips. The UI keeps saying "Design".
- Library item names stay "Manufacturer Product".
- A bad stored value opens with a warning.
- `InternalsVisibleTo` for Data is approved.

**P2 done (the panel runs on `DesignSession`):**

- Schema v4 runs `LegacyDesignMigration`. `Library.UpgradeNotes`, `Library.TakeUpgradeNotes()` (once, for the panel) and a `<name>.before-v{N}.notes.txt` beside the safety copy keep the notes.
- The panel holds a `DesignSession` and no longer keeps its own design, new fields, dirty flag, recovery plumbing or note strings. The design list comes from `ListGuitars()`; Build passes `BuildKey` and `LegacyBuildKeys`; Export writes a guitar document (version 3); the old long-id `RecoveryStore.DeleteStale` call is gone (`DeleteStaleRecovery` replaces it).
- (Historical, done) Temporary bridge (P4 deleted `LegacyFieldAdapter`; P5 deleted `GuitarFieldMap`): `GuitarForm.Model/GuitarFieldMap.cs` (a Guitar as the old Instrument, Body and Soundhole records and back, tested in `GuitarFieldMapTests`) and `GuitarForm/LegacyFieldAdapter.cs` (the panel's `Design`, `Fields` and `Apply`). The new-field editors edit the session's Guitar directly; the `NewFields` file I/O was removed.
- (Historical, done) The Instrument, Body and Soundhole presets still use the old preset tables (P5 replaces them).

**P3 done (unused until P4):** field table `GuitarFields` in GuitarForm.Model/Parts and `FieldBinding` in the plug-in; tests 717. Open P4 questions:

1. Soundhole yes/no as its own group vs a pinned top check box.
2. Unticking the soundhole keeps SHD/SHOC/SHOT values (new model) whereas the bridge clears them.
3. A blank required field clears the value and Validate reports it, rather than "X is required".
4. Minimums come from the definitions' limits.
5. Required fields show a "required" placeholder.

**P4 done (the panel runs on the field table):**

- Every tab (Instrument, Body, Soundhole, Bracing, Fretboard, Neck) builds its groups from `GuitarFields` through `FieldBindings`; each binding's Changed event applies the text to the session's Guitar, marks it dirty and redraws. Deleted: `Sections.cs`, `NewFields.cs`, `LegacyFieldAdapter.cs`, `SectionEditor<T>`, `NumberField<T>`, `FieldGroup<T>` (`SectionEditor.cs` is now `FieldLayout.cs`: rows, groups, messages bar, `IFieldBox`). `StartingShape.cs` holds the starting-shape logic; the marker box uses `DotMarkerText`.
- Messages per tab come from the model's `Validate` (Instrument = the guitar's own checks, Body = body minus top plate, Soundhole = top plate minus bracing, Bracing, Fretboard, Neck = neck minus fretboard) plus the shapes' messages and the tab's input errors. The drawing takes the Guitar directly from the session.
- Soundhole: the check box above the groups changes only SH; SHD/SHOC/SHOT keep their values and are grayed while unticked; shapes draw no soundhole when SH is No (test `Soundhole_UntickedKeepsItsValuesAndDrawsNone`).
- Behaviour changes: a blank required field (SL, NJF, BDL) clears the value; Save, Save as, Export and Build refuse while one is blank or some text can't be read. A blank Soundhole Diameter/Offset with SH ticked is a model error (Messages bar; Build asks) but no longer blocks Save. Labels are glossary labels with the old descriptions as tooltips. Minimums are the definitions' limits (error text is the model's). Tab messages now include the model's text for the Instrument, Body and Soundhole values.
- (Historical, done in P5) Left for P5: `GuitarFieldMap` (+ `GuitarFieldMapTests`) and the Instrument/Body/Soundhole `PresetRow<T>` rows (`CurrentDesign`/`ApplyDesign` in `GuitarFormPanel.Layout.cs`), which still use the old records and preset tables.

**P5 done (presets are saved copies):** the Body, Neck and Bracing tabs each have a "Saved <part> copies" row (`SavedCopiesRow<T>` in `GuitarFormPanel.Layout.cs`): drop-down, Load, Save as…, Delete. `DesignSession` has `ListNamed<T>`, `CheckNamedName<T>`, `LoadNamed<T>` (Apply, so dirty; a Neck load clears the fretwire and string set notes; returns unreadable-value problems, shown as a warning), `SaveNamed<T>` (rejects empty or taken names, ignoring case) and `DeleteNamed`. A saved body carries its top plate, soundhole and bracing; Bracing replaces only `Body.TopPlate.Bracing`. The Instrument and Soundhole preset rows are gone. Deleted: `PresetRow<T>`, `CurrentDesign`, `ApplyDesign`, `GuitarFieldMap`, `GuitarFieldMapTests`. The old preset tables and the `Library` preset API remain for R1. Tests: 726 (9 removed, 7 added).

**Rhino check after P5:**

- Body tab: Save as… a copy of the body; load it into another design (Unsaved changes shows, fields and preview follow); Delete it (asks). Same for the Neck and Bracing tabs. A taken or empty name is refused in the dialog.
- The Instrument and Soundhole tabs no longer have a preset row.
- The converted body presets "standard body" and "test" are in the Body tab's drop-down and load.
- A neck copy with a fretwire or string set brings them along (the Frets and Strings tabs show them).

**Rhino check after P4** (cannot be unit-tested):

- Every tab shows the same groups, fields and order as before (Body shows the old fields plus Cutaway and Blocks; Neck shows its groups); labels are glossary labels; hovering shows the old description, unit and limits.
- Soundhole: the check box is above the groups. Untick it: the preview drops the circle, the fields gray out but keep their values; tick it again: the same soundhole returns. Open a design with no soundhole: the fields are gray.
- Fretboard Courses: Course spacing fields are enabled only when Strings per Course is 2.
- Neck profile mode: Squareness shows in simple mode and Blend in advanced mode; the placeholders show the defaults; a Starting shape pick fills the station's three values and the list goes blank.
- Dot markers: type `3 5 7 9 12:2`; a bad entry shows a message and Save/Export/Build refuse.
- Switch the document units (mm to inches): values, unit labels, slider steps and limits follow.
- Slider steps: drag, arrow, Shift and Ctrl steps stop at limits; angle and blend steps are 1.
- Clear Scale Length, Neck Join Fret or Body Length: the box shows "required", the Messages bar names it, Save/Export/Build refuse. Type junk in any box: the same.
- Recovery: edit, hide the panel, reopen the design and accept the offer; Revert and switching designs refill every tab.
- (Historical; the old Instrument, Body and Soundhole presets no longer exist. Check the saved body, neck and bracing copies instead.)

**Rhino check after P2** (stage 3):

- Open a copy of the real library. It upgrades to v4, leaves `GuitarForm.before-v3.bak` (or the matching `before-v{N}`) and a `.notes.txt` beside it, and the panel shows the notes once (a message box and the command history). Every old design is in the list, with the same values on the Instrument, Body, Soundhole, Bracing, Fretboard, Neck, Strings and Frets tabs.
- Edit a value: "Unsaved changes" appears, the preview updates, Save keeps it, Revert drops it. First Save of a design still asks overwrite or new copy.
- Switch design, New, Copy, Import and Delete with unsaved changes: Save, Don't save and Cancel each behave as before; after "Don't save" no recovery offer comes back for the discarded work.
- Edit, hide the panel (or close Rhino), reopen the design: the recovery offer appears and Yes brings the edits back. A recovery file left by the old version for a converted design is offered too, once.
- Untick and tick the soundhole; type dot markers and a bad value (the Messages bar names it and Save/Export/Build refuse).
- Export a design and import it again (also import an old version 1 file): the import opens as a new design; a missing string set or fretwire leaves a note on its tab.
- Build, edit, Build again: only that design's objects are replaced, and objects built before the upgrade (tagged with the old design id) are replaced too, not duplicated. `DesignBuilder`'s null-key guard has no automated test.
- Use a fretwire and a string set on a design, delete one in its tab: the design shows it as gone, with a note.

**Still needed:**

- A copy of the owner's real library and `NewFields` folder, for a dry run before stage 3 ships.
- ~~A decision on whether `MoveLibrary` moves `*.before-v*.bak` and notes files.~~ Decided yes; done (`LibraryFiles.Move`).

**Rhino check after stage 1:** open the current library. It should upgrade to v3 and leave `GuitarForm.before-v2.bak` beside it (or the matching `before-v{N}` name). Designs, presets, new fields, the Strings and Frets tabs, Build and recovery should all work as before.

## Open questions for the owner

- The plan in `docs/Plan-FretsAndLayers.md` is built.
- When the strings overlap, should the fallback draw rectangles instead of the two outer center lines?
- Should the fretwire's Slot Depth be cross-checked against its Tang Height or the fretboard thickness, and how?
- What unit or currency is the fretwire price in?
- Construction lines print at the outline weight. Is that what you want?
- The **Use no fretwire** button was added unasked. Keep it?
- The stricter value coercion now rejects wrong-typed values in hand-edited JSON with a `FormatException`. Is that the intended behavior?
- When should migration v5 drop the old tables (`designs`, `instruments`, `bodies`, `soundholes`)? Until then the old Instrument and Soundhole presets live only there, and the `NewFields` folders stay on disk.
