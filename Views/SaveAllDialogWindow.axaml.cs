using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MinePackEditor.Service;
using MinePackEditor.ViewModels;
using System.Threading.Tasks;

namespace MinePackEditor.Views
{
    public partial class SaveAllDialogWindow : Window
    {
        public SaveAllDialogWindow() => InitializeComponent();

        private void YesBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SaveAllViewModel vm) vm.ConfirmSave();
        }

        private void NoBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SaveAllViewModel vm) vm.ConfirmDontSave();
        }

        private void CancelBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SaveAllViewModel vm) vm.ConfirmCancel();
        }

        // 右键菜单
        private void CtxInclude_OnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { DataContext: SaveFileItem item }) item.IsIncluded = true;
        }

        private void CtxExclude_OnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { DataContext: SaveFileItem item }) item.IsIncluded = false;
        }

        private void CtxIncludeAll_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SaveAllViewModel vm)
                foreach (var f in vm.Files) f.IsIncluded = true;
        }

        private void CtxExcludeAll_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is SaveAllViewModel vm)
                foreach (var f in vm.Files) f.IsIncluded = false;
        }

        private void Border_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                this.BeginMoveDrag(e);
            }
        }
    }
}