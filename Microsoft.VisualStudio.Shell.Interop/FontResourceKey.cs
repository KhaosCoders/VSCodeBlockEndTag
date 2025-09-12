using System;

namespace Microsoft.VisualStudio.Shell.Interop;

internal enum FontResourceKeyType
{
    FontFamily,
    FontSize,
}

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
internal sealed class FontResourceKey
{
    public FontResourceKey(Guid category, FontResourceKeyType keyType)
    {
        Category = category;
        KeyType = keyType;
    }

    public override bool Equals(object obj)
    {
        if (obj is FontResourceKey key)
        {
            return Category == key.Category && KeyType == key.KeyType;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return Category.GetHashCode() ^ (int)KeyType;
    }

    public override string ToString()
    {
        return $"{Category}.{KeyType}";
    }

    public Guid Category { get; }
    public FontResourceKeyType KeyType { get; }
}
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
