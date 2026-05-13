using System.Text.Json;

namespace ER_HKX2Navmesh.Common
{
    public partial class ToolSettings : IDisposable, IAsyncDisposable
    {
        public string? OutputPath { get; set; }
        public string? LastMapId { get; set; }

        private static Lazy<ToolSettings> _instance = new(() => Load());
        private readonly SemaphoreSlim _saveLock = new(1, 1);
        private bool _disposed = false;

        public ToolSettings() { }
        public static ToolSettings Instance => _instance.Value;


        private static ToolSettings Load()
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache"));

            if (!File.Exists(_settingsFile))
            {
                var defaultSettings = new ToolSettings();
                // Sync save for initialization - use synchronous file write to avoid blocking async operations
                SaveSettingsSync(defaultSettings);
                return defaultSettings;
            }

            try
            {
                var json = File.ReadAllText(_settingsFile);
                var settings = JsonSerializer.Deserialize<ToolSettings>(json, _serializationOptions);
                return settings ?? new();
            }
            catch
            {
                var newSettings = new ToolSettings();
                // Sync save for initialization - use synchronous file write to avoid blocking async operations
                SaveSettingsSync(newSettings);
                Console.WriteLine("Application Settings: Settings file was corrupted. Default settings have been restored.");
                return newSettings;
            }
        }

        /// <summary>
        /// Synchronously saves settings to disk. Only used during initialization.
        /// </summary>
        private static void SaveSettingsSync(ToolSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, _serializationOptions);
                File.WriteAllText(_settingsFile, json);
            }
            catch (IOException)
            {
                // Silently fail - ToastService may not be available yet
            }
            catch (UnauthorizedAccessException)
            {
                // Silently fail - ToastService may not be available yet
            }
        }


        public async Task SaveAsync()
        {
            await _saveLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(this, _serializationOptions);
                await File.WriteAllTextAsync(_settingsFile, json);
            }
            catch (IOException)
            {
                Console.WriteLine("Application Settings: Failed to save settings due to an I/O error.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Application Settings: Failed to save settings due to unauthorized access.");
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _saveLock?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            // Wait for any pending save operations before disposing the lock
            await _saveLock.WaitAsync();
            try
            {
                _saveLock?.Dispose();
            }
            finally
            {
                _saveLock?.Dispose();
            }
        }

        private static readonly string _settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache", "settings.json");
        // Basic options for serialization with indentation
        private static readonly JsonSerializerOptions _serializationOptions = new() { WriteIndented = true };
    }
}
