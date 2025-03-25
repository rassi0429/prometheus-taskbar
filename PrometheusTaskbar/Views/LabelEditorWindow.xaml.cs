using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PrometheusTaskbar.Views;

public partial class LabelEditorWindow : Window, INotifyPropertyChanged
{
    private string _labelKey = string.Empty;
    private string _labelValue = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string LabelKey
    {
        get => _labelKey;
        set
        {
            _labelKey = value;
            OnPropertyChanged();
        }
    }

    public string LabelValue
    {
        get => _labelValue;
        set
        {
            _labelValue = value;
            OnPropertyChanged();
        }
    }

    public LabelEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LabelKey))
        {
            MessageBox.Show("キーを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

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
