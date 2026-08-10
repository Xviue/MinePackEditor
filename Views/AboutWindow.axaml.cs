using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinePackEditor.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LicenseBox.Text = "The MIT License (MIT) \n Copyright © 2026 <copyright holders> \n Permission is hereby granted, free of charge, " +
                "to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, " +
                "including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, " +
                "and to permit persons to whom the Software is furnished to do so, subject to the following conditions:" +
                "\n   The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software." +
                "\n   THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
                "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, " +
                "WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";

            SupportsBox.Text =
                "MIT License 组件 - \r\n以下组件均遵循 MIT 许可证（The MIT License）：" + "\n" +
                "• Avalonia\r\n      Copyright (c) Avalonia UI Team\r\n      许可证: MIT\r\n      源码: https://github.com/AvaloniaUI/Avalonia\r\n      用途: 跨平台 UI 框架" + "\n" +
                "• Avalonia.Desktop\r\n      Copyright (c) Avalonia UI Team\r\n      许可证: MIT\r\n      源码: https://github.com/AvaloniaUI/Avalonia\r\n      用途: 桌面平台支持" + "\n" +
                "• Avalonia.Fonts.Inter\r\n      Copyright (c) Avalonia UI Team\r\n      许可证: MIT\r\n      源码: https://github.com/AvaloniaUI/Avalonia\r\n      用途: Inter 字体集成" + "\n" +
                "• Avalonia.Themes.Fluent\r\n      Copyright (c) Avalonia UI Team\r\n      许可证: MIT\r\n      源码: https://github.com/AvaloniaUI/Avalonia\r\n      用途: Fluent 设计主题" + "\n" +
                "• Avalonia.AvaloniaEdit\r\n      Copyright (c) Avalonia UI Team\r\n      许可证: MIT\r\n      源码: https://github.com/AvaloniaUI/AvaloniaEdit\r\n      用途: 代码编辑器控件" + "\n" +
                "• AvaloniaEdit.TextMate\r\n      Copyright (c) Avalonia UI Team\r\n      许可证: MIT\r\n      源码: https://github.com/AvaloniaUI/AvaloniaEdit\r\n      用途: TextMate 语法高亮支持" + "\n" +
                "• CommunityToolkit.Mvvm\r\n      Copyright (c) Microsoft Corporation\r\n      许可证: MIT\r\n      源码: https://github.com/CommunityToolkit/dotnet\r\n      用途: MVVM 工具包" + "\n" +
                "• FluentAvaloniaUI\r\n      Copyright (c) amwx / FluentAvalonia Contributors\r\n      许可证: MIT\r\n      源码: https://github.com/amwx/FluentAvalonia\r\n      用途: Fluent Design 控件库" + "\n" +
                "• Svg.Controls.Skia.Avalonia (Svg.Skia)\r\n      Copyright (c) Wiesław Šoltés\r\n      许可证: MIT\r\n      源码: https://github.com/wieslawsoltes/Svg.Skia\r\n      用途: SVG 渲染支持\r\n      注: 部分代码改编自 vvvv/SVG 项目。" + "\n" +
                "• TextMateSharp\r\n      Copyright (c) Daniel Penalba\r\n      许可证: MIT\r\n      源码: https://github.com/danipen/TextMateSharp\r\n      用途: 语法高亮引擎" + "\n" +
                "• TextMateSharp.Grammars (语法文件)\r\n      各语法文件版权归属其原始作者\r\n      主要来源: microsoft/vscode-textmate 及相关语法仓库\r\n      许可证: 主要为 MIT\r\n      用途: 各编程语言的语法定义文件" + "\n" +
                "• AvaloniaUI.DiagnosticsSupport\r\n      Copyright (c) Avalonia UI Team\r\n      许可证: 集成支持包，免费使用，属 Avalonia 开发者工具生态的一部分\r\n      说明: https://www.nuget.org/packages/AvaloniaUI.DiagnosticsSupport\r\n      用途: 开发者工具连接支持\r\n      注: 本包为 Avalonia 开发者工具的集成桥接包，可在项目中自由引用。\r\n          实际使用开发者工具功能可能需要 Avalonia Portal 账户。" + "\n" +
                "Apache License 2.0 组件" + "\n" +
                "• IconPark 图标库\r\n      Copyright (c) 字节跳动 / 北京巨量引擎网络技术有限公司\r\n      许可证: Apache License 2.0\r\n      来源: https://iconpark.oceanengine.com\r\n      用途: 用户界面图标\r\n      说明: IconPark 图标来自字节跳动官方图标库，遵循 Apache 2.0 协议开源。\r\n            图标可在商业项目中免费使用，需保留版权声明。" + "\n" +
                "SIL Open Font License 1.1 字体 - 以下字体均遵循 SIL 开放字体许可证 1.1（SIL Open Font License 1.1）：" + "\n" +
                "• MiSans\r\n      Copyright (c) 小米集团\r\n      许可证: SIL Open Font License 1.1\r\n      来源: https://hyperos.mi.com/font/\r\n      用途: 界面中文字体\r\n      说明: 全球免费商用，可嵌入软件分发。\r\n            字体文件本身不得单独销售。" + "\n" +
                "• JetBrains Mono\r\n      Copyright (c) JetBrains s.r.o.\r\n      许可证: SIL Open Font License 1.1\r\n      来源: https://www.jetbrains.com/lp/mono/\r\n      用途: 代码等宽字体\r\n      说明: 可自由用于个人和商业项目，可嵌入软件。\r\n            字体文件本身不得单独销售。" + "\n" +
                "• Inter\r\n      Copyright (c) 2016-2020 Rasmus Andersson\r\n      许可证: SIL Open Font License 1.1\r\n      来源: https://rsms.me/inter/\r\n      用途: 界面西文字体\r\n      说明: Google Fonts 收录字体，可自由用于个人和商业项目，可嵌入软件。\r\n            字体文件本身不得单独销售。" + "\n";
        }

        private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}