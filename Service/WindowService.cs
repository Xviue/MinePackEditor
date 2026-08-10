using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Service
{
    public class WindowService
    {
        private readonly Window _window;

        public WindowService(Window window)
        {
            _window = window;
        }

        public void Close()
        {
            _window.Close();
        }

        public Window get()
        {
            return _window;
        }
    }
}
