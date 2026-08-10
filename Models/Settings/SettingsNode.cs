using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    /// <summary>
    /// 设置菜单的统一树节点。既可以是左侧导航的根分类，
    /// 也可以是中间的分组/子分组，叶子节点承载 SettingItem。
    /// </summary>
    public class SettingsNode
    {
        /// <summary>
        /// 全局唯一标识，由 CategoryPath 拼接而成。
        /// 如 "Viewer"、"Viewer.ImageViewer"、"Viewer.ImageViewer.Advanced"
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 直接覆盖的显示文本（非 Key），用于动态生成的标题。
        /// 如搜索结果节点的 "图片的搜索结果..."。
        /// 优先级高于 DisplayNameKey。
        /// </summary>
        public string? DisplayNameOverride { get; set; }

        /// <summary>国际化键</summary>
        public string DisplayNameKey { get; set; } = string.Empty;

        /// <summary>描述国际化键</summary>
        public string? DescriptionKey { get; set; }

        /// <summary>排序权重，仅对根节点生效</summary>
        public int Order { get; set; }

        /// <summary>父节点，根节点为 null</summary>
        public SettingsNode? Parent { get; set; }

        /// <summary>子分组</summary>
        public ObservableCollection<SettingsNode> Children { get; } = new();

        /// <summary>叶子节点上的配置项</summary>
        public ObservableCollection<SettingItem> Items { get; } = new();

        /// <summary>是否为纯容器（有子节点无配置项）</summary>
        public bool IsContainer => Children.Count > 0 && Items.Count == 0;

        /// <summary>是否为叶子（无子节点，直接承载配置项）</summary>
        public bool IsLeaf => Children.Count == 0;

        /// <summary>
        /// 递归收集本节点及所有后代节点下的配置项。
        /// 用于大分组点击时整合显示所有子分组内容。
        /// </summary>
        public ObservableCollection<SettingItem> FlattenedItems
        {
            get
            {
                var result = new ObservableCollection<SettingItem>();
                CollectItemsRecursive(this, result);
                return result;
            }
        }

        private static void CollectItemsRecursive(SettingsNode node, ObservableCollection<SettingItem> result)
        {
            foreach (var item in node.Items)
                result.Add(item);
            foreach (var child in node.Children)
                CollectItemsRecursive(child, result);
        }
    }
}
