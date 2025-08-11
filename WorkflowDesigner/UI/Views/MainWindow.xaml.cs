using System;
using System.Windows;
using Prism.Ioc;
using WorkflowDesigner.UI.ViewModels;
using AvalonDock.Layout.Serialization;

namespace WorkflowDesigner.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MainWindow初始化失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 支持依赖注入的构造函数
        public MainWindow(MainWindowViewModel viewModel) : this()
        {
            try
            {
                DataContext = viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置DataContext失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    Title = "工作流可视化设计器 - 已加载";
                    viewModel.StatusMessage = "主窗口已加载完成";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded异常: {ex.Message}");
            }
        }
    }
}