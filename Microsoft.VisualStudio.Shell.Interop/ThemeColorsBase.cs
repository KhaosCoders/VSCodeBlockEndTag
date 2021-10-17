using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.VisualStudio.Shell.Interop
{
    abstract class ThemeColorsBase
    {
        #region ColorEntryFlags
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
            readonly ThemeColorsBase m_parent;

            public ColorEntry(ThemeColorsBase parent)
            {
                m_parent = parent ?? throw new ArgumentNullException(nameof(parent));
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                FallbackBackground = Enumerable.Empty<VsColor>();
                FallbackForeground = Enumerable.Empty<VsColor>();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            }

            public string Name { get; protected set; }
            public ColorUsage Usage { get; protected set; }
            public IEnumerable<VsColor> FallbackBackground { get; protected set; }
            public IEnumerable<VsColor> FallbackForeground { get; protected set; }

            private Color GetBackgroundColor()
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Services.TryGetThemeColor(m_parent.CategoryGuid, Name, __THEMEDCOLORTYPE.TCT_Background, out var result))
                {
                    return Services.CreateWpfColor(result);
                }

                if (VsColor.TryGetValue(FallbackBackground, out result))
                {
                    return Services.CreateWpfColor(result);
                }

                return Services.CreateWpfColor(0);
            }

            private Color GetForegroundColor()
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Services.TryGetThemeColor(m_parent.CategoryGuid, Name, __THEMEDCOLORTYPE.TCT_Foreground, out var result))
                {
                    return Services.CreateWpfColor(result);
                }

                if (VsColor.TryGetValue(FallbackForeground, out result))
                {
                    return Services.CreateWpfColor(result);
                }

                return Services.CreateWpfColor(0);
            }

            public void ReloadColors(ResourceDictionary resources)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if ((Usage & ColorUsage.Background) == ColorUsage.Background)
                {
                    var color = GetBackgroundColor();
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    var key = new ThemeResourceKey(m_parent.CategoryGuid, Name, ThemeResourceKeyType.BackgroundBrush);
                    resources[key] = brush;
                }

                if ((Usage & ColorUsage.Foreground) == ColorUsage.Foreground)
                {
                    var color = GetForegroundColor();
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    var key = new ThemeResourceKey(m_parent.CategoryGuid, Name, ThemeResourceKeyType.ForegroundBrush);
                    resources[key] = brush;
                }
            }
        }
        #endregion

        private readonly ResourceDictionary _resourceDictionary;

        protected ThemeColorsBase()
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
                ReloadColors();
            }
        }

        public void ReloadColors()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _fontInfoInitialized = true;

            foreach (var colorEntry in ColorEntries)
            {
                colorEntry.ReloadColors(_resourceDictionary);
            }
        }

        protected Guid CategoryGuid { private get; set; }
        protected IReadOnlyList<ColorEntry> ColorEntries { private get; set; }
    }
}