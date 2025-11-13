using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace CodeBlockEndTag.Services;

/// <summary>
/// Service for showing information bars in Visual Studio
/// </summary>
internal class InfoBarService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IVsActivityLog _log;
    private IVsInfoBarUIElement _currentInfoBarElement;

    public InfoBarService(IServiceProvider serviceProvider, IVsActivityLog log)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _log = log;
    }

    /// <summary>
    /// Shows an info bar with information about expanded language support
    /// This will retry for a short period if Visual Studio hasn't yet exposed the MainWindowInfoBarHost (initialization may be too early).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "VSSDK007:ThreadHelper.JoinableTaskFactory.RunAsync", Justification = "<Pending>")]
    public void ShowLanguageSupportInfoBar()
    {
        // Run asynchronously on the joinable task factory so callers can call this early during package init.
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var shell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
                if (shell == null)
                {
                    _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, "InfoBarService", "Could not get IVsShell service");
                    return;
                }

                // Retry getting the main window info bar host for a short time — sometimes initialization is too early
                IVsInfoBarHost infoBarHost = null;
                const int maxAttempts = 20;
                const int delayMs = 200;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                    infoBarHost = obj as IVsInfoBarHost;
                    if (infoBarHost != null)
                        break;

                    await Task.Delay(delayMs).ConfigureAwait(false);
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                }

                if (infoBarHost == null)
                {
                    _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, "InfoBarService", "Could not get info bar host after retries");
                    return;
                }

                // Create the info bar model
                var infoBarModel = new InfoBarModel(
                    new[]
                    {
                        new InfoBarTextSpan("CodeBlock End Tagger now supports more languages! "),
                        new InfoBarTextSpan("Configure which languages to enable in the options."),
                    },
                    new[]
                    {
                        new InfoBarHyperlink("Open Settings", "OpenSettings"),
                    },
                    KnownMonikers.StatusInformation,
                    isCloseButtonVisible: true);

                // Create the UI element
                var infoBarFactory = _serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                if (infoBarFactory == null)
                {
                    _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, "InfoBarService", "Could not get info bar factory");
                    return;
                }

                _currentInfoBarElement = infoBarFactory.CreateInfoBar(infoBarModel);
                _currentInfoBarElement.Advise(new InfoBarEvents(this, _log), out _);

                // Add the info bar to the host
                infoBarHost.AddInfoBar(_currentInfoBarElement);

                _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, "InfoBarService", "Language support info bar displayed");
            }
            catch (Exception ex)
            {
                _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "InfoBarService", $"Error showing info bar: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Closes the current info bar if it's open
    /// </summary>
    public void CloseInfoBar()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            var infoBarElement = _currentInfoBarElement;
            _currentInfoBarElement = null;
            infoBarElement?.Close();
        }
        catch (Exception ex)
        {
            _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "InfoBarService", $"Error closing info bar: {ex.Message}");
        }
    }

    /// <summary>
    /// Event handler for info bar events
    /// </summary>
    private class InfoBarEvents : IVsInfoBarUIEvents
    {
        private readonly InfoBarService _service;
        private readonly IVsActivityLog _log;

        public InfoBarEvents(InfoBarService service, IVsActivityLog log)
        {
            _service = service;
            _log = log;
        }

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (actionItem.ActionContext?.ToString() == "OpenSettings")
                {
                    // Open the options page
                    CBETagPackage.Instance?.ShowOptionPage(typeof(OptionPage.CBEOptionPage));
                    _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, "InfoBarService", "User opened settings from info bar");
                }

                // Close the info bar after action
                _service.CloseInfoBar();

                // Mark as seen
                CBETagPackage.Instance?.MarkLanguageSupportInfoBarAsSeen();
            }
            catch (Exception ex)
            {
                _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "InfoBarService", $"Error handling action: {ex.Message}");
            }
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Mark as seen when closed
                CBETagPackage.Instance?.MarkLanguageSupportInfoBarAsSeen();
                _service.CloseInfoBar();
                _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, "InfoBarService", "Info bar closed");
            }
            catch (Exception ex)
            {
                _log?.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, "InfoBarService", $"Error handling close: {ex.Message}");
            }
        }
    }
}
