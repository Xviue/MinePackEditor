using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using FluentAvalonia.UI.Controls;
using MinePackEditor.Models;
using MinePackEditor.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MinePackEditor.Selectors
{
    public class FileViewTemplateSelector : IDataTemplate
    {
        public Control? Build(object? param)
        {
            return param is FileDocument
                ? new FileEditorContainer()   // 容器内部自行判断类型
                : new TextBlock { Text = "[Unexcepted Exception]错误的上下文对象", Margin = new(20) };
        }

        public bool Match(object? data) => data is FileDocument;
    }
}
