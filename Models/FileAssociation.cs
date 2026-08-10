using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Models
{
    /// <summary>
    /// 单个后缀关联配置
    /// </summary>
    public sealed class FileAssociation
    {
        /// <summary>后缀名，统一存储为小写且带点，如 ".txt"</summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>关联的编辑器 Id，如 "text", "image"</summary>
        public string EditorId { get; set; } = string.Empty;
    }
}
