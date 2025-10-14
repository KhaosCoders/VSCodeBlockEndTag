//------------------------------------------------------------------------------
// <copyright file="CBETagPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using CodeBlockEndTag.Model;
using EnvDTE;
using EnvDTE80;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBlockEndTag;

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
[ProvideOptionPage(typeof(OptionPage.CBEOptionPage), "KC Extensions", "CodeBlock End Tagger", 113, 114, true)]
// Load package at every (including none) project type
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideService(typeof(IFontAndColorDefaultsProvider))]
[FontAndColorRegistration(typeof(IFontAndColorDefaultsProvider), Shell.FontAndColorDefaultsCSharpTags.CategoryNameString, Shell.FontAndColorDefaultsCSharpTags.CategoryGuidString)]
public sealed class CBETagPackage : AsyncPackage, IVsFontAndColorDefaultsProvider, IFontAndColorDefaultsProvider
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
    private static OptionPage.CBEOptionPage _optionPage;

    /// <summary>
    /// Instance of ActivityLog
    /// </summary>
    public IVsActivityLog Log { get; private set; }

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
    public static IList<IContentType> ContentTypes { get; } = [];

    /// <summary>
    /// Load the list of content types
    /// </summary>
    internal static void ReadContentTypes(IContentTypeRegistryService ContentTypeRegistryService)
    {
        if (ContentTypes.Count > 0)
        {
            return;
        }

        foreach (var ct in ContentTypeRegistryService.ContentTypes)
        {
            if (ct.IsOfType("code"))
            {
                ContentTypes.Add(ct);
            }
        }
    }

    public static bool IsLanguageSupported(string lang) => _optionPage?.IsLanguageSupported(lang) ?? false;

    #region Option Values

    public static int CBEDisplayMode => _optionPage?.CBEDisplayMode ?? (int)DisplayModes.IconAndText;

    public static int CBEVisibilityMode => _optionPage?.CBEVisibilityMode ?? (int)VisibilityModes.Always;

    public static bool CBETaggerEnabled => _optionPage?.CBETaggerEnabled ?? false;

    public static int CBEClickMode => _optionPage?.CBEClickMode ?? (int)ClickMode.DoubleClick;

    public static int CBEMargin => _optionPage?.CBEMargin ?? 4;

    #endregion

    #region VS Build Version

    public static async Task<double> GetVsVersionAsync()
    {
        await Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
        DTE2 dte = (DTE2)await Instance.GetServiceAsync(typeof(DTE));

        if (dte == null || !double.TryParse(dte.Version, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out double version))
        {
            throw new Exception("Can't read Visual Studio Version!");
        }
        return version;
    }

    public static async Task<string> GetVsRegistryRootAsync()
    {
        await Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
        DTE2 dte = (DTE2)await Instance.GetServiceAsync(typeof(DTE));
        return dte?.RegistryRoot;
    }

    #endregion

    #region Package Members

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    /// </summary>
    protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);

        // Switches to the UI thread in order to consume some services used in command initialization
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var log = await GetServiceAsync(typeof(SVsActivityLog)) as IVsActivityLog;
        if (log == null)
        {
            return;
        }

        Log = log;
        Log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, ToString(), "InitializeAsync");

        // ensure that we have instance
        Shell.FontAndColorDefaultsCSharpTags.EnsureInstance();

        Log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, ToString(), "Register IFontAndColorDefaultsProvider");
        ((IServiceContainer)this).AddService(typeof(IFontAndColorDefaultsProvider), this, true);

        _optionPage = (OptionPage.CBEOptionPage)Instance.GetDialogPage(typeof(OptionPage.CBEOptionPage));
        _optionPage.OptionChanged += Page_OptionChanged;

        // Initialize telemetry
        await InitializeTelemetryAsync();

        // Update taggers, that were initialized before the package
        Page_OptionChanged(this);

        SubscribeForColorChangeEvents();

        Log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, ToString(), "InitializeAsync ended");
    }

    /// <summary>
    /// Initializes the telemetry service
    /// </summary>
    private async Task InitializeTelemetryAsync()
    {
        try
        {
            // Use Connection String (modern API) instead of deprecated Instrumentation Key
            const string connectionString = "InstrumentationKey=3a7d81b1-803b-43c5-a782-68c95fc32325;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=da69b065-c68c-4172-8ba3-aa3681bd12cc";

            // Initialize telemetry service
            Telemetry.TelemetryService.Instance.Initialize(
                connectionString,
                _optionPage?.TelemetryEnabled ?? true);

            // Track extension loaded
            var vsVersion = await GetVsVersionAsync();
            Telemetry.TelemetryEvents.TrackExtensionLoaded(vsVersion.ToString());

            Log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, ToString(), "Telemetry initialized");
        }
        catch (Exception ex)
        {
            // Telemetry should never break the extension
            Log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, ToString(), $"Telemetry initialization failed: {ex.Message}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        UnsubscribeFromColorChangeEvents();

        // Flush and dispose telemetry
        try
        {
            Telemetry.TelemetryService.Instance.Flush();
            Telemetry.TelemetryService.Instance.Dispose();
        }
        catch
        {
            // Fail silently
        }

        base.Dispose(disposing);
    }

    private void Page_OptionChanged(object _) => PackageOptionChanged?.Invoke(this);

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
    public int GetObject(ref Guid rguidCategory, out object ppObj)
    {
        Log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, ToString(), $"GetObject {rguidCategory}");
        ThreadHelper.ThrowIfNotOnUIThread();

        ppObj = rguidCategory == Shell.FontAndColorDefaultsCSharpTags.Instance.CategoryGuid ? Shell.FontAndColorDefaultsCSharpTags.Instance : null;

        Log.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, ToString(), $"GetObject {rguidCategory} obj: {ppObj}");
        return 0;
    }
    #endregion
}
