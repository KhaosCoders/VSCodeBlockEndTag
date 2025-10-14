using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;

namespace CodeBlockEndTag.Telemetry;

/// <summary>
/// Service for tracking telemetry events using Azure Application Insights
/// </summary>
internal sealed class TelemetryService : IDisposable
{
    private static TelemetryService _instance;
    private static readonly object _lock = new object();

    private TelemetryClient _telemetryClient;
    private bool _isEnabled;
    private bool _disposed;

    /// <summary>
    /// Gets the singleton instance of the telemetry service
    /// </summary>
    public static TelemetryService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new TelemetryService();
                    }
                }
            }
            return _instance;
        }
    }

    private TelemetryService()
    {
        // Private constructor for singleton
    }

    /// <summary>
    /// Initializes the telemetry service with the connection string
    /// </summary>
    /// <param name="connectionString">Application Insights connection string (or legacy instrumentation key)</param>
    /// <param name="isEnabled">Whether telemetry is enabled (respects user privacy settings)</param>
    public void Initialize(string connectionString, bool isEnabled = true)
    {
        if (_disposed)
        {
            return;
        }

        _isEnabled = isEnabled;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _isEnabled = false;
            return;
        }

        try
        {
            var configuration = TelemetryConfiguration.CreateDefault();
            
            // Use ConnectionString (modern API) - supports both connection strings and legacy instrumentation keys
            configuration.ConnectionString = connectionString;

            // Set basic properties
            configuration.TelemetryChannel.DeveloperMode = false;

            _telemetryClient = new TelemetryClient(configuration);

            // Set common properties
            _telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            _telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Telemetry] Initialized with connection string: {connectionString.Substring(0, Math.Min(20, connectionString.Length))}...");
#endif
        }
        catch (Exception ex)
        {
            // Fail silently - telemetry should never break the extension
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Telemetry] Initialization failed: {ex.Message}");
#endif
            _isEnabled = false;
        }
    }

    /// <summary>
    /// Enables or disables telemetry collection
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
    }

    /// <summary>
    /// Tracks a custom event
    /// </summary>
    /// <param name="eventName">Name of the event</param>
    /// <param name="properties">Optional properties</param>
    /// <param name="metrics">Optional metrics</param>
    public void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
    {
        if (!_isEnabled || _telemetryClient == null || _disposed)
        {
            return;
        }

        try
        {
            _telemetryClient.TrackEvent(eventName, properties, metrics);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Telemetry] Event: {eventName}");
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    System.Diagnostics.Debug.WriteLine($"  {prop.Key}: {prop.Value}");
                }
            }
#endif
        }
        catch (Exception ex)
        {
            // Fail silently
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Telemetry] TrackEvent failed: {ex.Message}");
#endif
        }
    }

    /// <summary>
    /// Tracks an exception
    /// </summary>
    public void TrackException(Exception exception, IDictionary<string, string> properties = null)
    {
        if (!_isEnabled || _telemetryClient == null || _disposed)
        {
            return;
        }

        try
        {
            _telemetryClient.TrackException(exception, properties);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Telemetry] Exception: {exception.Message}");
#endif
        }
        catch
        {
            // Fail silently
        }
    }

    /// <summary>
    /// Tracks a metric value
    /// </summary>
    public void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
    {
        if (!_isEnabled || _telemetryClient == null || _disposed)
        {
            return;
        }

        try
        {
            _telemetryClient.TrackMetric(name, value, properties);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Telemetry] Metric: {name} = {value}");
#endif
        }
        catch
        {
            // Fail silently
        }
    }

    /// <summary>
    /// Flushes any pending telemetry data
    /// </summary>
    public void Flush()
    {
        if (_telemetryClient == null || _disposed)
        {
            return;
        }

        try
        {
            _telemetryClient.Flush();
            // Allow time for flushing
            System.Threading.Thread.Sleep(1000);
        }
        catch
        {
            // Fail silently
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            Flush();
            _telemetryClient = null;
        }
        catch
        {
            // Fail silently
        }
    }
}
