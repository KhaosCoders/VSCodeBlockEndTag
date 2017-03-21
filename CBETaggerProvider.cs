using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using System.ComponentModel.Composition;

namespace CodeBlockEndTag
{
    
    /// <summary>
    /// This class serves as factory for VS to create the CBETagger
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal class CBETaggerProvider : IViewTaggerProvider
    {

        #region MEF-Imports: Services from VisualStudio
#pragma warning disable CS0649
        // Disabled waring about default value. Value will be set by VS.

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactory { get; set; }

        [Import]
        internal ITextSearchService TextSearchService { get; set; }
        
        [Import]
        internal IVsFontsAndColorsInformationService VsFontsAndColorsInformationService { get; set; }

#pragma warning restore CS0649
        #endregion

        /// <summary>
        /// Factory function for a CBETagger instance.
        /// Used by VS to create the tagger
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // only works with IWpfTextView
            var wpfTextView = textView as IWpfTextView;
            if (wpfTextView == null)
                return null;

            // provide the tag only on the top-level buffer
            if (textView.TextBuffer != buffer)
                return null;
            
            // Check if content type (language) is supported and active for tagging
            IContentType type = textView.TextBuffer.ContentType;
            if (!CBETagPackage.IsLanguageSupported(type.TypeName))
                return null;

            // return new instance of CBETagger
            return new CBETagger(this, wpfTextView) as ITagger<T>;
        }

        /// <summary>
        /// Returns a TextStructureNavigator for the given TextBuffer
        /// This is a service by VisualStudio for fast navigation in structured texts
        /// </summary>
        internal ITextStructureNavigator GetTextStructureNavigator(ITextBuffer buffer)
        {
            return TextStructureNavigatorSelector.GetTextStructureNavigator(buffer);
        }
    }
}
