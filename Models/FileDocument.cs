using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MinePackEditor.Models;

/// <summary>
/// 未审查
/// </summary>
public partial class FileDocument : INotifyPropertyChanged
{
    private string _fileName = string.Empty;
    private string _content = string.Empty;
    private string _fullPath = string.Empty;
    private bool _isModified;

    private bool _isLoading;
    private string _loadingStatus = string.Empty;

    private double _loadingProgress;

    public double LoadingProgress
    {
        get => _loadingProgress;
        private set
        {
            if (_loadingProgress == value) return;
            _loadingProgress = value;
            OnPropertyChanged(nameof(LoadingProgress));
        }
    }

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    public string Content
    {
        get => _content;
        set
        {
            if (_content == value) return;
            _content = value;
            IsModified = true;
            OnPropertyChanged();
        }
    }

    public string FullPath
    {
        get => _fullPath;
        set { _fullPath = value; OnPropertyChanged(); }
    }

    public bool IsModified
    {
        get => _isModified;
        set { _isModified = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    public string DisplayName => IsModified ? $"{FileName} *" : FileName;

    /// <summary>是否正在异步加载内容</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); }
    }

    /// <summary>加载状态提示</summary>
    public string LoadingStatus
    {
        get => _loadingStatus;
        private set { _loadingStatus = value; OnPropertyChanged(); }
    }

    public ICommand? CloseCommand { get; set; }

    public FileDocument() { }

    public FileDocument(string fileName, string fullPath, string content = "")
    {
        _fileName = fileName;
        _fullPath = fullPath;
        _content = content;
        _isModified = false;
    }

    // ── 异步加载（核心）──
    /// <summary>
    /// 异步从磁盘加载内容。加载完成后 Content 已填充且 IsModified=false。
    /// 大文件使用流式分块读取，不阻塞 UI 线程。
    /// </summary>
    public async Task LoadContentAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(FullPath) || !File.Exists(FullPath))
            return;

        IsLoading = true;
        _loadingProgress = 0;

        try
        {
            var fileInfo = new FileInfo(FullPath);
            const long hardLimit = 100L * 1024 * 1024;

            if (fileInfo.Length > hardLimit)
                throw new InvalidOperationException(
                    $"文件过大 ({fileInfo.Length / 1024 / 1024}MB)，超过 100MB 安全限制");

            string newContent;
            var progress = new Progress<double>(p => _loadingProgress = p);

            if (fileInfo.Length > 2 * 1024 * 1024)
            {
                newContent = await ReadLargeFileAsync(FullPath, fileInfo.Length, progress, ct);
            }
            else
            {
                newContent = await File.ReadAllTextAsync(FullPath, ct);
                _loadingProgress = 100;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_content != newContent)
                {
                    _content = newContent;
                    OnPropertyChanged(nameof(Content));
                }
                IsModified = false;
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLoading = false;
                _loadingProgress = 0;
            });
        }
    }

    /// <summary>
    /// 大文件分块流式读取。使用 StringBuilder 预分配，减少 GC 压力。
    /// </summary>
    private static async Task<string> ReadLargeFileAsync(
    string path, long fileLength, IProgress<double> progress, CancellationToken ct)
    {
        var sb = new StringBuilder((int)Math.Min(fileLength, int.MaxValue));

        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);

        using var reader = new StreamReader(
            stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        char[] buffer = new char[8192];
        int read;
        long totalRead = 0;

        while ((read = await reader.ReadAsync(buffer, ct)) > 0)
        {
            sb.Append(buffer, 0, read);
            totalRead += read;

            if (fileLength > 0)
            {
                progress.Report((double)totalRead / fileLength * 100);
            }
        }

        progress.Report(100);
        return sb.ToString();
    }

    /// <summary>保存到磁盘</summary>
    public void Save()
    {
        if (string.IsNullOrEmpty(FullPath)) return;
        File.WriteAllText(FullPath, Content);
        IsModified = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //Close Event
    public event EventHandler<FileDocument>? CloseRequested;

    public void RequestClose()
    {
        CloseRequested?.Invoke(this, this);
    }
}