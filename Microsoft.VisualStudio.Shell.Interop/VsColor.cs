using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal abstract class VsColor
    {
        public abstract bool TryGetValue(out uint result);

        public static bool TryGetValue(IEnumerable<VsColor> colors, out uint result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (colors == null)
            {
                throw new ArgumentNullException(nameof(colors));
            }

            foreach (var color in colors)
            {
                if (color.TryGetValue(out result))
                {
                    return true;
                }
            }

            result = 0;
            return false;
        }
    }

    internal sealed class AutoColor : VsColor
    {
        public override bool TryGetValue(out uint result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return ErrorHandler.Succeeded(Services.FontAndColorUtilities.EncodeAutomaticColor(out result));
        }
    }

    internal sealed class IndexedColor : VsColor
    {
        private readonly COLORINDEX _index;

        public IndexedColor(COLORINDEX index)
        {
            _index = index;
        }
        public override bool TryGetValue(out uint result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return ErrorHandler.Succeeded(Services.FontAndColorUtilities.GetRGBOfIndex(_index, out result));
        }
    }

    internal sealed class RgbColor : VsColor
    {
        private readonly uint _value;

        public RgbColor(byte r, byte b, byte g)
        {
            _value = (uint)(r | (g << 8) | (b << 16));
        }
        public override bool TryGetValue(out uint result)
        {
            result = _value;
            return true;
        }
    }

    internal sealed class SysColor : VsColor
    {
        private readonly int _index;

        public SysColor(int index)
        {
            _index = index;
        }
        public override bool TryGetValue(out uint result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            result = 0xff000000 | (uint)SafeNativeMethods.GetSysColor(_index);
            return true;
        }
    }

    internal sealed class VsSysColor : VsColor
    {
        private readonly int _index;

        public VsSysColor(__VSSYSCOLOREX index)
        {
            _index = (int)index;
        }
        public VsSysColor(__VSSYSCOLOREX2 index)
        {
            _index = (int)index;
        }
        public VsSysColor(__VSSYSCOLOREX3 index)
        {
            _index = (int)index;
        }
        public override bool TryGetValue(out uint result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return ErrorHandler.Succeeded(Services.VsUIShell2.GetVSSysColorEx(_index, out result));
        }
    }

    internal sealed class ThemeColor : VsColor
    {
        private readonly Guid _colorCategory;
        private readonly string _colorName;
        private readonly __THEMEDCOLORTYPE _colorType;

        public ThemeColor(ThemeResourceKey key)
        {
            _colorCategory = key.Category;
            _colorName = key.Name;
            _colorType = key.KeyType is ThemeResourceKeyType.BackgroundBrush or ThemeResourceKeyType.BackgroundColor ? __THEMEDCOLORTYPE.TCT_Background : __THEMEDCOLORTYPE.TCT_Foreground;
        }
        public override bool TryGetValue(out uint result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return Services.TryGetThemeColor(_colorCategory, _colorName, _colorType, out result);
        }
    }
}