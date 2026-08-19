using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using MinePackEditor.Models;
using MinePackEditor.ViewModels;
using System.Linq;

namespace MinePackEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
    }


    private void TitleBar_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
    }

    private void FileTree_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var treeViewItem = (e.Source as Visual)?
            .GetSelfAndVisualAncestors()
            .OfType<TreeViewItem>()
            .FirstOrDefault();

        if (treeViewItem == null) return;

        if (treeViewItem.DataContext is FileSystemNode node)
        {
            if (node.Children == null || !node.Children.Any())
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.OpenTabCommand.Execute(node.FullPath);
                }
                e.Handled = true;
            }
        }
    }
}