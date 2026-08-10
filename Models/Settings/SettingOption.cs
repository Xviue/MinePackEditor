using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    /// <summary>
    /// ComboBox 等控件的选项定义
    /// </summary>
    public class SettingOption
    {
        /// <summary>显示文本的国际化键，如 "Lang.Chinese"</summary>
        public string LabelKey { get; set; } = string.Empty;

        /// <summary>实际存储值</summary>
        public object Value { get; set; } = null!;
    }
}
