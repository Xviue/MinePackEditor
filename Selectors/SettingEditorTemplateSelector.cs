using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MinePackEditor.Models.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Selectors
{
    /// <summary>
    /// 根据 SettingItem.EditorType 动态选择对应的 DataTemplate。
    /// 所有模板在 XAML 中定义并传入此 Selector。
    /// </summary>
    public class SettingEditorTemplateSelector : IDataTemplate
    {
        public IDataTemplate? ToggleSwitchTemplate { get; set; }
        public IDataTemplate? SliderTemplate { get; set; }
        public IDataTemplate? NumericSpinnerTemplate { get; set; }
        public IDataTemplate? ComboBoxTemplate { get; set; }
        public IDataTemplate? TextBoxTemplate { get; set; }
        public IDataTemplate? FallbackTemplate { get; set; }
        public IDataTemplate? ListEditorTemplate { get; set; }

        public Control? Build(object? param)
        {
            var template = SelectTemplate(param);
            return template?.Build(param);
        }

        public bool Match(object? data)
        {
            return data is SettingItem;
        }

        private IDataTemplate? SelectTemplate(object? data)
        {
            if (data is not SettingItem item) return FallbackTemplate;

            return item.EditorType switch
            {
                SettingEditorType.ToggleSwitch => ToggleSwitchTemplate,
                SettingEditorType.Slider => SliderTemplate,
                SettingEditorType.NumericSpinner => NumericSpinnerTemplate,
                SettingEditorType.ComboBox => ComboBoxTemplate,
                SettingEditorType.TextBox => TextBoxTemplate,
                SettingEditorType.ListEditor => ListEditorTemplate,
                _ => FallbackTemplate
            };
        }
    }
}
