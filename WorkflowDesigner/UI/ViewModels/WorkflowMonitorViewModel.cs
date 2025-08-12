using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IWorkflowEngine _workflowEngine;
        private System.Windows.Threading.DispatcherTimer _refreshTimer;
        private WorkflowInstance _selectedWorkflow;
        private bool _isRefreshing;
        private bool _autoRefreshEnabled = true;
        private string _searchText = "";
        private string _statusFilter = "All";

        public WorkflowMonitorViewModel()
        {
            InitializeCollections();
            Logger.Info("WorkflowMonitorViewModel 初始化（基本模式）");
        }

        public WorkflowMonitorViewModel(IWorkflowEngine workflowEngine)
        {
            _workflowEngine = workflowEngine;
            InitializeCollections();
            SetupTimer();
            SetupSearchFilters();

            // 初始加载
            _ = RefreshAsync();

            Logger.Info("WorkflowMonitorViewModel 初始化完成");
        }

        public ObservableCollection<WorkflowInstance> ActiveWorkflows { get; private set; }
        public ObservableCollection<WorkflowInstance> FilteredWorkflows { get; private set; }

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
                if (_refreshTimer != null)
                {
                    if (value)
                        _refreshTimer.Start();
                    else
                        _refreshTimer.Stop();
                }
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

        private void InitializeCollections()
        {
            ActiveWorkflows = new ObservableCollection<WorkflowInstance>();
            FilteredWorkflows = new ObservableCollection<WorkflowInstance>();
        }

        private void SetupTimer()
        {
            if (_workflowEngine != null)
            {
                // 每5秒刷新一次
                _refreshTimer = new System.Windows.Threading.DispatcherTimer();
                _refreshTimer.Interval = TimeSpan.FromSeconds(5);
                _refreshTimer.Tick += async (s, e) => await RefreshAsync();
                _refreshTimer.Start();
            }
        }

        private void SetupSearchFilters()
        {
            // 监听搜索和过滤条件变化
            this.WhenAnyValue(x => x.SearchText, x => x.StatusFilter)
                .Subscribe(_ => ApplyFilter());
        }

        public async Task RefreshAsync()
        {
            if (IsRefreshing || _workflowEngine == null) return;

            try
            {
                IsRefreshing = true;
                Logger.Debug("开始刷新工作流状态");

                var workflows = await _workflowEngine.GetActiveWorkflowsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActiveWorkflows.Clear();
                    foreach (var workflow in workflows)
                    {
                        // 计算和设置扩展属性（在内存中计算，不查询数据库）
                        EnhanceWorkflowInstance(workflow);
                        ActiveWorkflows.Add(workflow);
                    }

                    ApplyFilter();
                    Logger.Debug($"刷新完成，获取到 {workflows.Count} 个工作流实例");
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "刷新工作流状态失败");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 在UI线程显示错误消息
                    ShowErrorMessage($"刷新失败: {ex.Message}");
                });
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 增强工作流实例，添加计算属性
        /// </summary>
        /// <param name="workflow">工作流实例</param>
        private void EnhanceWorkflowInstance(WorkflowInstance workflow)
        {
            try
            {
                // 这些属性现在都是NotMapped的计算属性，不会查询数据库
                // Duration, CanPause, CanResume, CanTerminate, HasError 都已经在模型中定义为计算属性

                // 如果需要额外的计算，可以在这里进行
                Logger.Debug($"增强工作流实例: {workflow.Id}, 状态: {workflow.Status}, 运行时长: {workflow.Duration}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"增强工作流实例失败: {workflow?.Id}");
            }
        }

        private void ApplyFilter()
        {
            try
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

                // 按开始时间降序排列
                foreach (var workflow in filtered.OrderByDescending(w => w.StartTime))
                {
                    FilteredWorkflows.Add(workflow);
                }

                Logger.Debug($"过滤完成，显示 {FilteredWorkflows.Count} 个工作流实例");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "应用过滤条件失败");
            }
        }

        /// <summary>
        /// 根据状态获取工作流列表
        /// </summary>
        /// <param name="status">状态</param>
        public async Task LoadWorkflowsByStatusAsync(WorkflowInstanceStatus? status = null)
        {
            if (_workflowEngine == null) return;

            try
            {
                IsRefreshing = true;
                Logger.Debug($"按状态加载工作流: {status}");

                List<WorkflowInstance> workflows;
                if (status.HasValue)
                {
                    // 如果仓储支持按状态查询，可以添加这个方法
                    workflows = await _workflowEngine.GetActiveWorkflowsAsync();
                    workflows = workflows.Where(w => w.Status == status.Value).ToList();
                }
                else
                {
                    workflows = await _workflowEngine.GetActiveWorkflowsAsync();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActiveWorkflows.Clear();
                    foreach (var workflow in workflows)
                    {
                        EnhanceWorkflowInstance(workflow);
                        ActiveWorkflows.Add(workflow);
                    }
                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"按状态加载工作流失败: {status}");
                ShowErrorMessage($"加载失败: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 根据用户获取工作流列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        public async Task LoadWorkflowsByUserAsync(string userId)
        {
            if (_workflowEngine == null || string.IsNullOrEmpty(userId)) return;

            try
            {
                IsRefreshing = true;
                Logger.Debug($"按用户加载工作流: {userId}");

                var workflows = await _workflowEngine.GetWorkflowsByUserAsync(userId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActiveWorkflows.Clear();
                    foreach (var workflow in workflows)
                    {
                        EnhanceWorkflowInstance(workflow);
                        ActiveWorkflows.Add(workflow);
                    }
                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"按用户加载工作流失败: {userId}");
                ShowErrorMessage($"加载用户工作流失败: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 获取工作流详细信息
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        public async Task<WorkflowInstance> GetWorkflowDetailsAsync(string instanceId)
        {
            if (_workflowEngine == null || string.IsNullOrEmpty(instanceId)) return null;

            try
            {
                Logger.Debug($"获取工作流详细信息: {instanceId}");
                var workflow = await _workflowEngine.GetWorkflowInstanceAsync(instanceId);

                if (workflow != null)
                {
                    EnhanceWorkflowInstance(workflow);
                }

                return workflow;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"获取工作流详细信息失败: {instanceId}");
                ShowErrorMessage($"获取详细信息失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void ClearData()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ActiveWorkflows.Clear();
                FilteredWorkflows.Clear();
                SelectedWorkflow = null;
            });
            Logger.Debug("清除监控数据");
        }

        /// <summary>
        /// 启动自动刷新
        /// </summary>
        public void StartAutoRefresh()
        {
            AutoRefreshEnabled = true;
            Logger.Debug("启动自动刷新");
        }

        /// <summary>
        /// 停止自动刷新
        /// </summary>
        public void StopAutoRefresh()
        {
            AutoRefreshEnabled = false;
            Logger.Debug("停止自动刷新");
        }

        /// <summary>
        /// 设置刷新间隔
        /// </summary>
        /// <param name="interval">间隔时间</param>
        public void SetRefreshInterval(TimeSpan interval)
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Interval = interval;
                Logger.Debug($"设置刷新间隔: {interval.TotalSeconds}秒");
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        private void ShowErrorMessage(string message)
        {
            try
            {
                // 这里可以集成到主窗口的输出面板
                // 或者显示通知
                Logger.Error($"UI错误消息: {message}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "显示错误消息失败");
            }
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public WorkflowMonitorStatistics GetStatistics()
        {
            try
            {
                var stats = new WorkflowMonitorStatistics();

                if (ActiveWorkflows.Count > 0)
                {
                    stats.TotalWorkflows = ActiveWorkflows.Count;
                    stats.RunningWorkflows = ActiveWorkflows.Count(w => w.Status == WorkflowInstanceStatus.Running);
                    stats.PausedWorkflows = ActiveWorkflows.Count(w => w.Status == WorkflowInstanceStatus.Paused);
                    stats.CompletedWorkflows = ActiveWorkflows.Count(w => w.Status == WorkflowInstanceStatus.Completed);
                    stats.FailedWorkflows = ActiveWorkflows.Count(w => w.Status == WorkflowInstanceStatus.Failed);
                    stats.AverageExecutionTimeMinutes = ActiveWorkflows
                        .Where(w => w.Status == WorkflowInstanceStatus.Completed)
                        .Average(w => w.DurationInMinutes);
                }

                return stats;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取统计信息失败");
                return new WorkflowMonitorStatistics();
            }
        }

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _refreshTimer?.Stop();
                        Logger.Debug("WorkflowMonitorViewModel 已释放");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "释放 WorkflowMonitorViewModel 时发生错误");
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// 工作流监控统计信息
    /// </summary>
    public class WorkflowMonitorStatistics
    {
        public int TotalWorkflows { get; set; }
        public int RunningWorkflows { get; set; }
        public int PausedWorkflows { get; set; }
        public int CompletedWorkflows { get; set; }
        public int FailedWorkflows { get; set; }
        public double AverageExecutionTimeMinutes { get; set; }

        public double SuccessRate => TotalWorkflows > 0 ? (double)CompletedWorkflows / TotalWorkflows * 100 : 0;
        public double FailureRate => TotalWorkflows > 0 ? (double)FailedWorkflows / TotalWorkflows * 100 : 0;
    }
}