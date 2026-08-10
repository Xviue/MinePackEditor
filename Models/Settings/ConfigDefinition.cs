using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    /// <summary>
    /// 配置项的元数据定义，用于 UI 自动生成设置页面
    /// </summary>
    public class ConfigDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string CategoryPath { get; set; } = string.Empty;
        public string DisplayNameKey { get; set; } = string.Empty;
        public string DescriptionKey { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public Type ValueType { get; set; } = typeof(object);
        public object? CurrentValue { get; set; }
        public object? DefaultValue { get; set; }
        public SettingEditorType EditorType { get; set; }
        public string? OptionsKey { get; set; }
        public SettingConstraint Constraint { get; set; } = new();
    }
}
