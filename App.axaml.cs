using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using MinePackEditor.Localization;
using MinePackEditor.Service;
using MinePackEditor.ViewModels;
using MinePackEditor.Views;
using System.Threading;

namespace MinePackEditor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 开始加载
            SettingsService.Instance.Load();
            LanguageManager.Instance.Initialize();

            var mainWindow = new MainWindow();

            // Services
            var folderPicker = new FilePickerService(mainWindow);
            var dialogService = new DialogService();
            var windowServive = new WindowService(mainWindow);

            // 注册额外对话框窗口
            dialogService.Register<SaveAllViewModel, SaveAllDialogWindow>();
            dialogService.Register<SettingsWindowViewModel, SettingsWindow>();

            var viewModel = new MainViewModel(folderPicker, dialogService, windowServive);

            mainWindow.DataContext = viewModel;

            desktop.MainWindow = mainWindow;

            desktop.MainWindow.Opened += (_, _) =>
            {
                viewModel.RestoreSession();
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                viewModel.SaveSession();
                // 退出时额外执行保存逻辑
                SettingsService.Instance.Save();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}