using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MinePackEditor.Service
{
    public interface IFilePickerService
    {
        // ── 文件夹 ──
        Task<string?> PickFolderAsync(string title = "选择文件夹");
        Task<IReadOnlyList<string>> PickFoldersAsync(string title = "选择文件夹");

        // ── 单文件 ──
        /// <summary>选择单个指定后缀的文件。extensions 示例：".txt" 或 "png"</summary>
        Task<string?> PickFileAsync(string title = "选择文件", params string[] extensions);

        // ── 多文件 ──
        /// <summary>选择多个任意文件</summary>
        Task<IReadOnlyList<string>> PickFilesAsync(string title = "选择文件");

        /// <summary>选择多个指定后缀的文件。extensions 示例：".aaa", ".bbb", ".ccc"</summary>
        Task<IReadOnlyList<string>> PickFilesAsync(string title = "选择文件", params string[] extensions);
    }
}
