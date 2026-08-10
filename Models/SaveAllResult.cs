using MinePackEditor.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Models
{
    public class SaveAllResult
    {
        /// <summary>用户点击了 保存/不保存/取消</summary>
        public DialogResult Result { get; set; }

        /// <summary>被纳入（勾选保存）的文件列表</summary>
        public List<FileDocument> FilesToSave { get; set; } = new();
    }
}
