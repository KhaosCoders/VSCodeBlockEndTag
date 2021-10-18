using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal abstract class FontAndColorDefaultsBase : IVsFontAndColorDefaults, IVsFontAndColorEvents
    {
        #region ColorUsage
        [Flags]
        protected enum ColorUsage
        {
            Background = 1,
            Foreground = 2,
        }
        #endregion
        #region ColorEntry
        protected abstract class ColorEntry
        {
            readonly FontAndColorDefaultsBase _parent;

            public ColorEntry(FontAndColorDefaultsBase parent)
            {
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));

                DefaultBackground = Enumerable.Empty<VsColor>();
                DefaultForeground = Enumerable.Empty<VsColor>();
            }

            public string Name { get; protected set; }
            public string LocalizedName { get; protected set; }
            public string Description { get; protected set; }
            public ColorUsage Usage { get; protected set; }
            public IEnumerable<VsColor> DefaultBackground { get; protected set; }
            public IEnumerable<VsColor> DefaultForeground { get; protected set; }

            private bool TryGetDefaultBackground(out uint result)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return Services.TryGetThemeColor(_parent.CategoryGuid, Name, __THEMEDCOLORTYPE.TCT_Background, out result) || VsColor.TryGetValue(DefaultBackground, out result);
            }

            private bool TryGetDefaultForeground(out uint result)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return Services.TryGetThemeColor(_parent.CategoryGuid, Name, __THEMEDCOLORTYPE.TCT_Foreground, out result) || VsColor.TryGetValue(DefaultForeground, out result);
            }

            public AllColorableItemInfo CreateColorInfo()
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (!TryGetDefaultBackground(out var defaultBackgroundColor))
                {
                    defaultBackgroundColor = 0;
                }
                if (!TryGetDefaultForeground(out var defaultForegroundColor))
                {
                    defaultForegroundColor = 0;
                }

                var flags = __FCITEMFLAGS.FCIF_ALLOWCUSTOMCOLORS;
                if ((Usage & ColorUsage.Background) == ColorUsage.Background)
                {
                    flags |= __FCITEMFLAGS.FCIF_ALLOWBGCHANGE;
                }
                if ((Usage & ColorUsage.Foreground) == ColorUsage.Foreground)
                {
                    flags |= __FCITEMFLAGS.FCIF_ALLOWFGCHANGE;
                }

                return new AllColorableItemInfo
                {
                    Info = new ColorableItemInfo
                    {
                        crBackground = 0x2000000,
                        bBackgroundValid = 1,
                        crForeground = 0x2000000,
                        bForegroundValid = 1,
                        dwFontFlags = 0,
                        bFontFlagsValid = 1
                    },
                    bstrName = Name,
                    bNameValid = (Name != null) ? 1 : 0,
                    bstrLocalizedName = LocalizedName,
                    bLocalizedNameValid = (LocalizedName != null) ? 1 : 0,
                    crAutoBackground = defaultBackgroundColor,
                    bAutoBackgroundValid = 1,
                    crAutoForeground = defaultForegroundColor,
                    bAutoForegroundValid = 1,
                    dwMarkerVisualStyle = 0,
                    bMarkerVisualStyleValid = 1,
                    eLineStyle = LINESTYLE.LI_NONE,
                    bLineStyleValid = 1,
                    fFlags = (uint)flags,
                    bFlagsValid = 1,
                    bstrDescription = Description,
                    bDescriptionValid = (Description != null) ? 1 : 0,
                };
            }

            public void UpdateResources(ResourceDictionary resources, Color? backgroundColor, Color? foregroundColor)
            {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                if ((Usage & ColorUsage.Background) == ColorUsage.Background)
                {
                    var key = new ThemeResourceKey(_parent.CategoryGuid, Name, ThemeResourceKeyType.BackgroundBrush);
                    if (backgroundColor.HasValue)
                    {
                        var brush = new SolidColorBrush(backgroundColor.Value);
                        brush.Freeze();
                        resources[key] = brush;
                    }
                    else
                    {
                        resources.Remove(key);
                    }
                }

                if ((Usage & ColorUsage.Foreground) == ColorUsage.Foreground)
                {
                    var key = new ThemeResourceKey(_parent.CategoryGuid, Name, ThemeResourceKeyType.ForegroundBrush);
                    if (foregroundColor.HasValue)
                    {
                        var brush = new SolidColorBrush(foregroundColor.Value);
                        brush.Freeze();
                        resources[key] = brush;
                    }
                    else
                    {
                        resources.Remove(key);
                    }
                }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            }
        }
        #endregion

        private readonly ResourceDictionary _resourceDictionary;

        protected FontAndColorDefaultsBase()
        {
            _resourceDictionary = new ResourceDictionary();
            Application.Current.Resources.MergedDictionaries.Add(_resourceDictionary);
        }

        private bool _fontInfoInitialized;

        public void EnsureFontAndColorsInitialized()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!_fontInfoInitialized)
            {
                ReloadFontAndColors();
            }
        }

        public void ReloadFontAndColors()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _fontInfoInitialized = true;

            #region fill out temorary resources
            var colorStorage = FontAndColorStorage;
            var categoryGuid = CategoryGuid;
            var fflags =
                (uint)__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS |
                (uint)__FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS |
                (uint)__FCSTORAGEFLAGS.FCSF_READONLY;

            if (!ErrorHandler.Succeeded(colorStorage.OpenCategory(ref categoryGuid, fflags))) return;

            try
            {
                var pLOGFONT = new LOGFONTW[1];
                var pFontInfo = new FontInfo[1];
                if (ErrorHandler.Succeeded(colorStorage.GetFont(pLOGFONT, pFontInfo)) && (pFontInfo[0].bFaceNameValid == 1))
                {
                    var fontInfoRef = pFontInfo[0];
                    var fontFamily = new FontFamily(fontInfoRef.bstrFaceName);
                    var fontSize = ConvertFromPoint(fontInfoRef.wPointSize);

                    _resourceDictionary[new FontResourceKey(CategoryGuid, FontResourceKeyType.FontFamily)] = fontFamily;
                    _resourceDictionary[new FontResourceKey(CategoryGuid, FontResourceKeyType.FontSize)] = fontSize;
                }
                else
                {
                    _resourceDictionary.Remove(new FontResourceKey(CategoryGuid, FontResourceKeyType.FontFamily));
                    _resourceDictionary.Remove(new FontResourceKey(CategoryGuid, FontResourceKeyType.FontSize));
                }

                foreach (var colorEntry in ColorEntries)
                {
                    Color? backgroundColor = null;
                    Color? foregroundColor = null;

                    var pColorInfo = new ColorableItemInfo[1];
                    if (ErrorHandler.Succeeded(colorStorage.GetItem(colorEntry.Name, pColorInfo)))
                    {
                        if (pColorInfo[0].bBackgroundValid == 1)
                        {
                            backgroundColor = Services.CreateWpfColor(pColorInfo[0].crBackground);
                        }
                        if (pColorInfo[0].bForegroundValid == 1)
                        {
                            foregroundColor = Services.CreateWpfColor(pColorInfo[0].crForeground);
                        }
                    }

                    colorEntry.UpdateResources(_resourceDictionary, backgroundColor, foregroundColor);
                }
            }
            finally
            {
                colorStorage.CloseCategory();
            }
            #endregion
        }

        protected internal Guid CategoryGuid { get; protected set; }
        protected string CategoryName { private get; set; }
        protected FontInfo Font { private get; set; }
        protected IReadOnlyList<ColorEntry> ColorEntries { private get; set; }

        #region IVsFontAndColorDefaults
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        int IVsFontAndColorDefaults.GetBaseCategory(out Guid guidBase)
        {
            guidBase = Guid.Empty;
            return 0;
        }

        int IVsFontAndColorDefaults.GetCategoryName(out string stringName)
        {
            stringName = CategoryName;
            return 0;
        }

        int IVsFontAndColorDefaults.GetFlags(out uint flags)
        {
            flags = (int)(__FONTCOLORFLAGS.FCF_ONLYTTFONTS | __FONTCOLORFLAGS.FCF_SAVEALL);
            return 0;
        }

        int IVsFontAndColorDefaults.GetFont(FontInfo[] info)
        {
            info[0] = Font;
            return 0;
        }

        int IVsFontAndColorDefaults.GetItem(int index, AllColorableItemInfo[] info)
        {
            if (0 > index || index >= ColorEntries.Count) return -2147467259;

            info[0] = ColorEntries[index].CreateColorInfo();
            return 0;
        }

        int IVsFontAndColorDefaults.GetItemByName(string name, AllColorableItemInfo[] info)
        {
            foreach (var entry in ColorEntries)
            {
                if (entry.Name != name) continue;

                info[0] = entry.CreateColorInfo();
                return 0;
            }
            return -2147467259;
        }

        int IVsFontAndColorDefaults.GetItemCount(out int items)
        {
            items = ColorEntries.Count;
            return 0;
        }

        int IVsFontAndColorDefaults.GetPriority(out ushort priority)
        {
            priority = 0x100;
            return 0;
        }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        #endregion

        #region IVsFontAndColorEvents
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        int IVsFontAndColorEvents.OnApply()
        {
            ReloadFontAndColors();
            return 0;
        }

        public int OnFontChanged(ref Guid rguidCategory, FontInfo[] pInfo, LOGFONTW[] pLOGFONT, uint HFONT)
        {
            return 0;
        }

        int IVsFontAndColorEvents.OnItemChanged(ref Guid category, string name, int item, ColorableItemInfo[] info, uint forground, uint background)
        {
            return 0;
        }

        int IVsFontAndColorEvents.OnReset(ref Guid category)
        {
            return 0;
        }

        int IVsFontAndColorEvents.OnResetToBaseCategory(ref Guid category)
        {
            return 0;
        }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        #endregion

        #region static helpers
        protected static FontInfo CreateFontInfo(string fontFaceName, ushort pointSize, byte charSet)
        {
            return new FontInfo
            {
                bstrFaceName = fontFaceName,
                wPointSize = pointSize,
                iCharSet = charSet,
                bFaceNameValid = 1,
                bPointSizeValid = 1,
                bCharSetValid = 1
            };
        }

        internal static double ConvertFromPoint(ushort pointSize)
        {
            return 96.0 * pointSize / 72;
        }
        #endregion

        private IVsFontAndColorStorage _fontAndColorStorage;

        protected IVsFontAndColorStorage FontAndColorStorage
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return _fontAndColorStorage ??= Package.GetGlobalService(typeof(SVsFontAndColorStorage)) as IVsFontAndColorStorage;
            }
        }
    }
}