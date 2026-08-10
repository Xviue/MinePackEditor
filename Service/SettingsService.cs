using MinePackEditor.Localization;
using MinePackEditor.Models.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinePackEditor.Service
{
    public class SettingsService
    {
        private static readonly Lazy<SettingsService> _lazy = new(() => new SettingsService());
        public static SettingsService Instance => _lazy.Value;

        private readonly string _settingsPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public AppSettings Settings { get; private set; } = new();

        private SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "MinePackEditor");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }

        public void Load()
        {
            try
            {
                AppSettings loaded;
                if (!File.Exists(_settingsPath))
                {
                    loaded = new AppSettings();
                }
                else
                {
                    var json = File.ReadAllText(_settingsPath);
                    loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
                }

                // 卸载旧实例的事件，防止重复订阅
                Settings.PropertyChanged -= OnSettingsPropertyChanged;
                Settings = loaded;
                Settings.PropertyChanged += OnSettingsPropertyChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] 加载失败: {ex.Message}");
                Settings = new AppSettings();
                Settings.PropertyChanged += OnSettingsPropertyChanged;
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Settings, _jsonOptions);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] 保存失败: {ex.Message}");
            }
        }

        public List<ConfigDefinition> ExportWithMetadata()
        {
            return Settings.GetAllDefinitions();
        }

        /// <summary>
        /// 监听所有配置变更，实现自动保存 + 语言联动
        /// </summary>
        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 语言联动：当 Language 配置项改变时，同步更新全局 Culture
            if (e.PropertyName == nameof(AppSettings.Language))
            {
                var newLang = Settings.Language;
                var currentLang = LanguageManager.Instance.CurrentCulture.Name;

                // 只有当实际发生变化时才通知 LanguageManager，避免循环
                if (!string.Equals(currentLang, newLang, StringComparison.OrdinalIgnoreCase))
                {
                    LanguageManager.Instance.ChangeCulture(newLang);
                }
            }

            // 自动持久化（生产环境建议加入防抖，如 500ms 延迟保存）
            Save();
        }
    }
}
