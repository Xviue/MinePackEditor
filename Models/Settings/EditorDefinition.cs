using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    /// <summary>
    /// 内置编辑器类型定义
    /// </summary>
    public sealed class EditorDefinition
    {
        public string Id { get; }
        public string DisplayNameKey { get; }
        public string DescriptionKey { get; }

        public EditorDefinition(string id, string? displayNameKey = null, string? descriptionKey = null)
        {
            Id = id;
            DisplayNameKey = displayNameKey ?? $"Editor:{id}:Name";
            DescriptionKey = descriptionKey ?? $"Editor:{id}:Desc";
        }
    }
}
