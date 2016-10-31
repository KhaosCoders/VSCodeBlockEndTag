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
        private static Dictionary<string, IconSelector> iconSelectors = new Dictionary<string, IconSelector>();

        public static ImageMoniker SelectMoniker(string header)
        {
            ImageMoniker icon = KnownMonikers.QuestionMark;
            if (string.IsNullOrWhiteSpace(header)) return icon;

            // split words of header
            string[] words = header.Split(' ');
            int modifierCount = 0;
            if (words.Length == 0) return icon;

            // find first visibility modifier
            string modifier = GetModifier(words, out modifierCount);
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
                icon = iconSelectors[keyword](keyword, modifier);
            }

            // icon for lambda
            else if (header.Contains("=>"))
            {
                icon = KnownMonikers.DelegatePublic;
            }

            // icon for method/ctor
            else if (words.Length - modifierCount >= 1 && header.Contains('(') && header.Contains(')'))
            {
                switch (modifier)
                {
                    case ModifierPublic: icon = KnownMonikers.MethodPublic; break;
                    case ModifierProtected: icon = KnownMonikers.MethodProtected; break;
                    case ModifierInternal: icon = KnownMonikers.MethodInternal; break;
                    default: icon = KnownMonikers.MethodPrivate; break;
                }
            }

            // icon for property
            else if (words.Length - modifierCount == 2)
            {
                switch (modifier)
                {
                    case ModifierPublic: icon = KnownMonikers.PropertyPublic; break;
                    case ModifierProtected: icon = KnownMonikers.PropertyProtected; break;
                    case ModifierInternal: icon = KnownMonikers.PropertyInternal; break;
                    default: icon = KnownMonikers.PropertyPrivate; break;
                }
            }

            return icon;
        }

        private static void InitIconSelectors()
        {
            iconSelectors.Add("namespace", new IconSelector((keyword, modifier) => KnownMonikers.Namespace));
            iconSelectors.Add("class", new IconSelector((keyword, modifier) =>
            {
                switch (modifier)
                {
                    case ModifierPublic: return KnownMonikers.ClassPublic;
                    case ModifierPrivate: return KnownMonikers.ClassPrivate;
                    case ModifierProtected: return KnownMonikers.ClassProtected;
                    default: return KnownMonikers.ClassInternal;
                }
            }));
            iconSelectors.Add("struct", new IconSelector((keyword, modifier) =>
            {
                switch (modifier)
                {
                    case ModifierPublic: return KnownMonikers.StructurePublic;
                    case ModifierPrivate: return KnownMonikers.StructurePrivate;
                    case ModifierProtected: return KnownMonikers.StructureProtected;
                    default: return KnownMonikers.StructureInternal;
                }
            }));
            iconSelectors.Add("enum", new IconSelector((keyword, modifier) =>
            {
                switch (modifier)
                {
                    case ModifierPublic: return KnownMonikers.EnumerationPublic;
                    case ModifierPrivate: return KnownMonikers.EnumerationPrivate;
                    case ModifierProtected: return KnownMonikers.EnumerationProtected;
                    default: return KnownMonikers.EnumerationInternal;
                }
            }));
            iconSelectors.Add("interface", new IconSelector((keyword, modifier) =>
            {
                switch (modifier)
                {
                    case ModifierPublic: return KnownMonikers.InterfacePublic;
                    case ModifierPrivate: return KnownMonikers.InterfacePrivate;
                    case ModifierProtected: return KnownMonikers.InterfaceProtected;
                    default: return KnownMonikers.InterfaceInternal;
                }
            }));
            iconSelectors.Add("event", new IconSelector((keyword, modifier) =>
            {
                switch (modifier)
                {
                    case ModifierPublic: return KnownMonikers.EventPublic;
                    case ModifierInternal: return KnownMonikers.EventInternal;
                    case ModifierProtected: return KnownMonikers.EventProtected;
                    default: return KnownMonikers.EventPrivate;
                }
            }));
            iconSelectors.Add("if", new IconSelector((keyword, modifier) => KnownMonikers.If));
            iconSelectors.Add("else", new IconSelector((keyword, modifier) => KnownMonikers.If));
            iconSelectors.Add("do", new IconSelector((keyword, modifier) => KnownMonikers.DoWhile));
            iconSelectors.Add("while", new IconSelector((keyword, modifier) => KnownMonikers.While));
            iconSelectors.Add("for", new IconSelector((keyword, modifier) => KnownMonikers.ForEachLoop));
            iconSelectors.Add("foreach", new IconSelector((keyword, modifier) => KnownMonikers.ForEachLoop));
            iconSelectors.Add("typedef", new IconSelector((keyword, modifier) => KnownMonikers.TypeDefinition));
            iconSelectors.Add("new", new IconSelector((keyword, modifier) => KnownMonikers.NewItem));
            iconSelectors.Add("switch", new IconSelector((keyword, modifier) => KnownMonikers.FlowSwitch));
            iconSelectors.Add("try", new IconSelector((keyword, modifier) => KnownMonikers.TryCatch));
            iconSelectors.Add("catch", new IconSelector((keyword, modifier) => KnownMonikers.TryCatch));
            iconSelectors.Add("finally", new IconSelector((keyword, modifier) => KnownMonikers.FinalState));
            iconSelectors.Add("unsafe", new IconSelector((keyword, modifier) => KnownMonikers.HotSpot));
            iconSelectors.Add("using", new IconSelector((keyword, modifier) => KnownMonikers.RectangleSelection));
            iconSelectors.Add("lock", new IconSelector((keyword, modifier) => KnownMonikers.Lock));
            iconSelectors.Add("add", new IconSelector((keyword, modifier) => KnownMonikers.AddEvent));
            iconSelectors.Add("remove", new IconSelector((keyword, modifier) => KnownMonikers.EventMissing));
            iconSelectors.Add("get", new IconSelector((keyword, modifier) => KnownMonikers.ReturnParameter));
            iconSelectors.Add("set", new IconSelector((keyword, modifier) => KnownMonikers.InsertParameter));

            // C/C++ Icons
            iconSelectors.Add("union", new IconSelector((keyword, modifier) => KnownMonikers.Union));
            iconSelectors.Add("template", new IconSelector((keyword, modifier) => KnownMonikers.Template));
            iconSelectors.Add("synchronized", new IconSelector((keyword, modifier) => KnownMonikers.SynchronousMessage));

            // PowerShell
            iconSelectors.Add("elseif", new IconSelector((keyword, modifier) => KnownMonikers.If));
            iconSelectors.Add("begin", new IconSelector((keyword, modifier) => KnownMonikers.StartPoint));
            iconSelectors.Add("process", new IconSelector((keyword, modifier) => KnownMonikers.Action));
            iconSelectors.Add("end", new IconSelector((keyword, modifier) => KnownMonikers.EndPoint));
            iconSelectors.Add("data", new IconSelector((keyword, modifier) => KnownMonikers.DataList));
            iconSelectors.Add("dynamicparam", new IconSelector((keyword, modifier) => KnownMonikers.NewParameter));
            iconSelectors.Add("filter", new IconSelector((keyword, modifier) => KnownMonikers.Filter));
            iconSelectors.Add("function", new IconSelector((keyword, modifier) => KnownMonikers.MethodPublic));
            iconSelectors.Add("workflow", new IconSelector((keyword, modifier) => KnownMonikers.WorkflowInterop));
            iconSelectors.Add("inlinescript", new IconSelector((keyword, modifier) => KnownMonikers.Inline));
            iconSelectors.Add("parallel", new IconSelector((keyword, modifier) => KnownMonikers.Parallel));
            iconSelectors.Add("sequence", new IconSelector((keyword, modifier) => KnownMonikers.Sequence));
            iconSelectors.Add("trap", new IconSelector((keyword, modifier) => KnownMonikers.TryCatch));



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
