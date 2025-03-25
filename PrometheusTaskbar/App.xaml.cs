﻿﻿﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrometheusTaskbar.Models;
using PrometheusTaskbar.Services;
using PrometheusTaskbar.Views;

namespace PrometheusTaskbar;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cts = new();
    private Task? _backgroundTask;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // Register services
        services.AddSingleton<ConfigManager>();
        services.AddSingleton<PrometheusClient>();
        services.AddSingleton<TaskbarManager>();

        // Register views
        services.AddTransient<SettingsWindow>();
        services.AddTransient<MetricEditorWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        var configManager = _serviceProvider.GetRequiredService<ConfigManager>();
        await configManager.LoadSettingsAsync();

        var taskbarManager = _serviceProvider.GetRequiredService<TaskbarManager>();
        taskbarManager.Initialize();

        // Handle events
        taskbarManager.SettingsRequested += (_, _) => OpenSettingsWindow();
        taskbarManager.RefreshRequested += (_, _) => RefreshMetrics();
        taskbarManager.ExitRequested += (_, _) => Shutdown();

        // Create main window (hidden by default)
        var mainWindow = new MainWindow();
        Current.MainWindow = mainWindow;

        // Start background task
        _backgroundTask = StartBackgroundTask(_cts.Token);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Cancel background task
        _cts.Cancel();
        
        // Dispose services
        var taskbarManager = _serviceProvider.GetRequiredService<TaskbarManager>();
        taskbarManager.Dispose();
        
        _serviceProvider.Dispose();
        
        base.OnExit(e);
    }

    private void OpenSettingsWindow()
    {
        var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.ShowDialog();
    }

    private void RefreshMetrics()
    {
        // Force an immediate refresh
        _ = FetchAndUpdateMetricsAsync();
    }

    private async Task StartBackgroundTask(CancellationToken cancellationToken)
    {
        var configManager = _serviceProvider.GetRequiredService<ConfigManager>();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await FetchAndUpdateMetricsAsync();
            
            // Wait for the configured interval
            try
            {
                // Default to 60 seconds if not configured
                var updateIntervalSeconds = 60;
                
                // Try to get the update interval from the settings
                if (configManager.CurrentSettings.Metrics.Count > 0)
                {
                    // In a real implementation, this would be a separate setting
                    // For now, we'll just use a default value
                    updateIntervalSeconds = 60;
                }
                
                await Task.Delay(TimeSpan.FromSeconds(updateIntervalSeconds), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Task was canceled, exit the loop
                break;
            }
        }
    }

    private async Task FetchAndUpdateMetricsAsync()
    {
        var configManager = _serviceProvider.GetRequiredService<ConfigManager>();
        var prometheusClient = _serviceProvider.GetRequiredService<PrometheusClient>();
        var taskbarManager = _serviceProvider.GetRequiredService<TaskbarManager>();

        // Check if there are any configured metrics
        if (configManager.CurrentSettings.Metrics.Count == 0)
        {
            return;
        }

        // If there's only one metric, use the existing method
        if (configManager.CurrentSettings.Metrics.Count == 1)
        {
            var metricSettings = configManager.CurrentSettings.Metrics[0];
            var metricResult = await prometheusClient.QueryMetricAsync(metricSettings);

            if (metricResult != null)
            {
                taskbarManager.UpdateMetric(metricResult);
            }
            return;
        }

        // Fetch all metrics
        var metricResults = new List<MetricResult>();
        foreach (var metricSettings in configManager.CurrentSettings.Metrics)
        {
            var metricResult = await prometheusClient.QueryMetricAsync(metricSettings);
            if (metricResult != null)
            {
                metricResults.Add(metricResult);
            }
        }

        // Update the taskbar with all metrics
        if (metricResults.Count > 0)
        {
            taskbarManager.UpdateMetrics(metricResults);
        }
    }
}
