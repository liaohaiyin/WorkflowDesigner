using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WorkflowDesigner.UI.Views
{
    public partial class WorkflowStartDialog : Window
    {
        public string StartedBy { get; private set; }
        public string Priority { get; private set; }
        public string Remarks { get; private set; }
        public Dictionary<string, object> InitialData { get; private set; }

        public WorkflowStartDialog()
        {
            InitializeComponent();
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // 设置默认值
            StartedByTextBox.Text = Environment.UserName;
            PriorityComboBox.SelectedIndex = 1; // 普通优先级

            // 设置默认初始数据
            var defaultData = new
            {
                Amount = 1000,
                Applicant = "张三",
                Department = "技术部",
                RequestDate = DateTime.Now.ToString("yyyy-MM-dd"),
                Description = "工作流申请"
            };

            InitialDataTextBox.Text = JsonConvert.SerializeObject(defaultData, Formatting.Indented);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证启动人
                StartedBy = StartedByTextBox.Text.Trim();
                if (string.IsNullOrEmpty(StartedBy))
                {
                    MessageBox.Show("请输入启动人", "验证失败",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    StartedByTextBox.Focus();
                    return;
                }

                // 获取优先级
                var selectedPriority = PriorityComboBox.SelectedItem as ComboBoxItem;
                Priority = selectedPriority?.Tag?.ToString() ?? "Normal";

                // 获取备注
                Remarks = RemarksTextBox.Text.Trim();

                // 验证和解析JSON数据
                var dataJson = InitialDataTextBox.Text.Trim();
                if (string.IsNullOrEmpty(dataJson))
                {
                    InitialData = new Dictionary<string, object>();
                }
                else
                {
                    // 验证JSON格式
                    try
                    {
                        JToken.Parse(dataJson); // 验证JSON格式
                        InitialData = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataJson);
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"初始数据JSON格式错误:\n{ex.Message}", "格式错误",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                        InitialDataTextBox.Focus();
                        return;
                    }
                }

                // 添加系统数据
                InitialData["StartedBy"] = StartedBy;
                InitialData["StartTime"] = DateTime.Now;
                InitialData["Priority"] = Priority;
                InitialData["Remarks"] = Remarks;
                InitialData["InstanceId"] = Guid.NewGuid().ToString();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void FormatJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var json = InitialDataTextBox.Text.Trim();
                if (string.IsNullOrEmpty(json)) return;

                var parsed = JToken.Parse(json);
                var formatted = parsed.ToString(Formatting.Indented);
                InitialDataTextBox.Text = formatted;

                MessageBox.Show("JSON格式化完成", "成功",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON格式错误:\n{ex.Message}", "格式化失败",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var json = InitialDataTextBox.Text.Trim();
                if (string.IsNullOrEmpty(json))
                {
                    MessageBox.Show("请输入JSON数据", "验证",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                JToken.Parse(json); // 验证JSON格式
                MessageBox.Show("JSON格式验证通过", "验证成功",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON格式错误:\n{ex.Message}", "验证失败",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}