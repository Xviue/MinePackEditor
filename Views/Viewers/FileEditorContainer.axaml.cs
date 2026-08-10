using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinePackEditor.Models;
using MinePackEditor.Models.Settings;
using System;
using System.IO;

namespace MinePackEditor.Views
{
    public partial class FileEditorContainer : UserControl
    {
        public FileEditorContainer()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is not FileDocument doc) return;

            if (EditorHost.Content is Control oldView)
            {
                oldView.DataContext = null;
                EditorHost.Content = null;
            }

            // 获得类型
            var editorId = EditorRegistry.Instance.ResolveEditor(doc.FullPath);

            

            Control viewer = editorId switch
            {
                "text" => new TextEditorView(),

                "image" => new ImageViewerView(),

                _ => new UnsupportedViewerView()
            };

            viewer.DataContext = doc;
            EditorHost.Content = viewer;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            DataContextChanged -= OnDataContextChanged;
            if (EditorHost.Content is Control c) c.DataContext = null;
        }
    }
}