//------------------------------------------------------------------------------
// <copyright file="CBETagPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Win32;

namespace CodeBlockEndTag
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(CBETagPackage.PackageGuidString)]
    // Add OptionPage to package
    [ProvideOptionPage(typeof(CBEOptionPage), "KC Extensions", "CodeBlock End Tagger", 113, 114, true)]
    // Load package at every (including none) project type
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideService(typeof(IFontAndColorDefaultsProvider))]
    [FontAndColorRegistration(typeof(IFontAndColorDefaultsProvider), Shell.FontAndColorDefaultsCSharpTags.CategoryNameString, Shell.FontAndColorDefaultsCSharpTags.CategoryGuidString)]
    public sealed class CBETagPackage : AsyncPackage, IFontAndColorDefaultsProvider, IVsFontAndColorDefaultsProvider
    {
        /// <summary>
        /// CBETagPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "D7C91E0F-240B-4605-9F35-ACCF63A68623";

        public delegate void PackageOptionChangedHandler(object sender);
        /// <summary>
        /// Event fired if any option in the OptionPage is changed
        /// </summary>
        public event PackageOptionChangedHandler PackageOptionChanged;

        /// <summary>
        /// Gets the singelton instance of the class
        /// </summary>
        public static CBETagPackage Instance { get; private set; }

        /// <summary>
        /// Reference on the package's option page
        /// </summary>
        private static CBEOptionPage _optionPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="CBETagPackage"/> class.
        /// </summary>
        public CBETagPackage()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets a list of all possible content types in VisualStudio
        /// </summary>
        public static IList<IContentType> ContentTypes { get; private set; }

        /// <summary>
        /// Load the list of content types
        /// </summary>
        internal static void ReadContentTypes(IContentTypeRegistryService ContentTypeRegistryService)
        {
            if (ContentTypes != null) return;
            ContentTypes = new List<IContentType>();
            foreach (var ct in ContentTypeRegistryService.ContentTypes)
            {
                if (ct.IsOfType("code"))
                    ContentTypes.Add(ct);
            }
        }

        public static bool IsLanguageSupported(string lang) => _optionPage?.IsLanguageSupported(lang) ?? false;

        #region Option Values

        public static int CBEDisplayMode => _optionPage?.CBEDisplayMode ?? (int)CBEOptionPage.DisplayModes.IconAndText;

        public static int CBEVisibilityMode => _optionPage?.CBEVisibilityMode ?? (int)CBEOptionPage.VisibilityModes.Always;

        public static bool CBETaggerEnabled => _optionPage?.CBETaggerEnabled ?? false;

        public static int CBEClickMode => _optionPage?.CBEClickMode ?? (int)CBEOptionPage.ClickMode.DoubleClick;

        public static double CBETagScale => _optionPage?.CBETagScale ?? 1d;

        #endregion

        #region VS Build Version

        public static async Task<double> GetVsVersionAsync()
        {
            DTE2 dte = (DTE2)await Instance.GetServiceAsync(typeof(DTE));

            if (!double.TryParse(dte.Version, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out double version))
            {
                throw new Exception("Can't read Visual Studio Version!");
            }
            return version;
        }

        public static async Task<string> GetVsRegistryRootAsync()
        {
            DTE2 dte = (DTE2)await Instance.GetServiceAsync(typeof(DTE));
            return dte.RegistryRoot;
        }

        #endregion

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);

            // Switches to the UI thread in order to consume some services used in command initialization
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // ensure that we have instance
            new Shell.FontAndColorDefaultsCSharpTags();

            ((IServiceContainer)this).AddService(typeof(IFontAndColorDefaultsProvider), this, true);

            _optionPage = (CBEOptionPage)Instance.GetDialogPage(typeof(CBEOptionPage));
            _optionPage.OptionChanged += Page_OptionChanged;

            // Update taggers, that were initialized before the package
            PackageOptionChanged?.Invoke(this);

            SubscribeForColorChangeEvents();
        }

        protected override void Dispose(bool disposing)
        {
            UnsubscribeFromColorChangeEvents();

            base.Dispose(disposing);
        }

        private void Page_OptionChanged(object sender) => PackageOptionChanged?.Invoke(this);

        private static void FontAndColorsChanged(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Shell.FontAndColorDefaultsCSharpTags.Instance.ReloadFontAndColors();
        }

        private static void SubscribeForColorChangeEvents()
        {
            SystemEvents.DisplaySettingsChanged += FontAndColorsChanged;
            SystemEvents.PaletteChanged += FontAndColorsChanged;
            SystemEvents.UserPreferenceChanged += FontAndColorsChanged;
        }

        private static void UnsubscribeFromColorChangeEvents()
        {
            SystemEvents.DisplaySettingsChanged -= FontAndColorsChanged;
            SystemEvents.PaletteChanged -= FontAndColorsChanged;
            SystemEvents.UserPreferenceChanged -= FontAndColorsChanged;
        }

        #endregion

        #region IVsFontAndColorDefaultsProvider
        int IVsFontAndColorDefaultsProvider.GetObject(ref Guid rguidCategory, out object ppObj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ppObj = rguidCategory == Shell.FontAndColorDefaultsCSharpTags.Instance.CategoryGuid ? Shell.FontAndColorDefaultsCSharpTags.Instance : null;
            return 0;
        }
        #endregion
    }
}
