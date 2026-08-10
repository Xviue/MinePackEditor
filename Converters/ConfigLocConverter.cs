using Avalonia.Data.Converters;
using MinePackEditor.Assets.Localization.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MinePackEditor.Converters
{
    public class ConfigLocConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string configId || string.IsNullOrWhiteSpace(configId))
                return null;

            return SettingsLang.Get(configId);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
