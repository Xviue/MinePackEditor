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
}
}
