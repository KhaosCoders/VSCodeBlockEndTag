# CodeBlockEndTag - Visual Studio Extension

## Project Overview

**CodeBlockEndTag** is a Visual Studio extension (VSIX) that enhances code readability by adding visual end tags to code blocks enclosed in curly braces `{ }`. These tags display the header or condition of the code block and allow quick navigation by clicking.

- **Extension Type**: Visual Studio 2017, 2019, and 2022 Extension
- **Target Framework**: .NET Framework 4.8
- **C# Language Version**: 14.0
- **License**: GNU GPL v3
- **Author**: Khaos66
- **Repository**: https://github.com/KhaosCoders/VSCodeBlockEndTag
- **Marketplace**: https://marketplace.visualstudio.com/items?itemName=KhaosPrinz.CodeBlockEndTag

## What It Does

The extension automatically:
1. Detects closing braces `}` in code files
2. Extracts the corresponding code block header (e.g., `if (condition)`, `class MyClass`, `public void Method()`)
3. Displays an inline adornment tag after the closing brace with an appropriate icon and text
4. Allows users to click the tag to jump to the block's header
5. Intelligently shows/hides tags based on visibility settings and caret position
6. Supports customization through Visual Studio options

## Core Architecture

### Main Components

#### 1. **CBETagger** (`CBETagger.cs`)
The heart of the extension. Implements `ITagger<IntraTextAdornmentTag>` to provide inline adornments.

**Key responsibilities:**
- Uses Visual Studio's outlining/folding regions to identify code blocks
- Extracts headers from collapsible regions
- Manages tag visibility based on caret position and viewport
- Caches adornment tags for performance
- Responds to outlining changes, text buffer changes, and layout changes

**Important methods:**
- `GetTagsCore()`: Main logic that queries outlining manager for collapsible regions and creates tags
- `GetHeaderFromRegion()`: Extracts the header text from a collapsible region (supports braces, #region, etc.)
- `IsTagVisible()`: Determines if a tag should be shown based on visibility mode and caret position
- `OnTextChanged()`: Updates cache when text is edited
- `OnOutliningRegionsChanged()`: Responds to folding region changes
- `Caret_PositionChanged()`: Hides/shows tags based on caret location

#### 2. **CBETaggerProvider** (`CBETaggerProvider.cs`)
Factory class that creates `CBETagger` instances. Uses MEF (Managed Extensibility Framework) attributes:
- `[Export(typeof(IViewTaggerProvider))]`
- `[ContentType("code")]` - Works with all code files
- `[TextViewRole(PredefinedTextViewRoles.Interactive)]`
- `[TagType(typeof(IntraTextAdornmentTag))]`

#### 3. **CBETagPackage** (`CBETagPackage.cs`)
The main VS Package that manages extension lifecycle and settings.

**Key responsibilities:**
- Implements `AsyncPackage` for background loading
- Provides options page integration
- Manages font and color settings
- Exposes static properties for settings (visibility mode, display mode, click mode, etc.)
- Implements `IVsFontAndColorDefaultsProvider` for custom color categories

#### 4. **CBETagControl** (`CBETagControl.cs`)
WPF control that renders the visual tag. Inherits from `ButtonBase`.

**Features:**
- Displays icon, text, or both based on settings
- Handles single-click, double-click, and Ctrl+Click interactions
- Uses dependency properties for data binding
- Fires `TagClicked` event with navigation information

#### 5. **IconMonikerSelector** (`IconMonikerSelector.cs`)
Intelligent icon selection based on code construct type.

**Capabilities:**
- Parses code headers to identify constructs (class, method, if, for, etc.)
- Detects access modifiers (public, private, protected, internal)
- Maps keywords to appropriate Visual Studio `KnownMonikers`
- Supports C#, C/C++, and PowerShell keywords

### Data Models

#### CBAdornmentData (`Model/CBAdornmentData.cs`)
Struct storing metadata about a code block:
- `StartPosition`: Opening brace position
- `EndPosition`: Closing brace position
- `HeaderStartPosition`: Where the header text begins
- `Adornment`: Reference to the UI element

#### Enums
- **DisplayModes**: Text, Icon, IconAndText
- **VisibilityModes**: Always, HeaderNotVisible
- **ClickMode**: SingleClick, DoubleClick, CtrlClick

### Options and Settings

#### CBEOptionPage (`OptionPage/CBEOptionPage.cs`)
Dialog page for extension settings:
- Enable/disable tagger
- Display mode (icon/text/both)
- Visibility mode (always/when header not visible)
- Click mode (single/double/Ctrl+click)
- Margin spacing
- Language support toggles

Supports multiple languages: C#, C/C++, JavaScript, TypeScript, PowerShell, XAML, JSON, XML. Any language with VS outlining support can potentially be added.

### Font and Color Customization

#### EndTagColors (`Shell/EndTagColors.cs`)
- Manages color resource keys per language
- Provides dynamic color lookup for tags

#### FontAndColorDefaultsCSharpTags (`Shell/FontAndColorDefaultsCSharpTags.cs`)
- Registers custom font and color category for tags
- Integrates with VS's Font and Colors settings
- Singleton pattern for color management

## Key Technologies & APIs

### Visual Studio SDK APIs
- **Text Editor Extensibility**: `ITextBuffer`, `ITextSnapshot`, `ITextView`, `IWpfTextView`
- **Tagging System**: `ITagger<T>`, `ITagSpan<T>`, `IntraTextAdornmentTag`
- **Text Structure**: `ITextStructureNavigator` for code navigation
- **MEF**: Dependency injection via `[Import]` and `[Export]` attributes
- **Packages**: `AsyncPackage`, `DialogPage` for options
- **Font/Color Services**: `IVsFontAndColorDefaultsProvider`, `IVsUIShell2`, `IVsUIShell5`

### External NuGet Packages
- **CommunityToolkit.HighPerformance**: For `PooledStringBuilder` (efficient string building)
- **Microsoft.VisualStudio.SDK**: VS extensibility framework
- **Community.VisualStudio.Toolkit.17**: Helper utilities for VS extensions

### Performance Optimizations
- Adornment caching with `Dictionary<AdornmentDataKey, CBAdornmentData>`
- `ReadOnlySpan<char>` for zero-allocation string parsing
- `PooledStringBuilder` for reduced GC pressure
- Visible span tracking to only process visible code
- Incremental cache updates on text changes

## Workflow: How Tags Are Created

1. **User opens a code file** → VS creates an `IWpfTextView`
2. **CBETaggerProvider.CreateTagger()** is called → Creates a `CBETagger` instance
3. **CBETagger initializes** → Gets `IOutliningManager` from `IOutliningManagerService`
4. **VS calls GetTags()** with snapshot spans to render
5. **CBETagger.GetTagsCore()** executes:
   - Queries `IOutliningManager.GetAllRegions()` for collapsible regions
   - For each `ICollapsible` region:
     - Gets region extent (start/end positions)
     - Checks if region is multi-line
     - Calls `GetHeaderFromRegion()` to extract header text
     - Checks visibility with `IsTagVisible()`
     - Either reuses cached adornment or creates new `CBETagControl`
     - Selects icon via `IconMonikerSelector.SelectMoniker()`
     - Returns `TagSpan<IntraTextAdornmentTag>` positioned at region end
6. **VS renders the tags** in the editor
7. **User clicks tag** → `TagClicked` event → `Adornment_TagClicked()` → Caret jumps to header

## Important Implementation Details

### Outlining-Based Architecture
The extension uses Visual Studio's built-in outlining/folding system to identify code blocks. This approach:
- Works with any language that has outlining support (C#, JavaScript, TypeScript, C++, etc.)
- Automatically handles comments, strings, and language syntax
- Supports not just braces but also `#region`, XML comments, and other collapsible constructs
- Leverages VS's existing, well-tested code structure analysis

### Region Types Supported
- **Brace-based blocks**: `if`, `for`, `while`, `class`, `method`, etc.
- **#region directives**: C# regions with custom names
- **XML documentation**: Collapsible doc comments
- **Language-specific**: Any construct that VS can fold

### Caret-Based Visibility
When visibility mode is "HeaderNotVisible":
- Tags are hidden if the caret is on the same line as the region end
- Tags are hidden if caret is at the exact end position
- This prevents visual clutter while editing

### Cache Management
- Cache key: `(StartPosition, EndPosition)` tuple
- Cache invalidated on text changes affecting block positions
- Adornments adjusted by delta when text inserted/deleted after them
- Event handlers unsubscribed when adornments removed from cache

## Configuration Files

### source.extension.vsixmanifest
Defines extension metadata:
- Display name, description, version
- Supported VS versions
- Assets (VsPackage)
- Icon and preview image

### source.extension.cs
Auto-generated class with extension constants (ID, name, version, etc.)

## Build and Packaging

The project builds to a `.vsix` file that can be:
1. Installed locally by double-clicking
2. Published to Visual Studio Marketplace
3. Distributed directly to users

## Testing Considerations

### Manual Testing Scenarios
1. **Basic functionality**: Open C# file with nested code blocks, verify tags appear
2. **Comment handling**: Add `}` inside comments, verify no tags
3. **Caret interaction**: Move caret to closing brace line, verify tag hides (if mode set)
4. **Click navigation**: Click tag, verify jump to header
5. **Options**: Change display mode, visibility mode, click mode in options
6. **Performance**: Open large files (>1000 lines), verify smooth scrolling
7. **Text editing**: Add/remove code blocks, verify tags update correctly

### Debug Mode Features
The code includes extensive `#if DEBUG` logging:
- Position tracking
- Visibility decisions
- Tag creation
- Performance timing

## Extension Points for Future Work

### Potential Enhancements
1. **Additional language support**: Add more languages as users request them
2. **Customizable templates**: Allow users to define tag text format
3. **Color themes**: More color customization options
4. **Collapse/expand integration**: Sync with folding state
5. **Annotation support**: Allow adding custom notes to tags
6. **Region naming**: Special handling for named #regions

### Known Limitations
1. Requires outlining support for the language (most major languages supported)
2. No support for files without outlining enabled
3. Performance degrades on extremely large files (>10,000 lines)
4. Tag placement depends on VS's outlining region boundaries

## Code Style and Conventions

- **Naming**: Private fields use `_PascalCase` (unusual but consistent in codebase)
- **Regions**: Code organized into `#region` blocks by functionality
- **Null handling**: Uses nullable types (`Span?`, `int?`) and null-conditional operators
- **Threading**: Requires UI thread for VS APIs, uses `ThreadHelper.ThrowIfNotOnUIThread()`
- **Disposal**: Proper `IDisposable` implementation with event unsubscription

## Dependencies and References

### Critical VS SDK Assemblies
- `Microsoft.VisualStudio.Text.UI.Wpf`
- `Microsoft.VisualStudio.Text.Logic`
- `Microsoft.VisualStudio.CoreUtility`
- `Microsoft.VisualStudio.Imaging`
- `Microsoft.VisualStudio.Shell.15.0`

### Community Packages
- `CommunityToolkit.HighPerformance`
- `Community.VisualStudio.Toolkit.17`

## Contributing Guidelines

When working on this project:

1. **Maintain compatibility**: Don't break existing user settings
2. **Performance first**: Always profile changes with large files
3. **Test thoroughly**: VS extension bugs are hard to debug in production
4. **Document complex logic**: Especially in `GetTagsCore()` and `GetCodeBlockHeader()`
5. **Preserve cache behavior**: Cache invalidation bugs cause major issues
6. **Follow UI thread rules**: Always check thread requirements for VS APIs
7. **Update version**: Increment version in `source.extension.cs` and manifest

## AI Agent Guidance

### When modifying parsing logic (`CBETagger.cs`):
- Test with nested blocks, empty blocks `{}`, and blocks with comments
- Verify comment detection doesn't break on edge cases like `*/` in strings
- Check performance impact—avoid allocations in hot paths
- Maintain cache consistency—ensure positions update correctly

### When adding new features:
- Check if `CBETagPackage` needs new option properties
- Update `CBEOptionPage` and `CBEOptionPageControl` for UI
- Fire `PackageOptionChanged` event when settings change
- Test with different Visual Studio themes (Light/Dark/Blue)

### When debugging issues:
- Enable DEBUG mode to see detailed logging in VS Output window
- Use Activity Log: `%AppData%\Microsoft\VisualStudio\<version>\ActivityLog.xml`
- Test in VS Experimental Instance (automatically launched during debugging)
- Check for `NullReferenceException` in `GetTagsCore()` (expected when closing editor)

## Useful Resources

- **VS SDK Documentation**: https://docs.microsoft.com/en-us/visualstudio/extensibility/
- **Known Monikers**: http://glyphlist.azurewebsites.net/knownmonikers/
- **Text Editor Extensibility**: https://docs.microsoft.com/en-us/visualstudio/extensibility/inside-the-editor
- **Marketplace Publishing**: https://marketplace.visualstudio.com/manage

---

*This project demonstrates advanced VS extensibility concepts including real-time text parsing, adornment rendering, MEF composition, and seamless IDE integration.*
