using Avalonia;
using System;
using System.Collections.Generic;
using System.Text;



namespace MinePackEditor.Models.Settings
{
    /// <summary>
    /// 标记一个属性为配置项，附加完整的 UI 与持久化元数据
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigItemAttribute : Attribute
    {
        /// <summary>全局唯一标识，如 "image.maxDecodeWidth"</summary>
        public string Id { get; }

        /// <summary>
        /// 分类路径，支持层级，如 "General" 或 "Viewer.ImageViewer"。
        /// 用点号分隔表示层级关系。
        /// </summary>
        public string CategoryPath { get; }

        /// <summary>显示名称的国际化键，如 "Config.Image.MaxDecodeWidth.Name"</summary>
        public string? DisplayNameKey { get; set; }

        /// <summary>详细说明的国际化键，如 "Config.Image.MaxDecodeWidth.Desc"</summary>
        public string? DescriptionKey { get; set; }

        /// <summary>默认值</summary>
        public object? DefaultValue { get; }

        /// <summary>UI 编辑器类型</summary>
        public SettingEditorType EditorType { get; set; } = SettingEditorType.Auto;

        /// <summary>最小值（用于 Slider / NumericSpinner）</summary>
        public object? Min { get; set; }

        /// <summary>最大值（用于 Slider / NumericSpinner）</summary>
        public object? Max { get; set; }

        /// <summary>步长（用于 Slider / NumericSpinner）</summary>
        public object? Step { get; set; }

        /// <summary>
        /// 列表编辑器中，显示字符串的来源属性
        /// </summary>
        public string? DisplayMemberPath { get; set; }
        /// <summary>
        /// 列表编辑器中，是否允许增删改操作（false = 只读列表）
        /// </summary>
        public bool AllowListEdit { get; set; } = true;

        /// <summary>
        /// 选项列表注册键，用于 ComboBox。
        /// 实际选项在 <see cref="SettingsMenuBuilder.RegisterOptions"/> 中提供。
        /// </summary>
        public string? OptionsKey { get; set; }

        public ConfigItemAttribute(string id, string categoryPath, object? defaultValue = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            CategoryPath = categoryPath ?? throw new ArgumentNullException(nameof(categoryPath));
            DefaultValue = defaultValue;

            // 自动生成国际化键，允许外部覆盖
            DisplayNameKey = $"Config:{id}:Name";
            DescriptionKey = $"Config:{id}:Desc";
        }
    }

}
