using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;

namespace MinePackEditor.Localization.UI
{
    public static class UILanguage
    {
        private static readonly ResourceManager _resourceManager = new(
            "MinePackEditor.Localization.UI.Resources",
            Assembly.GetExecutingAssembly());

        public static CultureInfo Culture { get; set; } = CultureInfo.CurrentUICulture;

        /// <summary>
        /// 通用资源读取（兜底返回 [Key]）
        /// </summary>
        public static string Get(string key)
        {
            var value = _resourceManager.GetString(key, Culture);
            return string.IsNullOrEmpty(value) ? $"[{key}]" : value;
        }
    }
}
