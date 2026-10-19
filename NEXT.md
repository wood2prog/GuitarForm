# Next

Short handoff for a new session. Update it at the end of each session. The full checklist, decisions and tech debt are in `IMPROVEMENTS.md`.

## State

- Branch `main`, latest commit 7d155aa (the Florentine and Venetian cutaway wall now follows the fretboard's treble edge).
- The move onto the new model is finished. The panel works on a `DesignSession` holding a `Guitar`; every tab is built from `GuitarFields`; preview and Build draw through `GuitarForm.Shapes` and `DesignDrawing.FromShapes`.
- The library is at schema v5: the old design tables are dropped, the body, neck and bracing have saved copies, and a neck holds its fretwire and string set. The `NewFields` folder is no longer used and can be deleted by the user.
- Tabs: Instrument, Body, Soundhole, Bracing, Fretboard, Neck, Strings, Frets. Each is documented in `docs/`.
- Workflow: all new work needs the user's explicit "build it" before implementation starts.

## Do next, in order

1. Glossary pass: finish naming and defining every setting in `docs/Glossary.md` (about 575 lines; only Fretboard Radius at Nut/End and Neck Joint Type are still marked planned). Confirm with the owner that it is finished, then tick it off.
2. Bridge plan settings (width along the centerline, saddle position in it) so the bridge patch can be drawn. Needs a decision.
3. UI polish: alignment and positioning in the panel. The owner will describe or screenshot what looks off.
4. Limits on values, field by field with the owner, so nonsense values can't be entered. Then slider feel and per-field steps (including steps in inches).
5. Rhino check of the migrated panel: open a copy of the real library (upgrade to v5, notes shown once), Frets and Strings tabs, saved body, neck and bracing copies, recovery, Build replacing only its own objects, slider mouse capture and the disabled look. Section "Rhino check" items from the old checklist are in git history (`git show 7d155aa:NEXT.md`).

## Questions for the product owner

- Florentine cutaway: the wall fix is in. Is anything about the arc's radius still wrong?
- Is the glossary pass finished?
- Do unsaved Frets and Strings edits still need crash recovery? They have none today.
- Should a hand-edited JSON with a wrong-typed value be rejected with a `FormatException`? (Current behavior.)
- Placing parts with planes (neck at the heel, neck angle): decide when the neck layout needs it.
