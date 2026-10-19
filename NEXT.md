# Next

Short handoff for a new session. Update it at the end of each session. The full checklist, decisions and tech debt are in `IMPROVEMENTS.md`.

## State

- Branch `main`. Crash recovery for Strings and Frets edits is checked in Rhino by the product owner.
- Known limitation of Strings and Frets recovery: after a library switch with "No" to unsaved work, the old library's recovery file isn't cleared, so it is offered again if you return to that library.
- The move onto the new model is finished. The panel works on a `DesignSession` holding a `Guitar`; every tab is built from `GuitarFields`; preview and Build draw through `GuitarForm.Shapes` and `DesignDrawing.FromShapes`.
- The library is at schema v5: the old design tables are dropped, the body, neck and bracing have saved copies, and a neck holds its fretwire and string set. The `NewFields` folder is no longer used and can be deleted by the user.
- Tabs: Instrument, Body, Soundhole, Bracing, Fretboard, Neck, Strings, Frets. Each is documented in `docs/`.
- Workflow: all new work needs the user's explicit "build it" before implementation starts.

## Do next, in order

Start here next session: the bridge's plan settings (product owner decision). Today only the bridge's heights exist (Bridge Height BRH, saddle height). Nothing defines the bridge's footprint seen from above, so the bridge patch (BPT, BPWO, BPO) isn't drawn and no brace heights are drawn (see docs/Bracing.md). Ask the product owner; don't guess at guitar geometry:
1. Bridge width along the centerline (neck to tail)?
2. Where the saddle sits within that width (distance from the neck-side edge, or centered)?
3. Bridge length across the guitar (side to side): string spacing at the bridge plus an overhang each side?
4. Shape: plain rectangle, or a profile such as tapered ends?
5. Placement: positioned by the saddle, which the scale length already fixes?

Then plan new fields (Glossary, Parameter definitions, GuitarFields, a shape, GuitarDrawing, docs, tests), draw the bridge and its patch.

1. Bridge plan settings (width along the centerline, saddle position in it) so the bridge patch can be drawn. Needs a decision.
2. UI polish: alignment and positioning in the panel. The owner will describe or screenshot what looks off.
3. Limits on values, field by field with the owner, so nonsense values can't be entered. Then slider feel and per-field steps (including steps in inches).
4. Rhino check of the migrated panel: open a copy of the real library (upgrade to v5, notes shown once), Frets and Strings tabs, saved body, neck and bracing copies, Build replacing only its own objects, slider mouse capture and the disabled look. Section "Rhino check" items from the old checklist are in git history (`git show 7d155aa:NEXT.md`).

## Questions for the product owner

- Should a hand-edited JSON with a wrong-typed value be rejected with a `FormatException`? (Current behavior.)
- Placing parts with planes (neck at the heel, neck angle): decide when the neck layout needs it.
