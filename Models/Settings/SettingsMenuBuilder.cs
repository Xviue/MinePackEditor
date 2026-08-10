using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    public class SettingsMenuBuilder
    {
        private readonly Dictionary<string, List<SettingOption>> _optionProviders = new();

        public void RegisterOptions(string key, List<SettingOption> options)
        {
            _optionProviders[key] = options;
        }

        /// <summary>
        /// 从 AppSettings 实例递归构建设置菜单树。
        /// CategoryPath 支持多级，如 "Viewer.ImageViewer.Advanced"
        /// </summary>
        public ObservableCollection<SettingsNode> Build(AppSettings settings)
        {
            var roots = new Dictionary<string, SettingsNode>(StringComparer.OrdinalIgnoreCase);
            var allNodes = new Dictionary<string, SettingsNode>(StringComparer.Ordinal); // 强制全局唯一
            var metas = AppSettings.GetConfigMetas();

            foreach (var (prop, attr) in metas)
            {
                var parts = attr.CategoryPath.Split('.');
                if (parts.Length == 0) continue;

                // 1. 创建/获取根节点
                var rootId = parts[0];
                if (!roots.TryGetValue(rootId, out var root))
                {
                    root = new SettingsNode
                    {
                        Id = rootId,
                        DisplayNameKey = $"Category.{rootId}",
                        Order = GetCategoryOrder(rootId)
                    };
                    roots[rootId] = root;
                    allNodes[rootId] = root;
                }

                // 2. 递归创建子节点
                var parent = root;
                var pathBuilder = new StringBuilder(rootId);

                for (int i = 1; i < parts.Length; i++)
                {
                    pathBuilder.Append('.').Append(parts[i]);
                    var nodeId = pathBuilder.ToString();

                    if (!allNodes.TryGetValue(nodeId, out var node))
                    {
                        node = new SettingsNode
                        {
                            Id = nodeId,
                            DisplayNameKey = $"Group.{nodeId}",
                            Parent = parent
                        };
                        allNodes[nodeId] = node;
                        parent.Children.Add(node);
                    }
                    parent = node;
                }

                // 3. parent 即为最终容器，创建 SettingItem 并挂载
                var item = CreateSettingItem(prop, attr, settings);
                parent.Items.Add(item);
            }

            return new ObservableCollection<SettingsNode>(
                roots.Values.OrderBy(r => r.Order));
        }

        private SettingItem CreateSettingItem(PropertyInfo prop, ConfigItemAttribute attr, AppSettings settings)
        {
            var item = new SettingItem
            {
                Id = attr.Id,
                PropertyName = prop.Name,
                DisplayNameKey = attr.DisplayNameKey ?? $"Config:{attr.Id}:Name",
                DescriptionKey = attr.DescriptionKey ?? $"Config:{attr.Id}:Desc",
                EditorType = attr.EditorType,
                ValueType = prop.PropertyType,
                DefaultValue = attr.DefaultValue
            };

            item.Constraint = new SettingConstraint
            {
                Min = attr.Min,
                Max = attr.Max,
                Step = attr.Step,
                Options = attr.OptionsKey != null && _optionProviders.TryGetValue(attr.OptionsKey, out var opts)
                    ? opts
                    : null
            };

            item.Attach(settings, prop);
            return item;
        }

        private static int GetCategoryOrder(string categoryId) => categoryId switch
        {
            "General" => 0,
            "Viewer" => 1,
            "Editor" => 2,
            "Session" => 99,
            _ => 50
        };
    }
}
