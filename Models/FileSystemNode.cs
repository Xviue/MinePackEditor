using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MinePackEditor.Models
{
    /// <summary>
    /// 文件系统树节点，用于 TreeView 展示。支持懒加载子目录。
    /// Author:Xviue -Kimi修改，暂未再次确认
    /// </summary>
    public sealed class FileSystemNode : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;
        private bool _isLoading;

        /// <summary>节点显示名称</summary>
        public string Name { get; }

        /// <summary>完整绝对路径</summary>
        public string FullPath { get; }

        /// <summary>是否为目录</summary>
        public bool IsDirectory { get; }

        /// <summary>父节点（可选，用于后续面包屑导航）</summary>
        public FileSystemNode? Parent { get; }

        /// <summary>子节点集合</summary>
        public ObservableCollection<FileSystemNode> Children { get; } = new();

        /// <summary>是否展开</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                OnPropertyChanged();

                if (_isExpanded && IsDirectory && !_isLoading)
                {
                    _ = LoadChildrenAsync();
                }
            }
        }

        /// <summary>是否选中（绑定 TreeView 选中状态）</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        /// <summary>图标键（预留，后续可接入 IconProvider）</summary>
        public string IconKey => IsDirectory ? "Folder" : "File";

        /// <summary>主构造：创建真实文件/目录节点</summary>
        public FileSystemNode(string path, bool isDirectory, FileSystemNode? parent = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("路径不能为空", nameof(path));

            FullPath = Path.GetFullPath(path);
            Name = Path.GetFileName(FullPath) is { Length: > 0 } fileName
                ? fileName
                : FullPath; // 盘符根目录时 GetFileName 为空
            IsDirectory = isDirectory;
            Parent = parent;

            if (IsDirectory)
            {
                // 插入占位节点，确保 TreeView 显示展开箭头
                Children.Add(new FileSystemNode(isPlaceholder: true));
            }
        }

        /// <summary>私有占位构造：仅用于显示"加载中"占位符</summary>
        private FileSystemNode(bool isPlaceholder)
        {
            if (!isPlaceholder) throw new InvalidOperationException("此构造仅用于内部占位");

            FullPath = string.Empty;
            Name = "Loading...";
            IsDirectory = false;
            Parent = null;
        }

        /// <summary>
        /// 异步加载子目录和文件。线程安全：后台读取，UI 线程更新集合。
        /// </summary>
        public async Task LoadChildrenAsync()
        {
            if (!IsDirectory || _isLoading) return;

            // 已加载过（非占位）则跳过
            if (Children.Count > 0 && Children[0].FullPath != string.Empty)
                return;

            _isLoading = true;
            OnPropertyChanged(nameof(IconKey));

            try
            {
                var entries = await Task.Run(() =>
                {
                    var list = new List<(string Path, bool IsDir)>();
                    try
                    {
                        if (Directory.Exists(FullPath))
                        {
                            list.AddRange(Directory
                                .EnumerateDirectories(FullPath)
                                .Select(d => (d, true)));
                            list.AddRange(Directory
                                .EnumerateFiles(FullPath)
                                .Select(f => (f, false)));
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 无权限：静默忽略，保持空目录
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // 目录被删除：静默忽略
                    }
                    return list;
                });

                // 必须回到 UI 线程操作 ObservableCollection
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Children.Clear();
                    foreach (var (entryPath, isDir) in entries
                        .OrderByDescending(e => e.IsDir)
                        .ThenBy(e => e.Path, StringComparer.OrdinalIgnoreCase))
                    {
                        Children.Add(new FileSystemNode(entryPath, isDir, parent: this));
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileSystemNode] 加载失败 [{FullPath}]: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                OnPropertyChanged(nameof(IconKey));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
