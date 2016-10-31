using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Settings;

namespace CodeBlockEndTag
{

    [Guid("B009CDB7-6900-47DC-8403-285191252811")]
    public class CBEOptionPage : DialogPage
    {
        // Name of settings collection where bit array is stored
        const string collectionName = "CodeBlockEndTag";

        public delegate void OptionChangedHandler(object sender);
        public event OptionChangedHandler OptionChanged;

        public CBEOptionPage()
        {
            // default: all languages are enabled
            SupportedLangActive = _supportedLangs.Select(l => true).ToArray();
        }

        #region supported languages

        // List of all supported languages 
        // Never remove any! User preferences are stored for each array position
        private SupportedLang[] _supportedLangs = new SupportedLang[]
        {
            new SupportedLang() { Name = "CSharp",       DisplayName = "CSharp C#" },
            /*
             All of these languages don't come with a decent TextStructureNavigator so they can't be used right now

            new SupportedLang() { Name = "C/C++",        DisplayName = "C/C++" },
            new SupportedLang() { Name = "PowerShell",   DisplayName = "PowerShell" },
            new SupportedLang() { Name = "JavaScript",   DisplayName = "JavaScript" },
            new SupportedLang() { Name = "CoffeeScript", DisplayName = "CoffeeScript" },
            new SupportedLang() { Name = "TypeScript",   DisplayName = "TypeScript" },
            new SupportedLang() { Name = "SCSS",         DisplayName = "SCSS/CSS" },
            new SupportedLang() { Name = "LESS",         DisplayName = "LESS" },
            new SupportedLang() { Name = "SASS",         DisplayName = "SASS" },
            new SupportedLang() { Name = "HTML",         DisplayName = "HTML(JS/CSS)" }
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
            get { return cbeTaggerEnabled; }
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
            get { return cbeDisplayMode; }
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

        public enum DisplayModes : int
        {
            Text = 1,
            Icon = 2,
            IconAndText = 3
        }

        /// <summary>
        /// Gets or sets the option: Navigate on (Single-Click / Double-Click)
        /// </summary>
        public int CBEClickCount
        {
            get { return cbeClickCount; }
            set
            {
                if (cbeClickCount != value)
                {
                    cbeClickCount = value;
                    OptionChanged?.Invoke(this);
                }
            }
        }
        private int cbeClickCount = 1;

        /// <summary>
        /// Gets or sets the option: Show Tags when (Always / Header not visible)
        /// </summary>
        public int CBEVisibilityMode
        {
            get { return cbeVisibilityMode; }
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

        public enum VisibilityModes : int
        {
            Always = 1,
            HeaderNotVisible = 2
        }

        #endregion

        #region save / load 

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();

            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!userSettingsStore.CollectionExists(collectionName))
                userSettingsStore.CreateCollection(collectionName);

            var converter = new BoolArrayConverter();
            userSettingsStore.SetString(
                collectionName,
                nameof(SupportedLangActive),
                converter.ConvertTo(SupportedLangActive, typeof(string)) as string);
        }

        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();

            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!userSettingsStore.PropertyExists(collectionName, nameof(SupportedLangActive)))
                return;

            var converter = new BoolArrayConverter();
            SupportedLangActive = converter.ConvertFrom(
                userSettingsStore.GetString(collectionName, nameof(SupportedLangActive))) as bool[];
        }


        #endregion

        protected override IWin32Window Window
        {
            get
            {
                CBEOptionPageControl page = new CBEOptionPageControl();
                page.optionsPage = this;
                page.Initialize();
                return page;
            }
        }
    }

    public struct SupportedLang
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }
}
