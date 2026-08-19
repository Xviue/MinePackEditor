using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;

namespace MinePackEditor.Converters;

public class ListDisplayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] == null)
            return "";

        var item = values[0];
        var memberPath = values[1] as string;

        if (string.IsNullOrEmpty(memberPath))
            return item.ToString() ?? "";

        var prop = item.GetType().GetProperty(memberPath, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item)?.ToString() ?? item.ToString() ?? "";
    }
}