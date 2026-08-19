using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinePackEditor.Controls.TemplatedControls
{
    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public class SlideTip : TemplatedControl
    {
        private Border? _rootBorder;
        private TranslateTransform? _motionTransform; // SlideTip 自己控制的位移
        private CancellationTokenSource? _autoCloseCts;

        // ========== ID ==========
        public static readonly StyledProperty<string?> IdProperty =
            AvaloniaProperty.Register<SlideTip, string?>(nameof(Id));

        public string? Id
        {
            get => GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        // ========== 显示控制 ==========
        public static readonly StyledProperty<bool> IsOpenProperty =
            AvaloniaProperty.Register<SlideTip, bool>(nameof(IsOpen));

        public bool IsOpen
        {
            get => GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        // ========== 内容 ==========
        public static readonly StyledProperty<object?> ContentProperty =
            ContentControl.ContentProperty.AddOwner<SlideTip>();

        [Content]
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
            ContentControl.ContentTemplateProperty.AddOwner<SlideTip>();

        public IDataTemplate? ContentTemplate
        {
            get => GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }

        // ========== 停留时间 ==========
        public static readonly StyledProperty<TimeSpan> DurationProperty =
            AvaloniaProperty.Register<SlideTip, TimeSpan>(nameof(Duration), TimeSpan.FromSeconds(3));

        public TimeSpan Duration
        {
            get => GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        // ========== 动画时间 ==========
        public static readonly StyledProperty<TimeSpan> EnterDurationProperty =
            AvaloniaProperty.Register<SlideTip, TimeSpan>(nameof(EnterDuration), TimeSpan.FromMilliseconds(350));

        public TimeSpan EnterDuration
        {
            get => GetValue(EnterDurationProperty);
            set => SetValue(EnterDurationProperty, value);
        }

        public static readonly StyledProperty<TimeSpan> ExitDurationProperty =
            AvaloniaProperty.Register<SlideTip, TimeSpan>(nameof(ExitDuration), TimeSpan.FromMilliseconds(250));

        public TimeSpan ExitDuration
        {
            get => GetValue(ExitDurationProperty);
            set => SetValue(ExitDurationProperty, value);
        }

        // ========== 动画函数 ==========
        public static readonly StyledProperty<Easing> EnterEasingProperty =
            AvaloniaProperty.Register<SlideTip, Easing>(nameof(EnterEasing), new CubicEaseOut());

        public Easing EnterEasing
        {
            get => GetValue(EnterEasingProperty);
            set => SetValue(EnterEasingProperty, value);
        }

        public static readonly StyledProperty<Easing> ExitEasingProperty =
            AvaloniaProperty.Register<SlideTip, Easing>(nameof(ExitEasing), new CubicEaseIn());

        public Easing ExitEasing
        {
            get => GetValue(ExitEasingProperty);
            set => SetValue(ExitEasingProperty, value);
        }

        // ========== 滑动方向 ==========
        public static readonly StyledProperty<SlideDirection> SlideDirectionProperty =
            AvaloniaProperty.Register<SlideTip, SlideDirection>(nameof(SlideDirection), SlideDirection.Left);

        public SlideDirection SlideDirection
        {
            get => GetValue(SlideDirectionProperty);
            set => SetValue(SlideDirectionProperty, value);
        }

        // ========== 事件 ==========
        public event EventHandler? Opened;
        public event EventHandler? Closed;

        public SlideTip()
        {
            // 强制初始化为 TransformGroup，确保后续所有 Transform 操作类型安全
            RenderTransform = new TransformGroup();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _rootBorder = e.NameScope.Find<Border>("PART_Root");
            EnsureMotionTransform();
            ApplyOffset(animate: false);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsOpenProperty)
            {
                ApplyOffset(animate: _rootBorder != null);
                if (IsOpen)
                {
                    Opened?.Invoke(this, EventArgs.Empty);
                    if (Duration > TimeSpan.Zero)
                        _ = StartAutoCloseAsync();
                }
                else
                {
                    Closed?.Invoke(this, EventArgs.Empty);
                    CancelAutoClose();
                }
            }
            else if (change.Property == BoundsProperty && _rootBorder != null && !IsOpen)
            {
                ApplyOffset(animate: false);
            }
        }

        /// <summary>在 UI 线程直接激活提示</summary>
        public void Activate()
        {
            if (!IsOpen)
                IsOpen = true;
        }

        public void Show()
        {
            if (IsOpen)
            {
                // 已打开：强制重置并重新播放
                var savedExit = ExitDuration;
                ExitDuration = TimeSpan.Zero;
                IsOpen = false;
                ExitDuration = savedExit;

                Dispatcher.UIThread.Post(() =>
                {
                    // 强制同步重置到隐藏位置（无动画）
                    ResetMotionToHidden();
                    IsOpen = true;
                }, DispatcherPriority.Render);
            }
            else
            {
                // 关键：先强制重置到隐藏位置，再打开，确保动画从屏幕外开始
                ResetMotionToHidden();
                IsOpen = true;
            }
        }

        /// <summary>
        /// 对当前已显示的 Tip 执行抖动提示，不影响滑入滑出位置。
        /// 振幅递减，总耗时约 250ms。
        /// </summary>
        public async void Nudge()
        {
            if (_motionTransform == null) return;

            // 暂存并关闭过渡，确保抖动是瞬移而非缓动
            var savedTransitions = _motionTransform.Transitions;
            _motionTransform.Transitions = null;

            bool isHorizontal = SlideDirection == SlideDirection.Left || SlideDirection == SlideDirection.Right;
            double baseValue = isHorizontal ? _motionTransform.X : _motionTransform.Y;

            // 振幅递减序列：6, -6, 4, -4, 2, -2, 0
            foreach (var offset in new[] { 6.0, -6.0, 4.0, -4.0, 2.0, -2.0, 0.0 })
            {
                if (isHorizontal)
                    _motionTransform.X = baseValue + offset;
                else
                    _motionTransform.Y = baseValue + offset;

                await Task.Delay(35);
            }

            _motionTransform.Transitions = savedTransitions;
        }

        /// <summary>
        /// 已显示状态下的回弹提示：向屏幕外方向回退约 30px(50ms)，再弹性滑回原位(300ms)。
        /// 不干扰滑入滑出的主过渡配置。
        /// 该动效暂时未使用
        /// </summary>
        public async void Bump()
        {
            if (_motionTransform == null) return;

            bool isHorizontal = SlideDirection == SlideDirection.Left || SlideDirection == SlideDirection.Right;
            bool fromNegative = SlideDirection == SlideDirection.Left || SlideDirection == SlideDirection.Up;
            double retreat = fromNegative ? -30 : 30; // 向屏幕外回退的距离

            var saved = _motionTransform.Transitions;

            // 阶段1：快速回退一点点（CubicEaseIn 制造刹车感）
            _motionTransform.Transitions = new Transitions
    {
        new DoubleTransition
        {
            Property = isHorizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty,
            Duration = TimeSpan.FromMilliseconds(50),
            Easing = new CubicEaseIn()
        }
    };

            if (isHorizontal) _motionTransform.X = retreat;
            else _motionTransform.Y = retreat;

            await Task.Delay(50);

            // 阶段2：弹性回到原位（CubicEaseOut 制造柔和吸附感）
            _motionTransform.Transitions = new Transitions
    {
        new DoubleTransition
        {
            Property = isHorizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty,
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new CubicEaseOut()
        }
    };

            if (isHorizontal) _motionTransform.X = 0;
            else _motionTransform.Y = 0;

            await Task.Delay(300);

            // 恢复主过渡配置
            _motionTransform.Transitions = saved;
        }

        /// <summary>
        /// 刷新显示时间：保持当前打开状态，重置自动关闭倒计时。
        /// 适用于已显示状态下再次触发，避免旧计时器提前关闭 Tip。
        /// </summary>
        public void Refresh()
        {
            if (!IsOpen || Duration <= TimeSpan.Zero) return;
            _ = StartAutoCloseAsync();
        }

        /// <summary>将 MotionTransform 强制重置到屏幕外（无动画）</summary>
        private void ResetMotionToHidden()
        {
            EnsureMotionTransform();
            if (_motionTransform == null) return;

            var isHorizontal = SlideDirection == SlideDirection.Left || SlideDirection == SlideDirection.Right;
            var isPositive = SlideDirection == SlideDirection.Right || SlideDirection == SlideDirection.Down;

            double size = isHorizontal
                ? Math.Max(DesiredSize.Width, _rootBorder?.DesiredSize.Width ?? 0) + 50
                : Math.Max(DesiredSize.Height, _rootBorder?.DesiredSize.Height ?? 0) + 50;
            if (size <= 50) size = 250;

            if (!isPositive) size = -size;

            var saved = _motionTransform.Transitions;
            _motionTransform.Transitions = null;

            if (isHorizontal) _motionTransform.X = size;
            else _motionTransform.Y = size;

            _motionTransform.Transitions = saved;
        }

        /// <summary>在 UI 线程直接关闭提示</summary>
        public void Deactivate()
        {
            if (IsOpen)
                IsOpen = false;
        }

        private void EnsureMotionTransform()
        {
            if (_motionTransform != null) return;
            if (RenderTransform is not TransformGroup tg)
            {
                // 防御：若外部代码覆盖了 RenderTransform，重新包装为 TransformGroup
                var group = new TransformGroup();
                if (RenderTransform is Transform existing)
                    group.Children.Add(existing);
                RenderTransform = group;
                tg = group;
            }

            _motionTransform = new TranslateTransform();
            tg.Children.Add(_motionTransform);
        }

        private void ApplyOffset(bool animate)
        {
            EnsureMotionTransform();
            if (_motionTransform == null) return;

            var isHorizontal = SlideDirection == SlideDirection.Left || SlideDirection == SlideDirection.Right;
            var isPositive = SlideDirection == SlideDirection.Right || SlideDirection == SlideDirection.Down;

            double offset;
            if (IsOpen)
            {
                offset = 0;
            }
            else
            {
                double size = isHorizontal ? DesiredSize.Width : DesiredSize.Height;
                if (size <= 0 && _rootBorder != null)
                    size = isHorizontal ? _rootBorder.DesiredSize.Width : _rootBorder.DesiredSize.Height;
                if (size <= 0) size = 200;

                offset = size + 50;
                if (!isPositive) offset = -offset;
            }

            if (!animate)
            {
                var saved = _motionTransform.Transitions;
                _motionTransform.Transitions = null;

                if (isHorizontal)
                    _motionTransform.X = offset;
                else
                    _motionTransform.Y = offset;

                _motionTransform.Transitions = saved;
            }
            else
            {
                _motionTransform.Transitions = new Transitions
                {
                    new DoubleTransition
                    {
                        Property = isHorizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty,
                        Duration = IsOpen ? EnterDuration : ExitDuration,
                        Easing = IsOpen ? EnterEasing : ExitEasing
                    }
                };

                if (isHorizontal)
                    _motionTransform.X = offset;
                else
                    _motionTransform.Y = offset;
            }
        }

        private async Task StartAutoCloseAsync()
        {
            CancelAutoClose();
            var cts = new CancellationTokenSource();
            _autoCloseCts = cts;

            try
            {
                await Task.Delay(Duration, cts.Token);
                if (!cts.Token.IsCancellationRequested)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (IsOpen) IsOpen = false;
                    });
                }
            }
            catch (OperationCanceledException) { }
        }

        private void CancelAutoClose()
        {
            _autoCloseCts?.Cancel();
            _autoCloseCts?.Dispose();
            _autoCloseCts = null;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            CancelAutoClose();
        }
    }
}
