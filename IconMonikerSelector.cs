//
// For a complete list of all KnownMonikers see 
// http://glyphlist.azurewebsites.net/knownmonikers/
//

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;

namespace CodeBlockEndTag
{
    internal sealed class IconMonikerSelector
    {
        private const string ModifierPublic = "public";
        private const string ModifierPrivate = "private";
        private const string ModifierProtected = "protected";
        private const string ModifierInternal = "internal";
        private const string ModifierSealed = "sealed";

        private static readonly string[] Modifiers = new string[]
        {
            ModifierPublic, ModifierPrivate, ModifierProtected, ModifierInternal
        };

        private const string ClassificationKeyword = "keyword";
        private const string ClassificationIdentifier = "identifier";
        private const string ClassificationPunctuation = "punctuation";
        private const string ClassificationOperator = "operator";



        public static ImageMoniker SelectMoniker(IList<IMappingTagSpan<IClassificationTag>> classifications, ITextStructureNavigator textStructureNavigator, ITextBuffer buffer)
        {
            ImageMoniker currentIcon = KnownMonikers.QuestionMark;

            SnapshotSpan firstSpan = classifications.First().Span.GetSpans(buffer).First();

            var classify = classifications.Select(c =>
                            new Classify()
                            {
                                Classification = c.Tag.ClassificationType.Classification.ToLower(),
                                Word = c.Span.GetSpans(buffer).First().GetText()
                            });

            string modifier = classify
                .Where(c => c.Classification.Contains(ClassificationKeyword))
                .Select(c => c.Word)
                .Intersect(Modifiers).FirstOrDefault();
            
            if (IsNamespace(classify))
            {
                currentIcon = KnownMonikers.Namespace;
            }
            else if (IsClass(classify))
            {
                if (string.IsNullOrWhiteSpace(modifier))
                    modifier = GetModifier(textStructureNavigator, firstSpan);
                switch (modifier)
                {
                    case ModifierPublic: currentIcon = KnownMonikers.ClassPublic; break;
                    case ModifierPrivate: currentIcon = KnownMonikers.ClassPrivate; break;
                    case ModifierProtected: currentIcon = KnownMonikers.ClassProtected; break;
                    case ModifierInternal: currentIcon = KnownMonikers.ClassInternal; break;
                }
            }
            else if (IsStruct(classify))
            {
                if (string.IsNullOrWhiteSpace(modifier)) 
                modifier = GetModifier(textStructureNavigator, firstSpan);
                switch (modifier)
                {
                    case ModifierPublic: currentIcon = KnownMonikers.StructurePublic; break;
                    case ModifierPrivate: currentIcon = KnownMonikers.StructurePrivate; break;
                    case ModifierProtected: currentIcon = KnownMonikers.StructureProtected; break;
                    case ModifierInternal: currentIcon = KnownMonikers.StructureInternal; break;
                }
            }
            else if (IsEnum(classify))
            {
                if (string.IsNullOrWhiteSpace(modifier))
                    modifier = GetModifier(textStructureNavigator, firstSpan);
                switch (modifier)
                {
                    case ModifierPublic: currentIcon = KnownMonikers.EnumerationPublic; break;
                    case ModifierPrivate: currentIcon = KnownMonikers.EnumerationPrivate; break;
                    case ModifierProtected: currentIcon = KnownMonikers.EnumerationProtected; break;
                    case ModifierInternal: currentIcon = KnownMonikers.EnumerationInternal; break;
                }
            }
            else if (IsInterface(classify))
            {
                if (string.IsNullOrWhiteSpace(modifier))
                    modifier = GetModifier(textStructureNavigator, firstSpan);
                switch (modifier)
                {
                    case ModifierPublic: currentIcon = KnownMonikers.InterfacePublic; break;
                    case ModifierPrivate: currentIcon = KnownMonikers.InterfacePrivate; break;
                    case ModifierProtected: currentIcon = KnownMonikers.InterfaceProtected; break;
                    case ModifierInternal: currentIcon = KnownMonikers.InterfaceInternal; break;
                }
            }
            else if (IsEvent(classify))
            {
                if (string.IsNullOrWhiteSpace(modifier))
                    modifier = ModifierPrivate;
                switch (modifier)
                {
                    case ModifierPublic: currentIcon = KnownMonikers.EventPublic; break;
                    case ModifierPrivate: currentIcon = KnownMonikers.EventPrivate; break;
                    case ModifierProtected: currentIcon = KnownMonikers.EventProtected; break;
                    case ModifierInternal: currentIcon = KnownMonikers.EventInternal; break;
                }
            }
            else if (IsIf(classify))
            {
                currentIcon = KnownMonikers.If;
            }
            else if (IsElse(classify))
            {
                currentIcon = KnownMonikers.If;
            }
            else if (IsWhile(classify))
            {
                currentIcon = KnownMonikers.While;
            }
            else if (IsFor(classify))
            {
                currentIcon = KnownMonikers.ForEachLoop;
            }
            else if (IsTypedef(classify))
            {
                currentIcon = KnownMonikers.TypeDefinition;
            }
            else if (IsNewInstanceCreation(classify))
            {
                currentIcon = KnownMonikers.NewItem;
            }
            else if (IsSwitch(classify))
            {
                currentIcon = KnownMonikers.FlowSwitch;
            }
            else if (IsTry(classify))
            {
                currentIcon = KnownMonikers.TryCatch;
            }
            else if (IsCatch(classify))
            {
                currentIcon = KnownMonikers.TryCatch;
            }
            else if (IsFinally(classify))
            {
                currentIcon = KnownMonikers.FinalState;
            }
            else if (IsUnsafe(classify))
            {
                currentIcon = KnownMonikers.HotSpot;
            }
            else if (IsUsing(classify))
            {
                currentIcon = KnownMonikers.RectangleSelection;
            }
            else if (IsLock(classify))
            {
                currentIcon = KnownMonikers.Lock;
            }
            else if (IsAddEventHandler(classify, textStructureNavigator, firstSpan))
            {
                currentIcon = KnownMonikers.AddEvent;
            }
            else if (IsRemoveEventHandler(classify, textStructureNavigator, firstSpan))
            {
                currentIcon = KnownMonikers.EventMissing;
            }
            else if (IsPropertyGetter(classify, textStructureNavigator, firstSpan))
            {
                currentIcon = KnownMonikers.ReturnParameter;
            }
            else if (IsPropertySetter(classify, textStructureNavigator, firstSpan))
            {
                currentIcon = KnownMonikers.InsertParameter;
            }
            else if (IsLambda(classify))
            {
                currentIcon = KnownMonikers.DelegatePublic;
            }
            else if (IsMethod(classify))
            {
                if (string.IsNullOrWhiteSpace(modifier))
                    modifier = ModifierPrivate;
                switch (modifier)
                {
                    case ModifierPublic: currentIcon = KnownMonikers.MethodPublic; break;
                    case ModifierPrivate: currentIcon = KnownMonikers.MethodPrivate; break;
                    case ModifierProtected: currentIcon = KnownMonikers.MethodProtected; break;
                    case ModifierInternal: currentIcon = KnownMonikers.MethodInternal; break;
                }
            }
            else if (IsProperty(classify))
            {
                if (string.IsNullOrWhiteSpace(modifier))
                    modifier = ModifierPrivate;
                switch (modifier)
                {
                    case ModifierPublic: currentIcon = KnownMonikers.PropertyPublic; break;
                    case ModifierPrivate: currentIcon = KnownMonikers.PropertyPrivate; break;
                    case ModifierProtected: currentIcon = KnownMonikers.PropertyProtected; break;
                    case ModifierInternal: currentIcon = KnownMonikers.PropertyInternal; break;
                }
            }

            
            return currentIcon;
        }

        private static bool IsNamespace(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c => 
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("namespace")).Any();
        }

        private static bool IsClass(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("class")).Any();
        }

        private static bool IsIf(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("if")).Any();
        }
        private static bool IsElse(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("else")).Any();
        }

        private static bool IsWhile(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && (c.Word.Equals("while") || c.Word.Equals("do"))).Any();
        }

        private static bool IsFor(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && (c.Word.Equals("for") || c.Word.Equals("foreach"))).Any();
        }

        private static bool IsStruct(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("struct")).Any();
        }

        private static bool IsEnum(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("enum")).Any();
        }

        private static bool IsInterface(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("interface")).Any();
        }

        private static bool IsEvent(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("event")).Any();
        }

        private static bool IsTypedef(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("typedef")).Any();
        }

        private static bool IsNewInstanceCreation(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("new")).Any();
        }

        private static bool IsSwitch(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("switch")).Any();
        }

        private static bool IsTry(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("try")).Any();
        }

        private static bool IsCatch(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("catch")).Any();
        }

        private static bool IsFinally(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("finally")).Any();
        }

        private static bool IsUnsafe(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("unsafe")).Any();
        }

        private static bool IsUsing(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("using")).Any();
        }

        private static bool IsLock(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("lock")).Any();
        }

        private static bool IsLambda(IEnumerable<Classify> classifications)
        {
            return classifications.Where(c =>
                                    c.Classification.Contains(ClassificationOperator)
                                    && c.Word.Equals("=>")).Any();
        }

        private static bool IsMethod(IEnumerable<Classify> classifications)
        {
            bool wasId = false;
            foreach (var classify in classifications)
            {
                if (classify.Classification.Contains(ClassificationIdentifier))
                {
                    wasId = true;
                }
                else if (wasId
                    && classify.Classification.Contains(ClassificationPunctuation)
                    && classify.Word.StartsWith("("))
                {
                    return true;
                }
                else if (wasId)
                {
                    wasId = false;
                }
            }
            return false;
        }

        // IsMethod must be called before! This sets up on it.
        private static bool IsProperty(IEnumerable<Classify> classifications)
        {
            return classifications.Last().Classification.Contains(ClassificationIdentifier);
        }

        private static bool IsAddEventHandler(IEnumerable<Classify> classifications, ITextStructureNavigator textStructureNavigator, SnapshotSpan span)
        {
            if (classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("add")).Any())
            {
                SnapshotSpan eventSpan = textStructureNavigator.GetSpanOfEnclosing(span);
                while (eventSpan.Span.Start == span.Span.Start
                    || eventSpan.Snapshot.GetText(new Span(eventSpan.Span.Start, 1)).Equals("{"))
                    eventSpan = textStructureNavigator.GetSpanOfEnclosing(eventSpan);
                string eventText = eventSpan.Snapshot.GetText(new Span(eventSpan.Span.Start, span.Start - eventSpan.Span.Start)).ToLower();
                return eventText.Contains("event");
            }
            return false;
        }

        private static bool IsRemoveEventHandler(IEnumerable<Classify> classifications, ITextStructureNavigator textStructureNavigator, SnapshotSpan span)
        {
            if (classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("remove")).Any())
            {
                SnapshotSpan eventSpan = textStructureNavigator.GetSpanOfEnclosing(span);
                while (eventSpan.Span.Start == span.Span.Start
                    || eventSpan.Snapshot.GetText(new Span(eventSpan.Span.Start, 1)).Equals("{"))
                    eventSpan = textStructureNavigator.GetSpanOfEnclosing(eventSpan);
                string eventText = eventSpan.Snapshot.GetText(new Span(eventSpan.Span.Start, span.Start - eventSpan.Span.Start)).ToLower();
                return eventText.Contains("event");
            }
            return false;
        }

        private static bool IsPropertySetter(IEnumerable<Classify> classifications, ITextStructureNavigator textStructureNavigator, SnapshotSpan span)
        {
            if (classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("set")).Any())
            {
                return true;
            }
            return false;
        }

        private static bool IsPropertyGetter(IEnumerable<Classify> classifications, ITextStructureNavigator textStructureNavigator, SnapshotSpan span)
        {
            if (classifications.Where(c =>
                                    c.Classification.Contains(ClassificationKeyword)
                                    && c.Word.Equals("get")).Any())
            {
                return true;
            }
            return false;
        }






        private static string GetModifier(ITextStructureNavigator textStructureNavigator, SnapshotSpan span)
        {
            SnapshotSpan parentSpan = textStructureNavigator.GetSpanOfEnclosing(span);
            if (parentSpan.Span.Start == span.Span.Start)
                parentSpan = textStructureNavigator.GetSpanOfEnclosing(parentSpan);
            string parentText = parentSpan.Snapshot.GetText(parentSpan.Span.Start, "namespace".Length).ToLower();
            if (parentText.Equals("namespace"))
                return ModifierInternal;
            return ModifierPrivate;
        }

        private struct Classify
        {
            public string Classification { get; set; }
            public string Word { get; set; }
        }
    }
}
