using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using PrometheusTaskbar.Models;

namespace PrometheusTaskbar.Services;

public class TaskbarManager
{
    private readonly ConfigManager _configManager;
    private TaskbarIcon? _taskbarIcon;
    private MetricResult? _currentMetric;
    private List<MetricResult> _allMetrics = new List<MetricResult>();

    public event EventHandler? SettingsRequested;
    public event EventHandler? RefreshRequested;
    public event EventHandler? ExitRequested;

    public TaskbarManager(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    public void Initialize()
    {
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Prometheus Taskbar"
        };
        
        // Try to get the app icon from resources
        if (Application.Current.TryFindResource("AppIcon") is ImageSource iconSource)
        {
            _taskbarIcon.IconSource = iconSource;
        }
        else
        {
            // Use a default icon if the app icon is not found
            _taskbarIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        // Create context menu
        var contextMenu = new ContextMenu();

        var refreshMenuItem = new MenuItem { Header = "更新" };
        refreshMenuItem.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(refreshMenuItem);

        var settingsMenuItem = new MenuItem { Header = "設定を開く" };
        settingsMenuItem.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(settingsMenuItem);

        contextMenu.Items.Add(new Separator());

        var exitMenuItem = new MenuItem { Header = "終了" };
        exitMenuItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        contextMenu.Items.Add(exitMenuItem);

        _taskbarIcon.ContextMenu = contextMenu;

        // Double-click opens settings
        _taskbarIcon.TrayMouseDoubleClick += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateMetric(MetricResult metricResult)
    {
        _currentMetric = metricResult;
        
        // Add or update the metric in the all metrics list
        var existingIndex = _allMetrics.FindIndex(m => m.MetricName == metricResult.MetricName);
        if (existingIndex >= 0)
        {
            _allMetrics[existingIndex] = metricResult;
        }
        else
        {
            _allMetrics.Add(metricResult);
        }
        
        UpdateDisplay();
    }
    
    public void UpdateMetrics(List<MetricResult> metricResults)
    {
        if (metricResults.Count > 0)
        {
            _currentMetric = metricResults[0];
        }
        
        _allMetrics = metricResults;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (_taskbarIcon == null)
        {
            return;
        }

        var displaySettings = _configManager.CurrentSettings.Display;
        
        // If no metrics are available, use the default app icon
        if (_currentMetric == null)
        {
            if (Application.Current.TryFindResource("AppIcon") is ImageSource iconSource)
            {
                _taskbarIcon.IconSource = iconSource;
            }
            else
            {
                _taskbarIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            
            _taskbarIcon.ToolTipText = "Prometheus Taskbar";
            return;
        }

        var formattedValue = _currentMetric.GetFormattedValue(displaySettings);

        // Check if alert is enabled and threshold is exceeded
        bool isAlertTriggered = displaySettings.Alert.Enabled && _currentMetric.Value > displaySettings.Alert.Threshold;
        
        // If alert is not triggered, use the default app icon instead of text
        if (!isAlertTriggered)
        {
            if (Application.Current.TryFindResource("AppIcon") is ImageSource iconSource)
            {
                _taskbarIcon.IconSource = iconSource;
            }
            else
            {
                _taskbarIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            
            // Update tooltip with all metrics
            UpdateTooltip();
            return;
        }
        
        // Only create text-based icon when alert is triggered
        // Create a TextBlock for the taskbar text
        var textBlock = new TextBlock
        {
            Text = formattedValue,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            Foreground = Brushes.White,
            Background = Brushes.Transparent,
            Padding = new Thickness(2)
        };

        // Set the alert color
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(displaySettings.Alert.Color);
            textBlock.Foreground = new SolidColorBrush(color);
        }
        catch
        {
            // If color conversion fails, use default red
            textBlock.Foreground = Brushes.Red;
        }

        // Measure the text size
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = textBlock.DesiredSize;

        // Create a DrawingVisual to render the TextBlock
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            // Draw a background
            drawingContext.DrawRectangle(
                Brushes.Transparent,
                null,
                new Rect(0, 0, size.Width, size.Height));

            // Create a VisualBrush from the TextBlock
            var visualBrush = new VisualBrush(textBlock);
            drawingContext.DrawRectangle(
                visualBrush,
                null,
                new Rect(0, 0, size.Width, size.Height));
        }

        // Convert the DrawingVisual to a bitmap
        var renderTargetBitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(size.Width),
            (int)Math.Ceiling(size.Height),
            96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);

        // Set the bitmap as the taskbar icon
        var bitmap = renderTargetBitmap.ToBitmap();
        _taskbarIcon.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());

        // Update tooltip with all metrics
        UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        if (_taskbarIcon == null)
        {
            return;
        }
        
        var displaySettings = _configManager.CurrentSettings.Display;
        
        if (_allMetrics.Count == 0)
        {
            _taskbarIcon.ToolTipText = "Prometheus Taskbar";
            return;
        }
        
        if (_allMetrics.Count == 1)
        {
            _taskbarIcon.ToolTipText = _allMetrics[0].GetFormattedValue(displaySettings);
            return;
        }
        
        // Build tooltip with all metrics
        var tooltipText = string.Join("\n", _allMetrics.Select(m => m.GetFormattedValue(displaySettings)));
        _taskbarIcon.ToolTipText = tooltipText;
    }
    
    public void Dispose()
    {
        _taskbarIcon?.Dispose();
    }
}

// Extension method to convert RenderTargetBitmap to System.Drawing.Bitmap
public static class BitmapExtensions
{
    public static System.Drawing.Bitmap ToBitmap(this RenderTargetBitmap renderTargetBitmap)
    {
        var bitmapEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        bitmapEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderTargetBitmap));

        using var stream = new System.IO.MemoryStream();
        bitmapEncoder.Save(stream);
        stream.Seek(0, System.IO.SeekOrigin.Begin);

        return new System.Drawing.Bitmap(stream);
    }
}
