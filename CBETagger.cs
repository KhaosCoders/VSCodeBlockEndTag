// Thanks to https://github.com/jaredpar/ControlCharAdornmentSample/blob/master/CharDisplayTaggerSource.cs

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Threading;
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

            //_TextView.LayoutChanged += OnTextViewLayoutChanged;
            //CBETagPackage.Instance.PackageOptionChanged += OnPackageOptionChanged;
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

        }

        #endregion

        #region ITagger<IntraTextAdornmentTag>

        IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                foreach (var tag in GetTags(span))
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
                span.Snapshot != _TextView.TextBuffer.CurrentSnapshot)
            {
                return EmptyTagColllection;
            }

            return GetTagsCore(span);
        }

        private ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>> GetTagsCore(SnapshotSpan span)
        {
            var list = new List<ITagSpan<IntraTextAdornmentTag>>();
            var offset = span.Start.Position;
            var snapshot = span.Snapshot;

            // vars used in loop
            SnapshotSpan cbSpan;
            CBAdornmentData cbAdornmentData;
            UIElement tagElement;
            int cbStartPosition;
            int cbEndPosition;
            int cbHeaderPosition;
            string cbHeader;
            IntraTextAdornmentTag cbTag;
            SnapshotSpan cbSnapshotSpan;
            TagSpan<IntraTextAdornmentTag> cbTagSpan;


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
                    cbHeader = GetCodeBlockHeader(cbSpan, out cbHeaderPosition);
                }

                // Trim header
                if (cbHeader != null && cbHeader.Length > 0)
                {
                    cbHeader = cbHeader.Trim().Replace(Environment.NewLine, "");
                    // Todo: remove [Guid]
                }

                var iconMoniker = Microsoft.VisualStudio.Imaging.KnownMonikers.QuestionMark;
                if (!string.IsNullOrWhiteSpace(cbHeader) && !cbHeader.Contains("{"))
                {
                    //IconMonikerSelector.SelectMoniker(headerClassifications, TextStructureNavigator, SourceBuffer);
                }

                // create new adornment
                cbAdornmentData = _adornmentCache
                                    .Where(o => 
                                        o.StartPosition == cbStartPosition && 
                                        o.EndPosition == cbEndPosition)
                                    .FirstOrDefault();
                // use cache or create new tag
                if (cbAdornmentData.Adornment != null)
                {
                    tagElement = cbAdornmentData.Adornment;
                }
                else
                {
                    tagElement = new CBETagControl()
                    {
                        Text = cbHeader,
                        IconMoniker = iconMoniker,
                        CBRegion = null,
                        DisplayMode = CBETagPackage.CBEDisplayMode
                    };

                    cbAdornmentData = new CBAdornmentData(cbStartPosition, cbEndPosition, tagElement);
                    _adornmentCache.Add(cbAdornmentData);
                }

                // Add new tag to list
                cbTag = new IntraTextAdornmentTag(tagElement, null);
                cbSnapshotSpan = new SnapshotSpan(snapshot, position + 1, 0);
                cbTagSpan = new TagSpan<IntraTextAdornmentTag>(cbSnapshotSpan, cbTag);
                list.Add(cbTagSpan);
            }

            return new ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>>(list);
        }

        /// <summary>
        /// Capture the header of a code block
        /// Returns the text and outputs the start position within the snapshot
        /// </summary>
        string GetCodeBlockHeader(SnapshotSpan cbSpan, out int headerStart)
        {
            int maxEndPosition = cbSpan.Start;
            var snapshot = cbSpan.Snapshot;
            var currentSpan = cbSpan;

            // set end of header to first start of code block {
            for (int i=cbSpan.Start; i<=cbSpan.End; i++)
            {
                if (snapshot[i]=='{')
                {
                    maxEndPosition = i;
                    break;
                }
            }

            Span headerSpan, headerSpan2;
            string headerText, headerText2;
            // check all enclosing spans until the header is complete
            do
            {
                // get text of current span
                headerStart = currentSpan.Start;
                headerSpan = new Span(headerStart, Math.Min(maxEndPosition, currentSpan.Span.End) - headerStart);
                if (headerSpan.Length == 0)
                    continue;
                headerText = snapshot.GetText(headerSpan);

                // found header if it begins with a letter or contains a lambda
                if (!string.IsNullOrWhiteSpace(headerText) 
                    && (char.IsLetter(headerText[0]) || headerText.Contains("=>")))
                {
                    // recognize "else if" too
                    if(headerText.StartsWith("if") && ((currentSpan = _TextStructureNavigator.GetSpanOfEnclosing(currentSpan)) != null))
                    {
                        // check what comes before the "if"
                        headerSpan2 = new Span(currentSpan.Start, Math.Min(maxEndPosition, currentSpan.Span.End) - currentSpan.Start);
                        headerText2 = snapshot.GetText(headerSpan2);
                        if(headerText2.StartsWith("else"))
                        {
                            headerStart = headerSpan2.Start;
                            headerText = headerText2;
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
                //_TextView.LayoutChanged -= OnTextViewLayoutChanged;
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









        #region TextView scrolling

        /*
        private void OnTextViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (CBETagPackage.CBEVisibilityMode == (int)CBEOptionPage.VisibilityModes.Always)
                return;

            Span? newVisibleSpan = GetNewVisibleSpan(sender as ITextView);
            if (newVisibleSpan != null && newVisibleSpan.HasValue &&
                (LastVisibleSpan == null || !LastVisibleSpan.HasValue ||
                LastVisibleSpan.Value.Start != newVisibleSpan.Value.Start ||
                LastVisibleSpan.Value.End != newVisibleSpan.Value.End))
            {
                // Reparse if nothing was parsed before
                if (CurrentRegions.Count == 0 && e.NewSnapshot.Length > 0)
                    ReParseSnapshot();

                UpdateCBETagVisibility(newVisibleSpan.Value);
            }
        }

        private void UpdateCBETagVisibility(Span newVisibleSpan)
        {
            // Recalculate visible tags
            bool displayTag;
            bool callChanged = false;
            foreach (CBRegion region in CurrentRegions)
            {
                displayTag = IsTagVisible(region.Span, newVisibleSpan);
                // If one tag changed, call TagsChanged event
                if (displayTag != region.IsDisplayed)
                {
                    callChanged = true;
                    break;
                }
            }

            LastVisibleSpan = newVisibleSpan;
            // Call tags changed if tags visibility changed
            if (callChanged)
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(Snapshot, newVisibleSpan)));
        }


        private Span? GetNewVisibleSpan(ITextView textView)
        {
            if (textView == null) return null;

            if (textView.TextViewLines.Count > 2)
            {
                // Index 0 not yet visible
                var firstVisibleLine = textView.TextViewLines[1];
                // Last index not visible, too
                var lastVisibleLine = textView.TextViewLines[textView.TextViewLines.Count - 2];

                return new Span(firstVisibleLine.Start, lastVisibleLine.End - firstVisibleLine.Start);
            }

            return null;
        }

        private bool IsTagVisible(Span tagSpan, Span? visibleSpan)
        {
            return (CBETagPackage.CBEVisibilityMode == (int)CBEOptionPage.VisibilityModes.Always ||
                visibleSpan == null || !visibleSpan.HasValue ||
                    (tagSpan.Start < visibleSpan.Value.Start
                    && tagSpan.End >= visibleSpan.Value.Start
                    && tagSpan.End <= visibleSpan.Value.End));
        }
        */

        #endregion

        #region ITagger<IntraTextAdornmentTag>


        /*
    public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        yield break;

        if (!CBETagPackage.CBETaggerEnabled)
            yield break;

        if (spans.Count == 0 || CurrentRegions.Count == 0)
            yield break;

        List<CBRegion> currentRegions = CurrentRegions;

        foreach (var region in currentRegions)
        {
            if (IsTagVisible(region.Span, LastVisibleSpan))
            {
                // Mark region as displayed
                region.IsDisplayed = true;
                // Create adornment
                if (region.Adornment == null)
                {
                    region.Adornment = new CBETagControl()
                    {
                        Text = region.Header,
                        IconMoniker = region.IconMoniker,
                        CBRegion = region,
                        DisplayMode = CBETagPackage.CBEDisplayMode
                    };

                    region.Adornment.TagClicked += Adornment_TagClicked;

                    region.Adornment.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                else
                {
                    if (region.Adornment.Visibility == Visibility.Collapsed)
                        region.Adornment.Visibility = Visibility.Visible;

                    if (region.Adornment.Text != region.Header)
                        region.Adornment.Text = region.Header;
                    if (region.Adornment.IconMoniker.Guid != region.IconMoniker.Guid ||
                            region.Adornment.IconMoniker.Id != region.IconMoniker.Id)
                        region.Adornment.IconMoniker = region.IconMoniker;
                    if (region.Adornment.DisplayMode != CBETagPackage.CBEDisplayMode)
                        region.Adornment.DisplayMode = CBETagPackage.CBEDisplayMode;

                }

                // Create tag
                int position = region.Span.End;
                var adornemntTag = new IntraTextAdornmentTag(region.Adornment, null);

                var adornmentSpan = new SnapshotSpan(_SourceBuffer.CurrentSnapshot, position, 0);
                yield return new TagSpan<IntraTextAdornmentTag>(adornmentSpan, adornemntTag);
            }
            else
            {
                // Mark region as not displayed
                region.IsDisplayed = false;
                if (region.Adornment != null && region.Adornment.Visibility == Visibility.Visible)
                {
                    region.Adornment.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

        */
        /*
        private void Adornment_TagClicked(CBRegion region)
        {
            if (_TextView != null)
            {
                SnapshotPoint targetPoint = new SnapshotPoint(_SourceBuffer.CurrentSnapshot, region.Span.Start);
                _TextView.DisplayTextLineContainingBufferPosition(targetPoint, 0, ViewRelativePosition.Top);
                _TextView.Caret.MoveTo(targetPoint);
            }
        }
        */

        #endregion

        #region Parsing

        /*      

        public void UpdateCBERegions(ITextSnapshot newSnapshot, List<CBRegion> newRegions)
        {
            // determine the changed span, and send a changed event with the new spans
            List<Span> oldSpans =
                new List<Span>(CurrentRegions.Select(r => AsSnapshotSpan(r, Snapshot)
                    .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span));
            List<Span> newSpans =
                    new List<Span>(newRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));
            NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
            NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

            // the changed regions are regions that appear in one set or the other, but not both.
            NormalizedSpanCollection removed =
                NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

            // Calculate changed span
            int changeStart = int.MaxValue;
            int changeEnd = -1;

            if (removed.Count > 0)
            {
                changeStart = removed[0].Start;
                changeEnd = removed[removed.Count - 1].End;
            }

            if (newSpans.Count > 0)
            {
                changeStart = Math.Min(changeStart, newSpans[0].Start);
                changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
            }

            Snapshot = newSnapshot;
            CurrentRegions = newRegions;

            //if (changeStart <= changeEnd)
            //{
            //    TextView.VisualElement.Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(Snapshot,
            //        Span.FromBounds(changeStart, changeEnd))));
            //    }));
            //}
        }

        static SnapshotSpan AsSnapshotSpan(CBRegion region, ITextSnapshot snapshot)
        {
            return new SnapshotSpan(snapshot, region.Span);
        }
        */

        #endregion

        #region Options changed

        private void OnPackageOptionChanged(object sender)
        {
            //_changedEvent?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(_SourceBuffer.CurrentSnapshot, Span.FromBounds(0, 1))));
        }

        #endregion

    }
}
