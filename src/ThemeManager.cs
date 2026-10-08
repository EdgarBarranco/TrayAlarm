using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TrayAlarm
{
    public enum ThemeMode
    {
        System = 0,
        Light = 1,
        Dark = 2
    }

    public class ThemeColors
    {
        public bool IsDarkMode { get; set; }

        // Window & Panels
        public Color WindowBackground { get; set; }
        public Color CardBackground { get; set; }
        public Color HeaderBackground { get; set; }
        public Color HeaderForeground { get; set; }
        public Color HeaderSecondaryForeground { get; set; }
        public Color HeaderButtonBackground { get; set; }
        public Color HeaderButtonForeground { get; set; }

        // Typography
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color TextMuted { get; set; }

        // Inputs & Form Controls
        public Color InputBackground { get; set; }
        public Color InputForeground { get; set; }
        public Color InputBorder { get; set; }

        // Buttons
        public Color ButtonBackground { get; set; }
        public Color ButtonForeground { get; set; }
        public Color ButtonHoverBackground { get; set; }

        // Accent Colors
        public Color AccentColor { get; set; }
        public Color AccentTextColor { get; set; }
        public Color AccentHoverColor { get; set; }
        public Color DangerColor { get; set; }

        // DataGridView
        public Color GridBackground { get; set; }
        public Color GridRowBackground { get; set; }
        public Color GridRowAlternateBackground { get; set; }
        public Color GridHeaderBackground { get; set; }
        public Color GridHeaderForeground { get; set; }
        public Color GridGridLineColor { get; set; }
        public Color GridSelectionBackground { get; set; }
        public Color GridSelectionForeground { get; set; }
        public Color GridButtonBackground { get; set; }
        public Color GridButtonForeground { get; set; }
        public Color GridDeleteButtonBackground { get; set; }
        public Color GridDeleteButtonForeground { get; set; }

        // StatusStrip & Menus
        public Color StatusStripBackground { get; set; }
        public Color StatusStripForeground { get; set; }
        public Color MenuBackground { get; set; }
        public Color MenuForeground { get; set; }
        public Color MenuBorder { get; set; }
        public Color MenuItemSelected { get; set; }
        public Color MenuItemSelectedText { get; set; }
        public Color MenuSeparator { get; set; }
    }

    public static class ThemeManager
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private static ThemeMode _mode = ThemeMode.System;
        private static ThemeColors _currentTheme;
        private static bool _initialized = false;
        private static bool _cachedSystemDark = false;
        private static Color _cachedAccent = Color.Empty;

        public static event EventHandler ThemeChanged;

        static ThemeManager()
        {
            RebuildTheme();
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            }
            catch { }
        }

        public static ThemeMode Mode
        {
            get { return _mode; }
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    RebuildTheme();
                    RaiseThemeChanged();
                }
            }
        }

        public static ThemeColors CurrentTheme
        {
            get
            {
                if (_currentTheme == null)
                {
                    RebuildTheme();
                }
                return _currentTheme;
            }
        }

        public static void SetThemeMode(ThemeMode mode)
        {
            Mode = mode;
        }

        public static void CheckAndUpdateTheme()
        {
            bool sysDark = IsSystemInDarkMode();
            Color accent = GetSystemAccentColor();

            bool changed = false;
            if (_mode == ThemeMode.System && sysDark != _cachedSystemDark)
            {
                changed = true;
            }
            if (accent != _cachedAccent)
            {
                changed = true;
            }

            if (changed)
            {
                RebuildTheme();
                RaiseThemeChanged();
            }
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            try
            {
                CheckAndUpdateTheme();
            }
            catch { }
        }

        private static void RaiseThemeChanged()
        {
            EventHandler handler = ThemeChanged;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        public static void RebuildTheme()
        {
            _cachedSystemDark = IsSystemInDarkMode();
            _cachedAccent = GetSystemAccentColor();

            bool useDark;
            switch (_mode)
            {
                case ThemeMode.Dark:
                    useDark = true;
                    break;
                case ThemeMode.Light:
                    useDark = false;
                    break;
                case ThemeMode.System:
                default:
                    useDark = _cachedSystemDark;
                    break;
            }

            _currentTheme = CreatePalette(useDark, _cachedAccent);
        }

        public static bool IsSystemInDarkMode()
        {
            try
            {
                object appsUseLight = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", null);
                if (appsUseLight != null)
                {
                    return Convert.ToInt32(appsUseLight) == 0;
                }

                object sysUseLight = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", null);
                if (sysUseLight != null)
                {
                    return Convert.ToInt32(sysUseLight) == 0;
                }
            }
            catch { }

            // Fallback: check SystemColors.Window brightness
            return SystemColors.Window.GetBrightness() < 0.5f;
        }

        public static Color GetSystemAccentColor()
        {
            try
            {
                // Check HKCU\Software\Microsoft\Windows\DWM -> AccentColor (ABGR DWORD)
                object accentObj = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "AccentColor", null);
                if (accentObj != null)
                {
                    uint val = 0;
                    if (accentObj is int) val = unchecked((uint)(int)accentObj);
                    else if (accentObj is long) val = unchecked((uint)(long)accentObj);
                    else val = Convert.ToUInt32(accentObj);

                    if (val != 0)
                    {
                        byte r = (byte)(val & 0xFF);
                        byte g = (byte)((val >> 8) & 0xFF);
                        byte b = (byte)((val >> 16) & 0xFF);
                        if (r > 10 || g > 10 || b > 10)
                        {
                            return Color.FromArgb(255, r, g, b);
                        }
                    }
                }

                // Check HKCU\Software\Microsoft\Windows\DWM -> ColorizationColor (ARGB DWORD)
                object colorObj = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColor", null);
                if (colorObj != null)
                {
                    uint val = 0;
                    if (colorObj is int) val = unchecked((uint)(int)colorObj);
                    else if (colorObj is long) val = unchecked((uint)(long)colorObj);
                    else val = Convert.ToUInt32(colorObj);

                    if (val != 0)
                    {
                        byte r = (byte)((val >> 16) & 0xFF);
                        byte g = (byte)((val >> 8) & 0xFF);
                        byte b = (byte)(val & 0xFF);
                        if (r > 10 || g > 10 || b > 10)
                        {
                            return Color.FromArgb(255, r, g, b);
                        }
                    }
                }
            }
            catch { }

            if (SystemColors.Highlight.A > 0 && (SystemColors.Highlight.R > 20 || SystemColors.Highlight.G > 20 || SystemColors.Highlight.B > 20))
            {
                return SystemColors.Highlight;
            }

            return Color.FromArgb(0, 120, 212); // Standard Windows Accent Blue (#0078D4)
        }

        public static double CalculateLuminance(Color color)
        {
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        }

        public static Color AdjustBrightness(Color color, float factor)
        {
            int r = Math.Min(255, Math.Max(0, (int)(color.R * factor)));
            int g = Math.Min(255, Math.Max(0, (int)(color.G * factor)));
            int b = Math.Min(255, Math.Max(0, (int)(color.B * factor)));
            return Color.FromArgb(color.A, r, g, b);
        }

        public static void ApplyImmersiveDarkMode(IntPtr handle, bool isDarkMode)
        {
            if (handle == IntPtr.Zero) return;
            try
            {
                int darkMode = isDarkMode ? 1 : 0;
                // Attribute 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 10 build 19041+ & Windows 11)
                int hr = DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));
                if (hr != 0)
                {
                    // Attribute 19 = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 (Windows 10 1809 - 1909)
                    DwmSetWindowAttribute(handle, 19, ref darkMode, sizeof(int));
                }
            }
            catch { }
        }

        public static ThemeColors CreatePalette(bool isDarkMode, Color accent)
        {
            var colors = new ThemeColors();
            colors.IsDarkMode = isDarkMode;

            // Ensure accent has contrast
            double accentLum = CalculateLuminance(accent);
            colors.AccentColor = accent;
            colors.AccentTextColor = accentLum > 0.65 ? Color.FromArgb(15, 23, 42) : Color.White;
            colors.AccentHoverColor = isDarkMode ? AdjustBrightness(accent, 1.15f) : AdjustBrightness(accent, 0.90f);
            colors.DangerColor = Color.FromArgb(220, 38, 38);

            if (isDarkMode)
            {
                // Dark Mode Palette (native Windows 10/11 dark aesthetics)
                colors.WindowBackground = Color.FromArgb(32, 32, 32);
                colors.CardBackground = Color.FromArgb(43, 43, 43);
                colors.HeaderBackground = Color.FromArgb(24, 24, 24);
                colors.HeaderForeground = Color.FromArgb(245, 245, 245);
                colors.HeaderSecondaryForeground = Color.FromArgb(180, 185, 195);
                colors.HeaderButtonBackground = Color.FromArgb(55, 58, 64);
                colors.HeaderButtonForeground = Color.FromArgb(240, 240, 240);

                colors.TextPrimary = Color.FromArgb(245, 245, 245);
                colors.TextSecondary = Color.FromArgb(170, 175, 185);
                colors.TextMuted = Color.FromArgb(120, 125, 135);

                colors.InputBackground = Color.FromArgb(45, 45, 48);
                colors.InputForeground = Color.FromArgb(250, 250, 250);
                colors.InputBorder = Color.FromArgb(70, 70, 75);

                colors.ButtonBackground = Color.FromArgb(50, 52, 58);
                colors.ButtonForeground = Color.FromArgb(235, 235, 240);
                colors.ButtonHoverBackground = Color.FromArgb(65, 68, 75);

                colors.GridBackground = Color.FromArgb(28, 28, 30);
                colors.GridRowBackground = Color.FromArgb(32, 32, 35);
                colors.GridRowAlternateBackground = Color.FromArgb(38, 38, 42);
                colors.GridHeaderBackground = Color.FromArgb(46, 48, 54);
                colors.GridHeaderForeground = Color.FromArgb(225, 230, 240);
                colors.GridGridLineColor = Color.FromArgb(55, 55, 60);
                colors.GridSelectionBackground = accentLum > 0.2 && accentLum < 0.7 ? accent : Color.FromArgb(14, 99, 156);
                colors.GridSelectionForeground = Color.White;
                colors.GridButtonBackground = Color.FromArgb(55, 58, 64);
                colors.GridButtonForeground = Color.FromArgb(235, 235, 240);
                colors.GridDeleteButtonBackground = Color.FromArgb(65, 25, 25);
                colors.GridDeleteButtonForeground = Color.FromArgb(252, 165, 165);

                colors.StatusStripBackground = Color.FromArgb(24, 24, 26);
                colors.StatusStripForeground = Color.FromArgb(180, 185, 195);
                colors.MenuBackground = Color.FromArgb(40, 40, 43);
                colors.MenuForeground = Color.FromArgb(240, 240, 240);
                colors.MenuBorder = Color.FromArgb(65, 65, 70);
                colors.MenuItemSelected = Color.FromArgb(60, 62, 68);
                colors.MenuItemSelectedText = Color.White;
                colors.MenuSeparator = Color.FromArgb(60, 60, 65);
            }
            else
            {
                // Light Mode Palette (System-aligned light aesthetics)
                colors.WindowBackground = SystemColors.Control;
                colors.CardBackground = Color.White;
                colors.HeaderBackground = Color.FromArgb(30, 41, 59);
                colors.HeaderForeground = Color.White;
                colors.HeaderSecondaryForeground = Color.FromArgb(203, 213, 225);
                colors.HeaderButtonBackground = Color.FromArgb(51, 65, 85);
                colors.HeaderButtonForeground = Color.White;

                colors.TextPrimary = Color.FromArgb(15, 23, 42);
                colors.TextSecondary = Color.FromArgb(71, 85, 105);
                colors.TextMuted = Color.FromArgb(148, 163, 184);

                colors.InputBackground = SystemColors.Window;
                colors.InputForeground = SystemColors.WindowText;
                colors.InputBorder = Color.FromArgb(203, 213, 225);

                colors.ButtonBackground = Color.FromArgb(241, 245, 249);
                colors.ButtonForeground = Color.FromArgb(51, 65, 85);
                colors.ButtonHoverBackground = Color.FromArgb(226, 232, 240);

                colors.GridBackground = Color.White;
                colors.GridRowBackground = Color.White;
                colors.GridRowAlternateBackground = Color.FromArgb(248, 250, 252);
                colors.GridHeaderBackground = Color.FromArgb(241, 245, 249);
                colors.GridHeaderForeground = Color.FromArgb(51, 65, 85);
                colors.GridGridLineColor = Color.FromArgb(226, 232, 240);
                colors.GridSelectionBackground = accentLum > 0.2 && accentLum < 0.7 ? accent : SystemColors.Highlight;
                colors.GridSelectionForeground = Color.White;
                colors.GridButtonBackground = Color.FromArgb(241, 245, 249);
                colors.GridButtonForeground = Color.FromArgb(51, 65, 85);
                colors.GridDeleteButtonBackground = Color.FromArgb(254, 226, 226);
                colors.GridDeleteButtonForeground = Color.FromArgb(220, 38, 38);

                colors.StatusStripBackground = Color.FromArgb(241, 245, 249);
                colors.StatusStripForeground = Color.FromArgb(71, 85, 105);
                colors.MenuBackground = Color.FromArgb(250, 250, 250);
                colors.MenuForeground = Color.FromArgb(30, 41, 59);
                colors.MenuBorder = Color.FromArgb(203, 213, 225);
                colors.MenuItemSelected = Color.FromArgb(226, 232, 240);
                colors.MenuItemSelectedText = Color.FromArgb(15, 23, 42);
                colors.MenuSeparator = Color.FromArgb(226, 232, 240);
            }

            return colors;
        }
    }

    public class ThemeColorTable : ProfessionalColorTable
    {
        private readonly ThemeColors _colors;

        public ThemeColorTable(ThemeColors colors)
        {
            _colors = colors;
            this.UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground
        {
            get { return _colors.MenuBackground; }
        }

        public override Color MenuBorder
        {
            get { return _colors.MenuBorder; }
        }

        public override Color MenuItemBorder
        {
            get { return Color.Transparent; }
        }

        public override Color MenuItemSelected
        {
            get { return _colors.MenuItemSelected; }
        }

        public override Color MenuItemSelectedGradientBegin
        {
            get { return _colors.MenuItemSelected; }
        }

        public override Color MenuItemSelectedGradientEnd
        {
            get { return _colors.MenuItemSelected; }
        }

        public override Color MenuStripGradientBegin
        {
            get { return _colors.StatusStripBackground; }
        }

        public override Color MenuStripGradientEnd
        {
            get { return _colors.StatusStripBackground; }
        }

        public override Color StatusStripGradientBegin
        {
            get { return _colors.StatusStripBackground; }
        }

        public override Color StatusStripGradientEnd
        {
            get { return _colors.StatusStripBackground; }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return _colors.MenuBackground; }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return _colors.MenuBackground; }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return _colors.MenuBackground; }
        }

        public override Color SeparatorDark
        {
            get { return _colors.MenuSeparator; }
        }

        public override Color SeparatorLight
        {
            get { return Color.Transparent; }
        }

        public override Color CheckBackground
        {
            get { return _colors.AccentColor; }
        }

        public override Color CheckSelectedBackground
        {
            get { return _colors.AccentColor; }
        }

        public override Color CheckPressedBackground
        {
            get { return _colors.AccentColor; }
        }

        public override Color ButtonSelectedHighlight
        {
            get { return _colors.MenuItemSelected; }
        }

        public override Color ButtonSelectedHighlightBorder
        {
            get { return _colors.InputBorder; }
        }

        public override Color ButtonPressedHighlight
        {
            get { return _colors.MenuItemSelected; }
        }

        public override Color ButtonPressedHighlightBorder
        {
            get { return _colors.InputBorder; }
        }

        public override Color ButtonCheckedHighlight
        {
            get { return _colors.AccentColor; }
        }

        public override Color ButtonCheckedHighlightBorder
        {
            get { return _colors.AccentColor; }
        }
    }

    public class ThemeToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemeColors _colors;

        public ThemeToolStripRenderer(ThemeColors colors)
            : base(new ThemeColorTable(colors))
        {
            _colors = colors;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.ToolStrip is StatusStrip)
            {
                e.TextColor = _colors.StatusStripForeground;
            }
            else if (!e.Item.Enabled)
            {
                e.TextColor = _colors.TextMuted;
            }
            else
            {
                e.TextColor = e.Item.Selected ? _colors.MenuItemSelectedText : _colors.MenuForeground;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item.Selected ? _colors.MenuItemSelectedText : _colors.MenuForeground;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle rect = new Rectangle(e.ImageRectangle.X - 2, e.ImageRectangle.Y - 2, e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4);
            using (var brush = new SolidBrush(_colors.AccentColor))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
            using (var pen = new Pen(_colors.AccentTextColor, 2f))
            {
                int x = rect.X + 3;
                int y = rect.Y + 6;
                e.Graphics.DrawLines(pen, new Point[] {
                    new Point(x, y + 2),
                    new Point(x + 3, y + 5),
                    new Point(x + 8, y)
                });
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                using (var pen = new Pen(_colors.MenuBorder))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                }
            }
            else
            {
                base.OnRenderToolStripBorder(e);
            }
        }
    }
}
