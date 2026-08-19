using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MinePackEditor.Converters;

public class TypeNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "null";
        return $"Type: {value.GetType().Name}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}