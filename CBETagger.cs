// Thanks to https://github.com/jaredpar/ControlCharAdornmentSample/blob/master/CharDisplayTaggerSource.cs

using CodeBlockEndTag.Extensions;
using CodeBlockEndTag.Model;
using CodeBlockEndTag.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace CodeBlockEndTag;

/// <summary>f
/// This tagger provides editor tags that are inserted into the TextView (IntraTextAdornmentTags)
/// The tags are added after each code block encapsulated by curly bracets: { ... }
/// The tags will show the code blocks condition, or whatever serves as header for the block
/// By clicking on a tag, the editor will jump to that code blocks header
/// </summary>
internal class CBETagger : ITagger<IntraTextAdornmentTag>, IDisposable
{
    private static readonly ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>> EmptyTagColllection =
        new(new List<ITagSpan<IntraTextAdornmentTag>>());

    #region Properties & Fields

    // EventHandler for ITagger<IntraTextAdornmentTag> tags changed event
    private EventHandler<SnapshotSpanEventArgs> _changedEvent;

    /// <summary>
    /// Service by VisualStudio for fast navigation in structured texts
    /// </summary>
    private readonly ITextStructureNavigator _TextStructureNavigator;

    /// <summary>
    /// The TextView this tagger is assigned to
    /// </summary>
    private readonly IWpfTextView _TextView;

    /// <summary>
    /// This is a list of already created adornment tags used as cache
    /// </summary>
    private readonly List<CBAdornmentData> _adornmentCache = new();

    /// <summary>
    /// This is the visible span of the textview
    /// </summary>
    private Span? _VisibleSpan;

    /// <summary>
    /// Is set, when the instance is disposed
    /// </summary>
    private bool _Disposed;

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
        {
            throw new ArgumentNullException("The arguments of CBETagger can't be null");
        }

        ThreadHelper.ThrowIfNotOnUIThread();

        _TextView = textView;

        // Getting services provided by VisualStudio
        _TextStructureNavigator = provider.GetTextStructureNavigator(_TextView.TextBuffer);

        // Hook up events
        _TextView.TextBuffer.Changed += TextBuffer_Changed;
        _TextView.LayoutChanged += OnTextViewLayoutChanged;
        _TextView.Caret.PositionChanged += Caret_PositionChanged;

        // Listen for package events
        InitializeCBEPackage();
    }

    #endregion

    #region TextBuffer changed

    private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
    {
        var start = Math.Min(e.OldPosition.BufferPosition.Position, e.NewPosition.BufferPosition.Position);
        var end = Math.Max(e.OldPosition.BufferPosition.Position, e.NewPosition.BufferPosition.Position);
        if (start != end)
        {
            InvalidateSpan(new Span(start, end - start), false);
        }
    }

    private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
    {
        foreach (var textChange in e.Changes)
        {
            OnTextChanged(textChange);
        }
    }

    private void OnTextChanged(ITextChange textChange)
    {
        // remove or update tags in adornment cache
        List<CBAdornmentData> remove = new();
        foreach (CBAdornmentData adornment in _adornmentCache)
        {
            if (!(adornment.HeaderStartPosition > textChange.OldEnd || adornment.EndPosition < textChange.OldPosition))
            {
                remove.Add(adornment);
            }
            else if (adornment.HeaderStartPosition > textChange.OldEnd)
            {
                adornment.Move(textChange.Delta);
            }
        }

        foreach (var adornment in remove)
        {
            RemoveFromCache(adornment);
        }
    }

    private void RemoveFromCache(CBAdornmentData adornment)
    {
        if (adornment.Adornment is CBETagControl tag)
        {
            tag.TagClicked -= Adornment_TagClicked;
        }
        _adornmentCache.Remove(adornment);
    }

    #endregion

    #region ITagger<IntraTextAdornmentTag>

    IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // Check if content type (language) is supported and active for tagging
        if (CBETagPackage.IsLanguageSupported(_TextView.TextBuffer.ContentType.TypeName))
        {
            // Second chance to hook up events
            InitializeCBEPackage();

            foreach (var span in spans)
            {
                var tags = GetTags(span);
                foreach (var tag in tags)
                {
                    yield return tag;
                }
            }
        }
    }

    event EventHandler<SnapshotSpanEventArgs> ITagger<IntraTextAdornmentTag>.TagsChanged
    { add => _changedEvent += value; remove => _changedEvent -= value;
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

        // if big span, return only tags for visible area
        if (span.Length > 1000 && _VisibleSpan != null && _VisibleSpan.HasValue)
        {
            var overlap = span.Overlap(_VisibleSpan.Value);
            if (overlap != null && overlap.HasValue)
            {
                span = overlap.Value;
                if (span.Length == 0)
                {
                    return EmptyTagColllection;
                }
            }
        }

        return GetTagsCore(span);
    }

#if DEBUG
    private System.Diagnostics.Stopwatch _watch;
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
        bool isSingleLineComment = false;
        bool isMultiLineComment = false;

#if DEBUG
        // Stop time
        _watch ??= new System.Diagnostics.Stopwatch();
        _watch.Restart();
#endif

        try
        {
            // Find all closing bracets
            for (int i = 0; i < span.Length; i++)
            {
                var position = i + offset;
                var chr = snapshot[position];

                // Skip comments
                switch (chr)
                {
                    case '/':
                        if (position > 0)
                        {
                            if (snapshot[position - 1] == '/')
                            {
                                isSingleLineComment = true;
                            }

                            if (snapshot[position - 1] == '*')
                            {
                                if (!isMultiLineComment)
                                {
                                    // Multiline comment was not started in this span
                                    // Every tag until now was inside a comment
                                    foreach (var tag in list)
                                    {
                                        CBAdornmentData? adornment = (tag.Tag.Adornment as CBETagControl)?.AdornmentData;
                                        if (adornment.HasValue)
                                        {
                                            RemoveFromCache(adornment.Value);
                                        }
                                    }
                                    list.Clear();
                                }
                                isMultiLineComment = false;
                            }
                        }
                        break;
                    case '*':
                        if (position > 0 && snapshot[position - 1] == '/')
                        {
                            isMultiLineComment = true;
                        }

                        break;
                    case (char)10:
                        isSingleLineComment = false;
                        break;
                    case (char)13:
                        isSingleLineComment = false;
                        break;
                }

                if (chr != '}' || isSingleLineComment || isMultiLineComment)
                {
                    continue;
                }

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
                {
                    continue;
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
                    cbHeader = GetCodeBlockHeader(cbSpan, out cbHeaderPosition, position);
                }

                // Trim header
                if (!string.IsNullOrEmpty(cbHeader))
                {
                    cbHeader = cbHeader.Trim()
                        .Replace(Environment.NewLine, "")
                        .Replace('\t', ' ');
                    // Strip unnecessary spaces
                    while (cbHeader.Contains("  "))
                    {
                        cbHeader = cbHeader.Replace("  ", " ");
                    }
                }

                // Skip tag if option "only when header not visible"
                if (_VisibleSpan != null && !IsTagVisible(cbHeaderPosition, cbEndPosition, _VisibleSpan, snapshot))
                {
                    continue;
                }

                var iconMoniker = Microsoft.VisualStudio.Imaging.KnownMonikers.QuestionMark;
                if (CBETagPackage.CBEDisplayMode != (int)DisplayModes.Text &&
                    !string.IsNullOrWhiteSpace(cbHeader) && !cbHeader.Contains("{"))
                {
                    iconMoniker = IconMonikerSelector.SelectMoniker(cbHeader);
                }

                // use cache or create new tag
                cbAdornmentData = _adornmentCache
                                    .Find(o =>
                                        o.StartPosition == cbStartPosition &&
                                        o.EndPosition == cbEndPosition);

                if (cbAdornmentData.Adornment is CBETagControl tagControl)
                {
                    tagElement = tagControl;
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

                tagElement.SetResourceReference(CBETagControl.LineHeightProperty, EndTagColors.FontSizeKey);
                tagElement.SetResourceReference(CBETagControl.TextColorProperty, EndTagColors.GetForegroundResourceKey(_TextView.TextBuffer.ContentType.TypeName));

                // Add new tag to list
                cbTag = new IntraTextAdornmentTag(tagElement, null);
                cbSnapshotSpan = new SnapshotSpan(snapshot, position + 1, 0);
                cbTagSpan = new TagSpan<IntraTextAdornmentTag>(cbSnapshotSpan, cbTag);
                list.Add(cbTagSpan);
            }
        }
        catch (NullReferenceException)
        {
            // May happen, when closing a text editor
        }

#if DEBUG
        _watch.Stop();
        if (_watch.Elapsed.Milliseconds > 100)
        {
            System.Diagnostics.Debug.WriteLine("Time elapsed: " + _watch.Elapsed +
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
    private string GetCodeBlockHeader(SnapshotSpan cbSpan, out int headerStart, int maxEndPosition = 0)
    {
        if (maxEndPosition == 0)
        {
            maxEndPosition = cbSpan.Start;
        }

        var snapshot = cbSpan.Snapshot;
        var currentSpan = cbSpan;

        // set end of header to first start of code block {
        for (int i = cbSpan.Start; i < cbSpan.End; i++)
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
            {
                break;
            }

            // get text of current span
            headerStart = currentSpan.Start;
            headerSpan = new Span(headerStart, Math.Min(maxEndPosition, currentSpan.Span.End) - headerStart);
            if (headerSpan.Length == 0)
            {
                continue;
            }

            headerText = snapshot.GetText(headerSpan);

            // found header if it begins with a letter or contains a lambda
            if (!string.IsNullOrWhiteSpace(headerText))
            //&& (char.IsLetter(headerText[0]) || headerText[0]=='[' || headerText.Contains("=>")))
            {
                // recognize "else if" too
                if (headerText.StartsWith("if") && ((currentSpan = _TextStructureNavigator.GetSpanOfEnclosing(currentSpan)) != default))
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
                            {
                                headerText += Environment.NewLine;
                            }

                            headerText += trimmedline;
                        }
                    }
                }
                return headerText;
            }

            // get next enclosing span of current span
        } while ((currentSpan = _TextStructureNavigator.GetSpanOfEnclosing(currentSpan)) != default);

        // No header found
        headerStart = -1;
        return null;
    }

    #endregion

    #region Tag Clicked Handler

    /// <summary>
    /// Handles the click event on a tag
    /// </summary>
    private void Adornment_TagClicked(CBAdornmentData adornment, bool jumpToHead)
    {
        if (_TextView != null)
        {
            SnapshotPoint targetPoint;
            if (jumpToHead)
            {
                // Jump to header
                targetPoint = new SnapshotPoint(_TextView.TextBuffer.CurrentSnapshot, adornment.HeaderStartPosition);
                _TextView.DisplayTextLineContainingBufferPosition(targetPoint, 30, ViewRelativePosition.Top);
            }
            else
            {
                // Set caret behind closing bracet
                targetPoint = new SnapshotPoint(_TextView.TextBuffer.CurrentSnapshot, adornment.EndPosition + 1);
            }
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
        int start = Math.Max(0, (_VisibleSpan?.Start) ?? 0);
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
        if (invalidateSpan.End <= _TextView.TextBuffer.CurrentSnapshot.Length)
        {
            _changedEvent?.Invoke(this, new SnapshotSpanEventArgs(
                new SnapshotSpan(_TextView.TextBuffer.CurrentSnapshot, invalidateSpan)));
        }
    }

    /// <summary>
    /// Hooks up events at the package
    /// Due to AsyncPackage usage, the Tagger may be initialized before the Package
    /// So be safe about this
    /// </summary>
    private void InitializeCBEPackage()
    {
        if (isPackageInitialized || CBETagPackage.Instance == null || _Disposed)
        {
            return;
        }

        ThreadHelper.ThrowIfNotOnUIThread();

        CBETagPackage.Instance.PackageOptionChanged += OnPackageOptionChanged;
        FontAndColorDefaultsCSharpTags.Instance.EnsureFontAndColorsInitialized();
        isPackageInitialized = true;
    }

    private bool isPackageInitialized;

    #endregion

    #region IDisposable

    /// <summary>
    /// Clean up all events and references
    /// </summary>
    private void Dispose(bool disposing)
    {
        if (_Disposed)
        {
            return;
        }

        if (disposing)
        {
            if (CBETagPackage.Instance != null)
            {
                CBETagPackage.Instance.PackageOptionChanged -= OnPackageOptionChanged;
            }
            if (_TextView != null)
            {
                _TextView.LayoutChanged -= OnTextViewLayoutChanged;
                if (_TextView.TextBuffer != null)
                {
                    _TextView.TextBuffer.Changed -= TextBuffer_Changed;
                }
            }
            _Disposed = true;
        }
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
    /// <param name="start">Start position of code block</param>
    /// <param name="end">End position of code block</param>
    /// <param name="visibleSpan">the visible span in the textview</param>
    /// <param name="snapshot">reference to text snapshot. Used for caret check</param>
    /// <returns>true if the tag is visible (or if all tags are shown)</returns>
    private bool IsTagVisible(int start, int end, Span? visibleSpan, ITextSnapshot snapshot)
    {
        bool isVisible = false;
        // Check general condition
        if (CBETagPackage.CBEVisibilityMode == (int)VisibilityModes.Always
            || visibleSpan == null || !visibleSpan.HasValue)
        {
            isVisible = true;
        }
        // Check visible span
        if (!isVisible)
        {
            var val = visibleSpan.Value;
            isVisible = start < val.Start && end >= val.Start && end <= val.End;
        }
        // Check if caret is in this line
        if (isVisible && _TextView != null)
        {
            var caretIndex = _TextView.Caret.Position.BufferPosition.Position;
            var lineStart = Math.Min(caretIndex, end);
            var lineEnd = Math.Max(caretIndex, end);
            if (lineStart == lineEnd)
            {
                isVisible = false;
            }
            else if (lineStart >= 0 && lineEnd <= snapshot.Length)
            {
                string line = snapshot.GetText(lineStart, lineEnd - lineStart);
                if (!line.Contains('\n'))
                {
                    isVisible = false;
                }
            }
        }
        return isVisible;
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
        if (visibleSpan == null || !visibleSpan.HasValue)
        {
            return;
        }

        // only if new visible span is different from old
        if (!_VisibleSpan.HasValue ||
            _VisibleSpan.Value.Start != visibleSpan.Value.Start ||
            _VisibleSpan.Value.End < visibleSpan.Value.End)
        {
            // invalidate new and/or old visible span
            List<Span> invalidSpans = new();
            var newSpan = visibleSpan.Value;
            if (!_VisibleSpan.HasValue)
            {
                invalidSpans.Add(newSpan);
            }
            else
            {
                var oldSpan = _VisibleSpan.Value;
                // invalidate two spans if old and new do not overlap
                if (newSpan.Start > oldSpan.End || newSpan.End < oldSpan.Start)
                {
                    invalidSpans.Add(newSpan);
                    invalidSpans.Add(oldSpan);
                }
                else
                {
                    // invalidate one big span (old and new joined)
                    invalidSpans.Add(newSpan.Join(oldSpan));
                }
            }

            _VisibleSpan = visibleSpan;

            // refresh tags
            foreach (var span in invalidSpans)
            {
                if (CBETagPackage.CBEVisibilityMode != (int)VisibilityModes.Always)
                {
                    InvalidateSpan(span, false);
                }
            }
        }
    }

    #endregion

}
