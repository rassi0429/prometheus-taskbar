using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using PrometheusTaskbar.Models;

namespace PrometheusTaskbar.Views;

public partial class MetricEditorWindow : Window, INotifyPropertyChanged
{
    private string _metricName = string.Empty;
    private string _promQlQuery = string.Empty;
    private string _displayName = string.Empty;
    private string _unit = string.Empty;
    private ObservableCollection<LabelItem> _labels = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string MetricName
    {
        get => _metricName;
        set
        {
            _metricName = value;
            OnPropertyChanged();
        }
    }

    public string PromQlQuery
    {
        get => _promQlQuery;
        set
        {
            _promQlQuery = value;
            OnPropertyChanged();
        }
    }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value;
            OnPropertyChanged();
        }
    }

    public string Unit
    {
        get => _unit;
        set
        {
            _unit = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<LabelItem> Labels
    {
        get => _labels;
        set
        {
            _labels = value;
            OnPropertyChanged();
        }
    }

    public MetricSettings? MetricSettings { get; private set; }

    public MetricEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void Initialize(MetricSettings? metricSettings)
    {
        if (metricSettings != null)
        {
            MetricName = metricSettings.MetricName;
            PromQlQuery = metricSettings.PromQlQuery;
            DisplayName = metricSettings.DisplayName;
            Unit = metricSettings.Unit;
            
            Labels = new ObservableCollection<LabelItem>();
            foreach (var label in metricSettings.Labels)
            {
                Labels.Add(new LabelItem { Key = label.Key, Value = label.Value });
            }
        }
        else
        {
            MetricName = string.Empty;
            PromQlQuery = string.Empty;
            DisplayName = string.Empty;
            Unit = string.Empty;
            Labels = new ObservableCollection<LabelItem>();
        }
    }

    private void AddLabel_Click(object sender, RoutedEventArgs e)
    {
        var labelWindow = new LabelEditorWindow();
        
        if (labelWindow.ShowDialog() == true)
        {
            Labels.Add(new LabelItem { Key = labelWindow.LabelKey, Value = labelWindow.LabelValue });
        }
    }

    private void DeleteLabel_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is LabelItem labelItem)
        {
            Labels.Remove(labelItem);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MetricName))
        {
            MessageBox.Show("メトリクス名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            MessageBox.Show("表示名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var metricSettings = new MetricSettings
        {
            MetricName = MetricName,
            PromQlQuery = PromQlQuery,
            DisplayName = DisplayName,
            Unit = Unit,
            Labels = new System.Collections.Generic.Dictionary<string, string>()
        };

        foreach (var label in Labels)
        {
            metricSettings.Labels[label.Key] = label.Value;
        }

        MetricSettings = metricSettings;
        DialogResult = true;
        Close();
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

public class LabelItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
