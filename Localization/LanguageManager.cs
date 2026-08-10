using MinePackEditor.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace MinePackEditor.Localization
{
    public sealed class LanguageManager : INotifyPropertyChanged
    {
        private static readonly Lazy<LanguageManager> _instance = new(() => new());
        public static LanguageManager Instance => _instance.Value;

        public static readonly string[] SupportedCultures = { "zh-hans", "en-us" };

        private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                if (Equals(_currentCulture, value)) return;

                _currentCulture = value;
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;

                // string.Empty 表示所有绑定属性都发生变化，触发 Avalonia 全局刷新
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }

        /// <summary>
        /// 切换语言。此方法会被 SettingsService 在检测到 Language 配置变化时调用，
        /// 也会内部回写 Settings.Language（但 AppSettings 的相等判断会阻断循环）。
        /// </summary>
        public void ChangeCulture(string cultureName)
        {
            if (!Array.Exists(SupportedCultures, x => x.Equals(cultureName, StringComparison.OrdinalIgnoreCase)))
                return;

            CurrentCulture = new CultureInfo(cultureName);

            // 回写配置（如果值相同，AppSettings setter 会直接 return，不会触发事件）
            SettingsService.Instance.Settings.Language = cultureName;
        }

        /// <summary>
        /// 应用启动时调用，恢复上次设置的语言
        /// </summary>
        public void Initialize()
        {
            var savedLang = SettingsService.Instance.Settings.Language;

            if (Array.Exists(SupportedCultures, x => x.Equals(savedLang, StringComparison.OrdinalIgnoreCase)))
            {
                CurrentCulture = new CultureInfo(savedLang);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
