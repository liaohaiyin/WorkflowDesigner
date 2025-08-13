using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.UI.Converters;

namespace WorkflowDesigner.UI.Views
{
    /// <summary>
    /// WorkflowSelectionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class WorkflowSelectionDialog : Window, INotifyPropertyChanged
    {
        private readonly List<WorkflowDefinition> _allWorkflows;
        private string _searchText = "";
        private string _categoryFilter = "All";
        private WorkflowDefinition _selectedWorkflow;

        public WorkflowSelectionDialog(List<WorkflowDefinition> workflows)
        {
            InitializeComponent();
            _allWorkflows = workflows ?? new List<WorkflowDefinition>();

            InitializeData();
            DataContext = this;

            // 注册转换器
            RegisterConverters();
        }

        #region 属性

        public ObservableCollection<WorkflowDefinition> FilteredWorkflows { get; private set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilter();
            }
        }

        public string CategoryFilter
        {
            get => _categoryFilter;
            set
            {
                SetProperty(ref _categoryFilter, value);
                ApplyFilter();
            }
        }

        public WorkflowDefinition SelectedWorkflow
        {
            get => _selectedWorkflow;
            set
            {
                SetProperty(ref _selectedWorkflow, value);
                OnPropertyChanged(nameof(HasSelectedWorkflow));
            }
        }

        public bool HasSelectedWorkflow => SelectedWorkflow != null;

        #endregion

        #region 初始化

        private void InitializeData()
        {
            FilteredWorkflows = new ObservableCollection<WorkflowDefinition>();

            // 填充类别筛选器
            var categories = _allWorkflows
                .Where(w => !string.IsNullOrEmpty(w.Category))
                .Select(w => w.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            CategoryFilterComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = "全部类别",
                Tag = "All"
            });

            foreach (var category in categories)
            {
                CategoryFilterComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem
                {
                    Content = category,
                    Tag = category
                });
            }

            CategoryFilterComboBox.SelectedIndex = 0;

            // 应用初始过滤
            ApplyFilter();
        }

        private void RegisterConverters()
        {
            // 注册转换器到资源
            Resources.Add("BooleanToVisibilityConverter", new BooleanToVisibilityConverter());
            Resources.Add("StringEmptyToVisibilityConverter", new StringEmptyToVisibilityConverter());
            Resources.Add("BooleanToActiveStatusConverter", new BooleanToActiveStatusConverter());
            Resources.Add("BooleanToStatusColorConverter", new BooleanToStatusColorConverter());
        }

        #endregion

        #region 过滤逻辑

        private void ApplyFilter()
        {
            try
            {
                FilteredWorkflows.Clear();

                var filtered = _allWorkflows.AsEnumerable();

                // 按搜索文本过滤
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(w =>
                        w.Name?.ToLower().Contains(searchLower) == true ||
                        w.Description?.ToLower().Contains(searchLower) == true ||
                        w.Id?.ToLower().Contains(searchLower) == true ||
                        w.CreatedBy?.ToLower().Contains(searchLower) == true);
                }

                // 按类别过滤
                if (CategoryFilter != "All" && !string.IsNullOrEmpty(CategoryFilter))
                {
                    filtered = filtered.Where(w => w.Category == CategoryFilter);
                }

                // 只显示活动的工作流
                filtered = filtered.Where(w => w.IsActive);

                // 按创建时间降序排列
                foreach (var workflow in filtered.OrderByDescending(w => w.CreatedTime))
                {
                    FilteredWorkflows.Add(workflow);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"过滤工作流时发生错误: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 事件处理

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedWorkflow != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("请选择一个工作流", "提示",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyFilter();
                MessageBox.Show("工作流列表已刷新", "提示",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }

    /// <summary>
    /// 布尔值转可见性转换器
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;
            return false;
        }
    }

    /// <summary>
    /// 字符串为空转可见性转换器
    /// </summary>
    public class StringEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值转活动状态文本转换器
    /// </summary>
    public class BooleanToActiveStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "活动" : "已禁用";
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status == "活动";
            }
            return false;
        }
    }

    /// <summary>
    /// 布尔值转状态颜色转换器
    /// </summary>
    public class BooleanToStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                if (isActive)
                {
                    // 活动状态 - 绿色
                    return new SolidColorBrush(Color.FromRgb(46, 204, 113)); // #2ECC71
                }
                else
                {
                    // 禁用状态 - 红色
                    return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // #E74C3C
                }
            }
            // 未知状态 - 灰色
            return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // #95A5A6
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}