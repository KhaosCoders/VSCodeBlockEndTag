using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeBlockEndTag.Shell;

internal class FontAndColorDefaultsCSharpTags : FontAndColorDefaultsBase
{
    public const string CategoryGuidString = "2A178C76-9E0A-4158-B583-47DD0649F10F";
    public const string CategoryNameString = "CodeBlockEndTag";

    #region color entries

    internal static class EntryNames
    {
        public const string CSharpEndTag = "CSharpEndTag";
    }

    sealed class CSharpEndTagEntry : ColorEntry
    {
        public CSharpEndTagEntry(FontAndColorDefaultsBase parent) : base(parent)
        {
            Name = EntryNames.CSharpEndTag;
            LocalizedName = "C# end tag";
            Usage = ColorUsage.Foreground;
            DefaultForeground = new[] { new RgbColor(0x99, 0x99, 0x99) };
        }
    }

    #endregion

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
    public FontAndColorDefaultsCSharpTags()
    {
        CategoryGuid = new Guid(CategoryGuidString);
        CategoryName = CategoryNameString;
        Font = CreateFontInfo("Consolas", 9, 1);
        ColorEntries = new ColorEntry[] {
            new CSharpEndTagEntry(this),
        };
    }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

    public static FontAndColorDefaultsCSharpTags Instance { get; private set; }

    public static void EnsureInstance()
    {
        Instance ??= new FontAndColorDefaultsCSharpTags();
    }
}
