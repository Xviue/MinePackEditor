using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Controls.TemplatedControls
{
    public class ChromeButton : Button
    {
        // ========== 状态颜色 ==========
        public static readonly StyledProperty<IBrush?> PointeroverColorProperty =
            AvaloniaProperty.Register<ChromeButton, IBrush?>(nameof(PointeroverColor));

        public IBrush? PointeroverColor
        {
            get => GetValue(PointeroverColorProperty);
            set => SetValue(PointeroverColorProperty, value);
        }

        public static readonly StyledProperty<IBrush?> PressedColorProperty =
            AvaloniaProperty.Register<ChromeButton, IBrush?>(nameof(PressedColor));

        public IBrush? PressedColor
        {
            get => GetValue(PressedColorProperty);
            set => SetValue(PressedColorProperty, value);
        }

        // ========== 提示文本（空/Null/空白时不显示） ==========
        public static readonly StyledProperty<string?> ToolTipTextProperty =
            AvaloniaProperty.Register<ChromeButton, string?>(nameof(ToolTipText));

        public string? ToolTipText
        {
            get => GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }

        // ========== 图标外观 ==========
        public static readonly StyledProperty<Geometry?> IconDataProperty =
            AvaloniaProperty.Register<ChromeButton, Geometry?>(nameof(IconData));

        public Geometry? IconData
        {
            get => GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public static readonly StyledProperty<IBrush?> IconBrushProperty =
            AvaloniaProperty.Register<ChromeButton, IBrush?>(nameof(IconBrush));

        public IBrush? IconBrush
        {
            get => GetValue(IconBrushProperty);
            set => SetValue(IconBrushProperty, value);
        }

        public static readonly StyledProperty<double> IconThicknessProperty =
            AvaloniaProperty.Register<ChromeButton, double>(
                nameof(IconThickness),
                defaultValue: 1.5);

        public double IconThickness
        {
            get => GetValue(IconThicknessProperty);
            set => SetValue(IconThicknessProperty, value);
        }

        public static readonly StyledProperty<double> IconSizeProperty =
            AvaloniaProperty.Register<ChromeButton, double>(
                nameof(IconSize),
                defaultValue: 16.0);

        public double IconSize
        {
            get => GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        static ChromeButton()
        {
            ToolTipTextProperty.Changed.AddClassHandler<ChromeButton>((btn, e) =>
            {
                var text = e.NewValue as string;
                btn.SetValue(ToolTip.TipProperty,
                    string.IsNullOrWhiteSpace(text) ? null : text);
            });
        }
    }
}
