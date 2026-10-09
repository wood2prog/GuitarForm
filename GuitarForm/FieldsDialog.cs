using System;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace GuitarForm
{
    // A small dialog with labelled text boxes and OK / Cancel. Validate returns an error to show, keeping the dialog
    // open, or null to accept.
    sealed class FieldsDialog : Dialog<bool>
    {
        readonly TextBox[] _boxes;
        readonly Label _error = new Label { TextColor = Colors.Red, Wrap = WrapMode.Word };

        FieldsDialog(string title, (string Label, string Text)[] fields, Func<string[], string> validate)
        {
            Title = title;
            Padding = 10;
            Resizable = false;
            _boxes = fields.Select(f => new TextBox { Text = f.Text, Width = 220 }).ToArray();

            var ok = new Button { Text = "OK" };
            ok.Click += (_, _) =>
            {
                _error.Text = validate?.Invoke(Values) ?? "";
                if (_error.Text.Length == 0)
                    Close(true);
            };
            var cancel = new Button { Text = "Cancel" };
            cancel.Click += (_, _) => Close(false);
            DefaultButton = ok;
            AbortButton = cancel;

            var layout = new TableLayout { Spacing = new Size(6, 6) };
            for (int i = 0; i < fields.Length; i++)
                layout.Rows.Add(new TableRow(new Label { Text = fields[i].Label, VerticalAlignment = VerticalAlignment.Center }, _boxes[i]));
            layout.Rows.Add(new TableRow(null, _error));
            layout.Rows.Add(new TableRow(null, new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Items = { ok, cancel },
            }));
            Content = layout;
        }

        string[] Values => _boxes.Select(b => b.Text.Trim()).ToArray();

        // The trimmed values, or null if cancelled.
        public static string[] Ask(Control parent, string title, (string Label, string Text)[] fields, Func<string[], string> validate = null)
        {
            var dialog = new FieldsDialog(title, fields, validate);
            return dialog.ShowModal(parent) ? dialog.Values : null;
        }
    }
}
