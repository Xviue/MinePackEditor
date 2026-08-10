using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MinePackEditor.Converters
{
    /// <summary>
    /// 集合 Count / 数值 > 0 时返回 true，否则返回 false。
    /// 用于控制 Button 的 IsEnabled 等。
    /// </summary>
    public class CountToBoolConverter : IValueConverter
    {
        public static readonly CountToBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var count = value switch
            {
                int i => i,
                ICollection col => col.Count,
                _ => 0
            };
            return count > 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
