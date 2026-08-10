using Avalonia.Data.Converters;
using MinePackEditor.Models.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MinePackEditor.Converters
{
    /// <summary>
    /// 将 SettingEditorType 与参数比较，返回 bool。
    /// 用于根据编辑器类型控制控件的 IsVisible。
    /// </summary>
    public class EditorTypeToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not SettingEditorType actual) return false;
            if (parameter is not string paramStr) return false;
            if (!Enum.TryParse<SettingEditorType>(paramStr, out var expected)) return false;
            return actual == expected;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
