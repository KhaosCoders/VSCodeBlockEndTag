// Thanks to https://github.com/jaredpar/ControlCharAdornmentSample/blob/master/CharDisplayTaggerSource.cs

using CodeBlockEndTag.Extensions;
using CodeBlockEndTag.Model;
using CodeBlockEndTag.Shell;
using CommunityToolkit.HighPerformance;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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
        new([]);

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
    private readonly Dictionary<AdornmentDataKey, CBAdornmentData> _adornmentCache = new(50);
    private struct AdornmentDataKey(int start, int end)
    {
        public int StartPosition = start;
        public int EndPosition = end;
    }

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
            InvalidateSpan(Span.FromBounds(start, end), false);
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
        int oldEnd = textChange.OldEnd;
        int oldPosition = textChange.OldPosition;
        int delta = textChange.Delta;

        foreach (var entry in _adornmentCache.ToList())
        {
            var adornment = entry.Value;
            bool isHeaderAfterChange = adornment.HeaderStartPosition > oldEnd;
            if (!(isHeaderAfterChange || adornment.EndPosition < oldPosition))
            {
                if (adornment.Adornment is CBETagControl tag)
                {
                    tag.TagClicked -= Adornment_TagClicked;
                }
                _adornmentCache.Remove(entry.Key);
            }

            if (isHeaderAfterChange)
            {
                adornment.Move(delta);
            }
        }
    }

    #endregion

    #region ITagger<IntraTextAdornmentTag>

    IEnumerable<ITagSpan<IntraTextAdornmentTag>> ITagger<IntraTextAdornmentTag>.GetTags(NormalizedSnapshotSpanCollection spans)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // Check if content type (language) is supported and active for tagging
        if (!CBETagPackage.IsLanguageSupported(_TextView.TextBuffer.ContentType.TypeName))
        {
            yield break;
        }

        // Second chance to hook up events
        InitializeCBEPackage();

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
        add => _changedEvent += value;
        remove => _changedEvent -= value;
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
        if (span.Length > 1000 && _VisibleSpan.HasValue)
        {
            var overlap = span.Overlap(_VisibleSpan.Value);
            if (overlap.HasValue)
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
        List<ITagSpan<IntraTextAdornmentTag>> list = new(32);
        var offset = span.Start.Position;
        var snapshot = span.Snapshot;

#if DEBUG
        // Stop time
        _watch ??= new System.Diagnostics.Stopwatch();
        _watch.Restart();
#endif

        try
        {
            var codeSpan = span.GetText().AsSpan();
            int specialCharIndex = 0;
            int lastSpecialCharIndex = -1;
            bool isSingleLineComment = false;
            bool isMultiLineComment = false;
            while ((specialCharIndex = codeSpan.Slice(lastSpecialCharIndex + 1).IndexOfAny(['}', '/', '*', '\r', '\n'])) >= 0)
            {
                lastSpecialCharIndex += 1 + specialCharIndex;
                char prevChar = lastSpecialCharIndex > 0 ? codeSpan[lastSpecialCharIndex - 1] : '\0';

                // Skip comments
                switch (codeSpan[lastSpecialCharIndex])
                {
                    case '/' when prevChar == '/':
                        isSingleLineComment = true;
                        continue;
                    case '/' when prevChar == '*':
                        if (!isMultiLineComment && list.Count > 0)
                        {
                            // Multiline comment was not started in this span
                            // Every tag until now was inside a comment
                            list.RemoveAll(tag =>
                            {
                                if (tag.Tag.Adornment is CBETagControl { AdornmentData: CBAdornmentData adornment } tagCtrl)
                                {
                                    tagCtrl.TagClicked -= Adornment_TagClicked;
                                    AdornmentDataKey key = new(adornment.StartPosition, adornment.EndPosition);
                                    _adornmentCache.Remove(key);
                                }
                                return true;
                            });
                        }
                        isMultiLineComment = false;
                        continue;
                    case '*' when prevChar == '/':
                        isMultiLineComment = true;
                        continue;
                    case '/' or '*': continue;
                    case '\r' or '\n':
                        isSingleLineComment = false;
                        continue;
                    case '}' when isSingleLineComment || isMultiLineComment:
                        // } inside comment
                        continue;
                    case '}' when prevChar == '{':
                        // empty code block {}
                        continue;
                }

                // only legit } end up here
                int cbEndPosition = lastSpecialCharIndex;

                // create inner span to navigate to get code block start
                var cbSpan = _TextStructureNavigator.GetSpanOfEnclosing(new SnapshotSpan(snapshot, offset + lastSpecialCharIndex - 1, 1));
                int cbStartPosition = cbSpan.Start;

                var cbCodeSpan = cbSpan.GetText().AsSpan();

                // Don't display tag for code blocks on same line
                if (cbCodeSpan.IndexOf('\n') < 0)
                {
                    continue;
                }

                // getting the code blocks header
                // eigher from cbSpan or the span before that
                int maxEndPosition = (cbCodeSpan[0] == '{') ? cbSpan.Start : offset + lastSpecialCharIndex;

                int indexOfFirstBracet = cbCodeSpan.IndexOf('{');
                if (indexOfFirstBracet > 0)
                {
                    maxEndPosition = cbSpan.Start + indexOfFirstBracet;
                }
                var cbHeader = GetCodeBlockHeader(snapshot, cbSpan, out int cbHeaderPosition, maxEndPosition);

                // Skip tag if option "only when header not visible"
                if (_VisibleSpan != null && !IsTagVisible(cbHeaderPosition, cbEndPosition, _VisibleSpan, snapshot))
                {
                    continue;
                }

                // Header as single line without too much spaces or tabs
                if (!cbHeader.IsEmpty)
                {
                    PooledStringBuilder sbPool = PooledStringBuilder.GetInstance();
                    StringBuilder stringBuilder = sbPool.Builder;
                    char lastChar = '\0';
                    int lastNonSpaceIndex = -1;
                    for (int i = 0; i < cbHeader.Length; i++)
                    {
                        char chr = cbHeader[i];
                        if (char.IsControl(chr) || chr == '\n' || chr == '\r')
                        {
                            continue;
                        }
                        if (chr == '\t')
                        {
                            chr = ' ';
                        }
                        if (chr == ' ' && (stringBuilder.Length == 0 || lastChar == ' '))
                        {
                            continue;
                        }
                        stringBuilder.Append(chr);
                        lastChar = chr;
                        if (chr != ' ')
                        {
                            lastNonSpaceIndex = stringBuilder.Length;
                        }
                    }
                    cbHeader = sbPool.ToStringAndFree(0, lastNonSpaceIndex);
                }

                // use cache or create new tag
                AdornmentDataKey adornmentDataKey = new(cbStartPosition, cbEndPosition);
                _adornmentCache.TryGetValue(adornmentDataKey, out var cbAdornmentData);

                CBETagControl tagElement;
                if (cbAdornmentData.Adornment is CBETagControl tagControl)
                {
                    tagElement = tagControl;
                }
                else
                {
                    // Icon for tag
                    ImageMoniker iconMoniker =
                        CBETagPackage.CBEDisplayMode == (int)DisplayModes.Text ||
                        cbHeader.IsWhiteSpace() ||
                        cbHeader.IndexOf("{") >= 0
                            ? Microsoft.VisualStudio.Imaging.KnownMonikers.QuestionMark
                            : IconMonikerSelector.SelectMoniker(cbHeader);

                    // create new adornment
                    tagElement = new CBETagControl()
                    {
                        Text = cbHeader.ToString(),
                        IconMoniker = iconMoniker,
                        DisplayMode = CBETagPackage.CBEDisplayMode,
                        Margin = new System.Windows.Thickness(CBETagPackage.CBEMargin, 0, 0, 0)
                    };

                    tagElement.TagClicked += Adornment_TagClicked;

                    cbAdornmentData = new CBAdornmentData(cbStartPosition, cbEndPosition, cbHeaderPosition, tagElement);
                    tagElement.AdornmentData = cbAdornmentData;
                    _adornmentCache.Add(adornmentDataKey, cbAdornmentData);
                }

                tagElement.SetResourceReference(CBETagControl.LineHeightProperty, EndTagColors.FontSizeKey);
                tagElement.SetResourceReference(CBETagControl.TextColorProperty, EndTagColors.GetForegroundResourceKey(_TextView.TextBuffer.ContentType.TypeName));

                // Add new tag to list
                IntraTextAdornmentTag cbTag = new(tagElement, null);
                SnapshotSpan cbSnapshotSpan = new(snapshot, offset + lastSpecialCharIndex + 1, 0);
                TagSpan<IntraTextAdornmentTag> cbTagSpan = new(cbSnapshotSpan, cbTag);
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
    private ReadOnlySpan<char> GetCodeBlockHeader(
        ITextSnapshot snapshot,
        SnapshotSpan cbSpan,
        out int headerStart,
        int maxEndPosition = 0)
    {
        var currentSpan = cbSpan;
        int loops = 0;
        // check all enclosing spans until the header is complete
        do
        {
            // get text of current span
            headerStart = currentSpan.Start;
            Span headerSpan = Span.FromBounds(headerStart, Math.Min(maxEndPosition, currentSpan.Span.End));
            if (headerSpan.Length == 0)
            {
                continue;
            }

            var textSpan = snapshot.GetText(headerSpan).AsSpan().Trim();

            // found header if it's not empty
            if (textSpan.IsEmpty)
            {
                continue;
            }

            // recognize "else if" too
            if (textSpan.StartsWith("if") && ((currentSpan = _TextStructureNavigator.GetSpanOfEnclosing(currentSpan)) != default))
            {
                // check what comes before the "if"
                Span outerSpan = Span.FromBounds(currentSpan.Start, Math.Min(maxEndPosition, currentSpan.Span.End));
                string headerText2 = snapshot.GetText(outerSpan);
                if (headerText2.StartsWith("else"))
                {
                    headerStart = outerSpan.Start;
                    return headerText2;
                }
            }
            else if (textSpan.IndexOfAny('\r', '\n') >= 0)
            {
                // skip annotations
                int indexOfLineBreak;
                int openBracets = 0;
                bool annotaions = true;
                PooledStringBuilder pooledStringBuilder = PooledStringBuilder.GetInstance();
                StringBuilder stringBuilder = pooledStringBuilder.Builder;
                while ((indexOfLineBreak = textSpan.IndexOfAny('\r', '\n')) >= 0 || !textSpan.IsEmpty)
                {
                    ReadOnlySpan<char> lineSpan;
                    if (indexOfLineBreak < 0)
                    {
                        lineSpan = textSpan;
                        textSpan = string.Empty;
                    }
                    else
                    {
                        lineSpan = textSpan.Slice(0, indexOfLineBreak);
                        textSpan = textSpan.Slice(indexOfLineBreak + 1).TrimStart();
                    }

                    if (lineSpan.IsEmpty || lineSpan.IsWhiteSpace())
                    {
                        continue;
                    }

                    if (annotaions && (lineSpan[0] == '[' || openBracets > 0))
                    {
                        openBracets += lineSpan.Count('[');
                        openBracets -= lineSpan.Count(']');
                        continue;
                    }

                    annotaions = false;

                    if (stringBuilder.Length > 0 && !char.IsWhiteSpace(stringBuilder[stringBuilder.Length - 1]))
                    {
                        stringBuilder.Append(' ');
                    }

                    stringBuilder.Append(lineSpan.Trim().ToArray());
                }

                return pooledStringBuilder.ToStringAndFree();
            }

            return textSpan;

            // get next enclosing span of current span
        } while (loops++ <= 10 && (currentSpan = _TextStructureNavigator.GetSpanOfEnclosing(currentSpan)) != default);

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
        if (_TextView == null)
        {
            return;
        }

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

    #endregion

    #region Options changed

    /// <summary>
    /// Handles the event when any package option is changed
    /// </summary>
    private void OnPackageOptionChanged(object sender)
    {
        InvalidateSpan(
            Span.FromBounds(
                Math.Max(0, _VisibleSpan?.Start ?? 0),
                Math.Max(1, _VisibleSpan?.End ?? 1)));
    }

    /// <summary>
    /// Invalidates all cached tags within or after the given span
    /// </summary>
    private void InvalidateSpan(Span invalidateSpan, bool clearCache = true)
    {
        // Remove tags from cache
        if (clearCache)
        {
            foreach (var entry in _adornmentCache.ToList())
            {
                var adornment = entry.Value;
                if (adornment.HeaderStartPosition < invalidateSpan.Start && adornment.EndPosition < invalidateSpan.Start)
                {
                    continue;
                }

                if (adornment.Adornment is CBETagControl tag)
                {
                    tag.TagClicked -= Adornment_TagClicked;
                }
                _adornmentCache.Remove(entry.Key);
            }
        }

        // Invalidate span
        var snapshop = _TextView.TextBuffer.CurrentSnapshot;
        if (invalidateSpan.End <= snapshop.Length)
        {
            _changedEvent?.Invoke(this, new(new(snapshop, invalidateSpan)));
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
        if (_Disposed || !disposing)
        {
            return;
        }

        CBETagPackage.Instance?.PackageOptionChanged -= OnPackageOptionChanged;

        _TextView?.LayoutChanged -= OnTextViewLayoutChanged;
        _TextView?.TextBuffer?.Changed -= TextBuffer_Changed;

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
    /// <param name="start">Start position of code block</param>
    /// <param name="end">End position of code block</param>
    /// <param name="visibleSpan">the visible span in the textview</param>
    /// <param name="snapshot">reference to text snapshot. Used for caret check</param>
    /// <returns>true if the tag is visible (or if all tags are shown)</returns>
    private bool IsTagVisible(int start, int end, Span? visibleSpan, ITextSnapshot snapshot)
    {
        // Check general condition
        if (CBETagPackage.CBEVisibilityMode == (int)VisibilityModes.Always || !visibleSpan.HasValue)
        {
            return true;
        }

        // Check non-visible span
        var val = visibleSpan.Value;
        if (!(start < val.Start && end >= val.Start && end <= val.End))
        {
            return false;
        }

        // Check if caret is in this line
        if (_TextView == null)
        {
            return true;
        }

        var caretIndex = _TextView.Caret.Position.BufferPosition.Position;
        var lineStart = Math.Min(caretIndex, end);
        var lineEnd = Math.Max(caretIndex, end);

        // Same line -> not visible
        if (lineStart == lineEnd)
        {
            return false;
        }

        // hide tag if caret is in same line
        if (lineStart >= 0 && lineEnd <= snapshot.Length)
        {
            string line = snapshot.GetText(lineStart, lineEnd - lineStart);
            if (!line.Contains('\n'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the visible span for the given textview
    /// </summary>
    private Span? GetVisibleSpan(ITextView textView)
    {
        ITextViewLineCollection lines = textView?.TextViewLines;
        int lineCount = lines?.Count ?? 0;

        if (lines == null || lineCount <= 2)
        {
            return null;
        }

        // Index 0 not yet visible
        // Last index not visible, too
        return Span.FromBounds(lines[1].Start, lines[lineCount - 2].End);
    }

    #endregion

    #region TextView scrolling

    private void OnTextViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
    {
        // get new visible span
        var visibleSpan = GetVisibleSpan(_TextView);
        if (!visibleSpan.HasValue)
        {
            return;
        }

        // only if new visible span is different from old
        if (_VisibleSpan.HasValue &&
            _VisibleSpan.Value.Start == visibleSpan.Value.Start &&
            _VisibleSpan.Value.End >= visibleSpan.Value.End)
        {
            return;
        }

        // invalidate new and/or old visible span
        List<Span> invalidSpans = new(2);
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

        // Skip if all tags are shown always
        if (CBETagPackage.CBEVisibilityMode == (int)VisibilityModes.Always)
        {
            return;
        }

        // refresh tags
        foreach (var span in invalidSpans)
        {
            InvalidateSpan(span, false);
        }
    }

    #endregion

}
