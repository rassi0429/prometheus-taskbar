using System.Collections.Generic;

namespace PrometheusTaskbar.Models;

public class AppSettings
{
    public ConnectionSettings Connection { get; set; } = new();
    public List<MetricSettings> Metrics { get; set; } = new();
    public DisplaySettings Display { get; set; } = new();
}

public class ConnectionSettings
{
    public string EndpointUrl { get; set; } = "http://localhost:9090";
    public int TimeoutSeconds { get; set; } = 30;
    public AuthSettings Authentication { get; set; } = new();
}

public class AuthSettings
{
    public AuthType Type { get; set; } = AuthType.None;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class MetricSettings
{
    public string MetricName { get; set; } = string.Empty;
    public string PromQlQuery { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new();
    public string Unit { get; set; } = string.Empty;
}

public class DisplaySettings
{
    public DisplayFormat Format { get; set; } = DisplayFormat.NameAndValue;
    public string CustomFormat { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; } = 2;
    public bool ShowUnit { get; set; } = true;
    public AlertSettings Alert { get; set; } = new();
    public bool AutoStartWithWindows { get; set; } = false;
}

public class AlertSettings
{
    public bool Enabled { get; set; } = false;
    public double Threshold { get; set; } = 0;
    public string Color { get; set; } = "#FF0000";
}

public enum AuthType
{
    None,
    Basic
}

public enum DisplayFormat
{
    ValueOnly,
    NameAndValue,
    Custom
}
