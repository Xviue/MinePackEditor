using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using MinePackEditor.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinePackEditor.Controls.TemplatedControls
{
    public enum StackDirection
    {
        BottomToTop,
        TopToBottom
    }

    public class SlideTipBar : TemplatedControl
    {
        private Canvas? _host;
        private readonly List<SlideTip> _activeTips = new();
        private readonly Dictionary<SlideTip, TranslateTransform> _layoutTransforms = new();
        private readonly Dictionary<SlideTip, CancellationTokenSource> _removeCts = new();

        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<SlideTipBar, double>(nameof(Spacing), 8.0);

        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public static readonly StyledProperty<Easing> LayoutEasingProperty =
            AvaloniaProperty.Register<SlideTipBar, Easing>(nameof(LayoutEasing), new CubicEaseOut());

        public Easing LayoutEasing
        {
            get => GetValue(LayoutEasingProperty);
            set => SetValue(LayoutEasingProperty, value);
        }

        public static readonly StyledProperty<TimeSpan> LayoutDurationProperty =
            AvaloniaProperty.Register<SlideTipBar, TimeSpan>(nameof(LayoutDuration), TimeSpan.FromMilliseconds(300));

        public TimeSpan LayoutDuration
        {
            get => GetValue(LayoutDurationProperty);
            set => SetValue(LayoutDurationProperty, value);
        }

        public static readonly StyledProperty<StackDirection> StackDirectionProperty =
        AvaloniaProperty.Register<SlideTipBar, StackDirection>(nameof(StackDirection), StackDirection.BottomToTop);

        public StackDirection StackDirection
        {
            get => GetValue(StackDirectionProperty);
            set => SetValue(StackDirectionProperty, value);
        }

        [Content]
        public AvaloniaList<SlideTip> Items { get; } = new AvaloniaList<SlideTip>();

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _host = e.NameScope.Find<Canvas>("PART_Host");

            // 模板就绪后，将已有的 Items 加入视觉树
            if (_host != null)
            {
                foreach (var tip in Items)
                {
                    if (!_host.Children.Contains(tip))
                    {
                        _host.Children.Add(tip);
                        tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    }
                }
            }

            // 注册到管理器
            if (!string.IsNullOrEmpty(BarId))
                SlideTipBarManager.Register(BarId, this);
        }

        public static readonly StyledProperty<string?> BarIdProperty =
    AvaloniaProperty.Register<SlideTipBar, string?>(nameof(BarId));

        public string? BarId
        {
            get => GetValue(BarIdProperty);
            set => SetValue(BarIdProperty, value);
        }

        public SlideTipBar()
        {
            Items.CollectionChanged += OnItemsChanged;
        }

        // 添加生命周期方法（若 Avalonia 12 有 OnLoaded/OnUnloaded，否则用 Loaded/Unloaded 事件）
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (!string.IsNullOrEmpty(BarId))
                SlideTipBarManager.Register(BarId, this);
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            if (!string.IsNullOrEmpty(BarId))
                SlideTipBarManager.Unregister(BarId);
        }

        private void OnItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_host == null) return; // 关键：模板未就绪时直接跳过

            if (e.NewItems != null)
            {
                foreach (SlideTip tip in e.NewItems)
                {
                    if (!_host.Children.Contains(tip))
                    {
                        _host.Children.Add(tip);
                        tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (SlideTip tip in e.OldItems)
                {
                    if (_host.Children.Contains(tip))
                        _host.Children.Remove(tip);
                }
            }
        }

        // Actions

        /// <summary>通过 ID 激活已注册的 SlideTip</summary>
        public void Activate(string id)
        {
            var tip = Items.FirstOrDefault(t => t.Id == id);
            if (tip == null)
            {
                System.Diagnostics.Debug.WriteLine($"SlideTipBar: Tip '{id}' not found in Items.");
                return;
            }
            ActivateTip(tip);
        }

        /// <summary>直接激活指定的 SlideTip 实例</summary>
        public void Activate(SlideTip tip)
        {
            ActivateTip(tip);
        }

        private void ActivateTip(SlideTip tip)
        {
            if (_host == null) return;

            if (_removeCts.TryGetValue(tip, out var oldCts))
            {
                oldCts.Cancel();
                _removeCts.Remove(tip);
            }

            bool wasActive = _activeTips.Contains(tip);

            if (wasActive)
            {
                // 列表末尾即堆叠起点（无论 BottomToTop 还是 TopToBottom，末尾都是最新且位于 Y=0）
                bool isAtStackOrigin = _activeTips.Count > 0
                    && _activeTips[_activeTips.Count - 1] == tip;

                if (isAtStackOrigin)
                {
                    tip.Bump();
                    tip.Refresh();
                    return;
                }

                _activeTips.Remove(tip);
            }

            if (!_host.Children.Contains(tip))
            {
                _host.Children.Add(tip);
                tip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            GetOrCreateLayoutTransform(tip);
            _activeTips.Add(tip);
            UpdateLayout();

            Dispatcher.UIThread.Post(() => tip.Show(), DispatcherPriority.Render);

            tip.Closed -= OnTipClosed;
            tip.Closed += OnTipClosed;
        }

        private void OnTipClosed(object? sender, EventArgs e)
        {
            if (sender is not SlideTip tip) return;
            tip.Closed -= OnTipClosed;

            var cts = new CancellationTokenSource();
            _removeCts[tip] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(tip.ExitDuration, cts.Token);
                    if (!cts.Token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _removeCts.Remove(tip);
                            // 关键：只有当前仍关闭时才移除，防止竞争条件
                            if (!tip.IsOpen)
                                RemoveTip(tip);
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    _removeCts.Remove(tip);
                }
            });
        }

        private void RemoveTip(SlideTip tip)
        {
            if (!_activeTips.Contains(tip)) return;
            _activeTips.Remove(tip);

            if (_layoutTransforms.TryGetValue(tip, out var yt))
            {
                if (tip.RenderTransform is TransformGroup tg)
                    tg.Children.Remove(yt);
                _layoutTransforms.Remove(tip);
            }

            if (_host?.Children.Contains(tip) == true)
                _host.Children.Remove(tip);

            UpdateLayout();
        }

        /// <summary>
        /// 堆叠布局：从列表末尾（最新）向开头遍历。
        /// BottomToTop：Y 从 0 开始递减（向上推）
        /// TopToBottom：Y 从 0 开始递增（向下推）
        /// </summary>
        private void UpdateLayout()
        {
            if (_host == null) return;

            double currentY = 0;
            bool bottomToTop = StackDirection == StackDirection.BottomToTop;

            for (int i = _activeTips.Count - 1; i >= 0; i--)
            {
                var tip = _activeTips[i];
                if (!_layoutTransforms.TryGetValue(tip, out var yTransform)) continue;

                yTransform.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = TranslateTransform.YProperty,
                    Duration = LayoutDuration,
                    Easing = LayoutEasing
                }
            };

                yTransform.Y = currentY;

                double height = tip.DesiredSize.Height > 0 ? tip.DesiredSize.Height : tip.Bounds.Height;
                if (height <= 0) height = 30;

                if (bottomToTop)
                    currentY -= height + Spacing;
                else
                    currentY += height + Spacing;
            }
        }

        private TranslateTransform GetOrCreateLayoutTransform(SlideTip tip)
        {
            if (_layoutTransforms.TryGetValue(tip, out var existing))
                return existing;

            var yt = new TranslateTransform();
            _layoutTransforms[tip] = yt;

            if (tip.RenderTransform is TransformGroup tg)
            {
                tg.Children.Insert(0, yt);
            }
            else
            {
                var group = new TransformGroup();
                // ITransform 在 Avalonia 12 中实际运行时均为 Transform 子类，安全转换
                if (tip.RenderTransform is Transform existing1)
                    group.Children.Add(existing1);
                group.Children.Add(yt);
                tip.RenderTransform = group;
            }

            return yt;
        }

        private void ResetToHidden(SlideTip tip)
        {
            var isHorizontal = tip.SlideDirection == SlideDirection.Left || tip.SlideDirection == SlideDirection.Right;
            var isPositive = tip.SlideDirection == SlideDirection.Right || tip.SlideDirection == SlideDirection.Down;

            double size = isHorizontal
                ? (tip.DesiredSize.Width > 0 ? tip.DesiredSize.Width : tip.Bounds.Width) + 50
                : (tip.DesiredSize.Height > 0 ? tip.DesiredSize.Height : tip.Bounds.Height) + 50;

            if (!isPositive) size = -size;

            if (tip.RenderTransform is not TransformGroup tg) return;

            foreach (var t in tg.Children.OfType<TranslateTransform>())
            {
                if (_layoutTransforms.Values.Contains(t)) continue;

                var saved = t.Transitions;
                t.Transitions = null;

                if (isHorizontal) t.X = size;
                else t.Y = size;

                t.Transitions = saved;
                return;
            }
        }
    }
}
