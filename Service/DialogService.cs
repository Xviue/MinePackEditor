using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MinePackEditor.ViewModels;
using MinePackEditor.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinePackEditor.Service
{
    public class DialogService : IDialogService
    {
        private readonly Dictionary<Type, Type> _vmToWindowMap = new();

        public void Register<TViewModel, TWindow>() where TWindow : Window, new()
        {
            _vmToWindowMap[typeof(TViewModel)] = typeof(TWindow);
        }

        private static Window? GetOwnerWindow()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;
            return desktop.Windows.FirstOrDefault(w => w.IsActive && w.IsVisible) ?? desktop.MainWindow;
        }

        // 提示框（只有确定）
        public async Task ShowMessageAsync(string title, string message, MessageType type = MessageType.Info)
        {
            var owner = GetOwnerWindow();
            if (owner == null) return;

            var dialog = new MessageBoxWindow(title, message, type, "确定", null, null);
            await dialog.ShowDialog(owner);
        }

        // 两态确认（是/否）
        public async Task<bool> ShowConfirmAsync(string title, string message, string yesText = "确定", string noText = "取消")
        {
            var owner = GetOwnerWindow();
            if (owner == null) return false;

            var dialog = new MessageBoxWindow(title, message, MessageType.Info, yesText, noText, null);
            var result = await dialog.ShowDialog(owner);
            return result == DialogResult.OK;
        }

        // 新增：三态确认（是/否/取消）
        public async Task<DialogResult> ShowYesNoCancelAsync(
            string title,
            string message,
            string yesText = "是",
            string noText = "否",
            string cancelText = "取消")
        {
            var owner = GetOwnerWindow();
            if (owner == null) return DialogResult.None;

            var dialog = new MessageBoxWindow(title, message, MessageType.Warning, yesText, noText, cancelText);
            return await dialog.ShowDialog(owner);
        }

        // 自定义弹窗（保持不变）
        public async Task<TResult?> ShowDialogAsync<TViewModel, TResult>(TViewModel viewModel)
            where TViewModel : DialogViewModelBase<TResult>
        {
            if (!_vmToWindowMap.TryGetValue(typeof(TViewModel), out var windowType))
                throw new InvalidOperationException($"未注册 {typeof(TViewModel).Name}");

            var window = (Window)Activator.CreateInstance(windowType)!;
            window.DataContext = viewModel;

            var tcs = new TaskCompletionSource<TResult?>();
            viewModel.RequestClose += (_, _) =>
            {
                tcs.TrySetResult(viewModel.Result);
                window.Close();
            };
            window.Closed += (_, _) => tcs.TrySetResult(viewModel.Result);

            var owner = GetOwnerWindow();
            if (owner != null) await window.ShowDialog(owner);
            else window.Show();

            return await tcs.Task;
        }
    }
}
