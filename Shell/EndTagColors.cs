using System;
using CodeBlockEndTag.Model;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeBlockEndTag.Shell;

internal static class EndTagColors
{
    static EndTagColors()
    {
        var category = Guid.Parse(FontAndColorDefaultsCSharpTags.CategoryGuidString);

        FontFamilyKey = new FontResourceKey(category, FontResourceKeyType.FontFamily);
        FontSizeKey = new FontResourceKey(category, FontResourceKeyType.FontSize);
        CSharpEndTagForegroundBrushKey = new ThemeResourceKey(category, FontAndColorDefaultsCSharpTags.EntryNames.CSharpEndTag, ThemeResourceKeyType.ForegroundBrush);
    }

    public static ThemeResourceKey GetForegroundResourceKey(string lang)
    {
        return lang switch
        {
            Languages.CSharp => CSharpEndTagForegroundBrushKey,
            _ => CSharpEndTagForegroundBrushKey
        };
    }

    public static FontResourceKey FontFamilyKey { get; }

    public static FontResourceKey FontSizeKey { get; }

    public static ThemeResourceKey CSharpEndTagForegroundBrushKey { get; }
}
