using CodeBlockEndTag.Converters;
using CodeBlockEndTag.Model;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace CodeBlockEndTag.OptionPage;

[Guid("B009CDB7-6900-47DC-8403-285191252811")]
[ComVisible(true)]
public partial class CBEOptionPage : DialogPage
{
    // Name of settings collection where bit array is stored
    private const string CollectionName = "CodeBlockEndTag";

    public delegate void OptionChangedHandler(object sender);
    public event OptionChangedHandler OptionChanged;

    public CBEOptionPage()
    {
        // Initialize with empty array - will be populated when ContentTypes are loaded
        SupportedLangActive = [];
        _supportedLangs = [];
    }

    #region supported languages

    // List of all supported languages - dynamically loaded from Visual Studio's content types
    private SupportedLang[] _supportedLangs;

    /// <summary>
    /// Gets an array with all supported languages display names
    /// </summary>
    public string[] SupportedLangDisplayNames
    {
        get
        {
            return _supportedLangs.Select(l => l.DisplayName).ToArray();
        }
    }

    /// <summary>
    /// Gets or sets the array storing whether the tagger is enabled for a language or not
    /// </summary>
    [TypeConverter(typeof(BoolArrayConverter))]
    public bool[] SupportedLangActive { get; set; }

    /// <summary>
    /// Enable or disable a supported language
    /// </summary>
    public void SetSupportedLangActive(int index, bool active)
    {
        if (index >= SupportedLangActive.Length)
        {
            bool[] a = SupportedLangActive;
            Array.Resize(ref a, index + 1);
            SupportedLangActive = a;
        }
        SupportedLangActive[index] = active;
    }

    /// <summary>
    /// Returns true if the given language is supported and active
    /// </summary>
    public bool IsLanguageSupported(string lang)
    {
        // Ensure languages are initialized before showing the UI
        if (_supportedLangs.Length == 0)
            InitializeSupportedLanguages();

        int index = Array.FindIndex(_supportedLangs, sl => sl.Name.Equals(lang, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || index >= SupportedLangActive.Length)
        {
            return false;
        }

        // C# is always free to use
        if (lang.Equals(Languages.CSharp, StringComparison.OrdinalIgnoreCase))
        {
            return SupportedLangActive[index];
        }

        // Other languages require PRO license
        if (!Services.LicenseService.HasValidProLicense())
        {
            return false;
        }

        return SupportedLangActive[index];
    }

    /// <summary>
    /// Initialize supported languages from Visual Studio's content type registry
    /// </summary>
    public void InitializeSupportedLanguages()
    {
        if (CBETagPackage.ContentTypes.Count == 0)
        {
            return; // ContentTypes not loaded yet
        }

        // Build language list from content types
        var languages = CBETagPackage.ContentTypes
            .Select(ct => new SupportedLang
            {
                Name = ct.TypeName,
                DisplayName = ContentTypeDisplayNameMapper.GetDisplayName(ct.TypeName)
            })
            .OrderBy(sl => sl.DisplayName)
            .ToArray();

        bool hasProLicense = Services.LicenseService.HasValidProLicense();

        // If we already have languages loaded, preserve user settings
        if (_supportedLangs.Length > 0 && languages.Length > 0)
        {
            // Create a dictionary of old settings
            var oldSettings = _supportedLangs
                .Select((lang, index) => new { lang.Name, Active = index < SupportedLangActive.Length && SupportedLangActive[index] })
                .ToDictionary(x => x.Name, x => x.Active, StringComparer.OrdinalIgnoreCase);

            // Apply old settings to new language list
            var newActiveArray = new bool[languages.Length];
            for (int i = 0; i < languages.Length; i++)
            {
                bool isCSharp = languages[i].Name.Equals(Languages.CSharp, StringComparison.OrdinalIgnoreCase);

                // Use old setting if available, otherwise default based on language and license
                if (oldSettings.TryGetValue(languages[i].Name, out bool active))
                {
                    newActiveArray[i] = active;
                }
                else
                {
                    // New language discovered: C# always enabled by default, others only if PRO license exists
                    newActiveArray[i] = isCSharp || hasProLicense;
                }
            }

            _supportedLangs = languages;
            SupportedLangActive = newActiveArray;
        }
        else
        {
            // First time initialization
            _supportedLangs = languages;
            var newActiveArray = new bool[languages.Length];

            for (int i = 0; i < languages.Length; i++)
            {
                bool isCSharp = languages[i].Name.Equals(Languages.CSharp, StringComparison.OrdinalIgnoreCase);
                // C# always enabled by default, others only if PRO license exists
                newActiveArray[i] = isCSharp || hasProLicense;
            }

            SupportedLangActive = newActiveArray;
        }
    }

    /// <summary>
    /// Gets the internal supported languages array (for UI access)
    /// </summary>
    internal SupportedLang[] GetSupportedLanguages() => _supportedLangs;

    #endregion

    #region other settings

    /// <summary>
    /// Gets or set the option: Enable CodeBlock End Tagger
    /// </summary>
    public bool CBETaggerEnabled
    {
        get => cbeTaggerEnabled;
        set
        {
            if (cbeTaggerEnabled != value)
            {
                cbeTaggerEnabled = value;
                OptionChanged?.Invoke(this);
            }
        }
    }
    private bool cbeTaggerEnabled = true;

    /// <summary>
    /// Gets or sets the option: Display Mode (Icon&Text / Icon / Text)
    /// </summary>
    public int CBEDisplayMode
    {
        get => cbeDisplayMode;
        set
        {
            if (cbeDisplayMode != value)
            {
                cbeDisplayMode = value;
                OptionChanged?.Invoke(this);
            }
        }
    }
    private int cbeDisplayMode = (int)DisplayModes.IconAndText;

    /// <summary>
    /// Gets or sets the option: Navigate on (Single-Click / Double-Click / CTRL+Click)
    /// </summary>
    public int CBEClickMode
    {
        get => cbeClickMode;
        set
        {
            if (cbeClickMode != value)
            {
                cbeClickMode = value;
                OptionChanged?.Invoke(this);
            }
        }
    }
    private int cbeClickMode = (int)ClickMode.SingleClick;

    /// <summary>
    /// Gets or sets the option: Show Tags when (Always / Header not visible)
    /// </summary>
    public int CBEVisibilityMode
    {
        get => cbeVisibilityMode;
        set
        {
            if (cbeVisibilityMode != value)
            {
                cbeVisibilityMode = value;
                OptionChanged?.Invoke(this);
            }
        }
    }
    private int cbeVisibilityMode = (int)VisibilityModes.HeaderNotVisible;

    /// <summary>
    /// Gets or sets the margin (in pixels) between the closing brace and the tag
    /// </summary>
    public int CBEMargin
    {
        get => cbeMargin;
        set
        {
            if (cbeMargin != value)
            {
                cbeMargin = value;
                OptionChanged?.Invoke(this);
            }
        }
    }
    private int cbeMargin = 4;

    /// <summary>
    /// Gets or sets whether anonymous usage data collection is enabled
    /// </summary>
    public bool TelemetryEnabled
    {
        get => telemetryEnabled;
        set
        {
            if (telemetryEnabled != value)
            {
                telemetryEnabled = value;
                // Update telemetry service
                Telemetry.TelemetryService.Instance.SetEnabled(value);
                OptionChanged?.Invoke(this);
            }
        }
    }
    private bool telemetryEnabled = true;

    /// <summary>
    /// Gets or sets whether the user has seen the language support info bar
    /// </summary>
    public bool LanguageSupportInfoBarSeen
    {
        get => languageSupportInfoBarSeen;
        set => languageSupportInfoBarSeen = value;
    }
    private bool languageSupportInfoBarSeen = false;

    /// <summary>
    /// Gets or sets the PRO license token (JWT)
    /// </summary>
    public string LicenseToken
    {
        get => licenseToken;
        set => licenseToken = value ?? string.Empty;
    }
    private string licenseToken = string.Empty;

    #endregion

    #region save / load

    public override void SaveSettingsToStorage()
    {
        base.SaveSettingsToStorage();

        Dispatcher.CurrentDispatcher.VerifyAccess();

        var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
        var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

        if (!userSettingsStore.CollectionExists(CollectionName))
        {
            userSettingsStore.CreateCollection(CollectionName);
        }

        // Save language settings as name:value pairs for better compatibility
        // Format: "LanguageName1:1,LanguageName2:0,..." where 1=enabled, 0=disabled
        if (_supportedLangs != null && _supportedLangs.Length > 0)
        {
            var languageSettings = new List<string>();
            for (int i = 0; i < _supportedLangs.Length && i < SupportedLangActive.Length; i++)
            {
                languageSettings.Add($"{_supportedLangs[i].Name}:{(SupportedLangActive[i] ? "1" : "0")}");
            }

            userSettingsStore.SetString(
                CollectionName,
                "SupportedLanguages",
                string.Join(",", languageSettings));
        }

        // Keep legacy format for backward compatibility (will be removed in future versions)
        var converter = new BoolArrayConverter();
        userSettingsStore.SetString(
            CollectionName,
            nameof(SupportedLangActive),
            converter.ConvertTo(SupportedLangActive, typeof(string)) as string);
    }

    public override void LoadSettingsFromStorage()
    {
        base.LoadSettingsFromStorage();

        Dispatcher.CurrentDispatcher.VerifyAccess();

        var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
        var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

        if (!userSettingsStore.CollectionExists(CollectionName))
        {
            return;
        }

        // Ensure languages are initialized before showing the UI
        if (_supportedLangs.Length == 0)
            InitializeSupportedLanguages();

        // Try loading new name-based format first
        if (userSettingsStore.PropertyExists(CollectionName, "SupportedLanguages"))
        {
            try
            {
                var settingsString = userSettingsStore.GetString(CollectionName, "SupportedLanguages");
                var languageSettings = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                foreach (var pair in settingsString.Split(','))
                {
                    var parts = pair.Split(':');
                    if (parts.Length == 2)
                    {
                        languageSettings[parts[0]] = parts[1] == "1";
                    }
                }

                // Apply loaded settings to current language list
                if (_supportedLangs != null && _supportedLangs.Length > 0)
                {
                    var newActiveArray = new bool[_supportedLangs.Length];
                    for (int i = 0; i < _supportedLangs.Length; i++)
                    {
                        // Use saved setting if available, otherwise default to true
                        newActiveArray[i] = languageSettings.TryGetValue(_supportedLangs[i].Name, out bool active) ? active : true;
                    }
                    SupportedLangActive = newActiveArray;
                }

                return; // Successfully loaded new format
            }
            catch
            {
                // Fall through to legacy format
            }
        }

        // Fall back to legacy positional format for backward compatibility
        if (userSettingsStore.PropertyExists(CollectionName, nameof(SupportedLangActive)))
        {
            var converter = new BoolArrayConverter();
            var loadedArray = converter.ConvertFrom(
                userSettingsStore.GetString(CollectionName, nameof(SupportedLangActive))) as bool[];

            // Only use if we have languages defined
            if (loadedArray != null && _supportedLangs != null && _supportedLangs.Length > 0)
            {
                // Map old positional settings to new language list (less reliable)
                var minLength = Math.Min(loadedArray.Length, SupportedLangActive.Length);
                Array.Copy(loadedArray, SupportedLangActive, minLength);
            }
        }
    }

    #endregion

    protected override IWin32Window Window
    {
        get
        {
            // Ensure languages are initialized before showing the UI
            InitializeSupportedLanguages();

            CBEOptionPageControl page = new()
            {
                optionsPage = this
            };
            page.Initialize();
            return page;
        }
    }

}
