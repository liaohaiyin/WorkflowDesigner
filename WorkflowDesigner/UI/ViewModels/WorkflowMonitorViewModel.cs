using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Engine;

namespace WorkflowDesigner.UI.ViewModels
{
    public class WorkflowMonitorViewModel : ReactiveObject
    {
        private readonly IWorkflowEngine _workflowEngine;
        private readonly System.Windows.Threading.DispatcherTimer _refreshTimer;
        private WorkflowInstance _selectedWorkflow;
        private bool _isRefreshing;
        private bool _autoRefreshEnabled = true;
        private string _searchText = "";
        private string _statusFilter = "All";

        public WorkflowMonitorViewModel(IWorkflowEngine workflowEngine)
        {
            _workflowEngine = workflowEngine;

            ActiveWorkflows = new ObservableCollection<WorkflowInstance>();
            FilteredWorkflows = new ObservableCollection<WorkflowInstance>();

            // 每5秒刷新一次
            _refreshTimer = new System.Windows.Threading.DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(5);
            _refreshTimer.Tick += async (s, e) => await RefreshAsync();
            _refreshTimer.Start();

            // 监听搜索和过滤条件变化
            this.WhenAnyValue(x => x.SearchText, x => x.StatusFilter)
                .Subscribe(_ => ApplyFilter());

            // 初始加载
            _ = RefreshAsync();
        }

        public ObservableCollection<WorkflowInstance> ActiveWorkflows { get; }
        public ObservableCollection<WorkflowInstance> FilteredWorkflows { get; }

        public WorkflowInstance SelectedWorkflow
        {
            get => _selectedWorkflow;
            set => this.RaiseAndSetIfChanged(ref _selectedWorkflow, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => this.RaiseAndSetIfChanged(ref _isRefreshing, value);
        }

        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                this.RaiseAndSetIfChanged(ref _autoRefreshEnabled, value);
                if (value)
                    _refreshTimer.Start();
                else
                    _refreshTimer.Stop();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set => this.RaiseAndSetIfChanged(ref _searchText, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => this.RaiseAndSetIfChanged(ref _statusFilter, value);
        }

        public async Task RefreshAsync()
        {
            if (IsRefreshing) return;

            try
            {
                IsRefreshing = true;
                var workflows = await _workflowEngine.GetActiveWorkflowsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActiveWorkflows.Clear();
                    foreach (var workflow in workflows)
                    {
                        // 添加扩展属性
                        workflow.Duration = CalculateDuration(workflow);
                        workflow.CanPause = workflow.Status == WorkflowInstanceStatus.Running;
                        workflow.CanResume = workflow.Status == WorkflowInstanceStatus.Paused;
                        workflow.CanTerminate = workflow.Status == WorkflowInstanceStatus.Running ||
                                              workflow.Status == WorkflowInstanceStatus.Paused;
                        workflow.HasError = !string.IsNullOrEmpty(workflow.ErrorMessage);

                        ActiveWorkflows.Add(workflow);
                    }

                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新工作流状态失败: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void ApplyFilter()
        {
            FilteredWorkflows.Clear();

            var filtered = ActiveWorkflows.AsEnumerable();

            // 按状态过滤
            if (StatusFilter != "All")
            {
                if (Enum.TryParse<WorkflowInstanceStatus>(StatusFilter, out var status))
                {
                    filtered = filtered.Where(w => w.Status == status);
                }
            }

            // 按搜索文本过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(w =>
                    w.Id.ToLower().Contains(searchLower) ||
                    w.Definition?.Name?.ToLower().Contains(searchLower) == true ||
                    w.StartedBy?.ToLower().Contains(searchLower) == true);
            }

            foreach (var workflow in filtered.OrderByDescending(w => w.StartTime))
            {
                FilteredWorkflows.Add(workflow);
            }
        }

        private string CalculateDuration(WorkflowInstance workflow)
        {
            var endTime = workflow.EndTime ?? DateTime.Now;
            var duration = endTime - workflow.StartTime;

            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}天 {duration.Hours}时{duration.Minutes}分";
            else if (duration.TotalHours >= 1)
                return $"{duration.Hours}时{duration.Minutes}分{duration.Seconds}秒";
            else if (duration.TotalMinutes >= 1)
                return $"{duration.Minutes}分{duration.Seconds}秒";
            else
                return $"{duration.Seconds}秒";
        }

        public void StartAutoRefresh()
        {
            AutoRefreshEnabled = true;
        }

        public void StopAutoRefresh()
        {
            AutoRefreshEnabled = false;
        }
    }
}