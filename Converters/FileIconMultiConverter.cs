using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MinePackEditor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MinePackEditor.Converters
{
    public class FileIconMultiConverter : IMultiValueConverter
    {
        private static readonly Dictionary<string, Bitmap> _iconCache = new();
        private static readonly string _assemblyName = typeof(FileIconMultiConverter).Assembly.GetName().Name!;

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2) return null;
            if (values[0] is not FileSystemNode node) return null;
            _ = values[1] is bool expanded;

            if (node.IsDirectory)
            {
                return node.IsExpanded
                    ? GetCachedImage("folder_open.png")
                    : GetCachedImage("folder_close.png");
            }

            string ext = Path.GetExtension(node.Name)?.ToLowerInvariant() ?? "";
            return ext switch
            {
                ".mfc" or ".mproj" => GetCachedImage("code_file.png"),
                ".mcfunction" or ".mcf" => GetCachedImage("function_file.png"),
                ".png" or ".jpg" or ".jpeg" or ".gif" => GetCachedImage("image_file.png"),
                _ => GetCachedImage("file.png")
            };
        }

        private Bitmap GetCachedImage(string fileName)
        {
            string uri = $"avares://{_assemblyName}/Assets/FileIcons/{fileName}";
            if (!_iconCache.TryGetValue(uri, out var bitmap))
            {
                bitmap = new Bitmap(AssetLoader.Open(new Uri(uri)));
                _iconCache[uri] = bitmap;
            }
            return bitmap;
        }
    }
}
