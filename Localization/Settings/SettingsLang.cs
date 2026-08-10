using System;
using System.Globalization;
using System.Resources;
using System.Reflection;

namespace MinePackEditor.Assets.Localization.Settings;

public static class SettingsLang
{
    private static readonly ResourceManager _resourceManager = new(
        "MinePackEditor.Localization.Settings.Resources",
        Assembly.GetExecutingAssembly());

    public static CultureInfo Culture { get; set; } = CultureInfo.CurrentUICulture;

    /// <summary>
    /// 通用资源读取（兜底返回 [Key]）
    /// </summary>
    public static string Get(string? key)
    {
        if (key == null) return "null";
        var value = _resourceManager.GetString(key, Culture);
        return string.IsNullOrEmpty(value) ? $"[{key}]" : value;
    }
}