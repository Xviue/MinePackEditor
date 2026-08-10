using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MinePackEditor.ViewModels
{
    public abstract class DialogViewModelBase : INotifyPropertyChanged
    {
        public event EventHandler? RequestClose;

        protected void Close() => RequestClose?.Invoke(this, EventArgs.Empty);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public abstract class DialogViewModelBase<TResult> : DialogViewModelBase
    {
        public TResult? Result { get; protected set; }
    }
}
