using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.ViewModels
{
    public interface ICloseable
    {
        event EventHandler? RequestClose;
    }
}
