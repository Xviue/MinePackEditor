using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinePackEditor.Assets.Localization.Settings;
using MinePackEditor.Models.Settings;
using MinePackEditor.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MinePackEditor.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{
    private readonly SettingsMenuBuilder _menuBuilder = new();

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

    /// <summary>
    /// 右侧显示的配置项。
    /// - 搜索结果节点：显示其 Items（扁平匹配项）
    /// - 保留分支节点：在原始树中查找并显示完整 FlattenedItems
    /// </summary>
    public ObservableCollection<SettingItem> DisplayedItems
    {
        get
        {
            if (SelectedNode == null) return new ObservableCollection<SettingItem>();

            // 搜索结果节点特殊处理
            if (SelectedNode.Id == "SearchResult")
                return SelectedNode.Items;

            // 保留分支节点：在原始完整树中查找对应节点，显示其完整内容
            var originalNode = FindNodeById(MenuTree, SelectedNode.Id);
            return originalNode?.FlattenedItems ?? new ObservableCollection<SettingItem>();
        }
    }

    public bool HasDisplayedItems => DisplayedItems.Count > 0;

    public event EventHandler? RequestClose;

    public SettingsWindowViewModel()
    {
        BuildMenuTree();
    }

    private void BuildMenuTree()
    {
        _menuBuilder.RegisterOptions("AvailableLanguages", new List<SettingOption>
        {
            new() { LabelKey = "Lang.SimplifiedChinese", Value = "zh-hans" },
            new() { LabelKey = "Lang.English", Value = "en-us" }
        });

        MenuTree = _menuBuilder.Build(SettingsService.Instance.Settings);
        SelectedNode = FindFirstLeaf(MenuTree.FirstOrDefault());
    }

    #region === 搜索视图构建 ===

    private ObservableCollection<SettingsNode> GetSearchTree(string keyword)
    {
        if (_cachedSearchTree != null && _cachedSearchKeyword == keyword)
            return _cachedSearchTree;

        _cachedSearchKeyword = keyword;
        _cachedSearchTree = BuildSearchMenuTree(keyword);
        return _cachedSearchTree;
    }

    /// <summary>
    /// 构建搜索视图：
    /// 1. 合成搜索结果节点（全局扁平匹配项）
    /// 2. 完整保留原始树中"路径上有匹配"的所有分支
    /// </summary>
    private ObservableCollection<SettingsNode> BuildSearchMenuTree(string keyword)
    {
        var result = new ObservableCollection<SettingsNode>();
        var lower = keyword.ToLowerInvariant();

        // 1. 合成搜索结果节点：包含所有匹配的配置项（不分分类，扁平化）
        var searchResultNode = new SettingsNode
        {
            Id = "SearchResult",
            DisplayNameKey = "SearchResultTemplate", // 可配置为 "{0}的搜索结果..."
            DisplayNameOverride = $"{SettingsLang.Get("SearchResultPrefix")}{keyword}{SettingsLang.Get("SearchResultSuffix")}",
            Order = -1
        };

        foreach (var root in MenuTree)
        {
            CollectMatchedItems(root, lower, searchResultNode.Items);
        }

        if (searchResultNode.Items.Count > 0)
            result.Add(searchResultNode);

        // 2. 保留路径上有匹配的完整分支（不裁剪任何子节点/配置项）
        foreach (var root in MenuTree)
        {
            if (ShouldPreserveBranch(root, lower))
            {
                result.Add(root);
            }
        }

        return result;
    }

    /// <summary>
    /// 判断分支是否应该完整保留：节点自身、任意后代节点、或任意配置项的本地化文本匹配
    /// </summary>
    private static bool ShouldPreserveBranch(SettingsNode node, string lower)
    {
        // 节点自身本地化名称匹配？
        if (SettingsLang.Get(node.DisplayNameKey).Contains(lower, StringComparison.OrdinalIgnoreCase))
            return true;

        // 任意后代节点匹配？
        foreach (var child in node.Children)
        {
            if (ShouldPreserveBranch(child, lower))
                return true;
        }

        // 任意配置项匹配？
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
    /// 重置当前选中节点下的所有配置项。
    /// 搜索结果节点时，重置其 Items 中所有匹配的配置项。
    /// 保留分支节点时，重置原始树中对应节点的完整内容。
    /// </summary>
    [RelayCommand]
    private void ResetCurrentGroup()
    {
        if (SelectedNode == null) return;

        if (SelectedNode.Id == "SearchResult")
        {
            // 搜索结果节点：重置其 Items 中所有匹配的配置项
            foreach (var item in SelectedNode.Items)
                item.ResetToDefault();
        }
        else
        {
            // 保留分支节点：在原始树中查找并重置完整内容
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

    [RelayCommand]
    private void ResetAll()
    {
        SettingsService.Instance.Settings.ResetToDefaults();
        _cachedSearchTree = null;
        BuildMenuTree();
    }

    [RelayCommand]
    private void Apply()
    {
        SettingsService.Instance.Save();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region === 属性变更 ===

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