using MinePackEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MinePackEditor.Service
{
    public enum DialogResult
    {
        None,   // 用户直接点 X 关闭，或异常
        OK,     // 确定 / 是
        Cancel, // 取消
        No      // 否
    }

    public enum MessageType { Info, Warning, Error }

    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message, MessageType type = MessageType.Info);

        // 原来的两态确认（保留，不破坏已有代码）
        Task<bool> ShowConfirmAsync(string title, string message, string yesText = "确定", string noText = "取消");

        // 新增：三态确认（是 / 否 / 取消）
        Task<DialogResult> ShowYesNoCancelAsync(
            string title,
            string message,
            string yesText = "是",
            string noText = "否",
            string cancelText = "取消");

        Task<TResult?> ShowDialogAsync<TViewModel, TResult>(TViewModel viewModel)
            where TViewModel : DialogViewModelBase<TResult>;
    }
}
