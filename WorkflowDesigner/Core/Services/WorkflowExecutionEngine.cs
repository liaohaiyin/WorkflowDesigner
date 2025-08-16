using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Nodes;
using NodeNetwork.ViewModels;

namespace WorkflowDesigner.Core.Services
{
    /// <summary>
    /// 工作流执行引擎
    /// </summary>
    public class WorkflowExecutionEngine
    {
        private readonly Dictionary<string, WorkflowInstance> _runningInstances = new Dictionary<string, WorkflowInstance>();
        private readonly Dictionary<string, WorkflowInstance> _completedInstances = new Dictionary<string, WorkflowInstance>();
        private readonly Dictionary<string, WorkflowInstance> _pausedInstances = new Dictionary<string, WorkflowInstance>();

        #region 事件

        /// <summary>
        /// 工作流状态变化事件
        /// </summary>
        public event EventHandler<WorkflowStatusChangedEventArgs> WorkflowStatusChanged;

        /// <summary>
        /// 节点状态变化事件
        /// </summary>
        public event EventHandler<NodeStatusChangedEventArgs> NodeStatusChanged;

        /// <summary>
        /// 审批事件
        /// </summary>
        public event EventHandler<ApprovalEventArgs> ApprovalEvent;

        #endregion

        #region 工作流实例管理

        /// <summary>
        /// 创建工作流实例
        /// </summary>
        /// <param name="workflowId">工作流定义ID</param>
        /// <param name="name">实例名称</param>
        /// <param name="description">实例描述</param>
        /// <param name="initiatorId">发起人ID</param>
        /// <param name="initiatorName">发起人姓名</param>
        /// <param name="variables">初始变量</param>
        /// <returns>工作流实例</returns>
        public WorkflowInstance CreateWorkflowInstance(string workflowId, string name, string description, 
            string initiatorId, string initiatorName, Dictionary<string, object> variables = null)
        {
            var instance = new WorkflowInstance
            {
                WorkflowId = workflowId,
                Name = name,
                Description = description,
                InitiatorId = initiatorId,
                InitiatorName = initiatorName
            };

            if (variables != null)
            {
                foreach (var kvp in variables)
                {
                    instance.SetVariable(kvp.Key, kvp.Value);
                }
            }

            _runningInstances[instance.Id] = instance;
            OnWorkflowStatusChanged(instance, WorkflowInstanceStatus.Draft, WorkflowInstanceStatus.Draft);

            return instance;
        }

        /// <summary>
        /// 启动工作流实例
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <returns>是否成功启动</returns>
        public async Task<bool> StartWorkflowInstanceAsync(string instanceId)
        {
            if (!_runningInstances.TryGetValue(instanceId, out var instance))
            {
                return false;
            }

            try
            {
                instance.Start();
                
                // 初始化节点实例
                await InitializeNodeInstancesAsync(instance);
                
                // 设置第一个节点为当前节点
                var firstNode = instance.NodeInstances.FirstOrDefault();
                if (firstNode != null)
                {
                    instance.CurrentNodeId = firstNode.NodeId;
                    firstNode.Start();
                    OnNodeStatusChanged(firstNode, NodeInstanceStatus.Pending, NodeInstanceStatus.Running);
                }

                OnWorkflowStatusChanged(instance, WorkflowInstanceStatus.Draft, WorkflowInstanceStatus.Running);
                return true;
            }
            catch (Exception ex)
            {
                instance.SetError($"启动工作流失败: {ex.Message}");
                OnWorkflowStatusChanged(instance, WorkflowInstanceStatus.Draft, WorkflowInstanceStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// 暂停工作流实例
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <returns>是否成功暂停</returns>
        public bool PauseWorkflowInstance(string instanceId)
        {
            if (!_runningInstances.TryGetValue(instanceId, out var instance))
            {
                return false;
            }

            instance.Pause();
            _pausedInstances[instanceId] = instance;
            _runningInstances.Remove(instanceId);

            OnWorkflowStatusChanged(instance, WorkflowInstanceStatus.Running, WorkflowInstanceStatus.Paused);
            return true;
        }

        /// <summary>
        /// 恢复工作流实例
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <returns>是否成功恢复</returns>
        public bool ResumeWorkflowInstance(string instanceId)
        {
            if (!_pausedInstances.TryGetValue(instanceId, out var instance))
            {
                return false;
            }

            instance.Resume();
            _runningInstances[instanceId] = instance;
            _pausedInstances.Remove(instanceId);

            OnWorkflowStatusChanged(instance, WorkflowInstanceStatus.Paused, WorkflowInstanceStatus.Running);
            return true;
        }

        /// <summary>
        /// 终止工作流实例
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <returns>是否成功终止</returns>
        public bool TerminateWorkflowInstance(string instanceId)
        {
            if (!_runningInstances.TryGetValue(instanceId, out var instance))
            {
                if (!_pausedInstances.TryGetValue(instanceId, out instance))
                {
                    return false;
                }
                _pausedInstances.Remove(instanceId);
            }
            else
            {
                _runningInstances.Remove(instanceId);
            }

            instance.Terminate();
            _completedInstances[instanceId] = instance;

            OnWorkflowStatusChanged(instance, instance.Status, WorkflowInstanceStatus.Terminated);
            return true;
        }

        #endregion

        #region 节点执行

        /// <summary>
        /// 执行节点
        /// </summary>
        /// <param name="instanceId">工作流实例ID</param>
        /// <param name="nodeId">节点ID</param>
        /// <returns>是否成功执行</returns>
        public async Task<bool> ExecuteNodeAsync(string instanceId, string nodeId)
        {
            if (!_runningInstances.TryGetValue(instanceId, out var instance))
            {
                return false;
            }

            var nodeInstance = instance.GetNodeInstance(nodeId);
            if (nodeInstance == null)
            {
                return false;
            }

            try
            {
                // 根据节点类型执行不同的逻辑
                switch (nodeInstance.NodeType)
                {
                    case "StartNode":
                        return await ExecuteStartNodeAsync(nodeInstance);
                    case "TaskNode":
                        return await ExecuteTaskNodeAsync(nodeInstance);
                    case "ApprovalNode":
                        return await ExecuteApprovalNodeAsync(nodeInstance);
                    case "DecisionNode":
                        return await ExecuteDecisionNodeAsync(nodeInstance);
                    case "NotificationNode":
                        return await ExecuteNotificationNodeAsync(nodeInstance);
                    case "EndNode":
                        return await ExecuteEndNodeAsync(nodeInstance);
                    default:
                        nodeInstance.SetFailed($"未知的节点类型: {nodeInstance.NodeType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                nodeInstance.SetFailed($"执行节点失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行开始节点
        /// </summary>
        private async Task<bool> ExecuteStartNodeAsync(WorkflowNodeInstance nodeInstance)
        {
            nodeInstance.Start();
            await Task.Delay(100); // 模拟执行时间
            nodeInstance.Complete();
            
            OnNodeStatusChanged(nodeInstance, NodeInstanceStatus.Pending, NodeInstanceStatus.Completed);
            return true;
        }

        /// <summary>
        /// 执行任务节点
        /// </summary>
        private async Task<bool> ExecuteTaskNodeAsync(WorkflowNodeInstance nodeInstance)
        {
            nodeInstance.Start();
            
            // 模拟任务执行
            await Task.Delay(2000);
            
            // 设置输出数据
            nodeInstance.SetOutputData("TaskResult", "任务执行完成");
            nodeInstance.SetOutputData("ExecutionTime", DateTime.Now);
            
            nodeInstance.Complete();
            
            OnNodeStatusChanged(nodeInstance, NodeInstanceStatus.Running, NodeInstanceStatus.Completed);
            return true;
        }

        /// <summary>
        /// 执行审批节点
        /// </summary>
        private async Task<bool> ExecuteApprovalNodeAsync(WorkflowNodeInstance nodeInstance)
        {
            nodeInstance.Start();
            nodeInstance.WaitForApproval();
            
            OnNodeStatusChanged(nodeInstance, NodeInstanceStatus.Running, NodeInstanceStatus.WaitingForApproval);
            return true;
        }

        /// <summary>
        /// 执行判断节点
        /// </summary>
        private async Task<bool> ExecuteDecisionNodeAsync(WorkflowNodeInstance nodeInstance)
        {
            nodeInstance.Start();
            
            // 获取判断条件
            var condition = nodeInstance.GetInputData<string>("ConditionExpression", "true");
            
            // 简单的条件判断（实际应用中需要更复杂的表达式解析器）
            bool result = condition.ToLower() == "true";
            
            nodeInstance.SetOutputData("DecisionResult", result);
            nodeInstance.SetOutputData("OutputPort", result ? "是" : "否");
            
            nodeInstance.Complete();
            
            OnNodeStatusChanged(nodeInstance, NodeInstanceStatus.Running, NodeInstanceStatus.Completed);
            return true;
        }

        /// <summary>
        /// 执行通知节点
        /// </summary>
        private async Task<bool> ExecuteNotificationNodeAsync(WorkflowNodeInstance nodeInstance)
        {
            nodeInstance.Start();
            
            // 获取通知信息
            var title = nodeInstance.GetInputData<string>("NotificationTitle", "通知");
            var content = nodeInstance.GetInputData<string>("NotificationContent", "");
            var recipients = nodeInstance.GetInputData<List<string>>("Recipients", new List<string>());
            
            // 模拟发送通知
            await Task.Delay(1000);
            
            nodeInstance.SetOutputData("NotificationSent", true);
            nodeInstance.SetOutputData("SentTime", DateTime.Now);
            
            nodeInstance.Complete();
            
            OnNodeStatusChanged(nodeInstance, NodeInstanceStatus.Running, NodeInstanceStatus.Completed);
            return true;
        }

        /// <summary>
        /// 执行结束节点
        /// </summary>
        private async Task<bool> ExecuteEndNodeAsync(WorkflowNodeInstance nodeInstance)
        {
            nodeInstance.Start();
            await Task.Delay(100);
            nodeInstance.Complete();
            
            // 完成工作流
            var instance = GetWorkflowInstanceByNodeInstance(nodeInstance.Id);
            if (instance != null)
            {
                instance.Complete();
                _runningInstances.Remove(instance.Id);
                _completedInstances[instance.Id] = instance;
                
                OnWorkflowStatusChanged(instance, WorkflowInstanceStatus.Running, WorkflowInstanceStatus.Completed);
            }
            
            OnNodeStatusChanged(nodeInstance, NodeInstanceStatus.Running, NodeInstanceStatus.Completed);
            return true;
        }

        #endregion

        #region 审批处理

        /// <summary>
        /// 审批通过
        /// </summary>
        /// <param name="instanceId">工作流实例ID</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="approverId">审批人ID</param>
        /// <param name="approverName">审批人姓名</param>
        /// <param name="comment">审批意见</param>
        /// <returns>是否成功</returns>
        public async Task<bool> ApproveNodeAsync(string instanceId, string nodeId, string approverId, 
            string approverName, string comment = "")
        {
            if (!_runningInstances.TryGetValue(instanceId, out var instance))
            {
                return false;
            }

            var nodeInstance = instance.GetNodeInstance(nodeId);
            if (nodeInstance == null || nodeInstance.Status != NodeInstanceStatus.WaitingForApproval)
            {
                return false;
            }

            try
            {
                nodeInstance.Approve(approverId, approverName, comment);
                
                OnNodeStatusChanged(nodeInstance, NodeInstanceStatus.WaitingForApproval, NodeInstanceStatus.Approved);
                OnApprovalEvent(nodeInstance, "Approve", approverId, approverName, comment);
                
                // 继续执行下一个节点
                await ContinueWorkflowAsync(instance, nodeId);
                
                return true;
            }
            catch (Exception ex)
            {
                nodeInstance.SetFailed($"审批失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 审批拒绝
        /// </summary>
        /// <param name="instanceId">工作流实例ID</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="approverId">审批人ID</param>
        /// <param name="approverName">审批人姓名</param>
        /// <param name="comment">拒绝原因</param>
        /// <returns>是否成功</returns>
        public async Task<bool> RejectNodeAsync(string instanceId, string nodeId, string approverId, 
            string approverName, string comment = "")
        {
            if (!_runningInstances.TryGetValue(instanceId, out var instance))
            {
                return false;
            }

            var nodeInstance = instance.GetNodeInstance(nodeId);
            if (nodeInstance == null || nodeInstance.Status != NodeInstanceStatus.WaitingForApproval)
            {
                return false;
            }

            try
            {
                nodeInstance.Reject(approverId, approverName, comment);
                
                OnNodeStatusChanged(nodeInstance, NodeInstanceStatus.WaitingForApproval, NodeInstanceStatus.Rejected);
                OnApprovalEvent(nodeInstance, "Reject", approverId, approverName, comment);
                
                // 工作流结束
                instance.SetError($"节点 {nodeInstance.NodeName} 被拒绝: {comment}");
                _runningInstances.Remove(instance.Id);
                _completedInstances[instance.Id] = instance;
                
                OnWorkflowStatusChanged(instance, WorkflowInstanceStatus.Running, WorkflowInstanceStatus.Error);
                
                return true;
            }
            catch (Exception ex)
            {
                nodeInstance.SetFailed($"审批失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化节点实例
        /// </summary>
        private async Task InitializeNodeInstancesAsync(WorkflowInstance instance)
        {
            // 这里需要根据工作流定义创建节点实例
            // 实际应用中需要从数据库或配置中读取工作流定义
            await Task.CompletedTask;
        }

        /// <summary>
        /// 继续工作流执行
        /// </summary>
        private async Task ContinueWorkflowAsync(WorkflowInstance instance, string completedNodeId)
        {
            // 查找下一个要执行的节点
            var nextNode = FindNextNode(instance, completedNodeId);
            if (nextNode != null)
            {
                instance.CurrentNodeId = nextNode.NodeId;
                await ExecuteNodeAsync(instance.Id, nextNode.NodeId);
            }
        }

        /// <summary>
        /// 查找下一个节点
        /// </summary>
        private WorkflowNodeInstance FindNextNode(WorkflowInstance instance, string completedNodeId)
        {
            // 这里需要根据工作流定义和连接关系查找下一个节点
            // 实际应用中需要实现更复杂的逻辑
            return null;
        }

        /// <summary>
        /// 根据节点实例ID获取工作流实例
        /// </summary>
        private WorkflowInstance GetWorkflowInstanceByNodeInstance(string nodeInstanceId)
        {
            return _runningInstances.Values.FirstOrDefault(i => 
                i.NodeInstances.Any(n => n.Id == nodeInstanceId));
        }

        #endregion

        #region 事件触发

        private void OnWorkflowStatusChanged(WorkflowInstance instance, WorkflowInstanceStatus oldStatus, WorkflowInstanceStatus newStatus)
        {
            WorkflowStatusChanged?.Invoke(this, new WorkflowStatusChangedEventArgs
            {
                Instance = instance,
                OldStatus = oldStatus,
                NewStatus = newStatus
            });
        }

        private void OnNodeStatusChanged(WorkflowNodeInstance nodeInstance, NodeInstanceStatus oldStatus, NodeInstanceStatus newStatus)
        {
            NodeStatusChanged?.Invoke(this, new NodeStatusChangedEventArgs
            {
                NodeInstance = nodeInstance,
                OldStatus = oldStatus,
                NewStatus = newStatus
            });
        }

        private void OnApprovalEvent(WorkflowNodeInstance nodeInstance, string action, string approverId, string approverName, string comment)
        {
            ApprovalEvent?.Invoke(this, new ApprovalEventArgs
            {
                NodeInstance = nodeInstance,
                Action = action,
                ApproverId = approverId,
                ApproverName = approverName,
                Comment = comment
            });
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取运行中的工作流实例
        /// </summary>
        public IEnumerable<WorkflowInstance> GetRunningInstances()
        {
            return _runningInstances.Values;
        }

        /// <summary>
        /// 获取已完成的工作流实例
        /// </summary>
        public IEnumerable<WorkflowInstance> GetCompletedInstances()
        {
            return _completedInstances.Values;
        }

        /// <summary>
        /// 获取暂停的工作流实例
        /// </summary>
        public IEnumerable<WorkflowInstance> GetPausedInstances()
        {
            return _pausedInstances.Values;
        }

        /// <summary>
        /// 根据ID获取工作流实例
        /// </summary>
        public WorkflowInstance GetInstanceById(string instanceId)
        {
            if (_runningInstances.TryGetValue(instanceId, out var instance))
                return instance;
            if (_completedInstances.TryGetValue(instanceId, out instance))
                return instance;
            if (_pausedInstances.TryGetValue(instanceId, out instance))
                return instance;
            return null;
        }

        #endregion
    }

    #region 事件参数类

    public class WorkflowStatusChangedEventArgs : EventArgs
    {
        public WorkflowInstance Instance { get; set; }
        public WorkflowInstanceStatus OldStatus { get; set; }
        public WorkflowInstanceStatus NewStatus { get; set; }
    }

    public class NodeStatusChangedEventArgs : EventArgs
    {
        public WorkflowNodeInstance NodeInstance { get; set; }
        public NodeInstanceStatus OldStatus { get; set; }
        public NodeInstanceStatus NewStatus { get; set; }
    }

    public class ApprovalEventArgs : EventArgs
    {
        public WorkflowNodeInstance NodeInstance { get; set; }
        public string Action { get; set; }
        public string ApproverId { get; set; }
        public string ApproverName { get; set; }
        public string Comment { get; set; }
    }

    #endregion
}