using System;
using System.Globalization;
using Eto.Drawing;
using Eto.Forms;

namespace GuitarForm
{
    // A decimal number shown on the lever of a fader-style slider. Drag the lever along its track to change the value:
    // each step is Step (a tenth unless the field sets its own), or a tenth of that while Shift is held. The lever rests in the middle of the track and springs
    // back there when let go, keeping the new value, so each drag goes as far as the track allows. Click the lever
    // without dragging to type a value; click the track either side of it to step once that way. The value is kept as
    // text, like a TextBox's, and TextChanged is raised for each step and each edit.
    sealed class NumberSlider : Panel, IFieldBox
    {
        // The sizes below are scaled to the screen with UiMetrics.Scale. The slider fills the width it is given, from
        // SliderMinWidth up; past SliderMaxWidth the track stops growing and the extra space is left empty.
        static readonly int ControlHeight = UiMetrics.Scale(UiMetrics.SliderHeight);
        static readonly int LeverWidth = UiMetrics.Scale(UiMetrics.LeverWidth);
        static readonly int LeverHeight = UiMetrics.Scale(UiMetrics.LeverHeight);

        // Pixels the lever moves for one step, and before a press on it counts as a drag rather than a click.
        static readonly int PixelsPerStep = UiMetrics.Scale(UiMetrics.LeverPixelsPerStep);
        static readonly int DragThreshold = UiMetrics.Scale(UiMetrics.DragThreshold);
        static readonly float TickSpacing = UiMetrics.Scale(UiMetrics.TickSpacing);

        readonly Drawable _bar = new Drawable { CanFocus = true, Cursor = Cursors.Default };
        readonly TextBox _textBox = new TextBox();
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

        // The change for each step of a drag or click on the track, in the units the value is shown in. Shift steps a
        // tenth of it.
        public double Step { get; set; } = 0.1;

        // The lowest value a drag or a click on the track goes to, in the units the value is shown in. None when null.
        public double? Minimum { get; set; }

        // The highest value a drag or a click on the track goes to, in the units the value is shown in. None when null.
        public double? Maximum { get; set; }

        double CurrentStep(bool fine) => fine ? Step / 10 : Step;

        public NumberSlider()
        {
            // The bar fills the cell; while typing, the text box takes its place across the same width.
            _bar.MinimumSize = new Size(UiMetrics.Scale(UiMetrics.SliderMinWidth), ControlHeight);
            _bar.Height = ControlHeight;
            _textBox.Height = ControlHeight;
            Content = _bar;

            _bar.GotFocus += (_, _) => _bar.Invalidate();
            _bar.LostFocus += (_, _) =>
            {
                CancelPressWithoutCapture();
                _bar.Invalidate();
            };
            _bar.MouseLeave += (_, _) =>
            {
                _bar.Cursor = Cursors.Default;
                CancelPressWithoutCapture();
            };
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
                else if ((e.Key == Keys.Left || e.Key == Keys.Right) && _bar.Enabled && !_editing)
                {
                    // One step per press; Shift takes a finer step and Ctrl a coarse one (ten steps).
                    double step = e.Modifiers.HasFlag(Keys.Control) ? Step * 10 : CurrentStep(e.Modifiers.HasFlag(Keys.Shift));
                    SetValue(CurrentValue + (e.Key == Keys.Left ? -step : step));
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
                    StopEditing(true);
                    e.Handled = true;
                }
                else if (e.Key == Keys.Escape)
                {
                    _textBox.Text = _textBeforeEdit;
                    StopEditing(true);
                    e.Handled = true;
                }
            };
            _textBox.LostFocus += (_, _) => StopEditing();
            // Removed from the window while typing: finish, so edit mode can't be left stuck.
            UnLoad += (_, _) => StopEditing();
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

        // An empty box steps from its placeholder's value when that is a number (the default it falls back to), else 0.
        double CurrentValue =>
            double.TryParse(_text.Length == 0 ? PlaceholderText : _text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value) &&
            double.IsFinite(value)
                ? value : 0;

        // Rounding drops floating-point noise, and keeps any finer digits typed in by hand.
        void SetValue(double value) =>
            SetText((Math.Round(Math.Min(Math.Max(value, Minimum ?? double.NegativeInfinity), Maximum ?? double.PositiveInfinity), 6) + 0.0).ToString("0.######", CultureInfo.CurrentCulture));

        // --- Typing ---

        void StartEditing()
        {
            if (_editing || !_bar.Enabled)
                return;
            _editing = true;
            _textBeforeEdit = _text;
            _textBox.Text = _text;
            Content = _textBox;
            _bar.Invalidate();
            // The text box isn't loaded until the layout has run, so focusing it now can silently fail.
            Application.Instance.AsyncInvoke(() =>
            {
                if (!_editing)
                    return;
                _textBox.Focus();
                _textBox.SelectAll();
            });
        }

        // Ends typing, keeping what was typed (Esc puts the old text back first). After Enter or Esc the bar takes the
        // focus back, so arrow keys and Tab carry on from here; when focus went elsewhere it is left there.
        void StopEditing(bool refocus = false)
        {
            if (!_editing)
                return;
            _editing = false;
            Content = _bar;
            _bar.Invalidate();
            if (refocus)
                _bar.Focus();
        }

        // --- Dragging ---

        // The track is as wide as the bar, up to SliderMaxWidth, and starts at the bar's left edge.
        float TrackWidth => Math.Min(_bar.Width, UiMetrics.Scale(UiMetrics.SliderMaxWidth));

        float Center => TrackWidth / 2f;

        // How far the lever's center can move from the middle of the track.
        float Travel => Math.Max(0, (TrackWidth - LeverWidth) / 2f - UiMetrics.Scale(UiMetrics.TrackInset));

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
                double step = CurrentStep(fine);
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
            {
                // The typed value is on the lever, so the pointer shows a text cursor over it.
                _bar.Cursor = _bar.Enabled && LeverBounds.Contains(e.Location) ? Cursors.IBeam : Cursors.Default;
                return;
            }
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
            SetValue(_baseValue + steps * CurrentStep(_fine));
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

        // Without mouse capture the button-up can go to another control, so a drag in progress is ended when the pointer
        // leaves or focus moves, rather than left stuck.
        void CancelPressWithoutCapture()
        {
            if (!_pressedOnLever || CanCaptureMouse)
                return;
            _pressedOnLever = false;
            _dragging = false;
            _leverOffset = 0;
            _bar.Invalidate();
        }

        static readonly bool CanCaptureMouse = typeof(Control).GetMethod("CaptureMouse", Type.EmptyTypes) != null;

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
            var tick = UiMetrics.MutedTickColor;
            float s1 = UiMetrics.Scale(1), four = UiMetrics.Scale(4), nine = UiMetrics.Scale(9);

            // The track: a groove the length of the lever's travel, with ticks along it and a longer one at the middle.
            float left = Center - Travel;
            float right = Center + Travel;
            for (float x = Center; x <= right + 0.5f; x += TickSpacing)
            {
                float length = x == Center ? nine : four;
                g.DrawLine(tick, x, middle - length, x, middle - four);
                g.DrawLine(tick, x, middle + four, x, middle + length);
                float mirrored = 2 * Center - x;
                g.DrawLine(tick, mirrored, middle - length, mirrored, middle - four);
                g.DrawLine(tick, mirrored, middle + four, mirrored, middle + length);
            }
            g.FillRectangle(SystemColors.Control, left, middle - 2 * s1, right - left, four);
            g.DrawRectangle(groove, left, middle - 2 * s1, right - left, four);

            // A focus ring around the whole slider, in the theme's highlight colour.
            if (_bar.HasFocus && enabled)
                g.DrawRectangle(new Pen(SystemColors.Highlight, 2 * s1), s1, s1, width - 2 * s1, height - 2 * s1);

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
            float textY = lever.Y + (lever.Height - textSize.Height) / 2;
            var textColor = empty || !enabled ? SystemColors.DisabledText : SystemColors.ControlText;
            g.DrawText(font, textColor, lever.X + (lever.Width - textSize.Width) / 2, textY, text);
        }
    }
}
