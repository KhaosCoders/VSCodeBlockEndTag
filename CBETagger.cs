using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Threading;
using System.ComponentModel;

namespace CodeBlockEndTag
{
    internal class CBETagger : ITagger<IntraTextAdornmentTag>, IDisposable
    {

        #region Properties

        public ITagAggregator<IClassificationTag> ClassificationTagAggregator { get; private set; }

        public ITextStructureNavigator TextStructureNavigator { get; private set; }

        IWpfTextView TextView { get; set; }

        ITextBuffer SourceBuffer { get; set; }

        ITextSnapshot Snapshot { get; set; }

        bool Disposed { get; set; }

        List<CBRegion> CurrentRegions { get; set; } = new List<CBRegion>();

        Span? LastVisibleSpan { get; set; }

        object _synclock;

        CancellationTokenSource CancellationSource { get; set; }

        System.Threading.Tasks.Task ParserTask { get; set; }

        CBETaggerProvider Provider { get; set; }

        #endregion

        #region Ctor

        internal CBETagger(CBETaggerProvider provider, IWpfTextView textView, ITextBuffer sourceBuffer, ITextStructureNavigator textStructureNavigator, ITagAggregator<IClassificationTag> aggregator)
        {
            _synclock = new object();
            Provider = provider;

            TextStructureNavigator = textStructureNavigator;
            TextView = textView;
            SourceBuffer = sourceBuffer;
            ClassificationTagAggregator = aggregator;
            Snapshot = sourceBuffer.CurrentSnapshot;

            CancellationSource = new CancellationTokenSource();

            if (TextView == null)
                return;

            ReParseSnapshot();
            SourceBuffer.Changed += OnSourceBufferChanged;
            TextView.LayoutChanged += OnTextViewLayoutChanged;
            CBETagPackage.Instance.PackageOptionChanged += OnPackageOptionChanged;
        }

        #endregion

        #region TextView scrolling

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

        #endregion

        #region ITagger<IntraTextAdornmentTag>

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
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

                    var adornmentSpan = new SnapshotSpan(SourceBuffer.CurrentSnapshot, position, 0);
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

        private void Adornment_TagClicked(CBRegion region)
        {
            if (TextView != null)
            {
                SnapshotPoint targetPoint = new SnapshotPoint(SourceBuffer.CurrentSnapshot, region.Span.Start);
                TextView.DisplayTextLineContainingBufferPosition(targetPoint, 0, ViewRelativePosition.Top);
                TextView.Caret.MoveTo(targetPoint);
            }
        }

        #endregion

        #region Parsing

        private void OnSourceBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
            if (e.After != SourceBuffer.CurrentSnapshot)
                return;
            ReParseSnapshot();
        }


        void ReParseSnapshot()
        {
            if (!CBETagPackage.CBETaggerEnabled)
                return;

            if (ParserTask != null && !ParserTask.IsCompleted)
            {
                CancellationSource.Cancel();
                CancellationSource = new CancellationTokenSource();
            }

            ParserTask = System.Threading.Tasks.Task.Factory.StartNew(DoParseSnapshot, CancellationSource.Token, new CancellationTokenSource().Token);
        }


        private void DoParseSnapshot(object token)
        {
            CancellationToken cancelToken = (CancellationToken)token;
            try
            {
                ITextSnapshot newSnapshot = SourceBuffer.CurrentSnapshot;
                SnapshotSpan snapshotSpan = new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length);

                List<CBRegion> newRegions = new List<CBRegion>();

                lock (_synclock)
                {
                    // Get Roslyn classification spans
                    IEnumerable<IMappingTagSpan<IClassificationTag>> classifications = ClassificationTagAggregator.GetTags(snapshotSpan);
                    if (cancelToken.IsCancellationRequested) return;

                    string classificationText = string.Empty;
                    int innerStart;
                    int headerStart;
                    SnapshotSpan innerSpan;
                    SnapshotSpan enclosingSpan;
                    string headerText = null;

                    foreach (IMappingTagSpan<IClassificationTag> classification in classifications)
                    {
                        if (cancelToken.IsCancellationRequested) return;
                        // is classified as punctuation
                        if (classification.Tag.ClassificationType.Classification.ToLower().Contains("punctuation"))
                        {
                            var spans = classification.Span.GetSpans(SourceBuffer);
                            if (spans.Count > 0)
                            {
                                if (cancelToken.IsCancellationRequested) return;
                                // is opening bracet '{'
                                var punctuationSpan = spans.First();
                                classificationText = punctuationSpan.GetText();
                                if ((innerStart = classificationText.IndexOf('{')) >= 0)
                                {
                                    if (cancelToken.IsCancellationRequested) return;
                                    headerStart = -1;
                                    // Get the enclosing span
                                    innerStart += punctuationSpan.Start + 1;
                                    innerSpan = new SnapshotSpan(newSnapshot, innerStart, 1);
                                    enclosingSpan = TextStructureNavigator.GetSpanOfEnclosing(innerSpan);

                                    // Get the header text
                                    if (enclosingSpan.Start < punctuationSpan.Start)
                                    {
                                        // enclosing span contains header
                                        headerText = newSnapshot.GetText(enclosingSpan.Start, punctuationSpan.Start - enclosingSpan.Start);
                                    }
                                    else
                                    {
                                        headerText = GetSpanHeadline(enclosingSpan, newSnapshot, cancelToken, enclosingSpan.Start, out headerStart);
                                    }
                                    if (cancelToken.IsCancellationRequested) return;
                                    if (headerText != null && headerText.Length > 0)
                                        headerText = headerText.Trim();

                                    if (!string.IsNullOrWhiteSpace(headerText) && !headerText.Contains("{"))
                                    {
                                        // Add to list if header text found
                                        if (headerStart < 0)
                                        {
                                            headerStart = enclosingSpan.Start;
                                        }

                                        SnapshotSpan headerSpan = new SnapshotSpan(newSnapshot, headerStart, headerText.Length);
                                        IList<IMappingTagSpan<IClassificationTag>> headerClassifications = ClassificationTagAggregator.GetTags(headerSpan).ToList();
                                        if (cancelToken.IsCancellationRequested) return;
                                        var iconMoniker = IconMonikerSelector.SelectMoniker(headerClassifications, TextStructureNavigator, SourceBuffer);
                                        newRegions.Add(new CBRegion(headerText, new Span(headerStart, enclosingSpan.Span.End - headerStart), iconMoniker));
                                    }
                                }
                            }
                        }
                    }
                }

                UpdateCBERegions(newSnapshot, newRegions);
            }
            catch (Exception ex)
            {

            }
        }

        string GetSpanHeadline(SnapshotSpan span, ITextSnapshot textSnapshot, CancellationToken cancelToken, int maxEnd, out int headerStart)
        {
            headerStart = -1;
            SnapshotSpan currentSpan = span;
            bool first = true;
            bool found = false;
            while ((currentSpan = first ? currentSpan :
                TextStructureNavigator.GetSpanOfEnclosing(currentSpan)) != null)
            {
                first = false;
                if (cancelToken.IsCancellationRequested) return string.Empty;
                IList<IMappingTagSpan<IClassificationTag>> classifications = ClassificationTagAggregator.GetTags(currentSpan).ToList();
                if (classifications.Any())
                {
                    // Accept spans that don't start with punctuation (no braces, commas, points, ...)
                    found = !classifications.First().Tag.ClassificationType.Classification.ToLower().Contains("punctuation");

                    if (!found)
                    {
                        // Accept lambdas
                        foreach(var classification in classifications)
                        {
                            if (cancelToken.IsCancellationRequested) return string.Empty;
                            if (classification.Tag.ClassificationType.Classification.ToLower().Contains("operator"))
                            {
                                var spans = classification.Span.GetSpans(SourceBuffer);
                                if (spans.Any())
                                {
                                    var operatorSpan = spans.First();
                                    if (operatorSpan.Start < maxEnd && textSnapshot.GetText(operatorSpan).Contains("=>"))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (found)
                    {
                        headerStart = currentSpan.Start;
                        Span headerSpan = new Span(headerStart, Math.Min(maxEnd, currentSpan.Span.End) - headerStart);
                        string headerText = textSnapshot.GetText(headerSpan);
                        if (cancelToken.IsCancellationRequested) return string.Empty;
                        // Recognize "else if" too
                        if (headerText.StartsWith("if"))
                        {
                            currentSpan = TextStructureNavigator.GetSpanOfEnclosing(currentSpan);
                            found = classifications.First().Tag.ClassificationType.Classification.ToLower().Contains("keyword");
                            if (cancelToken.IsCancellationRequested) return string.Empty;
                            if (found)
                            {
                                Span headerSpan2 = new Span(currentSpan.Start, Math.Min(maxEnd, currentSpan.Span.End) - currentSpan.Start);
                                string headerText2 = textSnapshot.GetText(headerSpan2);
                                if (cancelToken.IsCancellationRequested) return string.Empty;
                                if (headerText2.StartsWith("else"))
                                {
                                    headerStart = currentSpan.Start;
                                    headerText = headerText2;
                                }
                            }
                        }
                        return headerText;
                    }
                }
            }
            return null;
        }

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

            if (changeStart <= changeEnd)
            {
                TextView.VisualElement.Dispatcher.BeginInvoke(new Action(() =>
                {
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(Snapshot,
                    Span.FromBounds(changeStart, changeEnd))));
                }));
            }
        }

        static SnapshotSpan AsSnapshotSpan(CBRegion region, ITextSnapshot snapshot)
        {
            return new SnapshotSpan(snapshot, region.Span);
        }

        #endregion

        #region Options changed

        private void OnPackageOptionChanged(object sender)
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(Snapshot, Span.FromBounds(0, Snapshot.Length))));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.Disposed)
            {
                if (disposing)
                {
                    CBETagPackage.Instance.PackageOptionChanged -= OnPackageOptionChanged;
                    TextView.LayoutChanged -= OnTextViewLayoutChanged;
                    TextView = null;
                    SourceBuffer.Changed -= OnSourceBufferChanged;
                    SourceBuffer = null;
                    ClassificationTagAggregator = null;
                    Snapshot = null;
                }
                Disposed = true;
            }
        }

        #endregion

    }
}
