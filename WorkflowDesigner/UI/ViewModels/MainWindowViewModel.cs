using Microsoft.Win32;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Models;
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

        public MainWindowViewModel(
            IWorkflowEngine workflowEngine,
            IWorkflowRepository workflowRepository,
            ToolboxViewModel toolboxViewModel,
            WorkflowDesignerViewModel designerViewModel,
            PropertyPanelViewModel propertyPanelViewModel,
            WorkflowMonitorViewModel monitorViewModel,
            OutputPanelViewModel outputPanelViewModel)
        {
            _workflowEngine = workflowEngine;
            _workflowRepository = workflowRepository;

            ToolboxViewModel = toolboxViewModel;
            DesignerViewModel = designerViewModel;
            PropertyPanelViewModel = propertyPanelViewModel;
            MonitorViewModel = monitorViewModel;
            OutputPanelViewModel = outputPanelViewModel;

            InitializeCommands();
            StartTimeTimer();

            // 绑定设计器事件
            DesignerViewModel.NodeSelectionChanged += OnNodeSelectionChanged;
            DesignerViewModel.WorkflowChanged += OnWorkflowChanged;
        }

        // 属性
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

        // 子视图模型
        public ToolboxViewModel ToolboxViewModel { get; }
        public WorkflowDesignerViewModel DesignerViewModel { get; }
        public PropertyPanelViewModel PropertyPanelViewModel { get; }
        public WorkflowMonitorViewModel MonitorViewModel { get; }
        public OutputPanelViewModel OutputPanelViewModel { get; }

        // 命令
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
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => CurrentTime = DateTime.Now;
            timer.Start();
        }

        private void OnNodeSelectionChanged(object sender, EventArgs e)
        {
            PropertyPanelViewModel.SelectedNode = DesignerViewModel.SelectedNode;
        }

        private void OnWorkflowChanged(object sender, EventArgs e)
        {
            StatusMessage = "工作流已修改";
            RaiseCanExecuteChanged();
        }

        private void OnNewWorkflow()
        {
            if (CheckForUnsavedChanges())
            {
                DesignerViewModel.CreateNewWorkflow();
                CurrentWorkflow = null;
                StatusMessage = "创建新工作流";
                OutputPanelViewModel.AddMessage("创建新工作流");
            }
        }

        private async void OnOpenWorkflow()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "工作流文件 (*.wfd)|*.wfd|所有文件 (*.*)|*.*",
                    Title = "打开工作流文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var json = System.IO.File.ReadAllText(openFileDialog.FileName);
                    var workflow = JsonConvert.DeserializeObject<WorkflowDefinition>(json);

                    await DesignerViewModel.LoadWorkflowAsync(workflow);
                    CurrentWorkflow = workflow;
                    StatusMessage = $"已打开工作流: {workflow.Name}";
                    OutputPanelViewModel.AddMessage($"已打开工作流: {workflow.Name}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                OutputPanelViewModel.AddMessage($"打开工作流失败: {ex.Message}");
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

                var workflow = DesignerViewModel.BuildWorkflowDefinition();
                workflow.Id = CurrentWorkflow.Id;
                workflow.Name = CurrentWorkflow.Name;

                await _workflowRepository.UpdateWorkflowDefinitionAsync(workflow);
                StatusMessage = $"已保存工作流: {workflow.Name}";
                OutputPanelViewModel.AddMessage($"已保存工作流: {workflow.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                OutputPanelViewModel.AddMessage($"保存工作流失败: {ex.Message}");
            }
        }

        private async void OnSaveAsWorkflow()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "工作流文件 (*.wfd)|*.wfd|所有文件 (*.*)|*.*",
                    Title = "保存工作流文件"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var workflow = DesignerViewModel.BuildWorkflowDefinition();
                    workflow.Name = System.IO.Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                    await _workflowRepository.SaveWorkflowDefinitionAsync(workflow);

                    var json = JsonConvert.SerializeObject(workflow, Formatting.Indented);
                    System.IO.File.WriteAllText(saveFileDialog.FileName, json);

                    CurrentWorkflow = workflow;
                    StatusMessage = $"已保存工作流: {workflow.Name}";
                    OutputPanelViewModel.AddMessage($"已保存工作流: {workflow.Name}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                OutputPanelViewModel.AddMessage($"保存工作流失败: {ex.Message}");
            }
        }

        private void OnExit()
        {
            if (CheckForUnsavedChanges())
            {
                Application.Current.Shutdown();
            }
        }

        private void OnUndo()
        {
            DesignerViewModel.Undo();
        }

        private void OnRedo()
        {
            DesignerViewModel.Redo();
        }

        private void OnDelete()
        {
            DesignerViewModel.DeleteSelectedNodes();
        }

        private void OnValidateWorkflow()
        {
            var validationResult = DesignerViewModel.ValidateWorkflow();
            if (validationResult.IsValid)
            {
                StatusMessage = "工作流验证通过";
                OutputPanelViewModel.AddMessage("工作流验证通过");
                MessageBox.Show("工作流验证通过！", "验证结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "工作流验证失败";
                OutputPanelViewModel.AddMessage("工作流验证失败:");
                foreach (var error in validationResult.Errors)
                {
                    OutputPanelViewModel.AddMessage($"  - {error}");
                }
                MessageBox.Show($"工作流验证失败:\n{string.Join("\n", validationResult.Errors)}",
                               "验证结果", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OnStartWorkflow()
        {
            try
            {
                var workflow = DesignerViewModel.BuildWorkflowDefinition();
                var validationResult = DesignerViewModel.ValidateWorkflow();

                if (!validationResult.IsValid)
                {
                    MessageBox.Show($"工作流验证失败，无法启动:\n{string.Join("\n", validationResult.Errors)}",
                                   "启动失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取启动参数
                var startDialog = new WorkflowStartDialog();
                if (startDialog.ShowDialog() == true)
                {
                    var instanceId = await _workflowEngine.StartWorkflowAsync(workflow, startDialog.InitialData, startDialog.StartedBy);
                    StatusMessage = $"工作流已启动，实例ID: {instanceId}";
                    OutputPanelViewModel.AddMessage($"工作流已启动，实例ID: {instanceId}");

                    // 刷新监控面板
                    await MonitorViewModel.RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                OutputPanelViewModel.AddMessage($"启动工作流失败: {ex.Message}");
            }
        }

        private async void OnPauseWorkflow()
        {
            if (MonitorViewModel.SelectedWorkflow != null)
            {
                try
                {
                    await _workflowEngine.PauseWorkflowAsync(MonitorViewModel.SelectedWorkflow.Id);
                    StatusMessage = "工作流已暂停";
                    OutputPanelViewModel.AddMessage($"工作流已暂停: {MonitorViewModel.SelectedWorkflow.Id}");
                    await MonitorViewModel.RefreshAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"暂停工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OnStopWorkflow()
        {
            if (MonitorViewModel.SelectedWorkflow != null)
            {
                if (MessageBox.Show("确定要停止此工作流吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _workflowEngine.TerminateWorkflowAsync(MonitorViewModel.SelectedWorkflow.Id);
                        StatusMessage = "工作流已停止";
                        OutputPanelViewModel.AddMessage($"工作流已停止: {MonitorViewModel.SelectedWorkflow.Id}");
                        await MonitorViewModel.RefreshAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"停止工作流失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OnAbout()
        {
            MessageBox.Show("工作流可视化设计器 v1.0\n\n基于WPF + NodeNetwork + AvalonDock开发\n适用于企业级工作流设计和执行",
                           "关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanSaveWorkflow() => DesignerViewModel?.HasChanges == true;
        private bool CanUndo() => DesignerViewModel?.CanUndo == true;
        private bool CanRedo() => DesignerViewModel?.CanRedo == true;
        private bool CanDelete() => DesignerViewModel?.SelectedNode != null;
        private bool CanValidateWorkflow() => DesignerViewModel?.HasNodes == true;
        private bool CanStartWorkflow() => DesignerViewModel?.HasNodes == true;
        private bool CanPauseWorkflow() => MonitorViewModel?.SelectedWorkflow?.Status == WorkflowInstanceStatus.Running;
        private bool CanStopWorkflow() => MonitorViewModel?.SelectedWorkflow?.Status == WorkflowInstanceStatus.Running ||
                                         MonitorViewModel?.SelectedWorkflow?.Status == WorkflowInstanceStatus.Paused;

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
            ((DelegateCommand)SaveWorkflowCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)SaveAsWorkflowCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)UndoCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)RedoCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)DeleteCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)ValidateWorkflowCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)StartWorkflowCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)PauseWorkflowCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)StopWorkflowCommand).RaiseCanExecuteChanged();
        }
    }
}

