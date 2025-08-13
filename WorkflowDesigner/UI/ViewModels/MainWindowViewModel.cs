using Microsoft.Win32;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows;
using System.Windows.Input;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Core.Services;
using WorkflowDesigner.Engine;
using WorkflowDesigner.UI.Views;

namespace WorkflowDesigner.UI.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IWorkflowEngine _workflowEngine;
        private readonly IWorkflowRepository _workflowRepository;
        private string _statusMessage = "就绪";
        private DateTime _currentTime = DateTime.Now;
        private bool _isToolboxVisible = true;
        private bool _isPropertyPanelVisible = true;
        private bool _isOutputPanelVisible = true;
        private WorkflowDefinition _currentWorkflow;

        // 子视图模型
        public ToolboxViewModel ToolboxViewModel { get; private set; }
        public WorkflowDesignerViewModel DesignerViewModel { get; private set; }
        public PropertyPanelViewModel PropertyPanelViewModel { get; private set; }
        public WorkflowMonitorViewModel MonitorViewModel { get; private set; }
        public OutputPanelViewModel OutputPanelViewModel { get; private set; }

        // 默认构造函数 - 创建基本功能
        public MainWindowViewModel()
        {
            try
            {
                // 创建默认的服务实例
                CreateDefaultServices();
                InitializeViewModels();
                InitializeCommands();
                StartTimeTimer();
                SetupEventHandlers();

                StatusMessage = "应用程序已启动（基本模式）";
            }
            catch (Exception ex)
            {
                StatusMessage = $"初始化失败: {ex.Message}";
                // 创建最基本的视图模型
                CreateMinimalViewModels();
                InitializeCommands();
            }
        }

        // 依赖注入构造函数 - 完整功能
        public MainWindowViewModel(
            IWorkflowEngine workflowEngine,
            IWorkflowRepository workflowRepository,
            ToolboxViewModel toolboxViewModel,
            WorkflowDesignerViewModel designerViewModel,
            PropertyPanelViewModel propertyPanelViewModel,
            WorkflowMonitorViewModel monitorViewModel,
            OutputPanelViewModel outputPanelViewModel) : this()
        {
            _workflowEngine = workflowEngine;
            _workflowRepository = workflowRepository;

            ToolboxViewModel = toolboxViewModel;
            DesignerViewModel = designerViewModel;
            PropertyPanelViewModel = propertyPanelViewModel;
            MonitorViewModel = monitorViewModel;
            OutputPanelViewModel = outputPanelViewModel;

            SetupEventHandlers();
            StatusMessage = "应用程序已启动（完整模式）";
        }

        #region 属性

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public DateTime CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public bool IsToolboxVisible
        {
            get => _isToolboxVisible;
            set => SetProperty(ref _isToolboxVisible, value);
        }

        public bool IsPropertyPanelVisible
        {
            get => _isPropertyPanelVisible;
            set => SetProperty(ref _isPropertyPanelVisible, value);
        }

        public bool IsOutputPanelVisible
        {
            get => _isOutputPanelVisible;
            set => SetProperty(ref _isOutputPanelVisible, value);
        }

        public WorkflowDefinition CurrentWorkflow
        {
            get => _currentWorkflow;
            set => SetProperty(ref _currentWorkflow, value);
        }

        private bool _isMonitorPanelVisible = true;

        public bool IsMonitorPanelVisible
        {
            get => _isMonitorPanelVisible;
            set => this.SetProperty(ref _isMonitorPanelVisible, value);
        }
        #endregion

        #region 命令

        public ICommand NewWorkflowCommand { get; private set; }
        public ICommand OpenWorkflowCommand { get; private set; }
        public ICommand SaveWorkflowCommand { get; private set; }
        public ICommand SaveAsWorkflowCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        public ICommand UndoCommand { get; private set; }
        public ICommand RedoCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand ValidateWorkflowCommand { get; private set; }
        public ICommand StartWorkflowCommand { get; private set; }
        public ICommand PauseWorkflowCommand { get; private set; }
        public ICommand StopWorkflowCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }

        #endregion

        #region 初始化方法

        private void CreateDefaultServices()
        {
            try
            {
                // 创建默认的服务实例（如果依赖注入不可用）
                // 注意：这些服务可能功能有限，因为它们没有数据库连接

                // 这里暂时不创建服务，避免数据库连接问题
                // 实际的服务创建将通过依赖注入完成
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建默认服务失败: {ex.Message}");
            }
        }

        private void InitializeViewModels()
        {
            try
            {
                // 创建基本的视图模型
                ToolboxViewModel = new ToolboxViewModel();
                DesignerViewModel = new WorkflowDesignerViewModel();
                PropertyPanelViewModel = new PropertyPanelViewModel(); // 使用无参构造函数
                MonitorViewModel = new WorkflowMonitorViewModel(); // 使用无参构造函数
                OutputPanelViewModel = new OutputPanelViewModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化视图模型失败: {ex.Message}");
                CreateMinimalViewModels();
            }
        }

        private void CreateMinimalViewModels()
        {
            // 创建最基本的视图模型实例
            try
            {
                ToolboxViewModel = new ToolboxViewModel();
                DesignerViewModel = new WorkflowDesignerViewModel();
                OutputPanelViewModel = new OutputPanelViewModel();

                // 这些视图模型如果创建失败，设置为null
                PropertyPanelViewModel = null;
                MonitorViewModel = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建最小视图模型失败: {ex.Message}");
                // 如果连最基本的都创建不了，那就只能让它们为null了
            }
        }

        private void InitializeCommands()
        {
            NewWorkflowCommand = new DelegateCommand(OnNewWorkflow);
            OpenWorkflowCommand = new DelegateCommand(OnOpenWorkflow);
            SaveWorkflowCommand = new DelegateCommand(OnSaveWorkflow, CanSaveWorkflow);
            SaveAsWorkflowCommand = new DelegateCommand(OnSaveAsWorkflow, CanSaveWorkflow);
            ExitCommand = new DelegateCommand(OnExit);
            UndoCommand = new DelegateCommand(OnUndo, CanUndo);
            RedoCommand = new DelegateCommand(OnRedo, CanRedo);
            DeleteCommand = new DelegateCommand(OnDelete, CanDelete);
            ValidateWorkflowCommand = new DelegateCommand(OnValidateWorkflow, CanValidateWorkflow);
            StartWorkflowCommand = new DelegateCommand(OnStartWorkflow, CanStartWorkflow);
            PauseWorkflowCommand = new DelegateCommand(OnPauseWorkflow, CanPauseWorkflow);
            StopWorkflowCommand = new DelegateCommand(OnStopWorkflow, CanStopWorkflow);
            AboutCommand = new DelegateCommand(OnAbout);
        }

        private void StartTimeTimer()
        {
            try
            {
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += (s, e) => CurrentTime = DateTime.Now;
                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动时间定时器失败: {ex.Message}");
            }
        }

        private void SetupEventHandlers()
        {
            try
            {
                if (DesignerViewModel != null)
                {
                    DesignerViewModel.NodeSelectionChanged += OnNodeSelectionChanged;
                    DesignerViewModel.WorkflowChanged += OnWorkflowChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置事件处理器失败: {ex.Message}");
            }
        }

        #endregion

        #region 事件处理
        private void OnNodeSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (PropertyPanelViewModel != null && DesignerViewModel != null)
                {
                    PropertyPanelViewModel.SelectedNode = DesignerViewModel.SelectedNode;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"节点选择处理失败: {ex.Message}";
            }
        }

        private void OnWorkflowChanged(object sender, EventArgs e)
        {
            StatusMessage = "工作流已修改";
            RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令实现

        private void OnNewWorkflow()
        {
            try
            {
                if (CheckForUnsavedChanges())
                {
                    DesignerViewModel?.CreateNewWorkflow();
                    CurrentWorkflow = null;
                    StatusMessage = "创建新工作流";
                    OutputPanelViewModel?.AddMessage("创建新工作流");
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("创建新工作流", ex);
            }
        }

        private void OnOpenWorkflow()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "工作流文件 (*.wfd)|*.wfd|JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    Title = "打开工作流文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var json = System.IO.File.ReadAllText(openFileDialog.FileName);
                    var workflow = JsonConvert.DeserializeObject<WorkflowDefinition>(json);

                    if (DesignerViewModel != null)
                    {
                        DesignerViewModel.LoadWorkflow(workflow);
                    }

                    CurrentWorkflow = workflow;
                    StatusMessage = $"已打开工作流: {workflow.Name}";
                    OutputPanelViewModel?.AddMessage($"已打开工作流: {workflow.Name}");
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("打开工作流", ex);
            }
        }

        private async void OnSaveWorkflow()
        {
            try
            {
                if (CurrentWorkflow == null)
                {
                    OnSaveAsWorkflow();
                    return;
                }

                if (DesignerViewModel == null)
                {
                    throw new InvalidOperationException("设计器不可用");
                }

                var workflow = DesignerViewModel.BuildWorkflowDefinition();
                workflow.Id = CurrentWorkflow.Id;
                workflow.Name = CurrentWorkflow.Name;

                if (_workflowRepository != null)
                {
                    await _workflowRepository.UpdateWorkflowDefinitionAsync(workflow);
                    StatusMessage = $"已保存工作流到数据库: {workflow.Name}";
                }
                else
                {
                    // 如果没有数据库连接，保存到文件
                    var json = JsonConvert.SerializeObject(workflow, Formatting.Indented);
                    var fileName = $"{workflow.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    System.IO.File.WriteAllText(fileName, json);
                    StatusMessage = $"已保存工作流到文件: {fileName}";
                }

                OutputPanelViewModel?.AddMessage($"已保存工作流: {workflow.Name}");
            }
            catch (Exception ex)
            {
                HandleCommandException("保存工作流", ex);
            }
        }

        private async void OnSaveAsWorkflow()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "工作流文件 (*.wfd)|*.wfd|JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    Title = "保存工作流文件",
                    DefaultExt = "json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    if (DesignerViewModel == null)
                    {
                        throw new InvalidOperationException("设计器不可用");
                    }

                    var workflow = DesignerViewModel.BuildWorkflowDefinition();
                    workflow.Name = System.IO.Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                    // 保存到数据库（如果可用）
                    if (_workflowRepository != null)
                    {
                        try
                        {
                            await _workflowRepository.SaveWorkflowDefinitionAsync(workflow);
                        }
                        catch (Exception dbEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"保存到数据库失败: {dbEx.Message}");
                            // 继续保存到文件
                        }
                    }

                    // 保存到文件
                    var json = JsonConvert.SerializeObject(workflow, Formatting.Indented);
                    System.IO.File.WriteAllText(saveFileDialog.FileName, json);

                    CurrentWorkflow = workflow;
                    StatusMessage = $"已保存工作流: {workflow.Name}";
                    OutputPanelViewModel?.AddMessage($"已保存工作流: {workflow.Name}");
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("另存工作流", ex);
            }
        }

        private void OnExit()
        {
            try
            {
                if (CheckForUnsavedChanges())
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("退出应用程序", ex);
            }
        }

        private void OnUndo()
        {
            try
            {
                DesignerViewModel?.Undo();
                StatusMessage = "已撤销操作";
            }
            catch (Exception ex)
            {
                HandleCommandException("撤销操作", ex);
            }
        }

        private void OnRedo()
        {
            try
            {
                DesignerViewModel?.Redo();
                StatusMessage = "已重做操作";
            }
            catch (Exception ex)
            {
                HandleCommandException("重做操作", ex);
            }
        }

        private void OnDelete()
        {
            try
            {
                DesignerViewModel?.DeleteSelectedNodes();
                StatusMessage = "已删除选中节点";
            }
            catch (Exception ex)
            {
                HandleCommandException("删除节点", ex);
            }
        }

        private void OnValidateWorkflow()
        {
            try
            {
                if (DesignerViewModel == null)
                {
                    throw new InvalidOperationException("设计器不可用");
                }

                var validationResult = DesignerViewModel.ValidateWorkflow();
                if (validationResult.IsValid)
                {
                    StatusMessage = "工作流验证通过";
                    OutputPanelViewModel?.AddMessage("工作流验证通过");
                    MessageBox.Show("工作流验证通过！", "验证结果",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "工作流验证失败";
                    OutputPanelViewModel?.AddMessage("工作流验证失败:");
                    foreach (var error in validationResult.Errors)
                    {
                        OutputPanelViewModel?.AddMessage($"  - {error}");
                    }
                    MessageBox.Show($"工作流验证失败:\n{string.Join("\n", validationResult.Errors)}",
                                   "验证结果", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("验证工作流", ex);
            }
        }

        private async void OnStartWorkflow()
        {
            try
            {
                if (DesignerViewModel == null)
                {
                    throw new InvalidOperationException("设计器不可用");
                }

                var workflow = DesignerViewModel.BuildWorkflowDefinition();
                var validationResult = DesignerViewModel.ValidateWorkflow();

                if (!validationResult.IsValid)
                {
                    MessageBox.Show($"工作流验证失败，无法启动:\n{string.Join("\n", validationResult.Errors)}",
                                   "启动失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_workflowEngine == null)
                {
                    MessageBox.Show("工作流引擎不可用，无法启动工作流。\n可能是数据库连接问题。",
                                   "启动失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取启动参数
                var startDialog = new WorkflowStartDialog();
                if (startDialog.ShowDialog() == true)
                {
                    var instanceId = await _workflowEngine.StartWorkflowAsync(workflow, startDialog.InitialData, startDialog.StartedBy);
                    StatusMessage = $"工作流已启动，实例ID: {instanceId}";
                    OutputPanelViewModel?.AddMessage($"工作流已启动，实例ID: {instanceId}");

                    // 刷新监控面板
                    if (MonitorViewModel != null)
                    {
                        await MonitorViewModel.RefreshAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("启动工作流", ex);
            }
        }

        private async void OnPauseWorkflow()
        {
            try
            {
                if (MonitorViewModel?.SelectedWorkflow != null && _workflowEngine != null)
                {
                    await _workflowEngine.PauseWorkflowAsync(MonitorViewModel.SelectedWorkflow.Id);
                    StatusMessage = "工作流已暂停";
                    OutputPanelViewModel?.AddMessage($"工作流已暂停: {MonitorViewModel.SelectedWorkflow.Id}");
                    await MonitorViewModel.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("暂停工作流", ex);
            }
        }

        private async void OnStopWorkflow()
        {
            try
            {
                if (MonitorViewModel?.SelectedWorkflow != null && _workflowEngine != null)
                {
                    if (MessageBox.Show("确定要停止此工作流吗？", "确认",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await _workflowEngine.TerminateWorkflowAsync(MonitorViewModel.SelectedWorkflow.Id);
                        StatusMessage = "工作流已停止";
                        OutputPanelViewModel?.AddMessage($"工作流已停止: {MonitorViewModel.SelectedWorkflow.Id}");
                        await MonitorViewModel.RefreshAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                HandleCommandException("停止工作流", ex);
            }
        }

        private void OnAbout()
        {
            MessageBox.Show("工作流可视化设计器 v1.0\n\n基于WPF + NodeNetwork + AvalonDock开发\n适用于企业级工作流设计和执行\n\n" +
                           $"当前模式: {(_workflowEngine != null ? "完整模式" : "基本模式")}",
                           "关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 命令条件

        private bool CanSaveWorkflow() => DesignerViewModel?.HasChanges == true;
        private bool CanUndo() => DesignerViewModel?.CanUndo == true;
        private bool CanRedo() => DesignerViewModel?.CanRedo == true;
        private bool CanDelete() => DesignerViewModel?.SelectedNode != null;
        private bool CanValidateWorkflow() => DesignerViewModel?.HasNodes == true;
        private bool CanStartWorkflow() => DesignerViewModel?.HasNodes == true && _workflowEngine != null;
        private bool CanPauseWorkflow() => MonitorViewModel?.SelectedWorkflow?.Status == WorkflowInstanceStatus.Running;
        private bool CanStopWorkflow() => MonitorViewModel?.SelectedWorkflow?.Status == WorkflowInstanceStatus.Running ||
                                         MonitorViewModel?.SelectedWorkflow?.Status == WorkflowInstanceStatus.Paused;

        #endregion

        #region 辅助方法

        private bool CheckForUnsavedChanges()
        {
            if (DesignerViewModel?.HasChanges == true)
            {
                var result = MessageBox.Show("当前工作流有未保存的更改，是否继续？", "确认",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            }
            return true;
        }

        private void RaiseCanExecuteChanged()
        {
            try
            {
                ((DelegateCommand)SaveWorkflowCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)SaveAsWorkflowCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)UndoCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)RedoCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)DeleteCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)ValidateWorkflowCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)StartWorkflowCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)PauseWorkflowCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)StopWorkflowCommand)?.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新命令状态失败: {ex.Message}");
            }
        }

        private void HandleCommandException(string commandName, Exception ex)
        {
            var message = $"{commandName}失败: {ex.Message}";
            StatusMessage = message;
            OutputPanelViewModel?.AddMessage(message);

            // 对于严重错误，显示消息框
            if (ex is InvalidOperationException || ex is System.IO.IOException)
            {
                MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}