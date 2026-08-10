using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    /// <summary>
    /// 配置项在 UI 中的编辑器类型
    /// </summary>
    public enum SettingEditorType
    {
        Auto,           // 根据 ValueType 自动推断
        TextBox,        // 单行文本
        MultiLineText,  // 多行文本
        NumericSpinner, // 数字增减框
        Slider,         // 滑块（需配合 Min/Max/Step）
        ToggleSwitch,   // 布尔开关
        ComboBox,       // 下拉选项
        FilePicker,     // 文件选择
        DirectoryPicker,// 目录选择
        ColorPicker,    // 颜色选择
        ListEditor,     // 列表编辑（如 FileAssociation）
        PasswordBox,    // 密码输入
    }
}
