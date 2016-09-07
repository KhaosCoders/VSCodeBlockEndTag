using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace CodeBlockEndTag
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal class CBETaggerProvider : IViewTaggerProvider
    {
#pragma warning disable CS0649
        // Disabled waring about default value. Value will be set externaly.

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactory { get; set; }

        [Import]
        internal ITextSearchService TextSearchService { get; set; }
#pragma warning restore CS0649

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null)
                return null;

            //provide the tag only on the top-level buffer
            if (textView.TextBuffer != buffer)
                return null;

            ITextStructureNavigator textStructureNavigator =
                TextStructureNavigatorSelector.GetTextStructureNavigator(buffer);

            return new CBETagger(this, textView as IWpfTextView, buffer, textStructureNavigator, TextSearchService) as ITagger<T>;
        }
    }
}
