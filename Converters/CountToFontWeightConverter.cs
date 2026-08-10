using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MinePackEditor.Converters
{
    /// <summary>
    /// 数值 > 0 时返回 Bold，否则返回 Normal。
    /// 用于 TreeView 分组标题的字体粗细。
    /// </summary>
    public class CountToFontWeightConverter : IValueConverter
    {
        public static readonly CountToFontWeightConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var count = value is int i ? i : 0;
            return count > 0 ? FontWeight.Bold : FontWeight.Normal;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
