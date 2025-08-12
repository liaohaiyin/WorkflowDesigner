using DynamicData;
using Newtonsoft.Json;
using NodeNetwork.ViewModels;
using Prism.Mvvm;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Core.Services;
using WorkflowDesigner.Engine;
using WorkflowDesigner.Nodes;
using WorkflowDesigner.UI.ViewModels;
using NLog;

namespace WorkflowDesigner.UI.ViewModels
{
    public class WorkflowDesignerViewModel : ReactiveObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private NetworkViewModel _network;
        private WorkflowNodeViewModel _selectedNode;
        private bool _hasChanges;
        private bool _canUndo;
        private bool _canRedo;
        private Stack<string> _undoStack = new Stack<string>();
        private Stack<string> _redoStack = new Stack<string>();

        public WorkflowDesignerViewModel()
        {
            try
            {
                // 使用try-catch包装NetworkViewModel创建，避免版本兼容性问题
                CreateNetworkViewModel();

                // 设置变化监听（使用更兼容的方式）
                SetupChangeListeners();

                Logger.Info("WorkflowDesignerViewModel 初始化成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WorkflowDesignerViewModel 初始化失败，使用备用方案");
                CreateFallbackNetwork();
            }
        }

        public NetworkViewModel Network
        {
            get => _network;
            set => this.RaiseAndSetIfChanged(ref _network, value);
        }

        public WorkflowNodeViewModel SelectedNode
        {
            get => _selectedNode;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedNode, value);
                NodeSelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set => this.RaiseAndSetIfChanged(ref _hasChanges, value);
        }

        public bool CanUndo
        {
            get => _canUndo;
            set => this.RaiseAndSetIfChanged(ref _canUndo, value);
        }

        public bool CanRedo
        {
            get => _canRedo;
            set => this.RaiseAndSetIfChanged(ref _canRedo, value);
        }

        public bool HasNodes => Network?.Nodes?.Items?.Count() > 0;

        public event EventHandler NodeSelectionChanged;
        public event EventHandler WorkflowChanged;

        /// <summary>
        /// 创建NetworkViewModel，处理版本兼容性问题
        /// </summary>
        private void CreateNetworkViewModel()
        {
            try
            {
                Network = new NetworkViewModel();
                Logger.Debug("NetworkViewModel 创建成功");
            }
            catch (MissingMethodException ex)
            {
                Logger.Error(ex, "NetworkViewModel 创建失败，DynamicData版本不兼容");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "NetworkViewModel 创建时发生未知错误");
                throw;
            }
        }

        /// <summary>
        /// 创建备用网络视图模型
        /// </summary>
        private void CreateFallbackNetwork()
        {
            try
            {
                // 创建一个最基本的NetworkViewModel实例
                _network = CreateSimpleNetworkViewModel();
                Logger.Info("使用备用网络视图模型");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建备用网络视图模型也失败");
                // 如果连备用方案都失败，则设置为null，UI层需要处理这种情况
                _network = null;
            }
        }

        /// <summary>
        /// 创建简单的网络视图模型（备用方案）
        /// </summary>
        private NetworkViewModel CreateSimpleNetworkViewModel()
        {
            // 这里可能需要根据实际的NodeNetwork版本创建一个简化的实现
            return null;
        }

        /// <summary>
        /// 设置变化监听器
        /// </summary>
        private void SetupChangeListeners()
        {
            if (Network == null) return;

            try
            {
                // 使用更安全的方式监听变化，避免DynamicData版本问题
                if (Network.Nodes != null)
                {
                    SetupNodesChangeListener();
                }

                if (Network.Connections != null)
                {
                    SetupConnectionsChangeListener();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "设置变化监听器失败，某些自动保存功能可能不可用");
            }
        }

        private void SetupNodesChangeListener()
        {
            try
            {
                // 使用属性变化通知而不是DynamicData的变化集
                this.WhenAnyValue(x => x.Network.Nodes.Count)
                    .Skip(1)
                    .Subscribe(_ => OnNetworkChanged());
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "节点变化监听设置失败");
            }
        }

        private void SetupConnectionsChangeListener()
        {
            try
            {
                this.WhenAnyValue(x => x.Network.Connections.Count)
                    .Skip(1)
                    .Subscribe(_ => OnNetworkChanged());
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "连接变化监听设置失败");
            }
        }

        public void CreateNewWorkflow()
        {
            try
            {
                if (Network != null)
                {
                    Network.Nodes.Clear();
                    Network.Connections.Clear();
                }
                SelectedNode = null;
                HasChanges = false;
                ClearUndoRedo();
                Logger.Info("创建新工作流");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建新工作流失败");
                throw new ApplicationException("创建新工作流失败", ex);
            }
        }

        public async Task LoadWorkflowAsync(WorkflowDefinition definition)
        {
            try
            {
                if (Network == null)
                {
                    throw new InvalidOperationException("网络视图模型未初始化");
                }

                Network.Nodes.Clear();
                Network.Connections.Clear();

                // 反序列化节点
                if (!string.IsNullOrEmpty(definition.NodesJson))
                {
                    var nodeDataList = JsonConvert.DeserializeObject<List<dynamic>>(definition.NodesJson);
                    foreach (var nodeData in nodeDataList)
                    {
                        WorkflowNodeViewModel node = CreateNodeFromType(nodeData.NodeType.ToString());
                        if (node != null)
                        {
                            node.DeserializeNodeData(nodeData.ToString());
                            Network.Nodes.Add(node);
                        }
                    }
                }

                // 反序列化连接
                if (!string.IsNullOrEmpty(definition.ConnectionsJson))
                {
                    var connections = JsonConvert.DeserializeObject<List<WorkflowConnection>>(definition.ConnectionsJson);
                    foreach (var conn in connections)
                    {
                        var sourceNode = Network.Nodes.Items.OfType<WorkflowNodeViewModel>()
                            .FirstOrDefault(n => n.NodeId == conn.SourceNodeId);
                        var targetNode = Network.Nodes.Items.OfType<WorkflowNodeViewModel>()
                            .FirstOrDefault(n => n.NodeId == conn.TargetNodeId);

                        if (sourceNode != null && targetNode != null)
                        {
                            var sourceOutput = sourceNode.Outputs.Items.FirstOrDefault(o => o.Name == conn.SourcePortName);
                            var targetInput = targetNode.Inputs.Items.FirstOrDefault(i => i.Name == conn.TargetPortName);

                            if (sourceOutput != null && targetInput != null)
                            {
                                Network.Connections.Add(new ConnectionViewModel(Network, targetInput, sourceOutput));
                            }
                        }
                    }
                }

                HasChanges = false;
                ClearUndoRedo();
                Logger.Info($"工作流加载成功: {definition.Name}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"加载工作流失败: {definition?.Name}");
                throw new ApplicationException("加载工作流失败", ex);
            }
        }

        public WorkflowDefinition BuildWorkflowDefinition()
        {
            try
            {
                if (Network == null)
                {
                    throw new InvalidOperationException("网络视图模型未初始化");
                }

                var definition = new WorkflowDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    CreatedTime = DateTime.Now,
                    Version = "1.0"
                };

                // 序列化节点
                var nodeDataList = new List<object>();
                foreach (var node in Network.Nodes.Items.OfType<WorkflowNodeViewModel>())
                {
                    var nodeData = JsonConvert.DeserializeObject(node.SerializeNodeData());
                    nodeDataList.Add(nodeData);
                }
                definition.NodesJson = JsonConvert.SerializeObject(nodeDataList);

                // 序列化连接
                var connections = new List<WorkflowConnection>();
                foreach (var conn in Network.Connections.Items)
                {
                    if (conn.Input?.Parent is WorkflowNodeViewModel inputNode &&
                        conn.Output?.Parent is WorkflowNodeViewModel outputNode)
                    {
                        connections.Add(new WorkflowConnection
                        {
                            Id = Guid.NewGuid().ToString(),
                            SourceNodeId = outputNode.NodeId,
                            SourcePortName = conn.Output.Name,
                            TargetNodeId = inputNode.NodeId,
                            TargetPortName = conn.Input.Name
                        });
                    }
                }
                definition.ConnectionsJson = JsonConvert.SerializeObject(connections);

                // 查找开始节点
                var startNode = Network.Nodes.Items.OfType<StartNodeViewModel>().FirstOrDefault();
                if (startNode != null)
                {
                    definition.StartNodeId = startNode.NodeId;
                }

                Logger.Info("工作流定义构建成功");
                return definition;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "构建工作流定义失败");
                throw new ApplicationException("构建工作流定义失败", ex);
            }
        }

        public ValidationResult ValidateWorkflow()
        {
            try
            {
                var errors = new List<string>();

                if (Network == null)
                {
                    errors.Add("网络视图模型未初始化");
                    return ValidationResult.Error(errors);
                }

                // 检查是否有节点
                if (!HasNodes)
                {
                    errors.Add("工作流必须至少包含一个节点");
                    return ValidationResult.Error(errors);
                }

                // 检查开始节点
                var startNodes = Network.Nodes.Items.OfType<StartNodeViewModel>().ToList();
                if (startNodes.Count == 0)
                {
                    errors.Add("工作流必须包含一个开始节点");
                }
                else if (startNodes.Count > 1)
                {
                    errors.Add("工作流只能包含一个开始节点");
                }

                // 检查结束节点
                var endNodes = Network.Nodes.Items.OfType<EndNodeViewModel>().ToList();
                if (endNodes.Count == 0)
                {
                    errors.Add("工作流必须包含至少一个结束节点");
                }

                // 验证每个节点的配置
                foreach (var node in Network.Nodes.Items.OfType<WorkflowNodeViewModel>())
                {
                    var nodeValidation = node.ValidateConfiguration();
                    if (!nodeValidation.IsValid)
                    {
                        foreach (var error in nodeValidation.Errors)
                        {
                            errors.Add($"节点 '{node.NodeName}': {error}");
                        }
                    }
                }

                return errors.Any() ? ValidationResult.Error(errors) : ValidationResult.Success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "验证工作流失败");
                return ValidationResult.Error(new List<string> { $"验证失败: {ex.Message}" });
            }
        }

        public void AddNode(Type nodeType, Point position)
        {
            try
            {
                if (Network == null)
                {
                    throw new InvalidOperationException("网络视图模型未初始化");
                }

                SaveState();

                var node = CreateNodeFromType(nodeType.Name);
                if (node != null)
                {
                    node.Position = position;
                    Network.Nodes.Add(node);
                    SelectedNode = node;
                    Logger.Info($"添加节点: {nodeType.Name} 在位置 ({position.X}, {position.Y})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"添加节点失败: {nodeType?.Name}");
                throw new ApplicationException("添加节点失败", ex);
            }
        }

        public void DeleteSelectedNodes()
        {
            try
            {
                if (SelectedNode != null && Network != null)
                {
                    SaveState();

                    // 删除相关连接
                    var connectionsToRemove = Network.Connections.Items
                        .Where(c => c.Input?.Parent == SelectedNode || c.Output?.Parent == SelectedNode)
                        .ToList();

                    foreach (var connection in connectionsToRemove)
                    {
                        Network.Connections.Remove(connection);
                    }

                    // 删除节点
                    Network.Nodes.Remove(SelectedNode);
                    SelectedNode = null;
                    Logger.Info("删除选中节点");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "删除节点失败");
                throw new ApplicationException("删除节点失败", ex);
            }
        }

        public void Undo()
        {
            try
            {
                if (_undoStack.Count > 0)
                {
                    _redoStack.Push(SerializeNetwork());
                    var state = _undoStack.Pop();
                    DeserializeNetwork(state);
                    UpdateUndoRedoState();
                    Logger.Debug("执行撤销操作");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "撤销操作失败");
            }
        }

        public void Redo()
        {
            try
            {
                if (_redoStack.Count > 0)
                {
                    _undoStack.Push(SerializeNetwork());
                    var state = _redoStack.Pop();
                    DeserializeNetwork(state);
                    UpdateUndoRedoState();
                    Logger.Debug("执行重做操作");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "重做操作失败");
            }
        }

        private WorkflowNodeViewModel CreateNodeFromType(string nodeTypeName)
        {
            try
            {
                switch (nodeTypeName)
                {
                    case "Start":
                    case "StartNodeViewModel":
                        return new StartNodeViewModel();

                    case "End":
                    case "EndNodeViewModel":
                        return new EndNodeViewModel();

                    case "Approval":
                    case "ApprovalNodeViewModel":
                        return new ApprovalNodeViewModel();

                    case "Decision":
                    case "DecisionNodeViewModel":
                        return new DecisionNodeViewModel();

                    case "Task":
                    case "TaskNodeViewModel":
                        return new TaskNodeViewModel();

                    case "Notification":
                    case "NotificationNodeViewModel":
                        return new NotificationNodeViewModel();

                    default:
                        Logger.Warn($"未知的节点类型: {nodeTypeName}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"创建节点失败: {nodeTypeName}");
                return null;
            }
        }

        private void OnNetworkChanged()
        {
            HasChanges = true;
            WorkflowChanged?.Invoke(this, EventArgs.Empty);
            this.RaisePropertyChanged(nameof(HasNodes));
        }

        private void SaveState()
        {
            try
            {
                _undoStack.Push(SerializeNetwork());
                _redoStack.Clear();
                UpdateUndoRedoState();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "保存状态失败");
            }
        }

        private void ClearUndoRedo()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            UpdateUndoRedoState();
        }

        private void UpdateUndoRedoState()
        {
            CanUndo = _undoStack.Count > 0;
            CanRedo = _redoStack.Count > 0;
        }

        private string SerializeNetwork()
        {
            try
            {
                if (Network == null) return "{}";

                var data = new
                {
                    Nodes = Network.Nodes.Items.OfType<WorkflowNodeViewModel>().Select(n => n.SerializeNodeData()).ToList(),
                    Connections = Network.Connections.Items.Select(c => new
                    {
                        OutputNodeId = (c.Output?.Parent as WorkflowNodeViewModel)?.NodeId,
                        OutputPortName = c.Output?.Name,
                        InputNodeId = (c.Input?.Parent as WorkflowNodeViewModel)?.NodeId,
                        InputPortName = c.Input?.Name
                    }).ToList()
                };
                return JsonConvert.SerializeObject(data);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "序列化网络失败");
                return "{}";
            }
        }

        private void DeserializeNetwork(string json)
        {
            try
            {
                if (Network == null || string.IsNullOrEmpty(json)) return;

                Network.Nodes.Clear();
                Network.Connections.Clear();

                var data = JsonConvert.DeserializeObject<dynamic>(json);

                // 恢复节点
                var nodeMap = new Dictionary<string, WorkflowNodeViewModel>();
                foreach (var nodeData in data.Nodes)
                {
                    var nodeJson = nodeData.ToString();
                    var nodeInfo = JsonConvert.DeserializeObject<dynamic>(nodeJson);
                    WorkflowNodeViewModel node = CreateNodeFromType(nodeInfo.NodeType.ToString());
                    if (node != null)
                    {
                        node.DeserializeNodeData(nodeJson);
                        Network.Nodes.Add(node);
                        nodeMap[node.NodeId] = node;
                    }
                }

                // 恢复连接
                foreach (var connData in data.Connections)
                {
                    string outputNodeId = connData.OutputNodeId;
                    string inputNodeId = connData.InputNodeId;
                    string outputPortName = connData.OutputPortName;
                    string inputPortName = connData.InputPortName;

                    if (nodeMap.ContainsKey(outputNodeId) && nodeMap.ContainsKey(inputNodeId))
                    {
                        var outputNode = nodeMap[outputNodeId];
                        var inputNode = nodeMap[inputNodeId];
                        var outputPort = outputNode.Outputs.Items.FirstOrDefault(o => o.Name == outputPortName);
                        var inputPort = inputNode.Inputs.Items.FirstOrDefault(i => i.Name == inputPortName);

                        if (outputPort != null && inputPort != null)
                        {
                            Network.Connections.Add(new ConnectionViewModel(Network, inputPort, outputPort));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "反序列化网络失败");
            }
        }
    }
}