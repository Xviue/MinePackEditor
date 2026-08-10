using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MinePackEditor.Converters
{
    /// <summary>
    /// null / 空字符串 / 空集合 时返回 false，否则返回 true。
    /// 支持 ConverterParameter=Invert 来反转结果。
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool visible = value switch
            {
                null => false,
                string s => !string.IsNullOrWhiteSpace(s),
                System.Collections.ICollection col => col.Count > 0,
                _ => true
            };

            // 关键修复：支持 Invert 参数
            bool invert = parameter?.ToString() == "Invert";
            return invert ? !visible : visible;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
