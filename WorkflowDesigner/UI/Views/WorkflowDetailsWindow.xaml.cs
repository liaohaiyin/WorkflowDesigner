using System;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WorkflowDesigner.Core.Models;

namespace WorkflowDesigner.UI.Views
{
    public partial class WorkflowDetailsWindow : Window
    {
        public WorkflowInstance WorkflowInstance { get; }

        public WorkflowDetailsWindow(WorkflowInstance workflowInstance)
        {
            InitializeComponent();
            WorkflowInstance = workflowInstance;
            DataContext = workflowInstance;

            Title = $"工作流详情 - {workflowInstance.Definition?.Name}";
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(DataTextBox.Text))
                {
                    var json = JToken.Parse(DataTextBox.Text);
                    DataTextBox.Text = json.ToString(Formatting.Indented);
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON格式错误: {ex.Message}", "格式化失败",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(DataTextBox.Text))
                {
                    Clipboard.SetText(DataTextBox.Text);
                    MessageBox.Show("数据已复制到剪贴板", "复制成功",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON文件 (*.json)|*.json|文本文件 (*.txt)|*.txt",
                    Title = "保存工作流数据",
                    FileName = $"WorkflowData_{WorkflowInstance.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveFileDialog.FileName, DataTextBox.Text);
                    MessageBox.Show($"数据已保存到: {saveFileDialog.FileName}", "保存成功",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // 这里可以重新加载工作流数据
            MessageBox.Show("数据已刷新", "刷新", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}