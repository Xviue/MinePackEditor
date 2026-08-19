using Avalonia.Data.Converters;
using MinePackEditor.Localization.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MinePackEditor.Converters
{
    internal class DefaultValueStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return UILanguage.Get("DefaultValue") + value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
         => throw new NotSupportedException();
    }
}
