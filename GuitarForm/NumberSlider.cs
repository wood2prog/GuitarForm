using System;
using System.Globalization;
using Eto.Drawing;
using Eto.Forms;

namespace GuitarForm
{
    // A decimal number shown on the lever of a fader-style slider. Drag the lever along its track to change the value:
    // each step is a tenth, or a hundredth while Shift is held. The lever rests in the middle of the track and springs
    // back there when let go, keeping the new value, so each drag goes as far as the track allows. Click the lever
    // without dragging to type a value; click the track either side of it to step once that way. The value is kept as
    // text, like a TextBox's, and TextChanged is raised for each step and each edit.
    sealed class NumberSlider : Panel, IFieldBox
    {
        const double CoarseStep = 0.1;
        const double FineStep = 0.01;

        const int ControlWidth = 240;
        const int ControlHeight = 24;
        const int LeverWidth = 64;
        const int LeverHeight = 20;

        // Pixels the lever moves for one step, and before a press on it counts as a drag rather than a click.
        const int PixelsPerStep = 2;
        const int DragThreshold = 3;

        readonly Drawable _bar = new Drawable { CanFocus = true, Size = new Size(ControlWidth, ControlHeight) };
        readonly TextBox _textBox = new TextBox { Visible = false, Size = new Size(LeverWidth, LeverHeight) };
        string _text = "";
        string _textBeforeEdit;
        bool _editing;

        // The drag in progress. The value changes by one step for every PixelsPerStep the lever moves past where the
        // step size was last set (the press, or a change of Shift).
        bool _pressedOnLever;
        float _pressX;
        float _leverOffset;
        bool _dragging;
        bool _fine;
        double _baseValue;
        float _baseOffset;

        public event EventHandler<EventArgs> TextChanged;

        public NumberSlider()
        {
            var layout = new PixelLayout();
            layout.Add(_bar, 0, 0);
            layout.Add(_textBox, (ControlWidth - LeverWidth) / 2, (ControlHeight - LeverHeight) / 2);
            Content = layout;
            Size = new Size(ControlWidth, ControlHeight);

            _bar.Paint += (_, e) => Paint(e.Graphics);
            _bar.MouseDown += OnMouseDown;
            _bar.MouseMove += OnMouseMove;
            _bar.MouseUp += OnMouseUp;
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

        // Shown greyed out on the lever while the value is empty.
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

        double CurrentValue =>
            double.TryParse(_text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) && double.IsFinite(value)
                ? value : 0;

        // Rounding drops floating-point noise, and keeps any finer digits typed in by hand.
        void SetValue(double value) =>
            SetText((Math.Round(value, 6) + 0.0).ToString("0.######", CultureInfo.CurrentCulture));

        // --- Typing ---

        void StartEditing()
        {
            if (_editing || !_bar.Enabled)
                return;
            _editing = true;
            _textBeforeEdit = _text;
            _textBox.Text = _text;
            _textBox.Visible = true;
            _textBox.Focus();
            _textBox.SelectAll();
            _bar.Invalidate();
        }

        void StopEditing()
        {
            if (!_editing)
                return;
            _editing = false;
            _textBox.Visible = false;
            _bar.Invalidate();
        }

        // --- Dragging ---

        float Center => _bar.Width / 2f;

        // How far the lever's center can move from the middle of the track.
        float Travel => (_bar.Width - LeverWidth) / 2f - 2;

        RectangleF LeverBounds => new RectangleF(Center + _leverOffset - LeverWidth / 2f,
            (_bar.Height - LeverHeight) / 2f, LeverWidth, LeverHeight);

        void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (!_bar.Enabled || e.Buttons != MouseButtons.Primary)
                return;
            _bar.Focus();
            e.Handled = true;
            bool fine = e.Modifiers.HasFlag(Keys.Shift);
            if (!LeverBounds.Contains(e.Location))
            {
                // A click on the track steps once toward it.
                double step = fine ? FineStep : CoarseStep;
                SetValue(CurrentValue + (e.Location.X < Center ? -step : step));
                return;
            }
            _pressedOnLever = true;
            _pressX = e.Location.X;
            _leverOffset = 0;
            _dragging = false;
            _fine = fine;
            _baseValue = CurrentValue;
            _baseOffset = 0;
            CaptureMouse(_bar);
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_pressedOnLever)
                return;
            float moved = e.Location.X - _pressX;
            if (!_dragging && Math.Abs(moved) < DragThreshold)
                return;
            _dragging = true;
            e.Handled = true;

            // Pressing or letting go of Shift mid-drag changes the step from here on, without making the value jump.
            bool fine = e.Modifiers.HasFlag(Keys.Shift);
            if (fine != _fine)
            {
                _baseValue = CurrentValue;
                _baseOffset = _leverOffset;
                _fine = fine;
            }

            _leverOffset = Math.Clamp(moved, -Travel, Travel);
            int steps = (int)Math.Round((_leverOffset - _baseOffset) / PixelsPerStep);
            SetValue(_baseValue + steps * (_fine ? FineStep : CoarseStep));
            _bar.Invalidate();
        }

        void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (!_pressedOnLever)
                return;
            bool clicked = !_dragging;
            _pressedOnLever = false;
            _dragging = false;
            _leverOffset = 0;
            ReleaseMouseCapture(_bar);
            _bar.Invalidate();
            e.Handled = true;
            if (clicked)
                StartEditing();
        }

        // Mouse capture keeps the drag going when the pointer leaves the slider. Rhino 8's Eto has it, but the Eto that
        // RhinoCommon 8.0's package compiles against doesn't, so it's called when it's there.
        static void CaptureMouse(Control control) =>
            typeof(Control).GetMethod("CaptureMouse", Type.EmptyTypes)?.Invoke(control, null);

        static void ReleaseMouseCapture(Control control) =>
            typeof(Control).GetMethod("ReleaseMouseCapture", Type.EmptyTypes)?.Invoke(control, null);

        // --- Drawing ---

        void Paint(Graphics g)
        {
            bool enabled = _bar.Enabled;
            float width = _bar.Width;
            float height = _bar.Height;
            float middle = height / 2f;
            var groove = enabled ? SystemColors.ControlText : SystemColors.DisabledText;
            var tick = SystemColors.DisabledText;

            // The track: a groove the length of the lever's travel, with ticks along it and a longer one at the middle.
            float left = Center - Travel;
            float right = Center + Travel;
            for (float x = Center; x <= right + 0.5f; x += 10)
            {
                float length = x == Center ? 9 : 4;
                g.DrawLine(tick, x, middle - length, x, middle - 4);
                g.DrawLine(tick, x, middle + 4, x, middle + length);
                float mirrored = 2 * Center - x;
                g.DrawLine(tick, mirrored, middle - length, mirrored, middle - 4);
                g.DrawLine(tick, mirrored, middle + 4, mirrored, middle + length);
            }
            g.FillRectangle(SystemColors.Control, left, middle - 2, right - left, 4);
            g.DrawRectangle(groove, left, middle - 2, right - left, 4);

            // The lever, with the value on it. While typing, the text box covers it.
            if (_editing)
                return;
            var lever = LeverBounds;
            g.FillRectangle(enabled ? SystemColors.ControlBackground : SystemColors.Control, lever);
            g.DrawRectangle(_bar.HasFocus || _dragging ? SystemColors.Highlight : groove,
                lever.X, lever.Y, lever.Width - 1, lever.Height - 1);

            var font = SystemFonts.Default();
            bool empty = _text.Length == 0;
            string text = empty ? PlaceholderText : _text;
            var textSize = g.MeasureString(font, text);
            var textColor = empty || !enabled ? SystemColors.DisabledText : SystemColors.ControlText;
            g.DrawText(font, textColor, lever.X + (lever.Width - textSize.Width) / 2, lever.Y + (lever.Height - textSize.Height) / 2, text);
        }
    }
}
