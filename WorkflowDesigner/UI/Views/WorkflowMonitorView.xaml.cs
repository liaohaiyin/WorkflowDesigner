using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Engine;
using WorkflowDesigner.UI.ViewModels;

namespace WorkflowDesigner.UI.Views
{
    public partial class WorkflowMonitorView : UserControl
    {
        private WorkflowMonitorViewModel ViewModel => DataContext as WorkflowMonitorViewModel;
        private IWorkflowEngine _workflowEngine;

        public WorkflowMonitorView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public WorkflowMonitorView(IWorkflowEngine workflowEngine) : this()
        {
            _workflowEngine = workflowEngine;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 如果没有通过构造函数注入，尝试从容器获取
            if (_workflowEngine == null)
            {
                // 这里可以通过依赖注入容器获取服务
                // _workflowEngine = ServiceLocator.GetService<IWorkflowEngine>();
            }
        }

        #region 事件处理

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel?.RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is WorkflowInstance workflow)
            {
                try
                {
                    var detailsWindow = new WorkflowDetailsWindow(workflow);
                    detailsWindow.Owner = Window.GetWindow(this);
                    detailsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"查看详情失败: {ex.Message}", "错误",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is WorkflowInstance workflow)
            {
                try
                {
                    if (_workflowEngine != null)
                    {
                        await _workflowEngine.PauseWorkflowAsync(workflow.Id);
                        await ViewModel?.RefreshAsync();
                        MessageBox.Show("工作流已暂停", "成功",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"暂停工作流失败: {ex.Message}", "错误",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is WorkflowInstance workflow)
            {
                try
                {
                    if (_workflowEngine != null)
                    {
                        await _workflowEngine.ResumeWorkflowAsync(workflow.Id);
                        await ViewModel?.RefreshAsync();
                        MessageBox.Show("工作流已恢复", "成功",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"恢复工作流失败: {ex.Message}", "错误",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void TerminateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is WorkflowInstance workflow)
            {
                var result = MessageBox.Show($"确定要终止工作流 '{workflow.Id}' 吗？\n此操作不可撤销。",
                                           "确认终止", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_workflowEngine != null)
                        {
                            await _workflowEngine.TerminateWorkflowAsync(workflow.Id);
                            await ViewModel?.RefreshAsync();
                            MessageBox.Show("工作流已终止", "成功",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"终止工作流失败: {ex.Message}", "错误",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv|Excel文件 (*.xlsx)|*.xlsx|JSON文件 (*.json)|*.json",
                    Title = "导出工作流数据",
                    FileName = $"WorkflowMonitor_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportWorkflowData(saveFileDialog.FileName);
                    MessageBox.Show($"数据已导出到: {saveFileDialog.FileName}", "导出成功",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FormatJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(WorkflowDataTextBox.Text))
                {
                    var json = JToken.Parse(WorkflowDataTextBox.Text);
                    WorkflowDataTextBox.Text = json.ToString(Formatting.Indented);
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON格式错误: {ex.Message}", "格式化失败",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(WorkflowDataTextBox.Text))
                {
                    Clipboard.SetText(WorkflowDataTextBox.Text);
                    MessageBox.Show("JSON数据已复制到剪贴板", "复制成功",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 辅助方法

        private void ExportWorkflowData(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLower();
            var workflows = ViewModel?.ActiveWorkflows?.ToList();

            if (workflows == null || !workflows.Any())
            {
                throw new InvalidOperationException("没有可导出的数据");
            }

            switch (extension)
            {
                case ".csv":
                    ExportToCsv(fileName, workflows);
                    break;
                case ".json":
                    ExportToJson(fileName, workflows);
                    break;
                case ".xlsx":
                    ExportToExcel(fileName, workflows);
                    break;
                default:
                    throw new NotSupportedException($"不支持的文件格式: {extension}");
            }
        }

        private void ExportToCsv(string fileName, System.Collections.Generic.List<WorkflowInstance> workflows)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("实例ID,工作流名称,状态,当前节点,启动时间,启动人,结束时间");

            foreach (var workflow in workflows)
            {
                csv.AppendLine($"{workflow.Id},{workflow.Definition?.Name},{workflow.Status}," +
                              $"{workflow.CurrentNodeId},{workflow.StartTime:yyyy-MM-dd HH:mm:ss}," +
                              $"{workflow.StartedBy},{workflow.EndTime:yyyy-MM-dd HH:mm:ss}");
            }

            System.IO.File.WriteAllText(fileName, csv.ToString(), System.Text.Encoding.UTF8);
        }

        private void ExportToJson(string fileName, System.Collections.Generic.List<WorkflowInstance> workflows)
        {
            var json = JsonConvert.SerializeObject(workflows, Formatting.Indented);
            System.IO.File.WriteAllText(fileName, json, System.Text.Encoding.UTF8);
        }

        private void ExportToExcel(string fileName, System.Collections.Generic.List<WorkflowInstance> workflows)
        {
            // 这里可以使用EPPlus或其他Excel库来生成Excel文件
            // 为了简化，这里导出为CSV格式
            ExportToCsv(fileName.Replace(".xlsx", ".csv"), workflows);
        }

        #endregion
    }

    // ============================================================================
    // 转换器类
    // ============================================================================

    // 工作流状态到颜色的转换器
    public class WorkflowStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowInstanceStatus status)
            {
                switch (status)
                {
                    case WorkflowInstanceStatus.Running:
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80));    // 绿色

                    case WorkflowInstanceStatus.Paused:
                        return new SolidColorBrush(Color.FromRgb(255, 152, 0));     // 橙色

                    case WorkflowInstanceStatus.Completed:
                        return new SolidColorBrush(Color.FromRgb(33, 150, 243));    // 蓝色

                    case WorkflowInstanceStatus.Failed:
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54));     // 红色

                    case WorkflowInstanceStatus.Terminated:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158));   // 灰色

                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158));   // 默认灰色
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 工作流状态到文本的转换器
    public class WorkflowStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowInstanceStatus status)
            {
                switch (status)
                {
                    case WorkflowInstanceStatus.Running:
                        return "运行中";

                    case WorkflowInstanceStatus.Paused:
                        return "已暂停";

                    case WorkflowInstanceStatus.Completed:
                        return "已完成";

                    case WorkflowInstanceStatus.Failed:
                        return "失败";

                    case WorkflowInstanceStatus.Terminated:
                        return "已终止";

                    default:
                        return "未知";
                }
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
