# Frets tab

The library of fretwires: the wire's manufacturer, product code, bead and tang dimensions, the slot depth it recommends and an optional price. A guitar chooses one fretwire for its neck. Every field's meaning is in [Glossary.md](Glossary.md); this page covers how the tab works.

The tab works with no design open, since fretwires are library items shared by every guitar. Lengths are shown and typed in the Rhino document's units and stored in millimeters. See [Units](../README.md#units). It works like the [Strings tab](Strings.md), and the two share their picker, prompts and delete flow.

## Picker and buttons

The **Fretwire** picker lists the saved fretwires by name: the manufacturer and product code, or the product code alone when there's no manufacturer. Above the editor, the buttons are:

| Button | What it does |
|---|---|
| **New** | Starts a new, unsaved fretwire with every field blank. It asks about unsaved changes first. |
| **Save** | Writes the working copy to the library. Enabled only while there are unsaved changes. |
| **Save as…** | Asks for a manufacturer (optional) and a product code, and saves the working copy as a new fretwire, which is then shown. The original is left as it was. A name already used by another fretwire (ignoring case) is refused. |
| **Revert** | Throws away the unsaved changes to the shown fretwire, after asking. Enabled only while there are unsaved changes. |
| **Delete** | Deletes the shown saved fretwire (see below). Enabled only for a saved fretwire. |
| **Use for this guitar** | Chooses the shown fretwire for the open design's guitar (see below). |
| **Use no fretwire** | Takes the fretwire away from the open design's guitar. Enabled only when a design is open. |

The label beside the picker says **Unsaved changes** while the working copy differs from the saved fretwire. Below the buttons, **Used by this guitar** names the fretwire the open design uses, or **none**.

Save is refused, with the reason shown, while any text can't be read, the **Product Code** is blank (it names the fretwire in the library), or the fretwire has errors. The errors are listed in the Messages bar.

### Delete

- A fretwire that no saved neck uses is deleted after you confirm (**OK** or **Cancel**). Its unsaved changes are lost too, and the message says so.
- The check for necks that use the fretwire covers only saved necks in the library, not designs that use it (a design using a deleted fretwire shows **none** with a note).
- A fretwire that saved necks use can't be deleted without a replacement. The dialog lists those necks and asks for the fretwire they should use instead, chosen from the other saved fretwires. **Delete** replaces the fretwire in those necks and then deletes it; **Cancel** does nothing.
- If no other fretwire exists, the dialog says so and asks you to save another one first.

Only saved necks in the library are checked for this. A design's own copy of the choice is not (see Used by a guitar).

### Use for this guitar

**Use for this guitar** is enabled only when a design is open, the shown fretwire is saved, and it has no unsaved changes. Otherwise its tooltip says what to do first: open a design, save the new fretwire, or save the changes to this fretwire.

Choosing it makes the fretwire the open design's guitar fretwire, which marks the design as having unsaved changes. Save the design to keep it. **Use no fretwire** does the same with none.

## Unsaved changes

- Choosing another fretwire in the picker, **New**, or changing the library in Settings asks **Save changes to "…"?** with **Yes** (save, then carry on), **No** (discard, then carry on) or **Cancel** (stay where you are). If the save is refused, the switch is cancelled too.
- Switching, creating, copying, opening or importing a design asks only about the design's own unsaved changes. Unsaved fretwire edits are independent of the design and stay in the tab while you change designs.
- Changing the library in Settings (switch or restore) asks about the design and then about unsaved fretwire changes; **No** puts the tab back to the saved fretwire, so the question isn't asked again.
- **Revert** asks to confirm before discarding.
- The Frets tab has no recovery file, and hiding the panel or closing Rhino doesn't ask, so Rhino's shutdown is never blocked. Unsaved changes to a fretwire are lost if Rhino closes before you save them.

## Fretwire fields

Limits come from the model. Meanings are in the Glossary.

| Field | Abbreviation | Notes |
|---|---|---|
| Fretwire Manufacturer | FWM | Optional. |
| Fretwire Product Code | FWPC | Required to save. |
| Fretwire Bead Height | FWBH | The height of the bead above the playing surface. |
| Fretwire Bead Width | FWBW | The width the frets are drawn at. |
| Fretwire Tang Height | FWTH | The depth of the tang below the bead's base. |
| Fretwire Tang Thickness | FWTT | The width of the fret slots. Must be less than the Bead Width. |
| Fretwire Barb Thickness | FWBT | Optional. When given, it must be more than the Tang Thickness. |
| Fretwire Slot Depth | FWSD | The depth of the slot the wire recommends, from the playing surface. Used by the slot lines in the side view. |
| Fretwire Price | FWPR | Optional. A plain number with no currency or per-length unit, for future job costing. |

Text that can't be read is shown in Messages as an input error and leaves the field's last good value; it blocks Save and Save as until it is fixed. The model's own messages for the fretwire are listed there as well.

## Used by a guitar

The design's chosen fretwire is kept by its id with the design in the library, and the design's JSON export also writes it in full. When a design is opened, its fretwire is looked up by that id.

- If the fretwire has been deleted, the design shows **none**, with the note "The fretwire this guitar used isn't in the library; choose another on the Frets tab." The frets are then drawn as center lines only.
- When a design is imported, the fretwire is matched by the exported name. If no fretwire of that name is in the library, the design opens with none and the same note.
- Saving a fretwire or deleting one reads the open guitar's copy again from the library; that is not an edit of the design.

## What is drawn

The fretwire changes the fretboard's drawing, which is described in [Fretboard.md](Fretboard.md):

- **Frets** are drawn orange as a closed rectangle as wide as the Bead Width, centered on the fret position and running edge to edge of the fretboard, with a thinner center line in the same orange marking where the tang goes. With no fretwire (or no Bead Width) only the center lines are drawn, with a Remark.
- **Fret slots** are thin rectangles as wide as the Tang Thickness, centered on each fret.
- **Slot lines** are drawn in the side view at each fret, from the playing surface down to the slot bottom. They need the Slot Depth (plus the Fretboard Slot Overage on the Fretboard tab).

Frets and slots are previewed and built in their component colors; see [Layers and colors](../README.md#layers-and-colors).

## Limitations

- Tension, cost totals and the price's unit are not decided; the price is only stored.
- Unsaved work on a fretwire has no crash recovery (see Unsaved changes).
- Deleting a fretwire can't see designs that use it, only saved necks in the library (see Delete). Such a design shows none the next time it's opened.
- A missing fretwire isn't offered a replacement yet.

## Quick Test

Use a document in millimeters. This is a checklist for the panel; the drawn values are checked by tests and listed in [Fretboard.md](Fretboard.md#quick-test) (a Bead Width of 2.5 gives 20 bead-width frets, fret 14 with corners at y = 601.25 and 598.75; a Tang Thickness of 0.53 gives 20 slots; a Slot Depth of 2 gives 20 slot lines).

1. Open the **Frets** tab with no design open. Click **New**. **Use for this guitar** and **Use no fretwire** are disabled.
2. Click **Save** with the Product Code blank. It is refused, naming the Product Code.
3. Enter Product Code `Quick 1`, Bead Width `2.5`, Tang Thickness `0.53`, Slot Depth `2`, and fill the other dimensions. Type `abc` in a field: Messages shows an input error and Save is refused; fix it and Save works.
4. Click **Save**. The picker shows `Quick 1` and the label clears.
5. Click **Save as…** and give it Manufacturer `Test` and Product Code `Quick 2`. The picker shows `Test Quick 2`. Try the name `quick 1` again: it is refused as a duplicate.
6. Change a value and switch the picker: the prompt appears. Cancel stays; No discards; Yes saves. **Revert** asks first.
7. Open a design with the Fretboard tab set up as in its Quick Test. On the Frets tab pick `Quick 1`, click **Use for this guitar**, and see **Used by this guitar** read `Quick 1` and the design show Unsaved changes. The preview now draws bead-width frets, slots and, in the side view, slot lines.
8. Save the design, reopen it, Save as a copy, and Export then Import it: the fretwire is still `Quick 1` each time. Change the document's units to inches and back: the fields follow.
9. Click **Use no fretwire**: the frets go back to center lines with the Remark.
10. Use `Quick 1` again, then delete it from the Frets tab. Because a saved neck may use it, expect the replacement dialog when one does; with none, a plain confirmation. Reopen the design: it shows none, with the note.
