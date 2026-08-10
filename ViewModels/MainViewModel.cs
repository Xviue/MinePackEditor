using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CSharp.RuntimeBinder;
using MinePackEditor.Models;
using MinePackEditor.Service;
using MinePackEditor.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MinePackEditor.ViewModels;

/// <summary>
/// Author:Xviue Kimi修改，暂未审查
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // ── 属性 ──
    [ObservableProperty] private string _projectName = "新的项目";
    [ObservableProperty] private string _actionLabelText = "就绪";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCurrentCommand))]
    private FileDocument? _selectedDocument;

    public ObservableCollection<FileDocument> OpenFiles { get; } = new();
    public ObservableCollection<FileSystemNode> OpenDirectories { get; } = new();

    // ── 命令 ──
    public IRelayCommand DirectoryOpenCommand { get; private set; } = null!;
    public IRelayCommand DirectorySetCommand { get; private set; } = null!;
    public IRelayCommand<string?> OpenDirectoryCommand { get; private set; } = null!;
    public IRelayCommand OpenFilesCommand { get; private set; } = null!;
    public IRelayCommand SetDirectoryCommand { get; private set; } = null!;
    public IRelayCommand<FileSystemNode?> CloseDirectoryCommand { get; private set; } = null!;
    public IAsyncRelayCommand<string?> OpenTabCommand { get; private set; } = null!;
    public IRelayCommand<FileDocument?> CloseTabCommand { get; private set; } = null!;
    public IRelayCommand SaveCurrentCommand { get; private set; } = null!;
    public IRelayCommand SaveAllCommand { get; private set; } = null!;
    public IRelayCommand OpenSettingsWindowCommand { get; private set; } = null!;

    public IRelayCommand ExitCommand { get; private set; } = null!;
    public IRelayCommand OpenAboutCommand { get; private set; } = null!;

    // ── 私有 ──
    private readonly IFilePickerService? _folderPicker;
    private readonly IDialogService? _dialogService;
    private readonly WindowService? _windowService;
    private readonly Dictionary<FileDocument, CancellationTokenSource> _loadingTokens = new();
    private FileDocument? _previousDocument;

    // ── 构造 ──
    public MainViewModel()
    {
        InitializeCommands();
        if (Avalonia.Controls.Design.IsDesignMode)
            LoadDesignTimeData();
    }

    public MainViewModel(IFilePickerService folderPickerService,IDialogService dialogService, WindowService windowService) : this()
    {
        _folderPicker = folderPickerService;
        _dialogService = dialogService;
        _windowService = windowService;
    }

    private void InitializeCommands()
    {
        DirectoryOpenCommand = new AsyncRelayCommand(PickFolderThenAddAsync);
        DirectorySetCommand = new AsyncRelayCommand(PickFolderThenSetAsync);

        OpenDirectoryCommand = new RelayCommand<string?>(AddWorkspace);
        SetDirectoryCommand = new RelayCommand<string?>(SetWorkspace);

        CloseDirectoryCommand = new RelayCommand<FileSystemNode?>(CloseWorkspace);
        OpenTabCommand = new AsyncRelayCommand<string?>(OpenFileCoreAsync);
        CloseTabCommand = new AsyncRelayCommand<FileDocument?>(CloseFileCore);
        SaveCurrentCommand = new RelayCommand(SaveCurrent, () => CanSaveCurrent);
        SaveAllCommand = new RelayCommand(SaveAll, () => CanSaveAll);

        OpenSettingsWindowCommand = new AsyncRelayCommand(OpenSettingsWindow);
        OpenFilesCommand = new AsyncRelayCommand(PickFilesThenAddAsync);

        ExitCommand = new AsyncRelayCommand(ExitWindowAsync);
        OpenAboutCommand = new AsyncRelayCommand(OpenAboutWindow);
    }

    // 窗口操作
    private async Task ExitWindowAsync()
    {
        if (_windowService == null || _dialogService == null) return;
        var modified = OpenFiles.Where(f => f.IsModified).ToList();

        if (modified.Any())
        {
            var result = await _dialogService.ShowDialogAsync<SaveAllViewModel, SaveAllResult>(new SaveAllViewModel(modified));
            if (result == null) return;
            if (result.Result == DialogResult.Cancel || result.Result == DialogResult.None) return;
            if (result.Result == DialogResult.OK)
            {
                foreach (var f in result.FilesToSave)
                {
                    try { f.Save(); }
                    catch (Exception ex)
                    {
                        var res = await _dialogService.ShowConfirmAsync("保存文件错误", $"发生{ex.GetType}错误: {ex.Message}", "继续", "返回");

                        if (res) continue;
                        else return;
                    }
                }
            }
        }

        _windowService.Close();
    }

    // ── 打开窗口 ──
    private async Task OpenSettingsWindow()
    {
        var settingsWindow = new SettingsWindow();
        //settingsWindow.DataContext = new SettingsWindowViewModel(); 已添加
        settingsWindow.Show();
    }

    private async Task OpenAboutWindow()
    {
        var aboutWindow= new AboutWindow();
        aboutWindow.Show();
    }

    // ── 设计时数据（纯内存，不触碰磁盘）──
    private void LoadDesignTimeData()
    {
        var root = new FileSystemNode(@"C:\Demo", isDirectory: true);
        root.IsExpanded = true;
        OpenDirectories.Add(root);

        var doc = new FileDocument("Program.cs", @"C:\Demo\Program.cs",
            "using System;\n\nclass Program\n{\n    static void Main() => Console.WriteLine(\"Hello\");\n}");
        OpenFiles.Add(doc);
        SelectedDocument = doc;
    }

    // ── 切换标签自动保存 ──
    partial void OnSelectedDocumentChanged(FileDocument? oldValue, FileDocument? newValue)
    {
        if (oldValue is { IsModified: true }
            && SettingsService.Instance.Settings.AutoSaveOnSwitch)
        {
            try { oldValue.Save(); }
            catch (Exception ex) { ActionLabelText = $"自动保存失败: {ex.Message}"; }
        }
        _previousDocument = newValue;
        SaveCurrentCommand.NotifyCanExecuteChanged();
    }

    // ── 工作区 ──
    private async Task PickFolderThenAddAsync()
    {
        if (_folderPicker == null) return;
        try
        {
            var paths = await _folderPicker.PickFoldersAsync();
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path)) { ActionLabelText = "取消添加目录"; return; }
                AddWorkspace(path);
            }
            ActionLabelText = $"成功选择 {paths.Count} 个目录";
        }
        catch (Exception ex) { ActionLabelText = $"选择失败: {ex.Message}"; }
    }

    private async Task PickFolderThenSetAsync()
    {
        if (_folderPicker == null) return;
        try
        {
            var path = await _folderPicker.PickFolderAsync();
            if (string.IsNullOrWhiteSpace(path)) { ActionLabelText = "取消选择"; return; }
            SetWorkspace(path);
        }
        catch (Exception ex) { ActionLabelText = $"选择失败: {ex.Message}"; }
    }

    private async Task PickFilesThenAddAsync()
    {
        if (_folderPicker == null) return;
        try
        {
            var paths = await _folderPicker.PickFilesAsync();
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path)) { ActionLabelText = "取消打开文件"; return; }
                OpenTabCommand.Execute(path);
            }
            ActionLabelText = $"成功选择 {paths.Count} 个文件";
        }
        catch (Exception ex) { ActionLabelText = $"选择失败: {ex.Message}"; }
    }

    private void AddWorkspace(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        if (OpenDirectories.Any(n => n.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        var node = new FileSystemNode(path, isDirectory: true) { IsExpanded = true };
        OpenDirectories.Add(node);
    }

    private void SetWorkspace(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        if (OpenDirectories.Count == 1 && OpenDirectories[0].FullPath.Equals(path, StringComparison.OrdinalIgnoreCase)) return;

        var node = new FileSystemNode(path, isDirectory: true) { IsExpanded = true };
        OpenDirectories.Clear();
        OpenDirectories.Add(node);
    }

    private void CloseWorkspace(FileSystemNode? node)
    {
        if (node != null) OpenDirectories.Remove(node);
    }


    private async Task OpenFileCoreAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!File.Exists(path)) { ActionLabelText = "文件不存在"; return; }

        var fullPath = Path.GetFullPath(path);

        // 已打开则直接切换
        var existing = OpenFiles.FirstOrDefault(d =>
            d.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { SelectedDocument = existing; return; }

        // 预检大小
        var fileInfo = new FileInfo(fullPath);
        const long hardLimit = 100L * 1024 * 1024;
        if (fileInfo.Length > hardLimit)
        {
            ActionLabelText = $"文件过大 ({fileInfo.Length / 1024 / 1024}MB)，已拒绝";
            return;
        }

        // 1. 先创建空壳文档，立即加入 UI（用户立刻看到 Tab）
        var doc = new FileDocument(
            fileName: Path.GetFileName(fullPath),
            fullPath: fullPath,
            content: string.Empty);

        var cts = new CancellationTokenSource();
        _loadingTokens[doc] = cts;

        doc.PropertyChanged += OnDocumentPropertyChanged;
        doc.CloseRequested += OnFileCloseRequested;

        OpenFiles.Add(doc);
        SelectedDocument = doc;
        ActionLabelText = $"正在打开 {doc.FileName}...";

        // 2. 后台异步加载内容
        try
        {
            await doc.LoadContentAsync(cts.Token);
            ActionLabelText = $"已打开: {doc.FileName}";
        }
        catch (OperationCanceledException)
        {
            CleanupDocument(doc);
        }
        catch (Exception ex)
        {
            ActionLabelText = $"打开失败: {ex.Message}";
            CleanupDocument(doc);
        }
        finally
        {
            _loadingTokens.Remove(doc);
        }
    }

    // ── 关闭文件 ──
    private async Task CloseFileCore(FileDocument? doc)
    {
        if (doc == null) return;
        if (_dialogService == null) return;

        // 正常关闭：未保存提示
        if (!SettingsService.Instance.Settings.AutoSaveOnSwitch && doc.IsModified)
        {
            var dialogResult = await _dialogService.ShowYesNoCancelAsync("确认关闭", "文件还未保存，是否直接关闭？", "保存", "关闭", "取消");
            if (dialogResult == DialogResult.Cancel || dialogResult == DialogResult.None) return;
            if (dialogResult == DialogResult.OK)
            {
                doc.Save();
            }
        }

        // 若正在加载，发送取消信号，让异步方法自行清理
        if (_loadingTokens.TryGetValue(doc, out var cts))
        {
            cts.Cancel();
            return;
        }

        CleanupDocument(doc);
    }

    private void CleanupDocument(FileDocument doc)
    {
        doc.PropertyChanged -= OnDocumentPropertyChanged;
        doc.CloseRequested -= OnFileCloseRequested;
        if (SelectedDocument == doc) SelectedDocument = null;
        OpenFiles.Remove(doc);
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileDocument.IsModified))
        {
            SaveCurrentCommand.NotifyCanExecuteChanged();
            SaveAllCommand.NotifyCanExecuteChanged();
        }
    }

    private async void OnFileCloseRequested(object? sender, FileDocument doc)
    {
        await CloseFileCore(doc);
    }

    // ── 保存 ──
    private void SaveCurrent()
    {
        if (SelectedDocument is not { IsModified: true }) return;
        try { SelectedDocument.Save(); ActionLabelText = $"已保存: {SelectedDocument.FileName}"; }
        catch (Exception ex) { ActionLabelText = $"保存失败: {ex.Message}"; }
    }
    private bool CanSaveCurrent => SelectedDocument?.IsModified == true;

    private void SaveAll()
    {
        var modified = OpenFiles.Where(f => f.IsModified).ToList();
        int ok = 0;
        foreach (var f in modified)
        {
            try { f.Save(); ok++; }
            catch (Exception ex) { ActionLabelText = $"{f.FileName} 保存失败: {ex.Message}"; }
        }
        ActionLabelText = $"批量保存: {ok}/{modified.Count} 成功";
    }
    private bool CanSaveAll => OpenFiles.Any(f => f.IsModified);

    // ── 会话持久化 ──
    public void SaveSession()
    {
        try
        {
            var s = SettingsService.Instance.Settings;
            s.LastOpenDirectories = OpenDirectories
                .Select(n => n.FullPath)
                .Where(Directory.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 只保存已完成加载的文件（正在加载的下次重新打开即可）
            s.LastOpenFiles = OpenFiles
                .Where(f => !f.IsLoading && File.Exists(f.FullPath))
                .Select(f => f.FullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaveSession] {ex.Message}");
        }
    }

    public void RestoreSession()
    {
        try
        {
            var s = SettingsService.Instance.Settings;

            foreach (var dir in s.LastOpenDirectories.Where(Directory.Exists))
                AddWorkspace(dir);

            foreach (var file in s.LastOpenFiles.Where(File.Exists))
                OpenTabCommand.Execute(file); // 触发异步加载

            ActionLabelText = OpenFiles.Count > 0 || OpenDirectories.Count > 0
                ? "会话已恢复" : "就绪";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RestoreSession] {ex.Message}");
        }
    }
}

