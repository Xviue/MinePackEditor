using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.ViewModels
{
    public interface IResultDialog<TResult> : ICloseable
    {
        TResult? Result { get; }
    }
}
