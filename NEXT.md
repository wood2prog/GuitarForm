# Next

Short handoff for a new session. Update it at the end of each session. The full checklist, decisions and tech debt are in `IMPROVEMENTS.md`.

## State

- Branch `main`. Latest commit: crash recovery for Strings and Frets edits (built, needs a Rhino check).
- The move onto the new model is finished. The panel works on a `DesignSession` holding a `Guitar`; every tab is built from `GuitarFields`; preview and Build draw through `GuitarForm.Shapes` and `DesignDrawing.FromShapes`.
- The library is at schema v5: the old design tables are dropped, the body, neck and bracing have saved copies, and a neck holds its fretwire and string set. The `NewFields` folder is no longer used and can be deleted by the user.
- Tabs: Instrument, Body, Soundhole, Bracing, Fretboard, Neck, Strings, Frets. Each is documented in `docs/`.
- Workflow: all new work needs the user's explicit "build it" before implementation starts.

## Do next, in order

1. Bridge plan settings (width along the centerline, saddle position in it) so the bridge patch can be drawn. Needs a decision.
2. UI polish: alignment and positioning in the panel. The owner will describe or screenshot what looks off.
3. Limits on values, field by field with the owner, so nonsense values can't be entered. Then slider feel and per-field steps (including steps in inches).
4. Rhino check of the migrated panel: open a copy of the real library (upgrade to v5, notes shown once), Frets and Strings tabs, saved body, neck and bracing copies, Build replacing only its own objects, slider mouse capture and the disabled look. Section "Rhino check" items from the old checklist are in git history (`git show 7d155aa:NEXT.md`).
5. Rhino check of Strings and Frets crash recovery (built, needs a Rhino check):
   - (a) Edit a string set and a fretwire, wait 2 s, kill Rhino, reopen, answer Yes: both come back as unsaved changes.
   - (b) Hide the panel right after an edit: the recovery file is written at once.
   - (c) With the recovery prompt open, hide the panel and reopen: the file is still there.
   - (d) Strings tab dirty, switch library, No on Strings, then Cancel on Frets, kill Rhino, restart: the Strings edit is still offered.
   - (e) Change library while the offer is up: only one prompt.
   - (f) Deleted item: "Recover as a new ..." is offered.
   - (g) Save, Revert, Delete and declining each remove the file.
   - (h) Guitar (design) recovery is still offered, and survives a design-list refresh.
   - Known limitation: after a library switch with "No" to unsaved work, the old library's recovery file isn't cleared, so it is offered again if you return to that library.

## Questions for the product owner

- Should a hand-edited JSON with a wrong-typed value be rejected with a `FormatException`? (Current behavior.)
- Placing parts with planes (neck at the heel, neck angle): decide when the neck layout needs it.
