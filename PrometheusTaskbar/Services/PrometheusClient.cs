using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PrometheusTaskbar.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace PrometheusTaskbar.Services;

public class PrometheusClient
{
    private readonly ConfigManager _configManager;
    private RestClient? _client;

    public PrometheusClient(ConfigManager configManager)
    {
        _configManager = configManager;
        _configManager.SettingsChanged += (_, _) => InitializeClient();
        InitializeClient();
    }

    private void InitializeClient()
    {
        var options = new RestClientOptions(_configManager.CurrentSettings.Connection.EndpointUrl)
        {
            ThrowOnAnyError = false,
            MaxTimeout = _configManager.CurrentSettings.Connection.TimeoutSeconds * 1000
        };

        if (_configManager.CurrentSettings.Connection.Authentication.Type == AuthType.Basic)
        {
            options.Authenticator = new HttpBasicAuthenticator(
                _configManager.CurrentSettings.Connection.Authentication.Username,
                _configManager.CurrentSettings.Connection.Authentication.Password);
        }

        _client = new RestClient(options);
    }

    public async Task<bool> TestConnectionAsync()
    {
        if (_client == null)
        {
            return false;
        }

        try
        {
            var request = new RestRequest("/-/healthy");
            var response = await _client.ExecuteGetAsync(request);
            return response.IsSuccessful;
        }
        catch
        {
            return false;
        }
    }

    public async Task<MetricResult?> QueryMetricAsync(MetricSettings metricSettings)
    {
        if (_client == null)
        {
            return null;
        }

        try
        {
            var request = new RestRequest("/api/v1/query");
            
            // Use the PromQL query if provided, otherwise construct a simple query from metric name and labels
            string query = !string.IsNullOrEmpty(metricSettings.PromQlQuery) 
                ? metricSettings.PromQlQuery 
                : ConstructQueryFromMetricAndLabels(metricSettings);
                
            request.AddQueryParameter("query", query);

            var response = await _client.ExecuteGetAsync(request);
            
            if (!response.IsSuccessful)
            {
                return null;
            }

            var prometheusResponse = JsonConvert.DeserializeObject<PrometheusQueryResponse>(response.Content!);
            
            if (prometheusResponse?.Status != "success" || prometheusResponse.Data?.Result == null || prometheusResponse.Data.Result.Count == 0)
            {
                return null;
            }

            // Get the first result
            var result = prometheusResponse.Data.Result[0];
            
            if (result.Value == null || result.Value.Count < 2)
            {
                return null;
            }

            // The value is in the second element of the value array
            if (double.TryParse(result.Value[1].ToString(), out double value))
            {
                return new MetricResult
                {
                    MetricName = metricSettings.MetricName,
                    DisplayName = metricSettings.DisplayName,
                    Value = value,
                    Unit = metricSettings.Unit,
                    Timestamp = DateTimeOffset.Now
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error querying metric: {ex.Message}");
            return null;
        }
    }

    private string ConstructQueryFromMetricAndLabels(MetricSettings metricSettings)
    {
        var query = metricSettings.MetricName;
        
        if (metricSettings.Labels.Count > 0)
        {
            var labelStrings = new List<string>();
            
            foreach (var label in metricSettings.Labels)
            {
                labelStrings.Add($"{label.Key}=\"{label.Value}\"");
            }
            
            query += "{" + string.Join(",", labelStrings) + "}";
        }
        
        return query;
    }
}

public class MetricResult
{
    public string MetricName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }

    public string GetFormattedValue(DisplaySettings displaySettings)
    {
        var valueString = Math.Round(Value, displaySettings.DecimalPlaces).ToString($"F{displaySettings.DecimalPlaces}");
        var unitString = displaySettings.ShowUnit && !string.IsNullOrEmpty(Unit) ? Unit : string.Empty;
        
        return displaySettings.Format switch
        {
            DisplayFormat.ValueOnly => $"{valueString}{unitString}",
            DisplayFormat.NameAndValue => $"{DisplayName}: {valueString}{unitString}",
            DisplayFormat.Custom => FormatCustom(displaySettings.CustomFormat, valueString, unitString),
            _ => $"{valueString}{unitString}"
        };
    }

    private string FormatCustom(string format, string value, string unit)
    {
        return format
            .Replace("{name}", DisplayName)
            .Replace("{value}", value)
            .Replace("{unit}", unit);
    }
}

// Classes to deserialize Prometheus API responses
public class PrometheusQueryResponse
{
    public string Status { get; set; } = string.Empty;
    public PrometheusData? Data { get; set; }
}

public class PrometheusData
{
    public string ResultType { get; set; } = string.Empty;
    public List<PrometheusResult> Result { get; set; } = new();
}

public class PrometheusResult
{
    public Dictionary<string, string> Metric { get; set; } = new();
    public List<object> Value { get; set; } = new();
}
