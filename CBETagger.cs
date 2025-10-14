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
using Microsoft.VisualStudio.Text.Outlining;
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
    /// The outlining manager for this text view (provides collapsible regions)
    /// </summary>
    private readonly IOutliningManager _OutliningManager;

    /// <summary>
    /// Whether outlining is supported and enabled for this buffer
    /// </summary>
    private readonly bool _OutliningSupported;

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
    /// Timer for debouncing layout changed events
    /// </summary>
    private System.Windows.Threading.DispatcherTimer _LayoutChangedDebounceTimer;

    /// <summary>
    /// Pending span to invalidate after debounce
    /// </summary>
    private Span? _PendingInvalidateSpan;

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

        // Get outlining manager for collapsible regions
        _OutliningManager = provider.OutliningManagerService?.GetOutliningManager(_TextView);
        _OutliningSupported = _OutliningManager != null && _OutliningManager.Enabled;

        // Hook up events
        _TextView.TextBuffer.Changed += TextBuffer_Changed;
        _TextView.LayoutChanged += OnTextViewLayoutChanged;
        _TextView.Caret.PositionChanged += Caret_PositionChanged;

        // Hook up outlining events if supported
        if (_OutliningManager != null)
        {
            _OutliningManager.RegionsChanged += OnOutliningRegionsChanged;
        }

        // Initialize debounce timer for layout changes (150ms delay)
        _LayoutChangedDebounceTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _LayoutChangedDebounceTimer.Tick += OnLayoutChangedDebounceTimerTick;

        // Listen for package events
        InitializeCBEPackage();
    }

    #endregion

    #region TextBuffer changed

    private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"Caret_PositionChanged called: VisibilityMode={CBETagPackage.CBEVisibilityMode}");
#endif

        var oldPos = e.OldPosition.BufferPosition.Position;
        var newPos = e.NewPosition.BufferPosition.Position;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"  oldPos={oldPos}, newPos={newPos}");
#endif

        // Get the lines containing the old and new caret positions
        var snapshot = _TextView.TextBuffer.CurrentSnapshot;
        var oldLine = snapshot.GetLineFromPosition(oldPos);
        var newLine = snapshot.GetLineFromPosition(newPos);

        // Invalidate from the start of the first line to the end of the last line
        var start = Math.Min(oldLine.Start.Position, newLine.Start.Position);
        var end = Math.Max(oldLine.End.Position, newLine.End.Position);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"  -> Invalidating span: start={start}, end={end} (full lines)");
#endif

        InvalidateSpan(Span.FromBounds(start, end), false);
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

    private void OnOutliningRegionsChanged(object sender, RegionsChangedEventArgs e)
    {
        // Invalidate cache for affected regions
        var affectedSpan = e.AffectedSpan;
        InvalidateSpan(affectedSpan, clearCache: true);
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

#if DEBUG
        System.Diagnostics.Debug.WriteLine($">>> GetTags called with {spans.Count} span(s)");
        foreach (var span in spans)
        {
            System.Diagnostics.Debug.WriteLine($"    Span: {span.Start.Position}-{span.End.Position} (length: {span.Length})");
        }
#endif

        foreach (var span in spans)
        {
            foreach (var tag in GetTags(span))
            {
                yield return tag;
            }
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"<<< GetTags finished");
#endif
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
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"  GetTags(span): {span.Start.Position}-{span.End.Position}");
        System.Diagnostics.Debug.WriteLine($"    CBETaggerEnabled: {CBETagPackage.CBETaggerEnabled}");
        System.Diagnostics.Debug.WriteLine($"    OutliningSupported: {_OutliningSupported}");
        System.Diagnostics.Debug.WriteLine($"    VisibleSpan: {(_VisibleSpan.HasValue ? $"{_VisibleSpan.Value.Start}-{_VisibleSpan.Value.End}" : "(null)")}");
#endif

        if (!CBETagPackage.CBETaggerEnabled ||
            span.Snapshot != _TextView.TextBuffer.CurrentSnapshot ||
            span.Length == 0)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"    -> Returning empty (disabled or invalid span)");
#endif
            return EmptyTagColllection;
        }

        // Check if outlining is supported
        if (!_OutliningSupported)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"    -> Returning empty (outlining not supported)");
#endif
            return EmptyTagColllection;
        }

        // if big span, return only tags for visible area
        if (span.Length > 1000 && _VisibleSpan.HasValue)
        {
            var overlap = span.Overlap(_VisibleSpan.Value);
            if (overlap.HasValue)
            {
                span = overlap.Value;
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"    -> Big span, using overlap: {span.Start.Position}-{span.End.Position}");
#endif
                if (span.Length == 0)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"    -> Returning empty (overlap is empty)");
#endif
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
        var snapshot = span.Snapshot;

#if DEBUG
        // Stop time
        _watch ??= new System.Diagnostics.Stopwatch();
        _watch.Restart();
        System.Diagnostics.Debug.WriteLine($"    GetTagsCore: Processing span {span.Start.Position}-{span.End.Position}");
#endif

        try
        {
            // Expand the query span to include regions that might end in our visible area
            // but start before it. Query from beginning of file to end of requested span.
            var expandedSpan = new SnapshotSpan(snapshot, 0, span.End.Position);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"      Expanded span for query: 0-{span.End.Position}");
#endif

            // Get all collapsible regions from the outlining manager
            var regions = _OutliningManager.GetAllRegions(expandedSpan);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"      Found {regions.Count()} total regions in expanded span");
#endif

            int processedCount = 0;
            int skippedBefore = 0;
            int skippedSingleLine = 0;
            int skippedInvisible = 0;
            int addedCount = 0;

            foreach (var region in regions)
            {
                processedCount++;

                // Get the extent of this collapsible region
                var extent = region.Extent.GetSpan(snapshot);

                // Only process regions that end within or after the requested span
                if (extent.End.Position < span.Start.Position)
                {
                    skippedBefore++;
                    continue;
                }

                // Skip if region is not multi-line
                var startLine = snapshot.GetLineFromPosition(extent.Start);
                var endLine = snapshot.GetLineFromPosition(extent.End);
                if (startLine.LineNumber == endLine.LineNumber)
                {
                    skippedSingleLine++;
                    continue;
                }

                // Get positions
                int regionStart = extent.Start.Position;
                int regionEnd = extent.End.Position;

#if DEBUG
                if (addedCount < 5 || processedCount <= 10) // Log first few for detail
                {
                    System.Diagnostics.Debug.WriteLine($"      Region #{processedCount}: {regionStart}-{regionEnd}");
                }
#endif

                // Check if this region should be blocklisted (comments, etc.) before processing
                // Strip closing brackets and whitespace from the beginning to handle cases like "} /*"
                var firstLineText = startLine.GetText().AsSpan().Trim();
                firstLineText = firstLineText.TrimStart('}').TrimStart();
                if (IsBlocklistedRegion(firstLineText))
                {
                    skippedInvisible++;
                    continue;
                }

                // Extract header from the first line of the region
                ReadOnlySpan<char> cbHeader = GetHeaderFromRegion(region, snapshot, out int cbHeaderPosition);

                // Check tag visibility (includes caret position check)
                if (!IsTagVisible(cbHeaderPosition, regionEnd, _VisibleSpan, snapshot))
                {
                    skippedInvisible++;
                    continue;
                }

                // Clean up header text - remove extra spaces and tabs
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

                // Use cache or create new tag
                AdornmentDataKey adornmentDataKey = new(regionStart, regionEnd);
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

                    cbAdornmentData = new CBAdornmentData(regionStart, regionEnd, cbHeaderPosition, tagElement);
                    tagElement.AdornmentData = cbAdornmentData;
                    _adornmentCache.Add(adornmentDataKey, cbAdornmentData);
                }

                tagElement.SetResourceReference(CBETagControl.LineHeightProperty, EndTagColors.FontSizeKey);
                tagElement.SetResourceReference(CBETagControl.TextColorProperty, EndTagColors.GetForegroundResourceKey(_TextView.TextBuffer.ContentType.TypeName));

                // Add new tag to list
                // Place tag at the end of the region
                IntraTextAdornmentTag cbTag = new(tagElement, null, PositionAffinity.Predecessor);
                SnapshotSpan cbSnapshotSpan = new(snapshot, regionEnd, 0);
                TagSpan<IntraTextAdornmentTag> cbTagSpan = new(cbSnapshotSpan, cbTag);
                list.Add(cbTagSpan);
                addedCount++;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"      Summary: Processed={processedCount}, Added={addedCount}, Skipped: Before={skippedBefore}, SingleLine={skippedSingleLine}, Invisible={skippedInvisible}");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"      Exception in GetTagsCore: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"      Stack: {ex.StackTrace}");
#endif
            // May happen when closing a text editor or during rapid edits
        }

#if DEBUG
        _watch.Stop();
        System.Diagnostics.Debug.WriteLine($"      GetTagsCore completed: {list.Count} tags in {_watch.ElapsedMilliseconds}ms");
#endif

        return new ReadOnlyCollection<ITagSpan<IntraTextAdornmentTag>>(list);
    }

    /// <summary>
    /// Extracts the header text from a collapsible region
    /// Returns the text and outputs the start position within the snapshot
    /// </summary>
    private ReadOnlySpan<char> GetHeaderFromRegion(
        ICollapsible region,
        ITextSnapshot snapshot,
        out int headerStart)
    {
        var extent = region.Extent.GetSpan(snapshot);
        var firstLine = snapshot.GetLineFromPosition(extent.Start);

        headerStart = firstLine.Start.Position;

        // Get text from the first line
        var lineText = firstLine.GetText().AsSpan().Trim();

        // For #endregion, don't show tags
        if (lineText.StartsWith("#endregion"))
        {
            headerStart = -1;
            return ReadOnlySpan<char>.Empty;
        }

        // Find opening brace and get everything before it
        int braceIndex = lineText.IndexOf('{');
        if (braceIndex > 0)
        {
            lineText = lineText.Slice(0, braceIndex).Trim();
        }
        else if (braceIndex == 0)
        {
            // Standalone block with just '{' - allow it with empty header
            return ReadOnlySpan<char>.Empty;
        }
        // For #region and other non-brace collapsibles, take the whole line
        // but remove #region/#endregion keywords
        else if (lineText.StartsWith("#region"))
        {
            lineText = lineText.Slice(7).Trim(); // Remove "#region"
        }

        return lineText;
    }

    /// <summary>
    /// Checks if a region header matches blocklisted patterns (comments, etc.)
    /// </summary>
    /// <param name="headerText">The header text to check</param>
    /// <returns>True if the region should be ignored</returns>
    private bool IsBlocklistedRegion(ReadOnlySpan<char> headerText)
    {
        if (headerText.IsEmpty)
        {
            return false; // Allow empty headers (standalone blocks)
        }

        // Block single-line comments
        if (headerText.StartsWith("//"))
        {
            return true;
        }

        // Block multi-line comments
        if (headerText.StartsWith("/*") || headerText.StartsWith("*"))
        {
            return true;
        }

        // Block XML documentation comments
        if (headerText.StartsWith("///"))
        {
            return true;
        }

        // Block #endregion
        if (headerText.StartsWith("#endregion"))
        {
            return true;
        }

        // Block using directives (namespace imports) but NOT using statements (resource disposal)
        // using directives: "using System;" or "using Microsoft.VisualStudio.Shell;"
        // using statements: "using (var conn = ...)" or "using var conn = ..."
        if (headerText.StartsWith("using"))
        {
            // Check if it's a using directive (has a semicolon and no parentheses/var keyword)
            // using statements will have either '(' or 'var' after 'using'
            ReadOnlySpan<char> afterUsing = headerText.Slice(5).TrimStart();
            
            // If it starts with '(' or 'var', it's a using statement (keep it)
            if (afterUsing.StartsWith("(") || afterUsing.StartsWith("var"))
            {
                return false; // Keep using statements
            }
            
            // Otherwise, it's a using directive (block it)
            return true;
        }

        return false;
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

        // Stop and dispose the debounce timer
        if (_LayoutChangedDebounceTimer != null)
        {
            _LayoutChangedDebounceTimer.Stop();
            _LayoutChangedDebounceTimer.Tick -= OnLayoutChangedDebounceTimerTick;
            _LayoutChangedDebounceTimer = null;
        }

        CBETagPackage.Instance?.PackageOptionChanged -= OnPackageOptionChanged;

        if (_OutliningManager != null)
        {
            _OutliningManager.RegionsChanged -= OnOutliningRegionsChanged;
        }

        _TextView?.LayoutChanged -= OnTextViewLayoutChanged;
        _TextView?.Caret?.PositionChanged -= Caret_PositionChanged;
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
        // Always check if caret is at the closing bracket position first
        if (_TextView != null)
        {
            var caretIndex = _TextView.Caret.Position.BufferPosition.Position;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"IsTagVisible: caretIndex={caretIndex}, end={end}, end+1={end + 1}, start={start}");
#endif

            // Hide tag if caret is at the closing bracket or right after it (where the tag is placed)
            if (caretIndex == end || caretIndex == end + 1)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"  -> Hiding tag: caret at closing bracket position");
#endif
                return false;
            }
        }

        // Check general condition for visibility mode
        if (CBETagPackage.CBEVisibilityMode == (int)VisibilityModes.Always || !visibleSpan.HasValue)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"  -> Showing tag: Always mode or no visible span");
#endif
            return true;
        }

        // Check non-visible span
        var val = visibleSpan.Value;
        if (!(start < val.Start && end >= val.Start && end <= val.End))
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"  -> Hiding tag: not in visible span");
#endif
            return false;
        }

        // Check if caret is in this line
        if (_TextView == null)
        {
            return true;
        }

        var caretIdx = _TextView.Caret.Position.BufferPosition.Position;
        var lineStart = Math.Min(caretIdx, end);
        var lineEnd = Math.Max(caretIdx, end);

        // Same line -> not visible
        if (lineStart == lineEnd)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"  -> Hiding tag: same line (lineStart == lineEnd)");
#endif
            return false;
        }

        // hide tag if caret is in same line
        if (lineStart >= 0 && lineEnd <= snapshot.Length)
        {
            string line = snapshot.GetText(lineStart, lineEnd - lineStart);
            if (!line.Contains('\n'))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"  -> Hiding tag: caret on same line (no newline between caret and end)");
#endif
                return false;
            }
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"  -> Showing tag");
#endif
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
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"=== OnTextViewLayoutChanged ===");
        System.Diagnostics.Debug.WriteLine($"  TranslatedLines: {e.TranslatedLines.Count}");
        System.Diagnostics.Debug.WriteLine($"  NewOrReformattedLines: {e.NewOrReformattedLines.Count}");
#endif

        // get new visible span
        var visibleSpan = GetVisibleSpan(_TextView);
        if (!visibleSpan.HasValue)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"  -> No visible span, returning");
#endif
            return;
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"  New visible span: {visibleSpan.Value.Start}-{visibleSpan.Value.End}");
        if (_VisibleSpan.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"  Old visible span: {_VisibleSpan.Value.Start}-{_VisibleSpan.Value.End}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"  Old visible span: (null)");
        }
#endif

        // only if new visible span is different from old
        if (_VisibleSpan.HasValue &&
            _VisibleSpan.Value.Start == visibleSpan.Value.Start &&
            _VisibleSpan.Value.End >= visibleSpan.Value.End)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"  -> Visible span unchanged, returning");
#endif
            return;
        }

        // Calculate the span to invalidate
        var newSpan = visibleSpan.Value;
        Span spanToInvalidate;
        
        if (!_VisibleSpan.HasValue)
        {
            spanToInvalidate = newSpan;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"  -> First time, will invalidate new span: {newSpan.Start}-{newSpan.End}");
#endif
        }
        else
        {
            var oldSpan = _VisibleSpan.Value;
            // invalidate two spans if old and new do not overlap
            if (newSpan.Start > oldSpan.End || newSpan.End < oldSpan.Start)
            {
                // Join both spans into one larger span
                spanToInvalidate = Span.FromBounds(
                    Math.Min(newSpan.Start, oldSpan.Start),
                    Math.Max(newSpan.End, oldSpan.End));
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"  -> No overlap, will invalidate joined span");
                System.Diagnostics.Debug.WriteLine($"     New: {newSpan.Start}-{newSpan.End}");
                System.Diagnostics.Debug.WriteLine($"     Old: {oldSpan.Start}-{oldSpan.End}");
                System.Diagnostics.Debug.WriteLine($"     Joined: {spanToInvalidate.Start}-{spanToInvalidate.End}");
#endif
            }
            else
            {
                // invalidate one big span (old and new joined)
                spanToInvalidate = newSpan.Join(oldSpan);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"  -> Overlap detected, will invalidate joined span: {spanToInvalidate.Start}-{spanToInvalidate.End}");
#endif
            }
        }

        // Update the visible span
        _VisibleSpan = visibleSpan;

        // Store the span to invalidate and restart debounce timer
        _PendingInvalidateSpan = spanToInvalidate;
        _LayoutChangedDebounceTimer.Stop();
        _LayoutChangedDebounceTimer.Start();

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"  -> Debounce timer started/restarted");
        System.Diagnostics.Debug.WriteLine($"=== End OnTextViewLayoutChanged ===");
#endif
    }

    /// <summary>
    /// Handles the debounce timer tick event to invalidate the pending span
    /// </summary>
    private void OnLayoutChangedDebounceTimerTick(object sender, EventArgs e)
    {
        _LayoutChangedDebounceTimer.Stop();

        if (!_PendingInvalidateSpan.HasValue)
        {
            return;
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"=== OnLayoutChangedDebounceTimerTick ===");
        System.Diagnostics.Debug.WriteLine($"  -> Invalidating pending span: {_PendingInvalidateSpan.Value.Start}-{_PendingInvalidateSpan.Value.End}");
#endif

        var spanToInvalidate = _PendingInvalidateSpan.Value;
        _PendingInvalidateSpan = null;

        // In "Always" mode, we still need to invalidate when scrolling
        // to ensure tags are created for newly visible regions
        // In "HeaderNotVisible" mode, we also need to invalidate to check visibility
        // So we always invalidate on scroll
        InvalidateSpan(spanToInvalidate, false);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"=== End OnLayoutChangedDebounceTimerTick ===");
#endif
    }

    #endregion

}
