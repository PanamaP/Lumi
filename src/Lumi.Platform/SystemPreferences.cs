using System.Runtime.InteropServices;
using Lumi.Core;

namespace Lumi.Platform;

/// <summary>
/// Detects OS-level display preferences (dark mode, high contrast, accent color).
/// </summary>
public class SystemPreferences
{
    public bool IsDarkMode { get; private set; }
    public bool IsHighContrast { get; private set; }
    public Color? AccentColor { get; private set; }

    /// <summary>
    /// Reads current OS preferences. Returns defaults on unsupported platforms.
    /// </summary>
    public void Detect()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DetectWindows();
            }
            else
            {
                // Non-Windows: defaults (dark=false, high-contrast=false)
                IsDarkMode = false;
                IsHighContrast = false;
                AccentColor = null;
            }
        }
        catch
        {
            // Graceful fallback on any failure
            IsDarkMode = false;
            IsHighContrast = false;
            AccentColor = null;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private void DetectWindows()
    {
        // Dark mode: AppsUseLightTheme = 0 means dark mode
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intVal)
                    IsDarkMode = intVal == 0;
            }
        }
        catch
        {
            IsDarkMode = false;
        }

        // High contrast detection via SystemParametersInfo
        try
        {
            var hc = new HIGHCONTRAST();
            hc.cbSize = (uint)Marshal.SizeOf<HIGHCONTRAST>();
            bool result = SystemParametersInfo(SPI_GETHIGHCONTRAST, hc.cbSize, ref hc, 0);
            IsHighContrast = result && (hc.dwFlags & HCF_HIGHCONTRASTON) != 0;
        }
        catch
        {
            IsHighContrast = false;
        }

        // Accent color from registry
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            if (key != null)
            {
                var value = key.GetValue("AccentColor");
                if (value is int colorVal)
                {
                    // ABGR format in registry
                    byte a = (byte)((colorVal >> 24) & 0xFF);
                    byte b = (byte)((colorVal >> 16) & 0xFF);
                    byte g = (byte)((colorVal >> 8) & 0xFF);
                    byte r = (byte)(colorVal & 0xFF);
                    AccentColor = new Color(r, g, b, a);
                }
            }
        }
        catch
        {
            AccentColor = null;
        }
    }

    // P/Invoke for high contrast detection
    private const uint SPI_GETHIGHCONTRAST = 0x0042;
    private const uint HCF_HIGHCONTRASTON = 0x00000001;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct HIGHCONTRAST
    {
        public uint cbSize;
        public uint dwFlags;
        public IntPtr lpszDefaultScheme;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
        ref HIGHCONTRAST pvParam, uint fWinIni);
}
