//------------------------------------------------------------------------------
// <copyright file="CBETagPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

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
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    // Add OptionPage to package
    [ProvideOptionPage(typeof(CBEOptionPage), "KC Extensions", "CodeBlock End Tagger", 113, 114, true)]
    // Load package at every (including none) project type
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class CBETagPackage : AsyncPackage
    {
        /// <summary>
        /// CBETagPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "d7c91e0f-240b-4605-9f35-accf63a68623";

        public delegate void PackageOptionChangedHandler(object sender);
        /// <summary>
        /// Event fired if any option in the OptionPage is changed
        /// </summary>
        public event PackageOptionChangedHandler PackageOptionChanged;

        /// <summary>
        /// Gets the singelton instance of the class
        /// </summary>
        public static CBETagPackage Instance
        {
            get
            {
                return _instance;
            }
        }
        private static CBETagPackage _instance;


        /// <summary>
        /// Initializes a new instance of the <see cref="CBETagPackage"/> class.
        /// </summary>
        public CBETagPackage()
        {
            _instance = this;
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

        public static bool IsLanguageSupported(string lang)
        {
            CBEOptionPage page = (CBEOptionPage)Instance.GetDialogPage(typeof(CBEOptionPage));
            return page.IsLanguageSupported(lang);
        }

        #region Option Values
        
        public static int CBEDisplayMode
        {
            get
            {
                CBEOptionPage page = (CBEOptionPage)Instance.GetDialogPage(typeof(CBEOptionPage));
                return page.CBEDisplayMode;
            }
        }

        public static int CBEVisibilityMode
        {
            get
            {
                CBEOptionPage page = (CBEOptionPage)Instance.GetDialogPage(typeof(CBEOptionPage));
                return page.CBEVisibilityMode;
            }
        }

        public static bool CBETaggerEnabled
        {
            get
            {
                CBEOptionPage page = (CBEOptionPage)Instance.GetDialogPage(typeof(CBEOptionPage));
                return page.CBETaggerEnabled;
            }
        }

        public static int CBEClickMode
        {
            get
            {
                CBEOptionPage page = (CBEOptionPage)Instance.GetDialogPage(typeof(CBEOptionPage));
                return page.CBEClickMode;
            }
        }

        public static double CBETagScale
        {
            get
            {
                CBEOptionPage page = (CBEOptionPage)Instance.GetDialogPage(typeof(CBEOptionPage));
                return page.CBETagScale;
            }
        }

        #endregion

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // Switches to the UI thread in order to consume some services used in command initialization
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            CBEOptionPage page = (CBEOptionPage)Instance.GetDialogPage(typeof(CBEOptionPage));
            page.OptionChanged += Page_OptionChanged;
        }

        private void Page_OptionChanged(object sender)
        {
            PackageOptionChanged?.Invoke(this);
        }

        #endregion
    }
}
