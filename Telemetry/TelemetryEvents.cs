using System.Collections.Generic;

namespace CodeBlockEndTag.Telemetry;

/// <summary>
/// Helper class for tracking common telemetry events
/// </summary>
internal static class TelemetryEvents
{
    // Event names
    public const string ExtensionLoaded = "ExtensionLoaded";
    public const string TagCreated = "TagCreated";
    public const string TagClicked = "TagClicked";
    public const string SettingChanged = "SettingChanged";
    public const string LanguageUsed = "LanguageUsed";
    public const string PerformanceMetric = "PerformanceMetric";
    public const string LicenseActivationAttempted = "LicenseActivationAttempted";
    public const string LicenseActivationSucceeded = "LicenseActivationSucceeded";
    public const string LicenseActivationFailed = "LicenseActivationFailed";
    public const string LicenseTokenRequested = "LicenseTokenRequested";
    public const string StorePageOpened = "StorePageOpened";
    public const string OptionsPageOpened = "OptionsPageOpened";

    // Property keys
    public const string PropertyLanguage = "Language";
    public const string PropertyDisplayMode = "DisplayMode";
    public const string PropertyVisibilityMode = "VisibilityMode";
    public const string PropertyClickMode = "ClickMode";
    public const string PropertyEnabled = "Enabled";
    public const string PropertySettingName = "SettingName";
    public const string PropertySettingValue = "SettingValue";
    public const string PropertyVsVersion = "VsVersion";
    public const string PropertyTagCount = "TagCount";
    public const string PropertyJumpToHead = "JumpToHead";
    public const string PropertyElapsedMs = "ElapsedMs";
    public const string PropertyHasEmail = "HasEmail";
    public const string PropertySuccess = "Success";
    public const string PropertyErrorType = "ErrorType";
    public const string PropertyErrorMessage = "ErrorMessage";
    public const string PropertyActivationType = "ActivationType";
    public const string PropertyHasLicense = "HasLicense";

    /// <summary>
    /// Track extension loaded event
    /// </summary>
    public static void TrackExtensionLoaded(string vsVersion)
    {
        var properties = new Dictionary<string, string>
        {
            { PropertyVsVersion, vsVersion }
        };

        TelemetryService.Instance.TrackEvent(ExtensionLoaded, properties);
    }

    /// <summary>
    /// Track tag created event
    /// </summary>
    public static void TrackTagsCreated(string language, int tagCount)
    {
        var properties = new Dictionary<string, string>
        {
            { PropertyLanguage, language }
        };

        var metrics = new Dictionary<string, double>
        {
            { PropertyTagCount, tagCount }
        };

        TelemetryService.Instance.TrackEvent(TagCreated, properties, metrics);
    }

    /// <summary>
    /// Track tag clicked event
    /// </summary>
    public static void TrackTagClicked(bool jumpToHead)
    {
        var properties = new Dictionary<string, string>
        {
            { PropertyJumpToHead, jumpToHead.ToString() }
        };

        TelemetryService.Instance.TrackEvent(TagClicked, properties);
    }

    /// <summary>
    /// Track setting changed event
    /// </summary>
    public static void TrackSettingChanged(string settingName, string settingValue)
    {
        var properties = new Dictionary<string, string>
        {
            { PropertySettingName, settingName },
            { PropertySettingValue, settingValue }
        };

        TelemetryService.Instance.TrackEvent(SettingChanged, properties);
    }

    /// <summary>
    /// Track language usage
    /// </summary>
    public static void TrackLanguageUsed(string language)
    {
        var properties = new Dictionary<string, string>
        {
            { PropertyLanguage, language }
        };

        TelemetryService.Instance.TrackEvent(LanguageUsed, properties);
    }

    /// <summary>
    /// Track performance metric
    /// </summary>
    public static void TrackPerformance(string operation, long elapsedMs, int itemCount = 0)
    {
        var properties = new Dictionary<string, string>();
        var metrics = new Dictionary<string, double>
        {
            { PropertyElapsedMs, elapsedMs }
        };

        if (itemCount > 0)
        {
            metrics[PropertyTagCount] = itemCount;
        }

        TelemetryService.Instance.TrackEvent($"{PerformanceMetric}_{operation}", properties, metrics);
    }

    /// <summary>
    /// Track license activation attempt
    /// </summary>
    public static void TrackLicenseActivationAttempted(bool hasEmail, string activationType = "new")
    {
        var properties = new Dictionary<string, string>
        {
            { PropertyHasEmail, hasEmail.ToString() },
            { PropertyActivationType, activationType }
        };

        TelemetryService.Instance.TrackEvent(LicenseActivationAttempted, properties);
    }

    /// <summary>
    /// Track successful license activation
    /// </summary>
    public static void TrackLicenseActivationSucceeded(string activationType = "new")
    {
        var properties = new Dictionary<string, string>
        {
            { PropertySuccess, "true" },
            { PropertyActivationType, activationType }
        };

        TelemetryService.Instance.TrackEvent(LicenseActivationSucceeded, properties);
    }

    /// <summary>
    /// Track failed license activation
    /// </summary>
    public static void TrackLicenseActivationFailed(string errorType, string errorMessage, string activationType = "new")
    {
        var properties = new Dictionary<string, string>
        {
            { PropertySuccess, "false" },
            { PropertyErrorType, errorType },
            { PropertyErrorMessage, errorMessage ?? "Unknown" },
            { PropertyActivationType, activationType }
        };

        TelemetryService.Instance.TrackEvent(LicenseActivationFailed, properties);
    }

    /// <summary>
    /// Track license token request (re-activation)
    /// </summary>
    public static void TrackLicenseTokenRequested(bool hasEmail)
    {
        var properties = new Dictionary<string, string>
        {
            { PropertyHasEmail, hasEmail.ToString() }
        };

        TelemetryService.Instance.TrackEvent(LicenseTokenRequested, properties);
    }

    /// <summary>
    /// Track store page opened event
    /// </summary>
    public static void TrackStorePageOpened(bool hasEmail)
    {
        var properties = new Dictionary<string, string>
        {
            { PropertyHasEmail, hasEmail.ToString() }
        };

        TelemetryService.Instance.TrackEvent(StorePageOpened, properties);
    }

    /// <summary>
    /// Track options page opened event
    /// </summary>
    public static void TrackOptionsPageOpened(bool hasLicense)
    {
        var properties = new Dictionary<string, string>
        {
            { PropertyHasLicense, hasLicense.ToString() }
        };

        TelemetryService.Instance.TrackEvent(OptionsPageOpened, properties);
    }
}
