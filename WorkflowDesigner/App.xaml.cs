using Prism.Ioc;
using Prism.Unity;
using System;
using System.Windows;
using WorkflowDesigner.UI.Views;
using WorkflowDesigner.UI.ViewModels;

namespace WorkflowDesigner
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            try
            {
                // 最简单的方式：直接创建窗口，不依赖容器
                var mainWindow = new MainWindow();
                var viewModel = new MainWindowViewModel();
                mainWindow.DataContext = viewModel;

                return mainWindow;
            }
            catch (Exception ex)
            {
                // 如果还是失败，创建一个基本窗口
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);

                return new Window
                {
                    Title = "WorkflowDesigner - 错误模式",
                    Width = 800,
                    Height = 600,
                    Content = new System.Windows.Controls.TextBlock
                    {
                        Text = "应用程序在简化模式下运行，部分功能可能不可用。",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16
                    }
                };
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 最基本的注册，只注册必需的类型
            try
            {
                containerRegistry.Register<MainWindow>();
                containerRegistry.Register<MainWindowViewModel>();
                containerRegistry.Register<ToolboxViewModel>();
                containerRegistry.Register<WorkflowDesignerViewModel>();
                containerRegistry.Register<OutputPanelViewModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"服务注册失败: {ex.Message}");
                // 即使注册失败也继续，因为我们在CreateShell中直接创建了实例
            }
        }
    }
}