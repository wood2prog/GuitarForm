using System;
using System.Globalization;
using Eto.Drawing;
using Eto.Forms;

namespace GuitarForm
{
    // A box for a decimal number that can also be dragged. Click it to type a value, as in a text box. Drag it left or
    // right to change the value: grabbed on its left half each step is a tenth, on its right half a hundredth. The
    // value is kept as text, like a TextBox's, and TextChanged is raised for each step of a drag and each edit.
    sealed class NumberSlider : Panel, IFieldBox
    {
        const double CoarseStep = 0.1;
        const double FineStep = 0.01;

        // Pixels the mouse moves for one step, and before a press counts as a drag rather than a click.
        const int PixelsPerStep = 3;
        const int DragThreshold = 3;

        readonly Drawable _bar = new Drawable { CanFocus = true };
        readonly TextBox _textBox = new TextBox();
        string _text = "";
        string _textBeforeEdit;
        bool _editing;

        // The drag in progress: where it started, the value then, and its step. Null when not dragging.
        PointF? _pressedAt;
        double _startValue;
        double _step;
        bool _dragging;
        float? _hoverX;

        public event EventHandler<EventArgs> TextChanged;

        public NumberSlider()
        {
            Width = 130;
            Height = 22;
            Content = _bar;
            _bar.Cursor = Cursors.VerticalSplit;

            _bar.Paint += (_, e) => Paint(e.Graphics);
            _bar.MouseDown += OnMouseDown;
            _bar.MouseMove += OnMouseMove;
            _bar.MouseUp += OnMouseUp;
            _bar.MouseLeave += (_, _) =>
            {
                _hoverX = null;
                _bar.Invalidate();
            };
            _bar.KeyDown += (_, e) =>
            {
                if (e.Key == Keys.Enter || e.Key == Keys.F2)
                {
                    StartEditing();
                    e.Handled = true;
                }
            };

            _textBox.TextChanged += (_, _) =>
            {
                if (_editing)
                    SetText(_textBox.Text);
            };
            _textBox.KeyDown += (_, e) =>
            {
                if (e.Key == Keys.Enter)
                {
                    StopEditing();
                    e.Handled = true;
                }
                else if (e.Key == Keys.Escape)
                {
                    _textBox.Text = _textBeforeEdit;
                    StopEditing();
                    e.Handled = true;
                }
            };
            _textBox.LostFocus += (_, _) => StopEditing();
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? "";
                if (_editing)
                    _textBox.Text = _text;
                _bar.Invalidate();
            }
        }

        // Shown greyed out while the box is empty.
        public string PlaceholderText { get; set; } = "";

        public new string ToolTip
        {
            get => _bar.ToolTip;
            set
            {
                _bar.ToolTip = value;
                _textBox.ToolTip = value;
            }
        }

        void SetText(string text)
        {
            if (text == _text)
                return;
            _text = text;
            _bar.Invalidate();
            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        // --- Typing ---

        void StartEditing()
        {
            if (_editing || !_bar.Enabled)
                return;
            _editing = true;
            _textBeforeEdit = _text;
            _textBox.Text = _text;
            Content = _textBox;
            _textBox.Focus();
            _textBox.SelectAll();
        }

        void StopEditing()
        {
            if (!_editing)
                return;
            _editing = false;
            Content = _bar;
            _bar.Invalidate();
        }

        // --- Dragging ---

        void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (!_bar.Enabled || e.Buttons != MouseButtons.Primary)
                return;
            _pressedAt = e.Location;
            _dragging = false;
            _step = e.Location.X < _bar.Width / 2f ? CoarseStep : FineStep;
            _startValue = double.TryParse(_text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) &&
                double.IsFinite(value) ? value : 0;
            CaptureMouse(_bar);
            e.Handled = true;
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            _hoverX = e.Location.X;
            if (_pressedAt is not PointF pressed)
            {
                _bar.Invalidate();
                return;
            }
            float moved = e.Location.X - pressed.X;
            if (!_dragging && Math.Abs(moved) < DragThreshold)
                return;
            _dragging = true;
            int steps = (int)Math.Round(moved / PixelsPerStep);
            // Rounding to the finest step drops floating-point noise, and keeps any finer digits typed in by hand.
            double value = Math.Round(_startValue + steps * _step, 6) + 0.0;
            SetText(value.ToString("0.######", CultureInfo.CurrentCulture));
            e.Handled = true;
        }

        void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_pressedAt == null)
                return;
            bool clicked = !_dragging;
            _pressedAt = null;
            _dragging = false;
            ReleaseMouseCapture(_bar);
            _bar.Invalidate();
            if (clicked)
                StartEditing();
            e.Handled = true;
        }

        // Mouse capture keeps the drag going when the pointer leaves the box. Rhino 8's Eto has it, but the Eto that
        // RhinoCommon 8.0's package compiles against doesn't, so it's called when it's there.
        static void CaptureMouse(Control control) =>
            typeof(Control).GetMethod("CaptureMouse", Type.EmptyTypes)?.Invoke(control, null);

        static void ReleaseMouseCapture(Control control) =>
            typeof(Control).GetMethod("ReleaseMouseCapture", Type.EmptyTypes)?.Invoke(control, null);

        // --- Drawing ---

        void Paint(Graphics g)
        {
            var size = _bar.Size;
            var bounds = new RectangleF(0, 0, size.Width - 1, size.Height - 1);
            bool enabled = _bar.Enabled;
            g.FillRectangle(enabled ? SystemColors.ControlBackground : SystemColors.Control, bounds);

            // The half the pointer is over (or the drag's half) is shaded, with its step in the corner.
            float? x = _pressedAt?.X ?? _hoverX;
            var font = SystemFonts.Default();
            if (enabled && x != null)
            {
                bool left = x < size.Width / 2f;
                var half = left
                    ? new RectangleF(1, 1, size.Width / 2f - 1, size.Height - 2)
                    : new RectangleF(size.Width / 2f, 1, size.Width / 2f - 2, size.Height - 2);
                var highlight = SystemColors.Highlight;
                g.FillRectangle(new Color(highlight, _pressedAt != null ? 0.25f : 0.12f), half);
                string step = left ? "0.1" : "0.01";
                var smallFont = new Font(font.Family, font.Size * 0.8f);
                var stepSize = g.MeasureString(smallFont, step);
                float stepX = left ? 3 : size.Width - stepSize.Width - 3;
                g.DrawText(smallFont, SystemColors.DisabledText, stepX, (size.Height - stepSize.Height) / 2, step);
            }

            bool empty = _text.Length == 0;
            string text = empty ? PlaceholderText : _text;
            var textSize = g.MeasureString(font, text);
            var textColor = empty || !enabled ? SystemColors.DisabledText : SystemColors.ControlText;
            g.DrawText(font, textColor, (size.Width - textSize.Width) / 2, (size.Height - textSize.Height) / 2, text);

            var border = _bar.HasFocus ? SystemColors.Highlight : SystemColors.DisabledText;
            g.DrawRectangle(border, bounds);
        }
    }
}
