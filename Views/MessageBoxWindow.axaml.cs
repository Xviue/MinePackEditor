using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinePackEditor.Service;
using System.Threading.Tasks;

namespace MinePackEditor.Views
{
    public partial class MessageBoxWindow : Window
    {
        private DialogResult _result = DialogResult.None;

        public MessageBoxWindow() => InitializeComponent();

        // 统一构造函数：mode 决定显示哪些按钮
        internal MessageBoxWindow(
            string title,
            string message,
            MessageType type,
            string? yes,
            string? no,
            string? cancel)
        {
            InitializeComponent();
            Title = title;
            TitleBlock.Text = title;
            MessageBlock.Text = message;

            string dialogIconPath = "/Assets/DialogIcon/";
            Icon.Path = type switch
            {
                MessageType.Warning => dialogIconPath + "warn.svg",
                MessageType.Error => dialogIconPath + "error.svg",
                _ => dialogIconPath + "info.svg"
            };

            // 配置按钮可见性和文本
            if (yes != null)
            {
                YesBtn.Content = yes;
                YesBtn.IsVisible = true;
                YesBtn.IsDefault = true;  // 回车触发
            }
            else
            {
                YesBtn.IsVisible = false;
            }

            if (no != null)
            {
                NoBtn.Content = no;
                NoBtn.IsVisible = true;
            }
            else
            {
                NoBtn.IsVisible = false;
            }

            if (cancel != null)
            {
                CancelBtn.Content = cancel;
                CancelBtn.IsVisible = true;
            }
            else
            {
                CancelBtn.IsVisible = false;
            }

            // 只有确定按钮的提示框
            if (no == null && cancel == null)
            {
                YesBtn.Content = yes ?? "确定";
                YesBtn.IsDefault = true;
            }
        }

        private void YesBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            _result = DialogResult.OK;
            Close();
        }

        private void NoBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            _result = DialogResult.No;
            Close();
        }

        private void CancelBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            _result = DialogResult.Cancel;
            Close();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (_result == DialogResult.None)
                _result = DialogResult.Cancel;
            base.OnClosing(e);
        }

        public new async Task<DialogResult> ShowDialog(Window owner)
        {
            var tcs = new TaskCompletionSource<DialogResult>();
            Closed += (_, _) => tcs.TrySetResult(_result);
            await base.ShowDialog(owner);
            return await tcs.Task;
        }

        private void Border_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                this.BeginMoveDrag(e);
            }
        }
    }
}