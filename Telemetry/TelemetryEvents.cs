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
}
