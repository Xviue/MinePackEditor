using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using MinePackEditor.Models;
using MinePackEditor.Service;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MinePackEditor.Views
{
    public partial class ImageViewerView : UserControl
    {
        private static double MinScale => SettingsService.Instance.Settings.MinZoomScale;
        private static double MaxScale => SettingsService.Instance.Settings.MaxZoomScale;
        private static double ZoomStep => SettingsService.Instance.Settings.ZoomStep;

        private readonly ScaleTransform _imageScale = new() { ScaleX = 1, ScaleY = 1 };
        private readonly TranslateTransform _imageOffset = new() { X = 0, Y = 0 };

        private Point _lastPointerPosition;
        private bool _isDragging;

        private CancellationTokenSource? _imageLoadCts;

        public ImageViewerView()
        {
            InitializeComponent();
            ImageControl.RenderTransform = new TransformGroup
            {
                Children = { _imageScale, _imageOffset }
            };

            DataContextChanged += OnDataContextChanged;
        }

        private async void OnDataContextChanged(object? sender, EventArgs e)
        {
 
            ResetTransform();

            CancelImageLoad();

            if (DataContext is not FileDocument doc || string.IsNullOrEmpty(doc.FullPath))
            {
                ImageControl.Source = null;
                return;
            }

            _imageLoadCts = new CancellationTokenSource();
            var ct = _imageLoadCts.Token;

            LoadingOverlay.IsVisible = true;
            try
            {
                var bitmap = await Task.Run(() => DecodeBitmap(doc.FullPath, ct), ct);

                if (ct.IsCancellationRequested)
                {
                    bitmap?.Dispose();
                    return;
                }

                // 清理旧图内存
                (ImageControl.Source as IDisposable)?.Dispose();
                ImageControl.Source = bitmap;
            }
            catch (OperationCanceledException)
            {
                // 正常取消，静默处理
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageViewer] 加载失败: {ex.Message}");
                ImageControl.Source = null;
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }

        /// <summary>
        /// 后台线程解码。CPU 密集型操作不阻塞 UI。
        /// </summary>
        private static Bitmap? DecodeBitmap(string path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!File.Exists(path)) return null;

            using var stream = File.OpenRead(path);

            var maxWidth = SettingsService.Instance.Settings.MaxDecodeWidth;
            return Bitmap.DecodeToWidth(stream, maxWidth);
        }

        private void CancelImageLoad()
        {
            _imageLoadCts?.Cancel();
            _imageLoadCts?.Dispose();
            _imageLoadCts = null;
        }



        private void ResetTransform()
        {
            _imageScale.ScaleX = 1;
            _imageScale.ScaleY = 1;
            _imageOffset.X = 0;
            _imageOffset.Y = 0;
            UpdateZoomText();
        }

        // ── 滚轮缩放 ──
        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (ImageControl.Source is not Bitmap) return;

            var delta = e.Delta.Y > 0 ? ZoomStep : 1 / ZoomStep;
            var newScale = Math.Clamp(_imageScale.ScaleX * delta, MinScale, MaxScale);

            var pointerInImage = e.GetPosition(ImageControl);

            var oldX = pointerInImage.X * _imageScale.ScaleX + _imageOffset.X;
            var oldY = pointerInImage.Y * _imageScale.ScaleY + _imageOffset.Y;

            _imageScale.ScaleX = newScale;
            _imageScale.ScaleY = newScale;

            _imageOffset.X = oldX - pointerInImage.X * newScale;
            _imageOffset.Y = oldY - pointerInImage.Y * newScale;

            UpdateZoomText();
            e.Handled = true;
        }

        // ── 鼠标拖动 ──
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDragging = true;
                _lastPointerPosition = e.GetPosition(this);
                Cursor = new Cursor(StandardCursorType.Hand);
                e.Handled = true;
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging) return;

            var currentPos = e.GetPosition(this);
            var delta = currentPos - _lastPointerPosition;

            _imageOffset.X += delta.X;
            _imageOffset.Y += delta.Y;
            _lastPointerPosition = currentPos;

            e.Handled = true;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                Cursor = Cursor.Default;
                e.Handled = true;
            }
        }

        // ── 按钮控制 ──
        private void ZoomIn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var newScale = Math.Min(_imageScale.ScaleX * ZoomStep, MaxScale);
            _imageScale.ScaleX = newScale;
            _imageScale.ScaleY = newScale;
            UpdateZoomText();
        }

        private void ZoomOut(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var newScale = Math.Max(_imageScale.ScaleX / ZoomStep, MinScale);
            _imageScale.ScaleX = newScale;
            _imageScale.ScaleY = newScale;
            UpdateZoomText();
        }

        private void ResetZoom(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ResetTransform();
        }

        private void UpdateZoomText()
        {
            if (ZoomText is not null)
                ZoomText.Text = $"{(int)(_imageScale.ScaleX * 100)}%";
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);

            CancelImageLoad();
            DataContextChanged -= OnDataContextChanged;

            (ImageControl.Source as IDisposable)?.Dispose();
            ImageControl.Source = null;
        }

        /// <summary>
        /// 按回车确认缩放
        /// </summary>
        private void OnZoomTextKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyZoomFromText();
                // 让焦点回到图片区域，方便继续操作
                Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // ESC 取消输入，恢复原值
                UpdateZoomText();
                Focus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 失去焦点时确认缩放
        /// </summary>
        private void OnZoomTextLostFocus(object? sender, RoutedEventArgs e)
        {
            ApplyZoomFromText();
        }

        /// <summary>
        /// 解析输入并应用缩放
        /// </summary>
        private void ApplyZoomFromText()
        {
            if (ZoomText is null) return;

            var input = ZoomText.Text ?? "100%";

            // 去掉 % 符号和空白
            input = input.Replace("%", "").Trim();

            if (!double.TryParse(input, out var percent) || percent <= 0)
            {
                // 输入无效，恢复原值
                UpdateZoomText();
                return;
            }

            var newScale = Math.Clamp(percent / 100.0, MinScale, MaxScale);

            _imageScale.ScaleX = newScale;
            _imageScale.ScaleY = newScale;

            // 输入合法，刷新显示为规范格式（去小数）
            UpdateZoomText();
        }
    }
}