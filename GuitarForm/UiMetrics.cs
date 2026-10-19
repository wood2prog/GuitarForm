using Eto.Drawing;
using Eto.Forms;

namespace GuitarForm
{
    /// <summary>Layout constants and helpers shared by the panel, so spacing, widths and colors stay consistent.</summary>
    internal static class UiMetrics
    {
        /// <summary>Space between neighboring controls, in pixels.</summary>
        public const int Gap = 6;

        /// <summary>Space between groups of fields, in pixels.</summary>
        public const int GroupGap = 8;

        /// <summary>Vertical space between the rows of a group, in pixels.</summary>
        public const int RowGap = 4;

        /// <summary>Padding inside a group box, in pixels.</summary>
        public const int GroupPadding = 4;

        /// <summary>Padding inside a tab page, in pixels.</summary>
        public const int PagePadding = 6;

        /// <summary>Width of a field's label column (longer labels wrap), in pixels.</summary>
        public const int LabelMinWidth = 92;

        /// <summary>Width of the unit column, enough for "mm", "mm²", "deg" and "%".</summary>
        public const int UnitWidth = 32;

        /// <summary>Width of a decimal number box, in pixels.</summary>
        public const int BoxWidth = 80;

        /// <summary>Width of a box for a whole-number count, in pixels.</summary>
        public const int CountBoxWidth = 56;

        /// <summary>Width of a drop-down for choices and yes/no values, in pixels.</summary>
        public const int ChoiceBoxWidth = 120;

        /// <summary>Narrowest a slider gets, in pixels.</summary>
        public const int SliderMinWidth = 96;

        /// <summary>Allowance for a vertical scroll bar when sizing wrapping text to a scrolling area, in pixels.</summary>
        public const int ScrollbarWidth = 17;

        /// <summary>Widest a slider gets, in pixels.</summary>
        public const int SliderMaxWidth = 260;

        /// <summary>Width of the text column in a small dialog, in pixels.</summary>
        public const int DialogTextWidth = 260;

        /// <summary>True when the window background is dark, so colors should suit a dark theme.</summary>
        public static bool IsDark
        {
            get
            {
                var c = SystemColors.WindowBackground;
                return 0.299f * c.R + 0.587f * c.G + 0.114f * c.B < 0.5f;
            }
        }

        /// <summary>Color for error text, readable on the current background.</summary>
        public static Color ErrorColor => IsDark ? Color.FromArgb(255, 125, 115) : Color.FromArgb(180, 25, 25);

        /// <summary>Color for warning text, readable on the current background.</summary>
        public static Color WarningColor => IsDark ? Color.FromArgb(240, 190, 80) : Color.FromArgb(150, 90, 0);

        /// <summary>Color for informational notes, readable on the current background.</summary>
        public static Color NoteColor => IsDark ? Color.FromArgb(130, 180, 240) : Color.FromArgb(25, 90, 160);

        /// <summary>Color for the text and outlines of a disabled control, clearly dimmer than enabled ones.</summary>
        public static Color DisabledColor => IsDark ? Color.FromArgb(105, 105, 105) : Color.FromArgb(165, 165, 165);

        /// <summary>Color for the tick marks of a disabled slider.</summary>
        public static Color DisabledTickColor => IsDark ? Color.FromArgb(75, 75, 75) : Color.FromArgb(205, 205, 205);

        /// <summary>Fill for the lever of a disabled slider, close to the background.</summary>
        public static Color DisabledFillColor => IsDark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(235, 235, 235);

        /// <summary>Color for faint tick marks and other secondary detail, visible but quiet on the current background.</summary>
        public static Color MutedTickColor => IsDark ? Color.FromArgb(150, 150, 150) : Color.FromArgb(120, 120, 120);

        /// <summary>Height of a slider and its number box, in pixels.</summary>
        public const int SliderHeight = 26;

        /// <summary>Width of a slider's lever, in pixels.</summary>
        public const int LeverWidth = 72;

        /// <summary>Height of a slider's lever, in pixels.</summary>
        public const int LeverHeight = 22;

        /// <summary>Pixels a slider's lever moves for one step.</summary>
        public const int LeverPixelsPerStep = 2;

        /// <summary>Pixels a press on a slider's lever moves before it counts as a drag rather than a click.</summary>
        public const int DragThreshold = 3;

        /// <summary>Distance between the tick marks along a slider's track, in pixels.</summary>
        public const int TickSpacing = 10;

        /// <summary>Half the thickness of a slider's groove and the margin of its end stops, in pixels.</summary>
        public const int TrackInset = 2;

        /// <summary>
        /// Returns the size unchanged. Rhino 8's Eto on Windows (WPF) already works in device-independent units, so
        /// scaling by the screen factor as well would double-scale on a high-DPI screen. Call sites keep using it so
        /// there is one place to change if that ever stops being true.
        /// </summary>
        public static int Scale(int px) => px;
    }
}
