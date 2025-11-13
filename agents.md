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

## Licensing Model

### Free vs PRO

The extension uses a **freemium licensing model**:

- **FREE**: C# language support is completely free to use, no restrictions
- **PRO**: All other languages require a PRO license (yearly subscription)

**PRO License Features:**
- Unlocks support for all languages (Visual Basic, F#, TypeScript, JavaScript, C++, Python, PowerShell, etc.)
- One-year subscription model
- License activation tied to user's Visual Studio email address
- JWT-based license token stored in user settings
- Automatic expiration date tracking

**Purchase & Activation:**
- PRO licenses available at: `https://khaoscoders.onfastspring.com/cbe-1y`
- License key required for activation
- Email address automatically retrieved from Visual Studio settings
- License binding confirmation required before first-time activation
- Activation status displayed in options page with expiration date

### License Service

The extension includes a `Services.LicenseService` that manages license validation:

**Key Methods:**
- `HasValidProLicense()`: Returns whether user has a valid, active PRO license
- `GetLicenseExpirationDate()`: Returns license expiration date (if available)
- `GetVisualStudioEmail()`: Retrieves the user's email from Visual Studio settings
- `RequireActivatedTokenAsync(key, email)`: Attempts to re-acquire an already activated license token
- `ActivateLicenseAsync(key, email)`: Activates a new license key and binds it to the user's email

**License Storage:**
- JWT token stored in `CBEOptionPage.LicenseToken` property
- Persisted to Visual Studio settings via `SaveSettingsToStorage()`
- Token validated on extension startup and when checking language support

### Language Restriction Enforcement

Language support restrictions are enforced at multiple levels:

#### 1. **UI Level (CBEOptionPageControl)**
- Languages without PRO license display with 🔒 [PRO] indicator
- Attempting to enable non-C# languages without PRO shows a modal dialog:
  - Message: "This language requires a PRO license.\n\nC# is free to use, but all other languages require a PRO license.\n\nClick 'Buy Pro' to unlock all languages!"
  - Checkbox change is prevented (`e.NewValue = CheckState.Unchecked`)
- License status displayed with color coding:
  - **Green**: "✓ PRO License Active (Expires: [date])"
  - **Gray**: "No PRO License - C# only"

#### 2. **Validation Logic**
```csharp
// In LviLanguages_ItemCheck event handler
var supportedLangs = optionsPage.GetSupportedLanguages();
string langName = supportedLangs[e.Index].Name;
bool isCSharp = langName.Equals(Languages.CSharp, StringComparison.OrdinalIgnoreCase);

// Prevent enabling non-C# languages without PRO license
if (!isCSharp && !Services.LicenseService.HasValidProLicense() && e.NewValue == CheckState.Checked)
{
    e.NewValue = CheckState.Unchecked;
    // Show dialog...
}
```

#### 3. **C# Constant Definition**
The `Languages.CSharp` constant is used to identify the free language:
- Comparison is case-insensitive
- Always allowed regardless of license status

### License Activation Flow

**User Experience:**

1. **Pre-Purchase Check**
   - User clicks "Buy Pro" link in options page
   - Extension attempts to retrieve Visual Studio email
   - If email not found, warns user: "Couldn't read your Microsoft email address from Visual Studio settings. This is required for license activation after you bought a key."
   - Browser opens to store URL

2. **Activation Process**
   - User enters license key in text box
   - "Activate" button enabled only when key is non-empty
   - User clicks "Activate" button
   - Button disabled and text changes to "Activating..."
   - Email validation performed
   - Attempt to re-acquire token if already activated (`RequireActivatedTokenAsync`)
   - If not already activated:
     - Confirmation dialog: "This will bind the license key to your email address:\n\n{email}\n\nDo you want to continue?"
     - If confirmed, calls `ActivateLicenseAsync(key, email)`
   - JWT token saved to settings
   - Success message shown
   - UI refreshed to show active license status
   - Language list updated to remove 🔒 indicators
   - License key input cleared

3. **Error Handling**
   - Missing email: "Email address is required for license activation."
   - Empty key: "Please enter a license key."
   - Activation failure: "Activation failed: {exception message}"
   - All errors shown as modal dialogs with appropriate icons

**Technical Implementation:**

```csharp
private async void BtnActivateLicense_Click(object sender, EventArgs e)
{
    // Validate input
  // Retrieve email
    // Try re-acquiring token first
    // If needed, confirm and activate
    // Save token to settings
    // Refresh UI
    // Handle errors
}
```

### License Status Display

**Options Page UI Elements:**

- **lblLicenseStatus**: Shows current license state
  - With PRO: Green text, checkmark, expiration date
  - Without PRO: Gray text, limitation notice
  
- **lblProInfo**: Contextual message
  - With PRO: "All features unlocked!"
  - Without PRO: "Unlock all features with PRO!"

- **lviLanguages**: CheckedListBox with language toggles
  - C# always available (no lock icon)
  - Other languages show "🔒 [PRO]" suffix when no license

- **txtLicenseKey**: TextBox for entering activation key
  - TextChanged event enables/disables activation button
  
- **btnActivateLicense**: Button to perform activation
  - Async handler for activation process
  - UI feedback during activation

- **lnkBuyPro**: LinkLabel to store URL
  - Pre-validates email before opening browser

## What It Does

The extension automatically:
1. Detects closing braces `}` in code files
2. Extracts the corresponding code block header (e.g., `if (condition)`, `class MyClass`, `public void Method()`)
3. Displays an inline adornment tag after the closing brace with an appropriate icon and text
4. Allows users to click the tag to jump to the block's header
5. Intelligently shows/hides tags based on visibility settings and caret position
6. Supports customization through Visual Studio options
7. Works with all Visual Studio content types that support outlining

### License-Aware Language Management

The extension now integrates license checking into language support:

1. **On Options Page Load**:
   - Checks `Services.LicenseService.HasValidProLicense()`
   - Adds lock indicators to non-C# languages if no license
   - Displays license status and expiration

2. **On Language Toggle**:
   - Validates license before allowing non-C# activation
   - Shows informative dialog if PRO required
   - Prevents checkbox state change for locked languages

3. **On License Activation**:
   - Immediately refreshes language list
   - Removes lock indicators
   - Updates license status display
   - Persists JWT token to settings

4. **On Extension Startup** (handled in other components):
   - Validates stored JWT token
   - Applies language restrictions based on license
   - May prompt for re-activation if expired

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
- Dynamically discovers supported content types from Visual Studio's content type registry

**Content Type Discovery:**
- Uses `IContentTypeRegistryService` to enumerate all code-related content types
- Filters for types inheriting from "code" base content type
- Builds list of supported languages dynamically at runtime
- Allows enabling/disabling extension per language/content type

#### 4. **CBETagControl** (`CBETagControl.cs`)
WPF control that renders the visual tag. Inherits from `ButtonBase`.

**Features:**
- Displays icon, text, or both based on settings
- Handles single-click, double-click, and Ctrl+Click interactions
- Uses dependency properties for data binding
- Fires `TagClicked` event with navigation information

#### 6. **IconMonikerSelector** (`IconMonikerSelector.cs`)
Intelligent icon selection based on code construct type.

**Capabilities:**
- Parses code headers to identify constructs (class, method, if, for, etc.)
- Detects access modifiers (public, private, protected, internal)
- Maps keywords to appropriate Visual Studio `KnownMonikers`
- Supports C#, C/C++, and PowerShell keywords

#### 7. **License Service** (`Services/LicenseService.cs`)
Manages PRO license validation and activation.

**Key responsibilities:**
- Validates JWT tokens for expiration and authenticity
- Retrieves Visual Studio user email from settings
- Activates new license keys via API call
- Re-acquires tokens for previously activated keys
- Provides license status for UI and enforcement logic

**Integration points:**
- Called by `CBEOptionPageControl` for UI decisions
- Called by language support validation logic
- Token stored in `CBEOptionPage.LicenseToken`
- Email retrieved from Visual Studio's `IVsShell` service

### Data Models

#### CBAdornmentData (`Model/CBAdornmentData.cs`)
Struct storing metadata about a code block:
- `StartPosition`: Opening brace position
- `EndPosition`: Closing brace position
- `HeaderStartPosition`: Where the header text begins
- `Adornment`: Reference to the UI element

#### SupportedLang (`Model/SupportedLang.cs`)
Struct representing a supported language/content type:
- `Name`: Internal content type name (e.g., "CSharp", "TypeScript")
- `DisplayName`: User-friendly display name (e.g., "C#", "TypeScript")

#### Languages Constant (`Model/Languages.cs` or similar)
Contains constant for free language identification:
- `Languages.CSharp`: The only language available without PRO license
- Used for case-insensitive comparison in license checks

#### Enums
- **DisplayModes**: Text, Icon, IconAndText
- **VisibilityModes**: Always, HeaderNotVisible
- **ClickMode**: SingleClick, DoubleClick, CtrlClick

### Options and Settings

#### CBEOptionPage (`OptionPage/CBEOptionPage.cs`)
Dialog page for extension settings with advanced language management:

**Settings:**
- Enable/disable tagger globally
- Display mode (icon/text/both)
- Visibility mode (always/when header not visible)
- Click mode (single/double/Ctrl+click)
- Margin spacing (pixels between brace and tag)
- Telemetry opt-in/opt-out
- Per-language enable/disable toggles
- **LicenseToken**: JWT token for PRO license (stored as string)

**Language Support Features:**
- Dynamic language discovery from VS content type registry
- Persists language settings by name (not position) for better compatibility
- Supports backward compatibility with legacy settings format
- Initializes languages on-demand to avoid startup delays
- Preserves user settings when language list changes between VS versions

**Supported Languages (dynamically discovered):**
C#, Visual Basic, F#, C/C++, JavaScript, TypeScript, Python, PowerShell, XAML, JSON, XML, YAML, HTML, CSS, SCSS, LESS, Razor, T-SQL, and many more—any language with VS outlining support.

**License Enforcement:**
- C# is always available (free tier)
- All other languages require valid PRO license
- License validation occurs on language toggle attempt
- Settings persist even without license (re-enabled when license activated)

#### ContentTypeDisplayNameMapper (`OptionPage/ContentTypeDisplayNameMapper.cs`)
**NEW in latest version**: Centralized mapping of content type names to user-friendly display names.

**Features:**
- Static dictionary-based mapping with case-insensitive lookups
- Comprehensive mappings for 50+ content types
- Intelligent fallback for unmapped types (capitalizes, replaces separators)
- Single source of truth for display name logic
- Easily extensible for new content types

**Example mappings:**
- `CSharp` → `C#`
- `VisualBasic` → `Visual Basic`
- `FSharpInteractive` → `F# Interactive`
- `LegacyRazorCoreCSharp` → `Razor (Legacy C#)`
- `html-delegation` → `HTML (delegation)`
- `code-languagesserver-preview` → `Code Language Server (Preview)`

**Benefits:**
- Consistent display names across the extension
- Easier maintenance and updates
- Better user experience with clear, recognizable language names
- Supports special characters and formatting (e.g., "C#" instead of "CSharp")

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
- **Content Types**: `IContentTypeRegistryService` for dynamic language discovery
- **MEF**: Dependency injection via `[Import]` and `[Export]` attributes
- **Packages**: `AsyncPackage`, `DialogPage` for options
- **Font/Color Services**: `IVsFontAndColorDefaultsProvider`, `IVsUIShell2`, `IVsUIShell5`
- **Settings**: `ShellSettingsManager`, `WritableSettingsStore` for persistent settings

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
- Lazy language initialization (only when options page opened)

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

## Settings Persistence

### Name-Based Settings Format (Current)
Language settings stored as name:value pairs for compatibility across VS versions:
```
"SupportedLanguages": "CSharp:1,VisualBasic:1,TypeScript:0,..."
```
Where `1` = enabled, `0` = disabled.

**Advantages:**
- Resilient to language list reordering
- Compatible when new languages added
- Preserves user preferences across extension updates
- Works across different VS versions with different content types

### Legacy Positional Format (Backward Compatibility)
Original format stored boolean array by position:
```
"SupportedLangActive": "1,1,0,1,..."
```

**Limitations:**
- Breaks when language order changes
- Loses settings when languages added/removed
- Not portable across VS versions

The extension supports both formats, preferring name-based when available.

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

### Dynamic Content Type Discovery
On first load of options page:
1. Extension queries `IContentTypeRegistryService` for all registered content types
2. Filters for types that inherit from "code" base type
3. Maps content type names to friendly display names via `ContentTypeDisplayNameMapper`
4. Sorts alphabetically by display name
5. Loads user's previous settings (if any)
6. Presents unified language list with checkboxes

This approach:
- Adapts to user's installed VS extensions that add new languages
- Doesn't hardcode language list
- Scales automatically with VS updates
- Supports community languages and custom content types

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
6. **Language toggling**: Disable language, verify tags disappear; re-enable, verify tags return
7. **Settings persistence**: Change settings, close VS, reopen, verify settings retained
8. **New content types**: Install extension with new language, verify it appears in options
9. **Performance**: Open large files (>1000 lines), verify smooth scrolling
10. **Text editing**: Add/remove code blocks, verify tags update correctly

**License-Related Testing:**

1. **No License State**:
   - Verify C# is available and toggleable
   - Verify other languages show 🔒 [PRO] indicator
   - Attempt to enable non-C# language, verify dialog and prevention
   - Verify license status shows "No PRO License - C# only"

2. **License Activation**:
   - Click "Buy Pro", verify email check and browser launch
   - Enter invalid key, verify error handling
   - Enter valid key, verify activation confirmation dialog
   - Cancel confirmation, verify no activation
   - Confirm activation, verify success message
   - Verify license status updates to show expiration date
   - Verify lock indicators removed from language list
   - Verify non-C# languages now toggleable

3. **Active License State**:
 - Verify license status shows green with expiration date
 - Verify all languages available without restrictions
   - Toggle non-C# languages, verify no prompts

4. **License Expiration**:
   - Test with expired JWT token
   - Verify falls back to free tier (C# only)
   - Verify lock indicators return for non-C# languages

5. **Email Handling**:
   - Test when VS email not available
   - Verify warning message before opening store
   - Verify activation prevented if email unavailable

6. **Settings Persistence**:
   - Activate license, close VS, reopen
   - Verify license token persisted
   - Verify language restrictions still apply correctly

### Debug Mode Features
The code includes extensive `#if DEBUG` logging:
- Position tracking
- Visibility decisions
- Tag creation
- Performance timing

**License Debugging:**
- Log license validation attempts
- Log JWT token parsing (sanitize sensitive data)
- Log email retrieval from VS settings
- Log API calls for activation/re-acquisition

## Extension Points for Future Work

### Potential Enhancements
1. **Additional language support**: Automatically supports new languages as VS adds them
2. **Customizable templates**: Allow users to define tag text format
3. **Color themes**: More color customization options per language
4. **Collapse/expand integration**: Sync with folding state
5. **Annotation support**: Allow adding custom notes to tags
6. **Region naming**: Special handling for named #regions
7. **Localization**: Translate display names and UI for international users
8. **Import/export settings**: Share language preferences between machines

**License System Enhancements:**
1. **License management UI**: View license details, deactivate, transfer
2. **Offline grace period**: Allow temporary use after expiration
3. **Team licenses**: Multi-user license management
4. **License recovery**: Retrieve lost license keys via email
5. **Trial period**: Time-limited full feature access
6. **Notification system**: Warn before expiration, prompt for renewal
7. **License usage analytics**: Track activation success rates (anonymized)

### Recent Improvements
1. **ContentTypeDisplayNameMapper**: Centralized, maintainable display name mapping
2. **Name-based settings**: Robust settings persistence across versions
3. **Dynamic language discovery**: No hardcoded language lists
4. **Telemetry opt-in**: Privacy-respecting usage analytics
5. **Improved fallback logic**: Better display names for unknown content types
6. **PRO licensing model**: Freemium approach with C# free forever
7. **JWT-based validation**: Secure, stateless license verification
8. **Email binding**: Ties licenses to VS user identity
9. **Re-acquisition support**: Seamless license recovery on new machines
10. **UI integration**: Clear status display and restriction enforcement
11. **Graceful degradation**: Falls back to C#-only on license issues

### Known Limitations
1. Requires outlining support for the language (most major languages supported)
2. No support for files without outlining enabled
3. Performance degrades on extremely large files (>10,000 lines)
4. Tag placement depends on VS's outlining region boundaries
5. Activation requires internet access (initially)
6. Email must be set up in Visual Studio for license binding
7. No offline activation or grace period after expiration
8. Single-device activation per license key

## Code Style and Conventions

- **Naming**: Private fields use `_PascalCase` (unusual but consistent in codebase)
- **Regions**: Code organized into `#region` blocks by functionality
- **Null handling**: Uses nullable types (`Span?`, `int?`) and null-conditional operators
- **Threading**: Requires UI thread for VS APIs, uses `ThreadHelper.ThrowIfNotOnUIThread()`
- **Disposal**: Proper `IDisposable` implementation with event unsubscription
- **Static helpers**: Utility classes like `ContentTypeDisplayNameMapper` are static for performance

**License Code Patterns:**
- Async/await for activation API calls
- Try-catch-finally for robust error handling
- User confirmation dialogs for binding operations
- Immediate UI feedback during long operations (button text changes)
- Modal dialogs for important notifications
- Color coding for status (green = active, gray = inactive)

## Dependencies and References

### Critical VS SDK Assemblies
- `Microsoft.VisualStudio.Text.UI.Wpf`
- `Microsoft.VisualStudio.Text.Logic`
- `Microsoft.VisualStudio.CoreUtility`
- `Microsoft.VisualStudio.Imaging`
- `Microsoft.VisualStudio.Shell.15.0`

### License System Dependencies
- HTTP client for API communication (if not using built-in)
- JWT parsing library (System.IdentityModel.Tokens.Jwt or similar)
- Visual Studio Shell services for email retrieval

## Contributing Guidelines

When working on this project:

1. **Maintain compatibility**: Don't break existing user settings
2. **Performance first**: Always profile changes with large files
3. **Test thoroughly**: VS extension bugs are hard to debug in production
4. **Document complex logic**: Especially in `GetTagsCore()` and `GetCodeBlockHeader()`
5. **Preserve cache behavior**: Cache invalidation bugs cause major issues
6. **Follow UI thread rules**: Always check thread requirements for VS APIs
7. **Update version**: Increment version in `source.extension.cs` and manifest
8. **Update display names**: Add new languages to `ContentTypeDisplayNameMapper`
9. **Test settings migration**: Ensure old settings load correctly with new code

**License System:**
1. **Never bypass license checks**: All language restrictions must be enforced
2. **Secure token handling**: Don't log full JWT tokens, sanitize in debug output
3. **Clear user messaging**: License prompts should be helpful, not annoying
4. **Graceful failures**: License issues shouldn't crash extension
5. **Privacy conscious**: Only collect email with user consent, explain usage
6. **Test thoroughly**: License edge cases (expired, invalid, missing email)
7. **Store consistency**: Keep store URL and messaging aligned
8. **Update documentation**: Document any license flow changes

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
- Add telemetry events if appropriate (with user consent)

### When adding language support:
- Add mapping to `ContentTypeDisplayNameMapper._displayNameMap`
- Use clear, recognizable display names (e.g., "C#" not "csharp")
- Follow existing naming conventions (e.g., "Language (variant)" for variants)
- Test that the mapping appears correctly in options UI
- Verify fallback logic handles edge cases gracefully

### When debugging issues:
- Enable DEBUG mode to see detailed logging in VS Output window
- Use Activity Log: `%AppData%\Microsoft\VisualStudio\<version>\ActivityLog.xml`
- Test in VS Experimental Instance (automatically launched during debugging)
- Check for `NullReferenceException` in `GetTagsCore()` (expected when closing editor)
- Verify settings persistence in registry: `HKCU\Software\Microsoft\VisualStudio\<version>\CodeBlockEndTag`

### When refactoring:
- Extract reusable logic into static helper classes (like `ContentTypeDisplayNameMapper`)
- Maintain single responsibility principle
- Keep UI logic separate from business logic
- Use dependency injection (MEF) for testability
- Document public APIs with XML comments

### When modifying license logic:
- Always check `Services.LicenseService.HasValidProLicense()` before allowing non-C# features
- Use `Languages.CSharp` constant for free language identification
- Maintain case-insensitive language name comparisons
- Update UI immediately after license state changes
- Handle network failures gracefully during activation
- Never hardcode license keys or bypass checks
- Test with expired tokens to verify fallback behavior
- Ensure email retrieval fails gracefully

### When adding new language-dependent features:
- Check license status before enabling for non-C# languages
- Show informative PRO-required messages
- Add visual indicators (🔒 or [PRO] tags) in UI
- Allow feature configuration but prevent execution without license
- Document that feature requires PRO license
- Consider free tier alternatives or limited functionality

### When debugging license issues:
- Check Activity Log for license validation failures
- Verify JWT token format and expiration in settings
- Test email retrieval from Visual Studio settings
- Confirm API endpoint availability
- Check for network connectivity issues
- Verify license key format matches expected pattern
- Test activation flow end-to-end with test keys

**Solution**: Centralized static class with dictionary-based mapping.

**Benefits**:
- Single source of truth for all display names
- Easy to add new languages (just add dictionary entry)
- Consistent naming across extension
- Testable and maintainable
- Intelligent fallback for unknown types

### Why Name-Based Settings?
**Problem**: Positional array settings broke when language order changed.

**Solution**: Store settings as "LanguageName:Value" pairs.

**Benefits**:
- Resilient to language list changes
- Portable across VS versions
- Preserves user intent even when languages added/removed
- Backward compatible with legacy format

### Why Dynamic Language Discovery?
**Problem**: Hardcoded language lists became outdated and didn't support user-installed extensions.

**Solution**: Query VS content type registry at runtime.

**Benefits**:
- Automatically supports new languages
- Works with third-party extensions
- No code changes needed for new languages
- Scales with VS ecosystem

## Useful Resources

- **VS SDK Documentation**: https://docs.microsoft.com/en-us/visualstudio/extensibility/
- **Known Monikers**: http://glyphlist.azurewebsites.net/knownmonikers/
- **Text Editor Extensibility**: https://docs.microsoft.com/en-us/visualstudio/extensibility/inside-the-editor
- **Marketplace Publishing**: https://marketplace.visualstudio.com/manage
- **Content Types**: https://docs.microsoft.com/en-us/visualstudio/extensibility/language-service-and-editor-extension-points

---

*This project demonstrates advanced VS extensibility concepts including real-time text parsing, adornment rendering, MEF composition, dynamic content type discovery, and seamless IDE integration.*