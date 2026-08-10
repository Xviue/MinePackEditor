using MinePackEditor.Models.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MinePackEditor.Designs
{
    /// <summary>
    /// 设计时示例数据工厂。仅在 XAML 预览器中使用，不进入运行时。
    /// </summary>
    public static class DesignTimeSettingItem
    {
        public static SettingItem Create(SettingEditorType editorType, string displayName, object? value)
        {
            return new SettingItem
            {
                Id = $"preview.{editorType}",
                DisplayNameKey = displayName,
                DescriptionKey = $"Preview description for {editorType}",
                EditorType = editorType,
                CurrentValue = value,
                DefaultValue = value,
                ValueType = value?.GetType() ?? typeof(object),
                Constraint = new SettingConstraint
                {
                    Min = 0,
                    Max = 100,
                    Step = 1,
                    Options = new ObservableCollection<SettingOption>
                {
                    new() { LabelKey = "Lang.Chinese", Value = "zh-hans" },
                    new() { LabelKey = "Lang.English", Value = "en-us" }
                }
                }
            };
        }

        public static SettingItem ToggleExample => Create(SettingEditorType.ToggleSwitch, "自动保存", true);
        public static SettingItem SliderExample => Create(SettingEditorType.Slider, "透明度", 75.0);
        public static SettingItem NumericExample => Create(SettingEditorType.NumericSpinner, "最大宽度", 1024);
        public static SettingItem ComboExample => Create(SettingEditorType.ComboBox, "语言", "zh-hans");
        public static SettingItem TextExample => Create(SettingEditorType.TextBox, "用户名", "Admin");
    }
}
