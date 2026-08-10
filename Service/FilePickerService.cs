using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinePackEditor.Service
{
    internal class FilePickerService : IFilePickerService
    {
        private readonly TopLevel _topLevel;

        public FilePickerService(TopLevel topLevel)
        {
            _topLevel = topLevel;
        }

        // ── 单个文件夹 ──
        public async Task<string?> PickFolderAsync(string title = "选择文件夹")
        {
            var provider = _topLevel.StorageProvider;
            if (!provider.CanPickFolder) return null;

            var options = new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            };

            var folders = await provider.OpenFolderPickerAsync(options);
            return folders?.Count > 0 ? folders[0].TryGetLocalPath() : null;
        }

        // ── 多个文件夹 ──
        public async Task<IReadOnlyList<string>> PickFoldersAsync(string title = "选择文件夹")
        {
            var provider = _topLevel.StorageProvider;
            if (!provider.CanPickFolder) return Array.Empty<string>();

            var options = new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = true
            };

            var folders = await provider.OpenFolderPickerAsync(options);
            return ToPathList(folders);
        }

        // ── 单个文件（指定后缀）──
        public async Task<string?> PickFileAsync(string title = "选择文件", params string[] extensions)
        {
            var provider = _topLevel.StorageProvider;
            if (!provider.CanOpen) return null;

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = BuildFileTypes(extensions)
            };

            var files = await provider.OpenFilePickerAsync(options);
            return files?.Count > 0 ? files[0].TryGetLocalPath() : null;
        }

        // ── 多个文件（任意）──
        public async Task<IReadOnlyList<string>> PickFilesAsync(string title = "选择文件")
        {
            var provider = _topLevel.StorageProvider;
            if (!provider.CanOpen) return Array.Empty<string>();

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>
            {
                new("所有文件") { Patterns = new[] { "*" } }
            }
            };

            var files = await provider.OpenFilePickerAsync(options);
            return ToPathList(files);
        }

        // ── 多个文件（指定后缀）──
        public async Task<IReadOnlyList<string>> PickFilesAsync(string title = "选择文件", params string[] extensions)
        {
            var provider = _topLevel.StorageProvider;
            if (!provider.CanOpen) return Array.Empty<string>();

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = true,
                FileTypeFilter = BuildFileTypes(extensions)
            };

            var files = await provider.OpenFilePickerAsync(options);
            return ToPathList(files);
        }

        // ── 私有辅助 ──

        private static IReadOnlyList<string> ToPathList(IReadOnlyList<IStorageItem>? items)
        {
            if (items == null || items.Count == 0) return Array.Empty<string>();
            return items
                .Select(i => i.TryGetLocalPath())
                .OfType<string>()
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();
        }

        /// <summary>
        /// 将扩展名转换为 Avalonia 的 FilePickerFileType。
        /// 支持 ".txt"、"txt"、或同时传入多个 ".aaa", ".bbb", ".ccc"。
        /// </summary>
        private static List<FilePickerFileType>? BuildFileTypes(string[] extensions)
        {
            if (extensions == null || extensions.Length == 0) return null;

            var patterns = extensions
                .Select(ext => ext.StartsWith('.') ? $"*{ext}" : ext)   // txt -> *.txt
                .Select(ext => ext.StartsWith('*') ? ext : $"*.{ext}")  // 兜底加 *
                .ToArray();

            return new List<FilePickerFileType>
        {
            new($"指定文件 ({string.Join(", ", patterns)})") { Patterns = patterns },
            new("所有文件") { Patterns = new[] { "*" } }
        };
        }
    }
}
