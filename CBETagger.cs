// Thanks to https://github.com/jaredpar/ControlCharAdornmentSample/blob/master/CharDisplayTaggerSource.cs

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace CodeBlockEndTag
{

    /// <summary>
    /// This tagger provides editor tags that are inserted into the TextView (IntraTextAdornmentTags)
    /// The tags are added after each code block encapsulated by curly bracets: { ... }
    /// The tags will show the code blocks condition, or whatever serves as header for the block
    /// By clicking on a tag, the editor will jump to that code blocks header
    /// </summary>
    internal class CBETagger : ITagger<IntraTextAdornmentTag>, IDisposable
    {
        private static readonly ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>> EmptyTagColllection =
            new ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>>(new List<ITagSpan<IntraTextAdornmentTag>>());


        #region Properties & Fields

        // EventHandler for ITagger<IntraTextAdornmentTag> tags changed event
        EventHandler<SnapshotSpanEventArgs> _changedEvent;

        /// <summary>
        /// Service by VisualStudio for fast searches in texts 
        /// </summary>
        readonly ITextSearchService _TextSearchService;

        /// <summary>
        /// Service by VisualStudio for fast navigation in structured texts
        /// </summary>
        readonly ITextStructureNavigator _TextStructureNavigator;

        /// <summary>
        /// The TextView this tagger is assigned to
        /// </summary>
        readonly IWpfTextView _TextView;

        /// <summary>
        /// This is a list of already created adornment tags used as cache
        /// </summary>
        readonly List<CBAdornmentData> _adornmentCache = new List<CBAdornmentData>();

        /// <summary>
        /// This is the visible span of the textview
        /// </summary>
        Span? _VisibleSpan;

        /// <summary>
        /// Is set, when the instance is disposed
        /// </summary>
        bool _Disposed { get; set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of CBRTagger
        /// </summary>
        /// <param name="provider">the CBETaggerProvider that created the tagger</param>
        /// <param name="textView">the WpfTextView this tagger is assigned to</param>
        /// <param name="sourceBuffer">the TextBuffer this tagger should work with</param>
        internal CBETagger(CBETaggerProvider provider, IWpfTextView textView)
        {
            if (provider == null || textView == null)
                throw new ArgumentNullException("The arguments of CBETagger can't be null");

            _TextView = textView;

            // Getting services provided by VisualStudio
            _TextStructureNavigator = provider.GetTextStructureNavigator(_TextView.TextBuffer);
            _TextSearchService = provider.TextSearchService;

            // Hook up events
            _TextView.TextBuffer.Changed += TextBuffer_Changed;
            _TextView.LayoutChanged += OnTextViewLayoutChanged;
            CBETagPackage.Instance.PackageOptionChanged += OnPackageOptionChanged;
        }

        #endregion

        #region TextBuffer changed

        void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            foreach (var textChange in e.Changes)
            {
                OnTextChanged(textChange);
            }
        }

        void OnTextChanged(ITextChange textChange)
        {
            // remove or update tags in adornment cache
            List<CBAdornmentData> remove = new List<CBAdornmentData>();
            foreach (var adornment in _adornmentCache)
            {
                if (!(adornment.HeaderStartPosition > textChange.OldEnd || adornment.EndPosition < textChange.OldPosition))
                    remove.Add(adornment);
                else if (adornment.HeaderStartPosition > textChange.OldEnd)
                {
                    adornment.HeaderStartPosition += textChange.Delta;
                    adornment.StartPosition += textChange.Delta;
                    adornment.EndPosition += textChange.Delta;
                }
            }

            foreach (var adornment in remove)
            {
                RemoveFromCache(adornment);
            }
        }

        private void RemoveFromCache(CBAdornmentData adornment)
        {
            var tag = adornment.Adornment as CBETagControl;
            if (tag != null)
            {
                tag.TagClicked -= Adornment_TagClicked;
            }
            _adornmentCache.Remove(adornment);
        }

        #endregion

        #region ITagger<IntraTextAdornmentTag>

        IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                var tags = GetTags(span);
                foreach (var tag in tags)
                {
                    yield return tag;
                }
            }
        }

        event EventHandler<SnapshotSpanEventArgs> ITagger<IntraTextAdornmentTag>.TagsChanged
        {
            add { _changedEvent += value; }
            remove { _changedEvent -= value; }
        }

        #endregion

        #region Tag placement

        internal ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>> GetTags(SnapshotSpan span)
        {
            if (!CBETagPackage.CBETaggerEnabled ||
                span.Snapshot != _TextView.TextBuffer.CurrentSnapshot ||
                span.Length == 0)
            {
                return EmptyTagColllection;
            }

            return GetTagsCore(span);
        }

#if DEBUG
        System.Diagnostics.Stopwatch watch;
#endif
        private ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>> GetTagsCore(SnapshotSpan span)
        {
            var list = new List<ITagSpan<IntraTextAdornmentTag>>();
            var offset = span.Start.Position;
            var snapshot = span.Snapshot;

            // vars used in loop
            SnapshotSpan cbSpan;
            CBAdornmentData cbAdornmentData;
            CBETagControl tagElement;
            int cbStartPosition;
            int cbEndPosition;
            int cbHeaderPosition;
            string cbHeader;
            IntraTextAdornmentTag cbTag;
            SnapshotSpan cbSnapshotSpan;
            TagSpan<IntraTextAdornmentTag> cbTagSpan;

#if DEBUG
            // Stop time
            if (watch == null)
                watch = new System.Diagnostics.Stopwatch();
            watch.Restart();
#endif
            // Find all closing bracets
            for (int i = 0; i < span.Length; i++)
            {
                var position = i + offset;
                var chr = snapshot[position];
                if (chr != '}')
                    continue;

                // getting start and end position of code block
                cbEndPosition = position;
                if (position >= 0 && snapshot[position - 1] == '{')
                {
                    // empty code block {} 
                    cbStartPosition = position - 1;
                    cbSpan = new SnapshotSpan(snapshot, cbStartPosition, cbEndPosition - cbStartPosition);
                }
                else
                {
                    // create inner span to navigate to get code block start
                    cbSpan = _TextStructureNavigator.GetSpanOfEnclosing(new SnapshotSpan(snapshot, position - 1, 1));
                    cbStartPosition = cbSpan.Start;
                }

                // Don't display tag for code blocks on same line
                if (!snapshot.GetText(cbSpan).Contains('\n'))
                    continue;

                // getting the code blocks header 
                cbHeaderPosition = -1;
                if (snapshot[cbStartPosition] == '{')
                {
                    // cbSpan does not contain the header
                    cbHeader = GetCodeBlockHeader(cbSpan, out cbHeaderPosition);
                }
                else
                {
                    // cbSpan does contain the header
                    cbHeader = GetCodeBlockHeader(cbSpan, out cbHeaderPosition, position);
                }

                // Trim header
                if (cbHeader != null && cbHeader.Length > 0)
                {
                    cbHeader = cbHeader.Trim()
                        .Replace(Environment.NewLine, "")
                        .Replace('\t', ' ');
                }

                // Skip tag if option "only when header not visible"
                if (_VisibleSpan != null && !IsTagVisible(cbHeaderPosition, cbEndPosition, _VisibleSpan))
                    continue;

                var iconMoniker = Microsoft.VisualStudio.Imaging.KnownMonikers.QuestionMark;
                if (CBETagPackage.CBEDisplayMode != (int)CBEOptionPage.DisplayModes.Text &&
                    !string.IsNullOrWhiteSpace(cbHeader) && !cbHeader.Contains("{"))
                {
                    iconMoniker = IconMonikerSelector.SelectMoniker(cbHeader);
                }

                // use cache or create new tag
                cbAdornmentData = _adornmentCache
                                    .Where(o =>
                                        o.StartPosition == cbStartPosition &&
                                        o.EndPosition == cbEndPosition)
                                    .FirstOrDefault();

                if (cbAdornmentData?.Adornment != null)
                {
                    tagElement = cbAdornmentData.Adornment as CBETagControl;
                }
                else
                {
                    // create new adornment
                    tagElement = new CBETagControl()
                    {
                        Text = cbHeader,
                        IconMoniker = iconMoniker,
                        DisplayMode = CBETagPackage.CBEDisplayMode
                    };

                    tagElement.TagClicked += Adornment_TagClicked;

                    cbAdornmentData = new CBAdornmentData(cbStartPosition, cbEndPosition, cbHeaderPosition, tagElement);
                    tagElement.AdornmentData = cbAdornmentData;
                    _adornmentCache.Add(cbAdornmentData);
                }

                // Add new tag to list
                cbTag = new IntraTextAdornmentTag(tagElement, null);
                cbSnapshotSpan = new SnapshotSpan(snapshot, position + 1, 0);
                cbTagSpan = new TagSpan<IntraTextAdornmentTag>(cbSnapshotSpan, cbTag);
                list.Add(cbTagSpan);
            }

#if DEBUG
            watch.Stop();
            if (watch.Elapsed.Milliseconds > 100)
            {
                System.Diagnostics.Debug.WriteLine("Time elapsed: " + watch.Elapsed +
                    " on Thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId +
                    " in Span: " + span.Start.Position + ":" + span.End.Position + " length: " + span.Length);
            }
#endif

            return new ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>>(list);
        }

        /// <summary>
        /// Capture the header of a code block
        /// Returns the text and outputs the start position within the snapshot
        /// </summary>
        string GetCodeBlockHeader(SnapshotSpan cbSpan, out int headerStart, int maxEndPosition = 0)
        {
            if (maxEndPosition == 0)
                maxEndPosition = cbSpan.Start;
            var snapshot = cbSpan.Snapshot;
            var currentSpan = cbSpan;

            // set end of header to first start of code block {
            for (int i = cbSpan.Start; i <= cbSpan.End; i++)
            {
                if (snapshot[i] == '{')
                {
                    maxEndPosition = i;
                    break;
                }
            }

            Span headerSpan, headerSpan2;
            string headerText, headerText2;
            int loops = 0;
            // check all enclosing spans until the header is complete
            do
            {
                // abort if in endless loop
                if (loops++ > 10)
                    break;

                // get text of current span
                headerStart = currentSpan.Start;
                headerSpan = new Span(headerStart, Math.Min(maxEndPosition, currentSpan.Span.End) - headerStart);
                if (headerSpan.Length == 0)
                    continue;
                headerText = snapshot.GetText(headerSpan);

                // found header if it begins with a letter or contains a lambda
                if (!string.IsNullOrWhiteSpace(headerText))
                //&& (char.IsLetter(headerText[0]) || headerText[0]=='[' || headerText.Contains("=>")))
                {
                    // recognize "else if" too
                    if (headerText.StartsWith("if") && ((currentSpan = _TextStructureNavigator.GetSpanOfEnclosing(currentSpan)) != null))
                    {
                        // check what comes before the "if"
                        headerSpan2 = new Span(currentSpan.Start, Math.Min(maxEndPosition, currentSpan.Span.End) - currentSpan.Start);
                        headerText2 = snapshot.GetText(headerSpan2);
                        if (headerText2.StartsWith("else"))
                        {
                            headerStart = headerSpan2.Start;
                            headerText = headerText2;
                        }
                    }
                    else if (headerText.Contains('\r') || headerText.Contains('\n'))
                    {
                        // skip annotations
                        headerText = headerText.Replace('\r', '\n').Replace("\n\n", "\n");
                        string[] headerLines = headerText.Split('\n');
                        bool annotaions = true;
                        int openBracets = 0;
                        headerText = string.Empty;
                        foreach (var line in headerLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                var trimmedline = line.Trim();
                                if (annotaions && (trimmedline[0] == '[' || openBracets > 0))
                                {
                                    openBracets += trimmedline.Count(c => c == '[');
                                    openBracets -= trimmedline.Count(c => c == ']');
                                    continue;
                                }
                                annotaions = false;
                                if (!string.IsNullOrWhiteSpace(headerText))
                                    headerText += Environment.NewLine;
                                headerText += trimmedline;
                            }
                        }
                    }
                    return headerText;
                }

                // get next enclosing span of current span
            } while ((currentSpan = _TextStructureNavigator.GetSpanOfEnclosing(currentSpan)) != null);

            // No header found
            headerStart = -1;
            return null;
        }


        #endregion

        #region Tag Clicked Handler

        /// <summary>
        /// Handles the click event on a tag
        /// </summary>
        private void Adornment_TagClicked(CBAdornmentData adornment)
        {
            if (_TextView != null)
            {
                SnapshotPoint targetPoint = new SnapshotPoint(_TextView.TextBuffer.CurrentSnapshot, adornment.HeaderStartPosition);
                _TextView.DisplayTextLineContainingBufferPosition(targetPoint, 30, ViewRelativePosition.Top);
                _TextView.Caret.MoveTo(targetPoint);
            }
        }

        #endregion

        #region Options changed

        /// <summary>
        /// Handles the event when any package option is changed
        /// </summary>
        private void OnPackageOptionChanged(object sender)
        {
            int start = Math.Max(0, _VisibleSpan.HasValue ? _VisibleSpan.Value.Start : 0);
            int end = Math.Max(1, _VisibleSpan.HasValue ? _VisibleSpan.Value.End : 1);
            InvalidateSpan(new Span(start, end - start));
        }

        /// <summary>
        /// Invalidates all cached tags within or after the given span
        /// </summary>
        private void InvalidateSpan(Span invalidateSpan, bool clearCache = true)
        {
            // Remove tags from cache
            if (clearCache)
            {
                _adornmentCache
                    .Where(a => a.HeaderStartPosition >= invalidateSpan.Start || a.EndPosition >= invalidateSpan.Start)
                    .ToList().ForEach(a => RemoveFromCache(a));
            }

            // Invalidate span
            _changedEvent?.Invoke(this, new SnapshotSpanEventArgs(
                new SnapshotSpan(_TextView.TextBuffer.CurrentSnapshot, invalidateSpan)));
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Clean up all events and references
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            if (disposing)
            {
                CBETagPackage.Instance.PackageOptionChanged -= OnPackageOptionChanged;
                _TextView.LayoutChanged -= OnTextViewLayoutChanged;
                _TextView.TextBuffer.Changed -= TextBuffer_Changed;
            }
            _Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region visibility of tags

        /// <summary>
        /// Checks if a tag's header is visible
        /// </summary>
        /// <param name="tagSpan">the span of the tag</param>
        /// <param name="visibleSpan">the visible span in the textview</param>
        /// <returns>true if the tag is visible (or if all tags are shown)</returns>
        private bool IsTagVisible(int start, int end, Span? visibleSpan)
        {
            if (CBETagPackage.CBEVisibilityMode == (int)CBEOptionPage.VisibilityModes.Always 
                || visibleSpan == null || !visibleSpan.HasValue)
                return true;
            var val = visibleSpan.Value;
            return (start < val.Start && end >= val.Start && end <= val.End);
        }

        /// <summary>
        /// Returns the visible span for the given textview
        /// </summary>
        private Span? GetVisibleSpan(ITextView textView)
        {
            if (textView?.TextViewLines != null && textView.TextViewLines.Count > 2)
            {
                // Index 0 not yet visible
                var firstVisibleLine = textView.TextViewLines[1];
                // Last index not visible, too
                var lastVisibleLine = textView.TextViewLines[textView.TextViewLines.Count - 2];

                return new Span(firstVisibleLine.Start, lastVisibleLine.End - firstVisibleLine.Start);
            }
            return null;
        }


        #endregion

        #region TextView scrolling

        private void OnTextViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // get new visible span
            var visibleSpan = GetVisibleSpan(_TextView);
            // only if new visible span is different from old
            if (!_VisibleSpan.HasValue ||
                _VisibleSpan.Value.Start != visibleSpan.Value.Start ||
                _VisibleSpan.Value.End < visibleSpan.Value.End)
            {
                // invalidate new and/or old visible span
                List<Span> invalidSpans = new List<Span>();
                var newSpan = visibleSpan.Value;
                if (!_VisibleSpan.HasValue)
                {
                    invalidSpans.Add(new Span(newSpan.Start, newSpan.End - newSpan.Start));
                }
                else
                {
                    var oldSpan = _VisibleSpan.Value;
                    // invalidate two spans if old and new do not overlap
                    if (newSpan.Start > oldSpan.End || newSpan.End < oldSpan.Start)
                    {
                        invalidSpans.Add(new Span(newSpan.Start, newSpan.End - newSpan.Start));
                        invalidSpans.Add(new Span(oldSpan.Start, oldSpan.End - oldSpan.Start));
                    }
                    else
                    {
                        // invalidate one big span (old and new joined)
                        int start = Math.Min(newSpan.Start, oldSpan.Start);
                        int end = Math.Max(newSpan.End, oldSpan.End);
                        invalidSpans.Add(new Span(start, end - start));
                    }
                }

                _VisibleSpan = visibleSpan;

                // refresh tags
                foreach (var span in invalidSpans)
                {
                    if (CBETagPackage.CBEVisibilityMode != (int)CBEOptionPage.VisibilityModes.Always)
                        InvalidateSpan(span, false);
                }
            }
        }

        #endregion

    }
}
