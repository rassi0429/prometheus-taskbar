using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrometheusTaskbar.Models;
using PrometheusTaskbar.Services;

namespace PrometheusTaskbar.Views;

public partial class SettingsWindow : Window, INotifyPropertyChanged
{
    private readonly ConfigManager _configManager;
    private readonly PrometheusClient _prometheusClient;
    private readonly IServiceProvider _serviceProvider;
    
    private ConnectionSettings _connectionSettings = new();
    private DisplaySettings _displaySettings = new();
    private ObservableCollection<MetricSettings> _metrics = new();
    
    private bool _isNoAuth;
    private bool _isBasicAuth;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConnectionSettings ConnectionSettings
    {
        get => _connectionSettings;
        set
        {
            _connectionSettings = value;
            OnPropertyChanged();
        }
    }

    public DisplaySettings DisplaySettings
    {
        get => _displaySettings;
        set
        {
            _displaySettings = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<MetricSettings> Metrics
    {
        get => _metrics;
        set
        {
            _metrics = value;
            OnPropertyChanged();
        }
    }

    public bool IsNoAuth
    {
        get => _isNoAuth;
        set
        {
            if (_isNoAuth != value)
            {
                _isNoAuth = value;
                if (value)
                {
                    ConnectionSettings.Authentication.Type = AuthType.None;
                }
                OnPropertyChanged();
            }
        }
    }

    public bool IsBasicAuth
    {
        get => _isBasicAuth;
        set
        {
            if (_isBasicAuth != value)
            {
                _isBasicAuth = value;
                if (value)
                {
                    ConnectionSettings.Authentication.Type = AuthType.Basic;
                }
                OnPropertyChanged();
            }
        }
    }

    public SettingsWindow(ConfigManager configManager, PrometheusClient prometheusClient, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        
        _configManager = configManager;
        _prometheusClient = prometheusClient;
        _serviceProvider = serviceProvider;
        
        DataContext = this;
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Clone the settings to avoid modifying the original until save is clicked
        ConnectionSettings = new ConnectionSettings
        {
            EndpointUrl = _configManager.CurrentSettings.Connection.EndpointUrl,
            TimeoutSeconds = _configManager.CurrentSettings.Connection.TimeoutSeconds,
            Authentication = new AuthSettings
            {
                Type = _configManager.CurrentSettings.Connection.Authentication.Type,
                Username = _configManager.CurrentSettings.Connection.Authentication.Username,
                Password = _configManager.CurrentSettings.Connection.Authentication.Password
            }
        };

        DisplaySettings = new DisplaySettings
        {
            Format = _configManager.CurrentSettings.Display.Format,
            CustomFormat = _configManager.CurrentSettings.Display.CustomFormat,
            DecimalPlaces = _configManager.CurrentSettings.Display.DecimalPlaces,
            ShowUnit = _configManager.CurrentSettings.Display.ShowUnit,
            AutoStartWithWindows = _configManager.CurrentSettings.Display.AutoStartWithWindows,
            Alert = new AlertSettings
            {
                Enabled = _configManager.CurrentSettings.Display.Alert.Enabled,
                Threshold = _configManager.CurrentSettings.Display.Alert.Threshold,
                Color = _configManager.CurrentSettings.Display.Alert.Color
            }
        };

        Metrics = new ObservableCollection<MetricSettings>();
        foreach (var metric in _configManager.CurrentSettings.Metrics)
        {
            Metrics.Add(new MetricSettings
            {
                MetricName = metric.MetricName,
                PromQlQuery = metric.PromQlQuery,
                DisplayName = metric.DisplayName,
                Unit = metric.Unit,
                Labels = new System.Collections.Generic.Dictionary<string, string>(metric.Labels)
            });
        }

        // Set auth radio buttons
        IsNoAuth = ConnectionSettings.Authentication.Type == AuthType.None;
        IsBasicAuth = ConnectionSettings.Authentication.Type == AuthType.Basic;

        // Set password
        PasswordBox.Password = ConnectionSettings.Authentication.Password;
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Create a temporary client with the current settings
            var tempSettings = new AppSettings
            {
                Connection = new ConnectionSettings
                {
                    EndpointUrl = ConnectionSettings.EndpointUrl,
                    TimeoutSeconds = ConnectionSettings.TimeoutSeconds,
                    Authentication = new AuthSettings
                    {
                        Type = IsBasicAuth ? AuthType.Basic : AuthType.None,
                        Username = ConnectionSettings.Authentication.Username,
                        Password = PasswordBox.Password
                    }
                }
            };

            await _configManager.UpdateSettingsAsync(tempSettings);
            
            var result = await _prometheusClient.TestConnectionAsync();
            
            MessageBox.Show(
                result ? "接続に成功しました。" : "接続に失敗しました。設定を確認してください。",
                "接続テスト",
                MessageBoxButton.OK,
                result ? MessageBoxImage.Information : MessageBoxImage.Error);
            
            // Reload the original settings
            await _configManager.LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"エラーが発生しました: {ex.Message}",
                "接続テスト",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void AddMetric_Click(object sender, RoutedEventArgs e)
    {
        var metricEditor = _serviceProvider.GetRequiredService<MetricEditorWindow>();
        metricEditor.Initialize(null);
        
        if (metricEditor.ShowDialog() == true && metricEditor.MetricSettings != null)
        {
            Metrics.Add(metricEditor.MetricSettings);
        }
    }

    private void EditMetric_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is MetricSettings metricSettings)
        {
            var index = Metrics.IndexOf(metricSettings);
            if (index >= 0)
            {
                var metricEditor = _serviceProvider.GetRequiredService<MetricEditorWindow>();
                metricEditor.Initialize(metricSettings);
                
                if (metricEditor.ShowDialog() == true && metricEditor.MetricSettings != null)
                {
                    Metrics[index] = metricEditor.MetricSettings;
                }
            }
        }
    }

    private void DeleteMetric_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is MetricSettings metricSettings)
        {
            var result = MessageBox.Show(
                $"メトリクス '{metricSettings.DisplayName}' を削除してもよろしいですか？",
                "メトリクス削除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                Metrics.Remove(metricSettings);
            }
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update password from PasswordBox
            ConnectionSettings.Authentication.Password = PasswordBox.Password;
            
            // Create new settings object
            var newSettings = new AppSettings
            {
                Connection = ConnectionSettings,
                Display = DisplaySettings,
                Metrics = new System.Collections.Generic.List<MetricSettings>(Metrics)
            };
            
            // Save settings
            await _configManager.UpdateSettingsAsync(newSettings);
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"設定の保存中にエラーが発生しました: {ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
