using System;
using System.Collections.Generic;
using System.Linq;
using NodeNetwork.Toolkit;
using NodeNetwork.ViewModels;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.ViewModels
{
    /// <summary>
    /// 节点工厂，用于创建各种类型的工作流节点
    /// </summary>
    public class NodeFactory : INodeFactory
    {
        private readonly Dictionary<string, Func<NodeViewModel>> _nodeCreators;

        public NodeFactory()
        {
            _nodeCreators = new Dictionary<string, Func<NodeViewModel>>
            {
                ["StartNode"] = () => new StartNodeViewModel(),
                ["TaskNode"] = () => new TaskNodeViewModel(),
                ["DecisionNode"] = () => new DecisionNodeViewModel(),
                ["ApprovalNode"] = () => new ApprovalNodeViewModel(),
                ["NotificationNode"] = () => new NotificationNodeViewModel(),
                ["EndNode"] = () => new EndNodeViewModel()
            };
        }

        /// <summary>
        /// 获取可用的节点类型列表
        /// </summary>
        public IEnumerable<NodeFactoryItem> GetAvailableNodes()
        {
            return new[]
            {
                new NodeFactoryItem
                {
                    Name = "开始节点",
                    Category = "流程控制",
                    Description = "工作流的起始点，自动生成工作流实例",
                    Icon = "▶",
                    IconColor = "#4CAF50",
                    NodeType = "StartNode"
                },
                new NodeFactoryItem
                {
                    Name = "任务节点",
                    Category = "业务处理",
                    Description = "执行具体的业务任务，支持超时设置",
                    Icon = "⚙",
                    IconColor = "#9C27B0",
                    NodeType = "TaskNode"
                },
                new NodeFactoryItem
                {
                    Name = "判断节点",
                    Category = "流程控制",
                    Description = "根据条件进行分支判断，支持多路径流程",
                    Icon = "?",
                    IconColor = "#FF9800",
                    NodeType = "DecisionNode"
                },
                new NodeFactoryItem
                {
                    Name = "审批节点",
                    Category = "审批流程",
                    Description = "支持多人审批，记录审批意见和历史",
                    Icon = "✓",
                    IconColor = "#3F51B5",
                    NodeType = "ApprovalNode"
                },
                new NodeFactoryItem
                {
                    Name = "通知节点",
                    Category = "消息通知",
                    Description = "发送消息推送和邮件通知",
                    Icon = "📧",
                    IconColor = "#009688",
                    NodeType = "NotificationNode"
                },
                new NodeFactoryItem
                {
                    Name = "结束节点",
                    Category = "流程控制",
                    Description = "工作流的终止点，自动完成流程",
                    Icon = "■",
                    IconColor = "#F44336",
                    NodeType = "EndNode"
                }
            };
        }

        /// <summary>
        /// 创建指定类型的节点
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <returns>节点视图模型</returns>
        public NodeViewModel CreateNode(string nodeType)
        {
            if (_nodeCreators.TryGetValue(nodeType, out var creator))
            {
                var node = creator();
                
                // 设置默认位置（避免重叠）
                var random = new Random();
                node.Position = new System.Windows.Point(
                    random.Next(100, 500),
                    random.Next(100, 400)
                );
                
                return node;
            }
            
            throw new ArgumentException($"未知的节点类型: {nodeType}");
        }

        /// <summary>
        /// 创建指定类型的节点并设置位置
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="position">节点位置</param>
        /// <returns>节点视图模型</returns>
        public NodeViewModel CreateNode(string nodeType, System.Windows.Point position)
        {
            var node = CreateNode(nodeType);
            node.Position = position;
            return node;
        }

        /// <summary>
        /// 检查节点类型是否支持
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <returns>是否支持</returns>
        public bool IsNodeTypeSupported(string nodeType)
        {
            return _nodeCreators.ContainsKey(nodeType);
        }

        /// <summary>
        /// 获取节点类型信息
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <returns>节点类型信息</returns>
        public NodeFactoryItem GetNodeTypeInfo(string nodeType)
        {
            return GetAvailableNodes().FirstOrDefault(n => n.NodeType == nodeType);
        }
    }

    /// <summary>
    /// 节点工厂项，描述可创建的节点类型
    /// </summary>
    public class NodeFactoryItem
    {
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 节点类别
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 节点描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 节点图标
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 图标颜色
        /// </summary>
        public string IconColor { get; set; }

        /// <summary>
        /// 节点类型标识
        /// </summary>
        public string NodeType { get; set; }
    }
}