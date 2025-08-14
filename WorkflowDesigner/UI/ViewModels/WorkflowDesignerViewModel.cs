using DynamicData;
using Newtonsoft.Json;
using NLog;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using Prism.Mvvm;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Core.Services;
using WorkflowDesigner.Engine;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.ViewModels
{
    public class WorkflowDesignerViewModel : ReactiveObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private CompositeDisposable _disposables = new CompositeDisposable();

        private NetworkViewModel _network;
        private WorkflowNodeViewModel _selectedNode;
        private bool _hasChanges;
        private bool _canUndo;
        private bool _canRedo;
        private Stack<string> _undoStack = new Stack<string>();
        private Stack<string> _redoStack = new Stack<string>();
        private bool _isDragging;
        private Point _dragStartPosition;

        public WorkflowDesignerViewModel()
        {
            try
            {
                CreateNetworkViewModel();
                SetupChangeListeners();
                SetupNodeSelection();

                Logger.Info("WorkflowDesignerViewModel 初始化成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "WorkflowDesignerViewModel 初始化失败，使用备用方案");
                CreateFallbackNetwork();
            }
        }

        #region 属性

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
                var oldNode = _selectedNode;
                if (this.RaiseAndSetIfChanged(ref _selectedNode, value) != null)
                {
                    // 更新旧节点的选择状态
                    if (oldNode != null)
                    {
                        oldNode.IsChecked = false;
                    }

                    // 更新新节点的选择状态
                    if (_selectedNode != null)
                    {
                        _selectedNode.IsChecked = true;
                    }

                    // 通知节点选择变化
                    NodeSelectionChanged?.Invoke(this, EventArgs.Empty);

                    // 更新相关属性
                    this.RaisePropertyChanged(nameof(HasSelectedNode));
                    this.RaisePropertyChanged(nameof(CanDeleteSelected));
                }
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

        public bool HasSelectedNode => SelectedNode != null;

        public bool CanDeleteSelected => SelectedNode != null;

        public bool IsDragging
        {
            get => _isDragging;
            set => this.RaiseAndSetIfChanged(ref _isDragging, value);
        }

        #endregion

        #region 事件

        public event EventHandler NodeSelectionChanged;
        public event EventHandler WorkflowChanged;
        public event EventHandler<NodeMovedEventArgs> NodeMoved;

        #endregion

        #region 初始化方法

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

        private void CreateFallbackNetwork()
        {
            try
            {
                _network = CreateSimpleNetworkViewModel();
                Logger.Info("使用备用网络视图模型");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建备用网络视图模型也失败");
                _network = null;
            }
        }

        private NetworkViewModel CreateSimpleNetworkViewModel()
        {
            return null;
        }

        private void SetupChangeListeners()
        {
            if (Network == null) return;

            try
            {
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
                this.WhenAnyValue(x => x.Network.Nodes.Count)
                    .Skip(1)
                    .ObserveOnDispatcher() // 确保在UI线程上执行
                    .Subscribe(
                        onNext: count =>
                        {
                            try
                            {
                                OnNetworkChanged();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "处理节点数量变化失败");
                            }
                        },
                        onError: ex =>
                        {
                            Logger.Error(ex, "节点数量监听Observable异常");
                        })
                    .DisposeWith(_disposables);
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
                    .ObserveOnDispatcher()
                    .Subscribe(
                        onNext: count =>
                        {
                            try
                            {
                                OnNetworkChanged();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "处理连接数量变化失败");
                            }
                        },
                        onError: ex =>
                        {
                            Logger.Error(ex, "连接数量监听Observable异常");
                        })
                    .DisposeWith(_disposables);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "连接变化监听设置失败");
            }
        }

        // 添加Dispose方法来清理资源
        public void Dispose()
        {
            _disposables?.Dispose();
        }

        private void SetupNodeSelection()
        {
            // 在节点被添加到网络时设置选择事件处理
            if (Network?.Nodes != null)
            {
                Network.Nodes.Connect()
                    .Subscribe(changes =>
                    {
                        foreach (var change in changes)
                        {
                            if (change.Reason == ListChangeReason.Add &&
                                change.Item.Current is WorkflowNodeViewModel node)
                            {
                                SetupNodeEventHandlers(node);
                            }
                        }
                    });
            }
        }

        private void SetupNodeEventHandlers(WorkflowNodeViewModel node)
        {
            // 设置节点点击事件
            node.WhenAnyValue(x => x.IsChecked)
                .Subscribe(isSelected =>
                {
                    if (isSelected && SelectedNode != node)
                    {
                        SelectedNode = node;
                    }
                    else if (!isSelected && SelectedNode == node)
                    {
                        SelectedNode = null;
                    }
                });

            // 设置节点位置变化事件
            node.WhenAnyValue(x => x.Position)
                .Skip(1) // 跳过初始值
                .Subscribe(newPosition =>
                {
                    OnNodePositionChanged(node, newPosition);
                });

            // 设置节点属性变化事件
            node.WhenAnyValue(x => x.NodeName, x => x.Description)
                .Skip(1) // 跳过初始值
                .Subscribe(_ => OnNodePropertyChanged(node));
        }

        #endregion

        #region 节点操作方法

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

        public void LoadWorkflow(WorkflowDefinition definition)
        {
            try
            {
                if (Network == null)
                {
                    throw new InvalidOperationException("网络视图模型未初始化");
                }

                Network.Nodes.Clear();
                Network.Connections.Clear();
                SelectedNode = null;

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
                            SetupNodeEventHandlers(node);
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

        public async Task LoadWorkflowAsync(WorkflowDefinition definition)
        {
            await Task.Run(() => LoadWorkflow(definition));
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
                    SetupNodeEventHandlers(node);

                    // 自动选择新添加的节点
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

        public void SelectNode(WorkflowNodeViewModel node)
        {
            try
            {
                if (node != null && Network.Nodes.Items.Contains(node))
                {
                    SelectedNode = node;
                    Logger.Debug($"选择节点: {node.NodeName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "选择节点失败");
            }
        }

        public void SelectNodeAt(Point position)
        {
            try
            {
                var node = GetNodeAt(position);
                if (node != null)
                {
                    SelectedNode = node;
                }
                else
                {
                    SelectedNode = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "按位置选择节点失败");
            }
        }

        public WorkflowNodeViewModel GetNodeAt(Point position)
        {
            try
            {
                if (Network?.Nodes == null) return null;

                // 查找位置附近的节点（允许一定的误差范围）
                const double tolerance = 50; // 像素误差范围

                return Network.Nodes.Items.OfType<WorkflowNodeViewModel>()
                    .FirstOrDefault(node =>
                    {
                        var nodePos = node.Position;
                        return Math.Abs(nodePos.X - position.X) <= tolerance &&
                               Math.Abs(nodePos.Y - position.Y) <= tolerance;
                    });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取位置处的节点失败");
                return null;
            }
        }

        public void MoveSelectedNode(Vector offset)
        {
            try
            {
                if (SelectedNode != null)
                {
                    var oldPosition = SelectedNode.Position;
                    var newPosition = new Point(
                        Math.Max(0, oldPosition.X + offset.X),
                        Math.Max(0, oldPosition.Y + offset.Y)
                    );

                    MoveNode(SelectedNode, newPosition);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "移动选中节点失败");
            }
        }

        public void MoveNode(WorkflowNodeViewModel node, Point newPosition)
        {
            try
            {
                if (node == null) return;

                var oldPosition = node.Position;
                node.Position = newPosition;

                // 触发节点移动事件
                NodeMoved?.Invoke(this, new NodeMovedEventArgs
                {
                    Node = node,
                    OldPosition = oldPosition,
                    NewPosition = newPosition
                });

                HasChanges = true;
                Logger.Debug($"移动节点 {node.NodeName} 从 ({oldPosition.X}, {oldPosition.Y}) 到 ({newPosition.X}, {newPosition.Y})");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "移动节点失败");
            }
        }

        public void DeleteSelectedNodes()
        {
            try
            {
                if (SelectedNode != null && Network != null)
                {
                    SaveState();
                    DeleteNode(SelectedNode);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "删除节点失败");
                throw new ApplicationException("删除节点失败", ex);
            }
        }

        public void DeleteNode(WorkflowNodeViewModel node)
        {
            try
            {
                if (node == null || Network == null) return;

                // 删除相关连接
                var connectionsToRemove = Network.Connections.Items
                    .Where(c => c.Input?.Parent == node || c.Output?.Parent == node)
                    .ToList();

                foreach (var connection in connectionsToRemove)
                {
                    Network.Connections.Remove(connection);
                }

                // 删除节点
                Network.Nodes.Remove(node);

                // 如果删除的是当前选中节点，清除选择
                if (SelectedNode == node)
                {
                    SelectedNode = null;
                }

                Logger.Info($"删除节点: {node.NodeName}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "删除节点失败");
            }
        }

        #endregion

        #region 拖拽操作

        public void StartDrag(Point startPosition)
        {
            try
            {
                _dragStartPosition = startPosition;
                IsDragging = true;
                Logger.Debug($"开始拖拽操作，起始位置: ({startPosition.X}, {startPosition.Y})");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "开始拖拽失败");
            }
        }

        public void UpdateDrag(Point currentPosition)
        {
            try
            {
                if (!IsDragging || SelectedNode == null) return;

                var offset = currentPosition - _dragStartPosition;
                var newPosition = new Point(
                    Math.Max(0, SelectedNode.Position.X + offset.X),
                    Math.Max(0, SelectedNode.Position.Y + offset.Y)
                );

                SelectedNode.Position = newPosition;
                _dragStartPosition = currentPosition;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新拖拽失败");
            }
        }

        public void EndDrag(Point endPosition)
        {
            try
            {
                if (!IsDragging) return;

                IsDragging = false;

                if (SelectedNode != null)
                {
                    var finalOffset = endPosition - _dragStartPosition;
                    var finalPosition = new Point(
                        Math.Max(0, SelectedNode.Position.X + finalOffset.X),
                        Math.Max(0, SelectedNode.Position.Y + finalOffset.Y)
                    );

                    MoveNode(SelectedNode, finalPosition);
                }

                Logger.Debug($"结束拖拽操作，结束位置: ({endPosition.X}, {endPosition.Y})");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "结束拖拽失败");
            }
        }

        public void CancelDrag()
        {
            try
            {
                IsDragging = false;
                Logger.Debug("取消拖拽操作");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "取消拖拽失败");
            }
        }

        #endregion

        #region 事件处理

        private void OnNodePositionChanged(WorkflowNodeViewModel node, Point newPosition)
        {
            try
            {
                HasChanges = true;
                Logger.Debug($"节点 {node.NodeName} 位置变化到 ({newPosition.X}, {newPosition.Y})");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理节点位置变化失败");
            }
        }

        private void OnNodePropertyChanged(WorkflowNodeViewModel node)
        {
            try
            {
                HasChanges = true;
                Logger.Debug($"节点 {node.NodeName} 属性发生变化");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理节点属性变化失败");
            }
        }

        private void OnNetworkChanged()
        {
            HasChanges = true;
            WorkflowChanged?.Invoke(this, EventArgs.Empty);
            this.RaisePropertyChanged(nameof(HasNodes));
        }

        #endregion

        #region

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

                if (!HasNodes)
                {
                    errors.Add("工作流必须至少包含一个节点");
                    return ValidationResult.Error(errors);
                }

                var startNodes = Network.Nodes.Items.OfType<StartNodeViewModel>().ToList();
                if (startNodes.Count == 0)
                {
                    errors.Add("工作流必须包含一个开始节点");
                }
                else if (startNodes.Count > 1)
                {
                    errors.Add("工作流只能包含一个开始节点");
                }

                var endNodes = Network.Nodes.Items.OfType<EndNodeViewModel>().ToList();
                if (endNodes.Count == 0)
                {
                    errors.Add("工作流必须包含至少一个结束节点");
                }

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
                SelectedNode = null;

                var data = JsonConvert.DeserializeObject<dynamic>(json);

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
                        SetupNodeEventHandlers(node);
                        nodeMap[node.NodeId] = node;
                    }
                }

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

        #endregion
    }

    #region 事件参数类

    public class NodeMovedEventArgs : EventArgs
    {
        public WorkflowNodeViewModel Node { get; set; }
        public Point OldPosition { get; set; }
        public Point NewPosition { get; set; }
    }

    #endregion
}