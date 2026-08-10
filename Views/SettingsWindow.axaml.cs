using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinePackEditor.ViewModels;

namespace MinePackEditor.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            var vm = new SettingsWindowViewModel();
            vm.RequestClose += (_, _) => Close();
            DataContext = vm;
        }

        private void Darg(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                this.BeginMoveDrag(e);
            }
        }
    }
}