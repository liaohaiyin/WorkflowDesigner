using DynamicData;
using NLog;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.Utilities
{
    /// <summary>
    /// 连接管理器 - 处理节点之间的连接逻辑
    /// </summary>
    public class ConnectionManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly NetworkViewModel _network;

        public ConnectionManager(NetworkViewModel network)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
        }

        /// <summary>
        /// 创建两个节点之间的连接
        /// </summary>
        public bool CreateConnection(WorkflowNodeViewModel sourceNode, WorkflowNodeViewModel targetNode)
        {
            try
            {
                if (!IsValidConnection(sourceNode, targetNode, out string errorMessage))
                {
                    Logger.Warn($"无效连接: {errorMessage}");
                    return false;
                }

                var sourceOutput = GetBestOutputPort(sourceNode);
                var targetInput = GetBestInputPort(targetNode);

                if (sourceOutput == null || targetInput == null)
                {
                    Logger.Warn("找不到合适的端口进行连接");
                    return false;
                }

                // 如果目标端口已有连接且不允许多输入，先移除现有连接
                if (!targetNode.AllowMultipleInputs)
                {
                    RemoveConnectionsToInput(targetInput);
                }

                // 创建新连接
                var connection = new ConnectionViewModel(_network, targetInput, sourceOutput);
                _network.Connections.Add(connection);

                Logger.Info($"成功创建连接: {sourceNode.NodeName} -> {targetNode.NodeName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建连接时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 检查连接是否有效
        /// </summary>
        public bool IsValidConnection(WorkflowNodeViewModel source, WorkflowNodeViewModel target, out string errorMessage)
        {
            errorMessage = null;

            if (source == null || target == null)
            {
                errorMessage = "源节点或目标节点为空";
                return false;
            }

            if (source == target)
            {
                errorMessage = "不能连接节点到自身";
                return false;
            }

            // 检查节点类型兼容性
            if (!AreNodesCompatible(source, target))
            {
                errorMessage = "节点类型不兼容";
                return false;
            }

            // 检查是否会造成循环
            if (WouldCreateCycle(source, target))
            {
                errorMessage = "连接会造成循环依赖";
                return false;
            }

            // 检查目标节点的输入限制
            if (!target.AllowMultipleInputs && HasInputConnection(target))
            {
                errorMessage = "目标节点已有输入连接且不允许多输入";
                return false;
            }

            // 检查是否已存在相同连接
            if (ConnectionExists(source, target))
            {
                errorMessage = "连接已存在";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查节点类型是否兼容
        /// </summary>
        private bool AreNodesCompatible(WorkflowNodeViewModel source, WorkflowNodeViewModel target)
        {
            // 开始节点不能有输入
            if (target is StartNodeViewModel)
                return false;

            // 结束节点不能有输出
            if (source is EndNodeViewModel)
                return false;

            // 其他节点类型的兼容性检查可以在这里添加
            return true;
        }

        /// <summary>
        /// 检查是否会造成循环
        /// </summary>
        private bool WouldCreateCycle(WorkflowNodeViewModel source, WorkflowNodeViewModel target)
        {
            return CanReachNode(target, source, new HashSet<WorkflowNodeViewModel>());
        }

        /// <summary>
        /// 检查是否能从起始节点到达目标节点
        /// </summary>
        private bool CanReachNode(WorkflowNodeViewModel start, WorkflowNodeViewModel target, HashSet<WorkflowNodeViewModel> visited)
        {
            if (start == target) return true;
            if (visited.Contains(start)) return false;

            visited.Add(start);

            // 获取起始节点的所有输出连接
            var outputConnections = _network.Connections.Items
                .Where(c => c.Output?.Parent == start);

            foreach (var connection in outputConnections)
            {
                var nextNode = connection.Input?.Parent as WorkflowNodeViewModel;
                if (nextNode != null && CanReachNode(nextNode, target, visited))
                {
                    return true;
                }
            }

            visited.Remove(start);
            return false;
        }

        /// <summary>
        /// 检查节点是否已有输入连接
        /// </summary>
        private bool HasInputConnection(WorkflowNodeViewModel node)
        {
            return _network.Connections.Items.Any(c => c.Input?.Parent == node);
        }

        /// <summary>
        /// 检查连接是否已存在
        /// </summary>
        private bool ConnectionExists(WorkflowNodeViewModel source, WorkflowNodeViewModel target)
        {
            return _network.Connections.Items.Any(c =>
                c.Output?.Parent == source && c.Input?.Parent == target);
        }

        /// <summary>
        /// 获取节点的最佳输出端口
        /// </summary>
        private NodeOutputViewModel GetBestOutputPort(WorkflowNodeViewModel node)
        {
            // 对于判断节点，根据条件选择输出端口
            if (node is DecisionNodeViewModel)
            {
                return node.Outputs.Items.FirstOrDefault(o => o.Name == "是") ??
                       node.Outputs.Items.FirstOrDefault();
            }

            // 对于其他节点，返回第一个输出端口
            return node.Outputs.Items.FirstOrDefault();
        }

        /// <summary>
        /// 获取节点的最佳输入端口
        /// </summary>
        private NodeInputViewModel GetBestInputPort(WorkflowNodeViewModel node)
        {
            // 返回第一个可用的输入端口
            return node.Inputs.Items.FirstOrDefault();
        }

        /// <summary>
        /// 移除到指定输入端口的连接
        /// </summary>
        private void RemoveConnectionsToInput(NodeInputViewModel input)
        {
            var connectionsToRemove = _network.Connections.Items
                .Where(c => c.Input == input)
                .ToList();

            foreach (var connection in connectionsToRemove)
            {
                _network.Connections.Remove(connection);
                Logger.Debug("移除现有连接以支持新连接");
            }
        }

        /// <summary>
        /// 移除节点的所有连接
        /// </summary>
        public void RemoveNodeConnections(WorkflowNodeViewModel node)
        {
            var connectionsToRemove = _network.Connections.Items
                .Where(c => c.Input?.Parent == node || c.Output?.Parent == node)
                .ToList();

            foreach (var connection in connectionsToRemove)
            {
                _network.Connections.Remove(connection);
            }

            Logger.Info($"移除节点 {node.NodeName} 的所有连接");
        }

        /// <summary>
        /// 获取节点的输入连接数
        /// </summary>
        public int GetInputConnectionCount(WorkflowNodeViewModel node)
        {
            return _network.Connections.Items.Count(c => c.Input?.Parent == node);
        }

        /// <summary>
        /// 获取节点的输出连接数
        /// </summary>
        public int GetOutputConnectionCount(WorkflowNodeViewModel node)
        {
            return _network.Connections.Items.Count(c => c.Output?.Parent == node);
        }

        /// <summary>
        /// 获取节点的所有前驱节点
        /// </summary>
        public IEnumerable<WorkflowNodeViewModel> GetPredecessors(WorkflowNodeViewModel node)
        {
            return _network.Connections.Items
                .Where(c => c.Input?.Parent == node)
                .Select(c => c.Output?.Parent as WorkflowNodeViewModel)
                .Where(n => n != null);
        }

        /// <summary>
        /// 获取节点的所有后继节点
        /// </summary>
        public IEnumerable<WorkflowNodeViewModel> GetSuccessors(WorkflowNodeViewModel node)
        {
            return _network.Connections.Items
                .Where(c => c.Output?.Parent == node)
                .Select(c => c.Input?.Parent as WorkflowNodeViewModel)
                .Where(n => n != null);
        }

        /// <summary>
        /// 验证整个网络的连接有效性
        /// </summary>
        public (bool IsValid, List<string> Errors) ValidateNetwork()
        {
            var errors = new List<string>();

            try
            {
                // 检查是否有开始节点
                var startNodes = _network.Nodes.Items.OfType<StartNodeViewModel>().ToList();
                if (startNodes.Count == 0)
                {
                    errors.Add("工作流必须包含至少一个开始节点");
                }
                else if (startNodes.Count > 1)
                {
                    errors.Add("工作流只能包含一个开始节点");
                }

                // 检查是否有结束节点
                var endNodes = _network.Nodes.Items.OfType<EndNodeViewModel>().ToList();
                if (endNodes.Count == 0)
                {
                    errors.Add("工作流必须包含至少一个结束节点");
                }

                // 检查孤立节点
                var allNodes = _network.Nodes.Items.OfType<WorkflowNodeViewModel>().ToList();
                foreach (var node in allNodes)
                {
                    if (!(node is StartNodeViewModel) && GetInputConnectionCount(node) == 0)
                    {
                        errors.Add($"节点 '{node.NodeName}' 没有输入连接");
                    }

                    if (!(node is EndNodeViewModel) && GetOutputConnectionCount(node) == 0)
                    {
                        errors.Add($"节点 '{node.NodeName}' 没有输出连接");
                    }
                }

                // 检查循环依赖
                if (HasCycles())
                {
                    errors.Add("工作流中存在循环依赖");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "验证网络时发生错误");
                errors.Add($"验证过程中发生错误: {ex.Message}");
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 检查网络中是否存在循环
        /// </summary>
        private bool HasCycles()
        {
            var visited = new HashSet<WorkflowNodeViewModel>();
            var recursionStack = new HashSet<WorkflowNodeViewModel>();

            foreach (var node in _network.Nodes.Items.OfType<WorkflowNodeViewModel>())
            {
                if (!visited.Contains(node))
                {
                    if (HasCyclesDFS(node, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 深度优先搜索检查循环
        /// </summary>
        private bool HasCyclesDFS(WorkflowNodeViewModel node, HashSet<WorkflowNodeViewModel> visited, HashSet<WorkflowNodeViewModel> recursionStack)
        {
            visited.Add(node);
            recursionStack.Add(node);

            foreach (var successor in GetSuccessors(node))
            {
                if (!visited.Contains(successor))
                {
                    if (HasCyclesDFS(successor, visited, recursionStack))
                    {
                        return true;
                    }
                }
                else if (recursionStack.Contains(successor))
                {
                    return true;
                }
            }

            recursionStack.Remove(node);
            return false;
        }
    }
}