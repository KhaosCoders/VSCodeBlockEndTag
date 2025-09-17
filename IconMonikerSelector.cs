//
// For a complete list of all KnownMonikers see
// http://glyphlist.azurewebsites.net/knownmonikers/
//

using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System;

namespace CodeBlockEndTag;

/// <summary>
/// Provides functionality to select appropriate Visual Studio icon monikers based on code header text.
/// This class analyzes code constructs and returns corresponding Visual Studio icons for display purposes.
/// </summary>
internal sealed class IconMonikerSelector
{
    #region Modifier Constants

    private const string ModifierPublic = "public";
    private const string ModifierPrivate = "private";
    private const string ModifierProtected = "protected";
    private const string ModifierInternal = "internal";
    private const string ModifierSealed = "sealed";
    private const string ModifierPartial = "partial";
    private const string ModifierStatic = "static";
    private const string ModifierConst = "const";
    private const string ModifierReadonly = "readonly";
    private const string ModifierExplicit = "explicit";
    private const string ModifierFriend = "friend";
    private const string ModifierInline = "inline";
    private const string ModifierVolatile = "volatile";
    private const string ModifierMutable = "mutable";
    private const string ModifierVirtual = "virtual";

    #endregion

    /// <summary>
    /// Array containing all recognized modifier keywords for parsing code headers.
    /// </summary>
    private static readonly string[] Modifiers =
    [
        ModifierPublic, ModifierPrivate, ModifierProtected, ModifierInternal,
        ModifierSealed, ModifierPartial, ModifierStatic, ModifierConst, ModifierReadonly,
        ModifierExplicit, ModifierFriend, ModifierInline, ModifierVolatile, ModifierMutable,
        ModifierVirtual
    ];

    /// <summary>
    /// Delegate type for icon selector functions that take a keyword and modifier and return an ImageMoniker.
    /// </summary>
    /// <param name="keyword">The keyword from the code header.</param>
    /// <param name="modifier">The access modifier from the code header.</param>
    /// <returns>The appropriate ImageMoniker for the given keyword and modifier combination.</returns>
    private delegate ImageMoniker IconSelector(ReadOnlySpan<char> keyword, ReadOnlySpan<char> modifier);

    /// <summary>
    /// Dictionary mapping keywords to their corresponding icon selector functions.
    /// </summary>
    private static readonly Dictionary<string, IconSelector> iconSelectors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Selects the appropriate Visual Studio icon moniker based on the provided code header text.
    /// Analyzes the header to identify the code construct type and access modifiers.
    /// </summary>
    /// <param name="header">The code header text to analyze (e.g., "public class MyClass").</param>
    /// <returns>An ImageMoniker representing the appropriate icon for the code construct.</returns>
    public static ImageMoniker SelectMoniker(ReadOnlySpan<char> header)
    {
        ImageMoniker icon = KnownMonikers.QuestionMark;
        if (header.IsEmpty || header.IsWhiteSpace())
        {
            return icon;
        }

        // find first visibility modifier
        var modifier = SplitModifierFromSymbol(header, out var symbol, out int wordCount, out int modifierCount);
        if (symbol.IsEmpty)
        {
            return icon;
        }

        // remove generics, parameters, etc.
        int indexOfSpecialChar = symbol.IndexOfAny(['<', '(', '[']);
        if (indexOfSpecialChar >= 0)
        {
            symbol = symbol.Slice(0, indexOfSpecialChar);
        }

        // setup keyword icons
        if (iconSelectors.Count == 0)
        {
            InitIconSelectors();
        }

        // get icon by keyword
        if (iconSelectors.TryGetValue(symbol.ToString(), out var selector))
        {
            return selector(symbol, modifier);
        }

        // icon for lambda
        else if (header.IndexOf("=>") >= 0)
        {
            return KnownMonikers.DelegatePublic;
        }

        // icon for method/ctor
        else if (wordCount - modifierCount >= 1 && header.IndexOf('(') >= 0 && header.IndexOf(')') >= 0)
        {
            return modifier switch
            {
                ModifierPublic => KnownMonikers.MethodPublic,
                ModifierProtected => KnownMonikers.MethodProtected,
                ModifierInternal => KnownMonikers.MethodInternal,
                _ => KnownMonikers.MethodPrivate,
            };
        }

        // icon for property
        else if (wordCount - modifierCount == 2)
        {
            return modifier switch
            {
                ModifierPublic => KnownMonikers.PropertyPublic,
                ModifierProtected => KnownMonikers.PropertyProtected,
                ModifierInternal => KnownMonikers.PropertyInternal,
                _ => KnownMonikers.PropertyPrivate,
            };
        }

        return icon;
    }

    /// <summary>
    /// Initializes the icon selector dictionary with mappings from keywords to their corresponding icon selection functions.
    /// Supports C#, C/C++, and PowerShell language constructs.
    /// </summary>
    private static void InitIconSelectors()
    {
        iconSelectors.Add("namespace", new IconSelector((_, __) => KnownMonikers.Namespace));
        iconSelectors.Add("class", new IconSelector((_, modifier) =>
        {
            return modifier switch
            {
                ModifierPublic => KnownMonikers.ClassPublic,
                ModifierPrivate => KnownMonikers.ClassPrivate,
                ModifierProtected => KnownMonikers.ClassProtected,
                _ => KnownMonikers.ClassInternal,
            };
        }));
        iconSelectors.Add("struct", new IconSelector((_, modifier) =>
        {
            return modifier switch
            {
                ModifierPublic => KnownMonikers.StructurePublic,
                ModifierPrivate => KnownMonikers.StructurePrivate,
                ModifierProtected => KnownMonikers.StructureProtected,
                _ => KnownMonikers.StructureInternal,
            };
        }));
        iconSelectors.Add("enum", new IconSelector((_, modifier) =>
        {
            return modifier switch
            {
                ModifierPublic => KnownMonikers.EnumerationPublic,
                ModifierPrivate => KnownMonikers.EnumerationPrivate,
                ModifierProtected => KnownMonikers.EnumerationProtected,
                _ => KnownMonikers.EnumerationInternal,
            };
        }));
        iconSelectors.Add("interface", new IconSelector((_, modifier) =>
        {
            return modifier switch
            {
                ModifierPublic => KnownMonikers.InterfacePublic,
                ModifierPrivate => KnownMonikers.InterfacePrivate,
                ModifierProtected => KnownMonikers.InterfaceProtected,
                _ => KnownMonikers.InterfaceInternal,
            };
        }));
        iconSelectors.Add("event", new IconSelector((_, modifier) =>
        {
            return modifier switch
            {
                ModifierPublic => KnownMonikers.EventPublic,
                ModifierInternal => KnownMonikers.EventInternal,
                ModifierProtected => KnownMonikers.EventProtected,
                _ => KnownMonikers.EventPrivate,
            };
        }));
        iconSelectors.Add("if", new IconSelector((_, __) => KnownMonikers.If));
        iconSelectors.Add("else", new IconSelector((_, __) => KnownMonikers.If));
        iconSelectors.Add("do", new IconSelector((_, __) => KnownMonikers.DoWhile));
        iconSelectors.Add("while", new IconSelector((_, __) => KnownMonikers.While));
        iconSelectors.Add("for", new IconSelector((_, __) => KnownMonikers.ForEachLoop));
        iconSelectors.Add("foreach", new IconSelector((_, __) => KnownMonikers.ForEachLoop));
        iconSelectors.Add("typedef", new IconSelector((_, __) => KnownMonikers.TypeDefinition));
        iconSelectors.Add("new", new IconSelector((__, _) => KnownMonikers.NewItem));
        iconSelectors.Add("switch", new IconSelector((_, __) => KnownMonikers.FlowSwitch));
        iconSelectors.Add("try", new IconSelector((_, __) => KnownMonikers.TryCatch));
        iconSelectors.Add("catch", new IconSelector((_, __) => KnownMonikers.TryCatch));
        iconSelectors.Add("finally", new IconSelector((_, __) => KnownMonikers.FinalState));
        iconSelectors.Add("unsafe", new IconSelector((_, __) => KnownMonikers.HotSpot));
        iconSelectors.Add("using", new IconSelector((_, __) => KnownMonikers.RectangleSelection));
        iconSelectors.Add("lock", new IconSelector((_, __) => KnownMonikers.Lock));
        iconSelectors.Add("add", new IconSelector((_, __) => KnownMonikers.AddEvent));
        iconSelectors.Add("remove", new IconSelector((_, __) => KnownMonikers.EventMissing));
        iconSelectors.Add("get", new IconSelector((_, __) => KnownMonikers.ReturnParameter));
        iconSelectors.Add("set", new IconSelector((_, __) => KnownMonikers.InsertParameter));

        // C/C++ Icons
        iconSelectors.Add("union", new IconSelector((_, __) => KnownMonikers.Union));
        iconSelectors.Add("template", new IconSelector((_, __) => KnownMonikers.Template));
        iconSelectors.Add("synchronized", new IconSelector((_, __) => KnownMonikers.SynchronousMessage));

        // PowerShell
        iconSelectors.Add("elseif", new IconSelector((_, __) => KnownMonikers.If));
        iconSelectors.Add("begin", new IconSelector((_, __) => KnownMonikers.StartPoint));
        iconSelectors.Add("process", new IconSelector((_, __) => KnownMonikers.Action));
        iconSelectors.Add("end", new IconSelector((_, __) => KnownMonikers.EndPoint));
        iconSelectors.Add("data", new IconSelector((_, __) => KnownMonikers.DataList));
        iconSelectors.Add("dynamicparam", new IconSelector((_, __) => KnownMonikers.NewParameter));
        iconSelectors.Add("filter", new IconSelector((_, __) => KnownMonikers.Filter));
        iconSelectors.Add("function", new IconSelector((_, __) => KnownMonikers.MethodPublic));
        iconSelectors.Add("workflow", new IconSelector((_, __) => KnownMonikers.WorkflowInterop));
        iconSelectors.Add("inlinescript", new IconSelector((_, __) => KnownMonikers.Inline));
        iconSelectors.Add("parallel", new IconSelector((_, __) => KnownMonikers.Parallel));
        iconSelectors.Add("sequence", new IconSelector((_, __) => KnownMonikers.Sequence));
        iconSelectors.Add("trap", new IconSelector((_, __) => KnownMonikers.TryCatch));
    }

    /// <summary>
    /// Extracts the first access modifier from the provided words array and counts total modifiers.
    /// </summary>
    /// <param name="header">the code header.</param>
    /// <param name="modifierCount">Output parameter containing the total number of modifiers found.</param>
    /// <returns>The first access modifier found, or an empty string if none are found.</returns>
    private static ReadOnlySpan<char> SplitModifierFromSymbol(ReadOnlySpan<char> header, out ReadOnlySpan<char> symbol, out int wordCount, out int modifierCount)
    {
        symbol = [];
        wordCount = 0;
        modifierCount = 0;
        var currentText = header;
        ReadOnlySpan<char> modifier = [];
        int indexOfSpace;
        while ((indexOfSpace = currentText.IndexOf(' ')) >= 0 || !currentText.IsEmpty)
        {
            wordCount++;

            ReadOnlySpan<char> word;
            if (indexOfSpace >= 0)
            {
                word = currentText.Slice(0, indexOfSpace);
                currentText = currentText.Slice(indexOfSpace + 1);
            }
            else
            {
                word = currentText;
                currentText = [];
            }

            if (Modifiers.Contains(word.ToString()))
            {
                modifierCount++;
                if (modifier.IsEmpty)
                {
                    modifier = word;
                }
                continue;
            }

            if (symbol.IsEmpty)
            {
                symbol = word;
            }
        }

        return modifier;
    }
}
