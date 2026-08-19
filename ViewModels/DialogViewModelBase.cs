using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MinePackEditor.ViewModels
{
    public abstract partial class DialogViewModelBase : ObservableObject, ICloseable
    {
        public event EventHandler? RequestClose;
        protected void Close() => RequestClose?.Invoke(this, EventArgs.Empty);
    }

    public abstract partial class DialogViewModelBase<TResult> : DialogViewModelBase, IResultDialog<TResult>
    {
        public TResult? Result { get; protected set; }
    }
}
