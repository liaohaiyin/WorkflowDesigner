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

namespace WorkflowDesigner.UI.ViewModels
{
    public class WorkflowDesignerViewModel : ReactiveObject
    {
        private NetworkViewModel _network;
        private WorkflowNodeViewModel _selectedNode;
        private bool _hasChanges;
        private bool _canUndo;
        private bool _canRedo;
        private Stack<string> _undoStack = new Stack<string>();
        private Stack<string> _redoStack = new Stack<string>();

        public WorkflowDesignerViewModel()
        {
            Network = new NetworkViewModel();

            // 使用ReactiveUI的方式监听网络变化
            this.WhenAnyValue(x => x.Network.Nodes.Count)
                .Skip(1) // 跳过初始值
                .Subscribe(_ => OnNetworkChanged());

            this.WhenAnyValue(x => x.Network.Connections.Count)
                .Skip(1) // 跳过初始值
                .Subscribe(_ => OnNetworkChanged());
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

        public bool HasNodes => Network.Nodes.Items.Count() > 0;

        public event EventHandler NodeSelectionChanged;
        public event EventHandler WorkflowChanged;

        public void CreateNewWorkflow()
        {
            Network.Nodes.Clear();
            Network.Connections.Clear();
            SelectedNode = null;
            HasChanges = false;
            ClearUndoRedo();
        }

        public async Task LoadWorkflowAsync(WorkflowDefinition definition)
        {
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
        }

        public WorkflowDefinition BuildWorkflowDefinition()
        {
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

            return definition;
        }

        public ValidationResult ValidateWorkflow()
        {
            var errors = new List<string>();

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

        public void AddNode(Type nodeType, Point position)
        {
            SaveState();

            var node = CreateNodeFromType(nodeType.Name);
            if (node != null)
            {
                node.Position = position;
                Network.Nodes.Add(node);
                SelectedNode = node;
            }
        }

        public void DeleteSelectedNodes()
        {
            if (SelectedNode != null)
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
            }
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(SerializeNetwork());
                var state = _undoStack.Pop();
                DeserializeNetwork(state);
                UpdateUndoRedoState();
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(SerializeNetwork());
                var state = _redoStack.Pop();
                DeserializeNetwork(state);
                UpdateUndoRedoState();
            }
        }

        private WorkflowNodeViewModel CreateNodeFromType(string nodeTypeName)
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
            _undoStack.Push(SerializeNetwork());
            _redoStack.Clear();
            UpdateUndoRedoState();
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

        private void DeserializeNetwork(string json)
        {
            try
            {
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
                System.Diagnostics.Debug.WriteLine($"反序列化网络失败: {ex.Message}");
            }
        }

    }
}
