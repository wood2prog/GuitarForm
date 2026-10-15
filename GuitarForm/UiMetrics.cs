using System;
using Eto.Drawing;
using Eto.Forms;

namespace GuitarForm
{
    /// <summary>Layout constants and helpers shared by the panel, so spacing, widths and colours stay consistent.</summary>
    internal static class UiMetrics
    {
        /// <summary>Space between neighbouring controls, in pixels.</summary>
        public const int Gap = 6;

        /// <summary>Space between groups of fields, in pixels.</summary>
        public const int GroupGap = 8;

        /// <summary>Padding inside a tab page, in pixels.</summary>
        public const int PagePadding = 8;

        /// <summary>Narrowest a field's label column gets, in pixels.</summary>
        public const int LabelMinWidth = 130;

        /// <summary>Width of the unit column, enough for "mm", "mm²", "deg" and "%".</summary>
        public const int UnitWidth = 36;

        /// <summary>Width of a decimal number box, in pixels.</summary>
        public const int BoxWidth = 80;

        /// <summary>Width of a box for a whole-number count, in pixels.</summary>
        public const int CountBoxWidth = 56;

        /// <summary>Width of a drop-down for choices and yes/no values, in pixels.</summary>
        public const int ChoiceBoxWidth = 110;

        /// <summary>Narrowest a slider gets, in pixels.</summary>
        public const int SliderMinWidth = 120;

        /// <summary>Widest a slider gets, in pixels.</summary>
        public const int SliderMaxWidth = 260;

        /// <summary>Width of the text column in a small dialog, in pixels.</summary>
        public const int DialogTextWidth = 260;

        /// <summary>True when the window background is dark, so colours should suit a dark theme.</summary>
        public static bool IsDark
        {
            get
            {
                var c = SystemColors.WindowBackground;
                return 0.299f * c.R + 0.587f * c.G + 0.114f * c.B < 0.5f;
            }
        }

        /// <summary>Colour for error text, readable on the current background.</summary>
        public static Color ErrorColor => IsDark ? Color.FromArgb(255, 125, 115) : Color.FromArgb(180, 25, 25);

        /// <summary>Colour for warning text, readable on the current background.</summary>
        public static Color WarningColor => IsDark ? Color.FromArgb(240, 190, 80) : Color.FromArgb(150, 90, 0);

        /// <summary>Colour for informational notes, readable on the current background.</summary>
        public static Color NoteColor => IsDark ? Color.FromArgb(130, 180, 240) : Color.FromArgb(25, 90, 160);

        /// <summary>Colour for faint tick marks and other secondary detail, visible but quiet on the current background.</summary>
        public static Color MutedTickColor => IsDark ? Color.FromArgb(150, 150, 150) : Color.FromArgb(120, 120, 120);

        /// <summary>The screen's scale factor, 1.0 when it can't be read.</summary>
        public static float ScreenScale
        {
            get
            {
                try
                {
                    float scale = Screen.PrimaryScreen?.Scale ?? 1f;
                    return scale > 0 ? scale : 1f;
                }
                catch (Exception)
                {
                    return 1f;
                }
            }
        }

        /// <summary>Scales a size in pixels by the screen's scale factor.</summary>
        public static int Scale(int px) => (int)Math.Round(px * ScreenScale);
    }
}
