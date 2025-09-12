using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Settings;
using System.Windows.Threading;
using CodeBlockEndTag.Converters;
using CodeBlockEndTag.Model;

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
        // default: all languages are enabled
        SupportedLangActive = _supportedLangs.Select(_ => true).ToArray();
    }

    #region supported languages

    // List of all supported languages
    // Never remove any! User preferences are stored for each array position
    private readonly SupportedLang[] _supportedLangs = {
        new() { Name = Languages.CSharp,       DisplayName = "CSharp C#" },
        /*
         All of these languages don't come with a decent TextStructureNavigator so they can't be used right now

        new() { Name = "C/C++",        DisplayName = "C/C++" },
        new() { Name = "PowerShell",   DisplayName = "PowerShell" },
        new() { Name = "JavaScript",   DisplayName = "JavaScript" },
        new() { Name = "CoffeeScript", DisplayName = "CoffeeScript" },
        new() { Name = "TypeScript",   DisplayName = "TypeScript" },
        new() { Name = "SCSS",         DisplayName = "SCSS/CSS" },
        new() { Name = "LESS",         DisplayName = "LESS" },
        new() { Name = "SASS",         DisplayName = "SASS" },
        new() { Name = "HTML",         DisplayName = "HTML(JS/CSS)" }
        */
    };

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
        int index = Array.FindIndex(_supportedLangs, sl => sl.Name.Equals(lang));
        if (index >= 0 && index < SupportedLangActive.Length)
        {
            return SupportedLangActive[index];
        }
        return false;
    }

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

        if (!userSettingsStore.PropertyExists(CollectionName, nameof(SupportedLangActive)))
        {
            return;
        }

        var converter = new BoolArrayConverter();
        SupportedLangActive = converter.ConvertFrom(
            userSettingsStore.GetString(CollectionName, nameof(SupportedLangActive))) as bool[];
    }

    #endregion

    protected override IWin32Window Window
    {
        get
        {
            CBEOptionPageControl page = new();
            page.optionsPage = this;
            page.Initialize();
            return page;
        }
    }
}
