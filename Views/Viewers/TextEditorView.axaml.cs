using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.ComponentModel;
using MinePackEditor.Models;
using System;
using System.ComponentModel;

namespace MinePackEditor.Views
{
    public partial class TextEditorView : UserControl
    {
        private readonly TextEditor _editor;
        private bool _isUpdating;
        private FileDocument? _previousDoc;

        public TextEditorView()
        {
            InitializeComponent();

            _editor = this.FindControl<TextEditor>("Editor")!;

            _editor.TextChanged += OnEditorTextChanged;
            DataContextChanged += OnDataContextChanged;
        }


        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            // 取消对旧文档的监听，防止内存泄漏
            if (_previousDoc != null)
            {
                _previousDoc.PropertyChanged -= OnDocumentPropertyChanged;
            }

            if (DataContext is not FileDocument doc) return;

            _previousDoc = doc;
            doc.PropertyChanged += OnDocumentPropertyChanged;

            // 立即同步当前值（可能是空，也可能是已加载好的内容）
            _isUpdating = true;
            _editor.Text = doc.Content;
            _isUpdating = false;
        }

        /// <summary>
        /// 关键：当 FileDocument.Content 在后台加载完成后变化时，同步到编辑器
        /// </summary>
        private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FileDocument.Content)) return;
            if (sender is not FileDocument doc) return;
            if (_isUpdating) return;

            _isUpdating = true;
            _editor.Text = doc.Content;
            _isUpdating = false;
        }


        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            if (_isUpdating || DataContext is not FileDocument doc) return;

            _isUpdating = true;
            doc.Content = _editor.Text ?? string.Empty;
            _isUpdating = false;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            _editor.TextChanged -= OnEditorTextChanged;
            DataContextChanged -= OnDataContextChanged;

            if (_previousDoc != null)
            {
                _previousDoc.PropertyChanged -= OnDocumentPropertyChanged;
                _previousDoc = null;
            }
        }
    }
}