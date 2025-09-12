using CodeBlockEndTag;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.Windows.Media;

namespace Microsoft.VisualStudio.Shell.Interop;

static class Services
{
    private static Type _colorNameType;

    public static bool TryGetThemeColor(Guid colorCategory, string colorName, __THEMEDCOLORTYPE colorType, out uint result)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            if (colorName == null)
            {
                throw new ArgumentNullException(nameof(colorName));
            }

            var currentTheme = ColorThemeService?.CurrentTheme;
            _colorNameType ??= (currentTheme.GetType() as Type).GetMethod("get_Item").GetParameters()[0]
                .ParameterType;

            dynamic colorNameInst = Activator.CreateInstance(_colorNameType);
            colorNameInst.Category = colorCategory;
            colorNameInst.Name = colorName;

            var entry = currentTheme?[colorNameInst];
            if (entry == null)
            {
                result = 0;
                return false;
            }

            switch (colorType)
            {
                case __THEMEDCOLORTYPE.TCT_Background:
                    result = entry.Background;
                    return true;
                case __THEMEDCOLORTYPE.TCT_Foreground:
                    result = entry.Foreground;
                    return true;
                default:
                    throw new InvalidEnumArgumentException(nameof(colorType), (int)colorType,
                        typeof(__THEMEDCOLORTYPE));
            }
        }
        catch (Exception e)
        {
            CBETagPackage.Instance.Log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "CBETagger.Services", $"TryGetThemeColor Error: {e}");
            throw;
        }
    }

    public static Color CreateWpfColor(uint dwRgbValue)
    {
        var color = System.Drawing.ColorTranslator.FromWin32((int)dwRgbValue);
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static IVsUIShell2 VsUIShell2
    {
        get
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return field ??= Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell2;
        }
    }

    public static IVsUIShell5 VsUIShell5
    {
        get
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return field ??= Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell5;
        }
    }

    public static IVsFontAndColorUtilities FontAndColorUtilities
    {
        get
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return field ??= Package.GetGlobalService(typeof(SVsFontAndColorStorage)) as IVsFontAndColorUtilities;
        }
    }

    public static dynamic ColorThemeService
    {
        get
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return field ??= Package.GetGlobalService(typeof(SVsColorThemeService));
        }
    }
}
