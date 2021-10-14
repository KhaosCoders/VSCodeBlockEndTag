//
// For a complete list of all KnownMonikers see
// http://glyphlist.azurewebsites.net/knownmonikers/
//

using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;

namespace CodeBlockEndTag
{
    internal sealed class IconMonikerSelector
    {
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

        private static readonly string[] Modifiers = new string[]
        {
            ModifierPublic, ModifierPrivate, ModifierProtected, ModifierInternal,
            ModifierSealed, ModifierPartial, ModifierStatic, ModifierConst, ModifierReadonly,
            ModifierExplicit, ModifierFriend, ModifierInline, ModifierVolatile, ModifierMutable,
            ModifierVirtual
        };

        private delegate ImageMoniker IconSelector(string keyword, string modifier);
        private static readonly Dictionary<string, IconSelector> iconSelectors = new();

        public static ImageMoniker SelectMoniker(string header)
        {
            ImageMoniker icon = KnownMonikers.QuestionMark;
            if (string.IsNullOrWhiteSpace(header)) return icon;

            // split words of header
            string[] words = header.Split(' ');
            if (words.Length == 0) return icon;

            // find first visibility modifier
            string modifier = GetModifier(words, out int modifierCount);
            int keywordIndex = modifierCount;
            if (words.Length <= keywordIndex) return icon;

            // take first keyword
            string keyword = words[keywordIndex].ToLower();
            if (keyword.Contains('('))
            {
                keyword = keyword.Substring(0, keyword.IndexOf('('));
            }
            if (keyword.Contains('['))
            {
                keyword = keyword.Substring(0, keyword.IndexOf('['));
            }
            if (keyword.Contains('<'))
            {
                keyword = keyword.Substring(0, keyword.IndexOf('<'));
            }

            // setup keyword icons
            if (iconSelectors.Count == 0)
                InitIconSelectors();

            // get icon by keyword
            if (iconSelectors.ContainsKey(keyword))
            {
                return iconSelectors[keyword](keyword, modifier);
            }

            // icon for lambda
            else if (header.Contains("=>"))
            {
                return KnownMonikers.DelegatePublic;
            }

            // icon for method/ctor
            else if (words.Length - modifierCount >= 1 && header.Contains('(') && header.Contains(')'))
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
            else if (words.Length - modifierCount == 2)
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

        private static string GetModifier(string[] words, out int modifierCount)
        {
            string modifier = string.Empty;
            modifierCount = 0;
            foreach (string word in words)
            {
                if (Modifiers.Contains(word))
                {
                    modifierCount++;
                    if (string.IsNullOrWhiteSpace(modifier))
                        modifier = word;
                    continue;
                }
                break;
            }
            return modifier;
        }
    }
}
