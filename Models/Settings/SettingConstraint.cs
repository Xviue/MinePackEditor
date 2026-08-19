using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    /// <summary>
    /// 配置项的验证/约束定义
    /// </summary>
    public class SettingConstraint
    {
        public object? Min { get; set; }
        public object? Max { get; set; }
        public object? Step { get; set; }
        public IReadOnlyList<SettingOption>? Options { get; set; }

        /// <summary>列表编辑器中，指定显示属性名（如 "Extension"、"FileName"）</summary>
        public string? DisplayMemberPath { get; set; }
        /// <summary>
        /// 列表编辑器中，是否允许增删改操作
        /// </summary>
        public bool AllowListEdit { get; set; } = true;
    }
}
