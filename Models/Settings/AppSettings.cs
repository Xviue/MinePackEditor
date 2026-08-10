using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    public partial class AppSettings : INotifyPropertyChanged
    {
        // ==================== 静态反射缓存（只构建一次） ====================
        private static readonly IReadOnlyList<(PropertyInfo Prop, ConfigItemAttribute Attr)> _configMeta;
        private static readonly Dictionary<string, (PropertyInfo Prop, ConfigItemAttribute Attr)> _idToMeta;

        static AppSettings()
        {
            var type = typeof(AppSettings);
            var metas = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => (Prop: p, Attr: p.GetCustomAttribute<ConfigItemAttribute>()))
                .Where(x => x.Attr != null)
                .Select(x => (x.Prop, Attr: x.Attr!))
                .ToList();

            _configMeta = metas;
            _idToMeta = metas.ToDictionary(
                x => x.Attr.Id,
                x => (x.Prop, x.Attr),
                StringComparer.Ordinal);
        }

        /// <summary>供 SettingsMenuBuilder 使用，避免重复反射</summary>
        public static IReadOnlyList<(PropertyInfo Prop, ConfigItemAttribute Attr)> GetConfigMetas() => _configMeta;

        // ==================== Viewer ====================
        private int _maxDecodeWidth = 1024;
        [ConfigItem("image.maxDecodeWidth", "Viewer.ImageViewer", 1024,
            EditorType = SettingEditorType.NumericSpinner, Min = 128, Max = 8192, Step = 64)]
        public int MaxDecodeWidth
        {
            get => _maxDecodeWidth;
            set { _maxDecodeWidth = value; OnPropertyChanged(); }
        }

        private double _maxZoomScale = 20.0;
        [ConfigItem("image.maxZoomScale", "Viewer.ImageViewer", 20.0,
            EditorType = SettingEditorType.Slider, Min = 1.0, Max = 50.0, Step = 0.5)]
        public double MaxZoomScale
        {
            get => _maxZoomScale;
            set { _maxZoomScale = value; OnPropertyChanged(); }
        }

        private double _minZoomScale = 0.1;
        [ConfigItem("image.minZoomScale", "Viewer.ImageViewer", 0.1,
            EditorType = SettingEditorType.Slider, Min = 0.01, Max = 1.0, Step = 0.01)]
        public double MinZoomScale
        {
            get => _minZoomScale;
            set { _minZoomScale = value; OnPropertyChanged(); }
        }

        private double _zoomStep = 1.2;
        [ConfigItem("image.zoomStep", "Viewer.ImageViewer", 1.2,
            EditorType = SettingEditorType.NumericSpinner, Min = 1.1, Max = 3.0, Step = 0.1)]
        public double ZoomStep
        {
            get => _zoomStep;
            set { _zoomStep = value; OnPropertyChanged(); }
        }

        // ==================== General ====================
        private string _language = "zh-hans";
        [ConfigItem("general.language", "General", "zh-hans",
            EditorType = SettingEditorType.ComboBox, OptionsKey = "AvailableLanguages")]
        public string Language
        {
            get => _language;
            set
            {
                if (string.Equals(_language, value, StringComparison.OrdinalIgnoreCase)) return;
                _language = value;
                OnPropertyChanged();
            }
        }

        private bool _autoSaveOnSwitch = false;
        [ConfigItem("general.autoSaveOnSwitch", "General", false,
            EditorType = SettingEditorType.ToggleSwitch)]
        public bool AutoSaveOnSwitch
        {
            get => _autoSaveOnSwitch;
            set { _autoSaveOnSwitch = value; OnPropertyChanged(); }
        }

        private List<FileAssociation> _fileAssociations = new();
        [ConfigItem("editor.fileAssociations", "Editor", null,
            EditorType = SettingEditorType.ListEditor)]
        public List<FileAssociation> FileAssociations
        {
            get => _fileAssociations;
            set { _fileAssociations = value ?? new(); OnPropertyChanged(); }
        }

        // ==================== Session ====================
        private List<string> _lastOpenDirectories = new();
        [ConfigItem("session.lastOpenDirectories", "Session", null,
            EditorType = SettingEditorType.ListEditor)]
        public List<string> LastOpenDirectories
        {
            get => _lastOpenDirectories;
            set { _lastOpenDirectories = value ?? new(); OnPropertyChanged(); }
        }

        private List<string> _lastOpenFiles = new();
        [ConfigItem("session.lastOpenFiles", "Session", null,
            EditorType = SettingEditorType.ListEditor)]
        public List<string> LastOpenFiles
        {
            get => _lastOpenFiles;
            set { _lastOpenFiles = value ?? new(); OnPropertyChanged(); }
        }

        // ==================== 元数据反射 API（基于缓存） ====================
        public List<ConfigDefinition> GetAllDefinitions()
        {
            var definitions = new List<ConfigDefinition>(_configMeta.Count);
            foreach (var (prop, attr) in _configMeta)
            {
                definitions.Add(new ConfigDefinition
                {
                    Id = attr.Id,
                    CategoryPath = attr.CategoryPath,
                    DisplayNameKey = attr.DisplayNameKey ?? $"Config:{attr.Id}:Name",
                    DescriptionKey = attr.DescriptionKey ?? $"Config:{attr.Id}:Desc",
                    PropertyName = prop.Name,
                    ValueType = prop.PropertyType,
                    CurrentValue = prop.GetValue(this),
                    DefaultValue = attr.DefaultValue,
                    EditorType = attr.EditorType,
                    OptionsKey = attr.OptionsKey,
                    Constraint = new SettingConstraint
                    {
                        Min = attr.Min,
                        Max = attr.Max,
                        Step = attr.Step
                    }
                });
            }
            return definitions;
        }

        public List<ConfigDefinition> GetDefinitionsByCategory(string category)
        {
            return GetAllDefinitions()
                .Where(d => d.CategoryPath.Equals(category, StringComparison.OrdinalIgnoreCase)
                         || d.CategoryPath.StartsWith(category + ".", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public object? GetValueById(string id)
        {
            if (!_idToMeta.TryGetValue(id, out var meta)) return null;
            return meta.Prop.GetValue(this);
        }

        public void SetValueById(string id, object? value)
        {
            if (!_idToMeta.TryGetValue(id, out var meta)) return;

            var prop = meta.Prop;
            if (!prop.CanWrite) return;

            try
            {
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var converted = Convert.ChangeType(value, targetType);
                prop.SetValue(this, converted);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettings] 设置 {id} 失败: {ex.Message}");
            }
        }

        public void ResetToDefaults()
        {
            foreach (var (prop, attr) in _configMeta)
            {
                if (attr.DefaultValue != null)
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    var converted = Convert.ChangeType(attr.DefaultValue, targetType);
                    prop.SetValue(this, converted);
                }
                else
                {
                    // 对集合类型创建空实例
                    if (prop.PropertyType == typeof(List<string>))
                        prop.SetValue(this, new List<string>());
                    else if (prop.PropertyType == typeof(List<FileAssociation>))
                        prop.SetValue(this, new List<FileAssociation>());
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
