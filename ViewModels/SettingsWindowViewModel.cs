using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinePackEditor.Assets.Localization.Settings;
using MinePackEditor.Managers;
using MinePackEditor.Models.Settings;
using MinePackEditor.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

namespace MinePackEditor.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject, ICloseable
{
    private readonly SettingsMenuBuilder _menuBuilder = new();
    private readonly JsonSerializerOptions _cloneOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// 工作副本：用户在此实例上修改，Apply 时才写回原始单例
    /// </summary>
    private AppSettings _workingSettings = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredMenuTree))]
    private ObservableCollection<SettingsNode> _menuTree = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayedItems))]
    [NotifyPropertyChangedFor(nameof(HasDisplayedItems))]
    private SettingsNode? _selectedNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredMenuTree))]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]
    [NotifyPropertyChangedFor(nameof(DisplayedItems))]
    [NotifyPropertyChangedFor(nameof(HasDisplayedItems))]
    private string _searchText = string.Empty;

    private ObservableCollection<SettingsNode>? _cachedSearchTree;
    private string? _cachedSearchKeyword;

    public ObservableCollection<SettingsNode> FilteredMenuTree =>
        IsSearchActive ? GetSearchTree(SearchText) : MenuTree;

    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);

    public IRelayCommand<SettingItem?> RevertItemCommand { get; private set; } = null!;

    public event EventHandler? RequestCancelConfirmation;
    private readonly List<SettingItem> _allSettingItems = new();

    public ObservableCollection<SettingItem> DisplayedItems
    {
        get
        {
            if (SelectedNode == null) return new ObservableCollection<SettingItem>();
            if (SelectedNode.Id == "SearchResult")
                return SelectedNode.Items;
            var originalNode = FindNodeById(MenuTree, SelectedNode.Id);
            return originalNode?.FlattenedItems ?? new ObservableCollection<SettingItem>();
        }
    }

    public bool HasDisplayedItems => DisplayedItems.Count > 0;

    public event EventHandler? RequestClose;
    private IDialogService? _dialogService;

    public SettingsWindowViewModel(IDialogService dialogService)
    {
        this._dialogService = dialogService;
        Initialize();
    }

    public SettingsWindowViewModel()
    {
        Initialize();
    }

    private void Initialize()
    {
        _workingSettings = DeepClone(SettingsService.Instance.Settings);
        BuildMenuTree();

        RequestCancelConfirmation += OnRequestCancelConfirmation;

        RevertItemCommand = new RelayCommand<SettingItem?>(
        execute: item =>
        {
            if (item != null)
                item.CurrentValue = item.OriginalValue;
        },
        canExecute: item => item?.IsModified == true
        );
    }

    /// <summary>
    /// 点击取消后，执行的事件
    /// </summary>
    private async void OnRequestCancelConfirmation(object? sender, EventArgs e)
    {
        if (_dialogService == null) return;
        var result = await _dialogService.ShowYesNoCancelAsync("是否保存?","是否保存已修改的配置项？", "保存", "丢弃");
        if(result == DialogResult.OK)
        {
            CopyToOriginal(_workingSettings, SettingsService.Instance.Settings);
            SettingsService.Instance.Save();
            RequestClose?.Invoke(this, EventArgs.Empty);
        } else if(result == DialogResult.No)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        } else
        {
            return;
        }
    }

    /// <summary>
    /// 通过 JSON 序列化/反序列化实现深拷贝
    /// </summary>
    private AppSettings DeepClone(AppSettings source)
    {
        var json = JsonSerializer.Serialize(source, _cloneOptions);
        return JsonSerializer.Deserialize<AppSettings>(json, _cloneOptions) ?? new AppSettings();
    }

    private void BuildMenuTree()
    {
        _menuBuilder.RegisterOptions("AvailableLanguages", new List<SettingOption>
        {
            new() { LabelKey = "Lang.SimplifiedChinese", Value = "zh-hans" },
            new() { LabelKey = "Lang.English", Value = "en-us" }
        });

        // 关键：使用工作副本构建菜单树，所有 SettingItem 绑定到 _workingSettings
        MenuTree = _menuBuilder.Build(_workingSettings);
        SelectedNode = FindFirstLeaf(MenuTree.FirstOrDefault());

        _allSettingItems.Clear();
        CollectAllSettingItems(MenuTree);
    }

    private void CollectAllSettingItems(IEnumerable<SettingsNode> nodes)
    {
        foreach (var node in nodes)
        {
            foreach (var item in node.Items)
            {
                _allSettingItems.Add(item);
                item.PropertyChanged += OnSettingItemPropertyChanged;
            }
            CollectAllSettingItems(node.Children);
        }
    }

    private void OnSettingItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingItem.IsModified))
        {
            RevertItemCommand.NotifyCanExecuteChanged();
        }
    }

    #region === 搜索视图构建（逻辑不变，基于 MenuTree） ===

    private ObservableCollection<SettingsNode> GetSearchTree(string keyword)
    {
        if (_cachedSearchTree != null && _cachedSearchKeyword == keyword)
            return _cachedSearchTree;

        _cachedSearchKeyword = keyword;
        _cachedSearchTree = BuildSearchMenuTree(keyword);
        return _cachedSearchTree;
    }

    private ObservableCollection<SettingsNode> BuildSearchMenuTree(string keyword)
    {
        var result = new ObservableCollection<SettingsNode>();
        var lower = keyword.ToLowerInvariant();

        // 1. 合成搜索结果节点
        var searchResultNode = new SettingsNode
        {
            Id = "SearchResult",
            DisplayNameKey = "SearchResultTemplate",
            DisplayNameOverride = $"{SettingsLang.Get("SearchResultPrefix")}{keyword}{SettingsLang.Get("SearchResultSuffix")}",
            Order = -1
        };

        foreach (var root in MenuTree)
        {
            CollectMatchedItems(root, lower, searchResultNode.Items);
        }

        if (searchResultNode.Items.Count > 0)
            result.Add(searchResultNode);

        // 2. 保留完整分支
        foreach (var root in MenuTree)
        {
            if (ShouldPreserveBranch(root, lower))
                result.Add(root);
        }

        return result;
    }

    private static bool ShouldPreserveBranch(SettingsNode node, string lower)
    {
        if (SettingsLang.Get(node.DisplayNameKey).Contains(lower, StringComparison.OrdinalIgnoreCase))
            return true;
        foreach (var child in node.Children)
        {
            if (ShouldPreserveBranch(child, lower))
                return true;
        }
        foreach (var item in node.Items)
        {
            if (ItemMatches(item, lower))
                return true;
        }
        return false;
    }

    private static bool ItemMatches(SettingItem item, string lower)
    {
        return item.DisplayNameKey.Contains(lower, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(lower, StringComparison.OrdinalIgnoreCase)
            || (item.DescriptionKey?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false)
            || SettingsLang.Get(item.DisplayNameKey).Contains(lower, StringComparison.OrdinalIgnoreCase)
            || SettingsLang.Get(item.DescriptionKey).ToLowerInvariant().Contains(lower);
    }

    private static void CollectMatchedItems(SettingsNode node, string lower, ObservableCollection<SettingItem> results)
    {
        foreach (var item in node.Items)
        {
            if (ItemMatches(item, lower))
                results.Add(item);
        }
        foreach (var child in node.Children)
        {
            CollectMatchedItems(child, lower, results);
        }
    }

    #endregion

    #region === 辅助方法 ===

    private static SettingsNode? FindNodeById(IEnumerable<SettingsNode> nodes, string id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id) return node;
            var found = FindNodeById(node.Children, id);
            if (found != null) return found;
        }
        return null;
    }

    private static SettingsNode? FindFirstLeaf(SettingsNode? node)
    {
        if (node == null) return null;
        if (node.Items.Count > 0) return node;
        foreach (var child in node.Children)
        {
            var leaf = FindFirstLeaf(child);
            if (leaf != null) return leaf;
        }
        return null;
    }

    #endregion

    #region === 命令 ===

    /// <summary>
    /// 重置当前选中节点下的所有配置项（仅影响工作副本）
    /// </summary>
    [RelayCommand]
    private void ResetCurrentGroup()
    {
        SlideTipBarManager.Activate("SettingsTipBar", "reset_group");
        if (SelectedNode == null) return;

        if (SelectedNode.Id == "SearchResult")
        {
            foreach (var item in SelectedNode.Items)
                item.ResetToDefault();
        }
        else
        {
            var target = FindNodeById(MenuTree, SelectedNode.Id);
            if (target == null) return;

            foreach (var item in target.Items)
                item.ResetToDefault();
            foreach (var child in target.Children)
                ResetNodeRecursive(child);
        }
    }

    private static void ResetNodeRecursive(SettingsNode node)
    {
        foreach (var item in node.Items)
            item.ResetToDefault();
        foreach (var child in node.Children)
            ResetNodeRecursive(child);
    }

    /// <summary>
    /// 重置所有配置项（仅影响工作副本）
    /// </summary>
    [RelayCommand]
    private void ResetAll()
    {
        SlideTipBarManager.Activate("SettingsTipBar", "reset-all");
        _workingSettings.ResetToDefaults();
        _cachedSearchTree = null;

        // 关键修复：不再重建菜单树，避免 SettingItem 被重新 Attach 导致 OriginalValue 丢失
        // SettingItem 通过 AppSettings.PropertyChanged 自动更新 _currentValue，OriginalValue 保持不变
        if (IsSearchActive)
        {
            OnPropertyChanged(nameof(FilteredMenuTree));
        }

        OnPropertyChanged(nameof(DisplayedItems));
        OnPropertyChanged(nameof(HasDisplayedItems));
    }

    /// <summary>
    /// 将工作副本的值复制回原始单例并保存，然后关闭窗口
    /// </summary>
    [RelayCommand]
    private void OK()
    {
        CopyToOriginal(_workingSettings, SettingsService.Instance.Settings);
        SettingsService.Instance.Save();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 将工作副本的值复制回原始单例并保存
    /// </summary>
    [RelayCommand]
    private void Apply()
    {
        SlideTipBarManager.Activate("SettingsTipBar", "apply");
        CopyToOriginal(_workingSettings, SettingsService.Instance.Settings);
        SettingsService.Instance.Save();
    }



    /// <summary>
    /// 将工作副本的属性值逐个复制回原始单例
    /// </summary>
    private static void CopyToOriginal(AppSettings source, AppSettings target)
    {
        foreach (var (prop, attr) in AppSettings.GetConfigMetas())
        {
            if (!prop.CanWrite) continue;
            var value = prop.GetValue(source);
            prop.SetValue(target, value);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_allSettingItems.Any(i => i.IsModified))
        {
            RequestCancelConfirmation?.Invoke(this, EventArgs.Empty);
            return;
        } else
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void ResetItem(SettingItem? item)
    {
        item?.ResetToDefault();
    }

    [RelayCommand]
    private void BackItem(SettingItem? item)
    {

    }

    #endregion

    #region === 属性变更通知 ===

    partial void OnSearchTextChanged(string value)
    {
        _cachedSearchTree = null;
        string? previousId = SelectedNode?.Id;

        OnPropertyChanged(nameof(FilteredMenuTree));
        OnPropertyChanged(nameof(IsSearchActive));
        OnPropertyChanged(nameof(DisplayedItems));
        OnPropertyChanged(nameof(HasDisplayedItems));

        if (IsSearchActive && previousId != null)
        {
            var newTree = GetSearchTree(value);
            var newSelected = FindNodeById(newTree, previousId)
                ?? newTree.FirstOrDefault(n => n.Id == "SearchResult")
                ?? newTree.FirstOrDefault();
            SelectedNode = newSelected;
        }
    }

    partial void OnSelectedNodeChanged(SettingsNode? value)
    {
        OnPropertyChanged(nameof(DisplayedItems));
        OnPropertyChanged(nameof(HasDisplayedItems));
    }

    #endregion
}