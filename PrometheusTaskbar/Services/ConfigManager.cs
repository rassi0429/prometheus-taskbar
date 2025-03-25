using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PrometheusTaskbar.Models;

namespace PrometheusTaskbar.Services;

public class ConfigManager
{
    private readonly string _configFilePath;
    private AppSettings _currentSettings;

    public event EventHandler? SettingsChanged;

    public ConfigManager(string configFilePath = "")
    {
        if (string.IsNullOrEmpty(configFilePath))
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "PrometheusTaskbar");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            _configFilePath = Path.Combine(appFolder, "settings.json");
        }
        else
        {
            _configFilePath = configFilePath;
        }

        _currentSettings = new AppSettings();
    }

    public AppSettings CurrentSettings => _currentSettings;

    public async Task LoadSettingsAsync()
    {
        if (!File.Exists(_configFilePath))
        {
            _currentSettings = new AppSettings();
            await SaveSettingsAsync();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath);
            var settings = JsonConvert.DeserializeObject<AppSettings>(json);
            
            if (settings != null)
            {
                _currentSettings = settings;
            }
            else
            {
                _currentSettings = new AppSettings();
                await SaveSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Error loading settings: {ex.Message}");
            _currentSettings = new AppSettings();
            await SaveSettingsAsync();
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_currentSettings, Formatting.Indented);
            await File.WriteAllTextAsync(_configFilePath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    public async Task UpdateSettingsAsync(AppSettings newSettings)
    {
        _currentSettings = newSettings;
        await SaveSettingsAsync();
    }
}
