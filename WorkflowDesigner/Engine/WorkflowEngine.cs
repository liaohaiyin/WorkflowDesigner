using NLog;
using NodeNetwork.Views;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Core.Services;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.Engine
{
    public interface IWorkflowEngine
    {
        Task<string> StartWorkflowAsync(WorkflowDefinition definition, Dictionary<string, object> initialData, string startedBy);
        Task<bool> PauseWorkflowAsync(string workflowInstanceId);
        Task<bool> ResumeWorkflowAsync(string workflowInstanceId);
        Task<bool> TerminateWorkflowAsync(string workflowInstanceId);
        Task<WorkflowInstance> GetWorkflowInstanceAsync(string workflowInstanceId);
        Task<List<WorkflowInstance>> GetActiveWorkflowsAsync();
        Task<List<WorkflowInstance>> GetWorkflowsByUserAsync(string userId);
    }

    public class WorkflowEngine : IWorkflowEngine
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IWorkflowRepository _workflowRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, CancellationTokenSource> _runningWorkflows;
        private readonly object _lockObject = new object();

        public WorkflowEngine(IWorkflowRepository workflowRepository, IServiceProvider serviceProvider)
        {
            _workflowRepository = workflowRepository;
            _serviceProvider = serviceProvider;
            _runningWorkflows = new Dictionary<string, CancellationTokenSource>();
        }

        public async Task<string> StartWorkflowAsync(WorkflowDefinition definition, Dictionary<string, object> initialData, string startedBy)
        {
            Logger.Info($"启动工作流: {definition.Name}, 启动人: {startedBy}");

            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid().ToString(),
                DefinitionId = definition.Id,
                Status = WorkflowInstanceStatus.Running,
                StartTime = DateTime.Now,
                DataJson = Newtonsoft.Json.JsonConvert.SerializeObject(initialData),
                CurrentNodeId = definition.StartNodeId,
                StartedBy = startedBy
            };

            await _workflowRepository.SaveWorkflowInstanceAsync(instance);

            var cancellationTokenSource = new CancellationTokenSource();
            lock (_lockObject)
            {
                _runningWorkflows[instance.Id] = cancellationTokenSource;
            }

            // 异步执行工作流
            _ = Task.Run(async () => await ExecuteWorkflowAsync(instance, definition, cancellationTokenSource.Token));

            return instance.Id;
        }

        public async Task<bool> PauseWorkflowAsync(string workflowInstanceId)
        {
            Logger.Info($"暂停工作流: {workflowInstanceId}");

            var instance = await _workflowRepository.GetWorkflowInstanceAsync(workflowInstanceId);
            if (instance != null && instance.Status == WorkflowInstanceStatus.Running)
            {
                instance.Status = WorkflowInstanceStatus.Paused;
                await _workflowRepository.UpdateWorkflowInstanceAsync(instance);

                lock (_lockObject)
                {
                    if (_runningWorkflows.ContainsKey(workflowInstanceId))
                    {
                        _runningWorkflows[workflowInstanceId].Cancel();
                        _runningWorkflows.Remove(workflowInstanceId);
                    }
                }
                return true;
            }
            return false;
        }

        public async Task<bool> ResumeWorkflowAsync(string workflowInstanceId)
        {
            Logger.Info($"恢复工作流: {workflowInstanceId}");

            var instance = await _workflowRepository.GetWorkflowInstanceAsync(workflowInstanceId);
            if (instance != null && instance.Status == WorkflowInstanceStatus.Paused)
            {
                instance.Status = WorkflowInstanceStatus.Running;
                await _workflowRepository.UpdateWorkflowInstanceAsync(instance);

                var definition = await _workflowRepository.GetWorkflowDefinitionAsync(instance.DefinitionId);
                var cancellationTokenSource = new CancellationTokenSource();

                lock (_lockObject)
                {
                    _runningWorkflows[instance.Id] = cancellationTokenSource;
                }

                // 重新启动工作流执行
                _ = Task.Run(async () => await ExecuteWorkflowAsync(instance, definition, cancellationTokenSource.Token));
                return true;
            }
            return false;
        }

        public async Task<bool> TerminateWorkflowAsync(string workflowInstanceId)
        {
            Logger.Info($"终止工作流: {workflowInstanceId}");

            var instance = await _workflowRepository.GetWorkflowInstanceAsync(workflowInstanceId);
            if (instance != null && (instance.Status == WorkflowInstanceStatus.Running || instance.Status == WorkflowInstanceStatus.Paused))
            {
                instance.Status = WorkflowInstanceStatus.Terminated;
                instance.EndTime = DateTime.Now;
                await _workflowRepository.UpdateWorkflowInstanceAsync(instance);

                lock (_lockObject)
                {
                    if (_runningWorkflows.ContainsKey(workflowInstanceId))
                    {
                        _runningWorkflows[workflowInstanceId].Cancel();
                        _runningWorkflows.Remove(workflowInstanceId);
                    }
                }
                return true;
            }
            return false;
        }

        public async Task<WorkflowInstance> GetWorkflowInstanceAsync(string workflowInstanceId)
        {
            return await _workflowRepository.GetWorkflowInstanceAsync(workflowInstanceId);
        }

        public async Task<List<WorkflowInstance>> GetActiveWorkflowsAsync()
        {
            return await _workflowRepository.GetActiveWorkflowsAsync();
        }

        public async Task<List<WorkflowInstance>> GetWorkflowsByUserAsync(string userId)
        {
            return await _workflowRepository.GetWorkflowsByUserAsync(userId);
        }

        private async Task ExecuteWorkflowAsync(WorkflowInstance instance, WorkflowDefinition definition, CancellationToken cancellationToken)
        {
            Logger.Info($"开始执行工作流: {instance.Id}");

            try
            {
                var context = new WorkflowContext
                {
                    Instance = instance,
                    Definition = definition,
                    Data = string.IsNullOrEmpty(instance.DataJson) ?
                           new Dictionary<string, object>() :
                           Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(instance.DataJson),
                    ServiceProvider = _serviceProvider
                };

                // 反序列化节点
                var nodes = DeserializeNodes(definition.NodesJson);
                var connections = DeserializeConnections(definition.ConnectionsJson);

                while (!cancellationToken.IsCancellationRequested && instance.Status == WorkflowInstanceStatus.Running)
                {
                    var currentNode = nodes.FirstOrDefault(n => n.NodeId == instance.CurrentNodeId);
                    if (currentNode == null)
                    {
                        instance.Status = WorkflowInstanceStatus.Failed;
                        instance.ErrorMessage = $"找不到节点: {instance.CurrentNodeId}";
                        break;
                    }

                    Logger.Info($"执行节点: {currentNode.NodeName} ({currentNode.NodeId})");

                    // 创建节点执行记录
                    var nodeExecution = new WorkflowNodeExecution
                    {
                        InstanceId = instance.Id,
                        NodeId = currentNode.NodeId,
                        NodeName = currentNode.NodeName,
                        Status = WorkflowNodeStatus.InProgress,
                        StartTime = DateTime.Now,
                        ExecutorId = currentNode.ExecutorId,
                        InputDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(context.Data)
                    };

                    await _workflowRepository.SaveNodeExecutionAsync(nodeExecution);

                    // 更新节点状态
                    currentNode.Status = WorkflowNodeStatus.InProgress;
                    currentNode.StartTime = DateTime.Now;
                    await _workflowRepository.UpdateWorkflowInstanceAsync(instance);

                    WorkflowNodeResult nodeResult;
                    try
                    {
                        // 执行节点，如果有超时设置则应用超时
                        if (currentNode.TimeoutDuration.HasValue)
                        {
                            var timeoutCts = new CancellationTokenSource(currentNode.TimeoutDuration.Value);
                            var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                            nodeResult = await ExecuteNodeWithTimeout(currentNode, context, combinedCts.Token);
                        }
                        else
                        {
                            nodeResult = await currentNode.ExecuteAsync(context);
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        Logger.Info($"工作流被取消: {instance.Id}");
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // 超时
                        nodeResult = new WorkflowNodeResult
                        {
                            Success = false,
                            ErrorMessage = "节点执行超时"
                        };
                        currentNode.Status = WorkflowNodeStatus.Timeout;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"节点执行异常: {currentNode.NodeName}");
                        nodeResult = new WorkflowNodeResult
                        {
                            Success = false,
                            ErrorMessage = ex.Message
                        };
                    }

                    // 更新节点执行状态
                    currentNode.Status = nodeResult.Success ? WorkflowNodeStatus.Completed : WorkflowNodeStatus.Failed;
                    currentNode.EndTime = DateTime.Now;

                    nodeExecution.Status = currentNode.Status;
                    nodeExecution.EndTime = DateTime.Now;
                    nodeExecution.OutputDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(nodeResult.Data);
                    nodeExecution.ErrorMessage = nodeResult.ErrorMessage;

                    await _workflowRepository.UpdateNodeExecutionAsync(nodeExecution);

                    if (!nodeResult.Success)
                    {
                        instance.Status = WorkflowInstanceStatus.Failed;
                        instance.ErrorMessage = nodeResult.ErrorMessage;
                        instance.EndTime = DateTime.Now;
                        break;
                    }

                    // 更新工作流数据
                    if (nodeResult.Data != null)
                    {
                        foreach (var kvp in nodeResult.Data)
                        {
                            context.Data[kvp.Key] = kvp.Value;
                        }
                        instance.DataJson = Newtonsoft.Json.JsonConvert.SerializeObject(context.Data);
                    }

                    // 查找下一个节点
                    var nextNodeId = FindNextNode(connections, currentNode.NodeId, nodeResult.OutputPort);

                    if (string.IsNullOrEmpty(nextNodeId))
                    {
                        // 到达结束节点
                        instance.Status = WorkflowInstanceStatus.Completed;
                        instance.EndTime = DateTime.Now;
                        Logger.Info($"工作流完成: {instance.Id}");
                        break;
                    }

                    instance.CurrentNodeId = nextNodeId;
                    await _workflowRepository.UpdateWorkflowInstanceAsync(instance);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"工作流执行异常: {instance.Id}");
                instance.Status = WorkflowInstanceStatus.Failed;
                instance.ErrorMessage = ex.Message;
                instance.EndTime = DateTime.Now;
            }
            finally
            {
                lock (_lockObject)
                {
                    _runningWorkflows.Remove(instance.Id);
                }
                await _workflowRepository.UpdateWorkflowInstanceAsync(instance);
            }
        }

        private async Task<WorkflowNodeResult> ExecuteNodeWithTimeout(WorkflowNodeViewModel node, WorkflowContext context, CancellationToken cancellationToken)
        {
            var task = node.ExecuteAsync(context);
            await task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return task.Result;
        }

        private List<WorkflowNodeViewModel> DeserializeNodes(string nodesJson)
        {
            if (string.IsNullOrEmpty(nodesJson))
                return new List<WorkflowNodeViewModel>();

            var nodes = new List<WorkflowNodeViewModel>();
            var nodeDataList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(nodesJson);

            foreach (var nodeData in nodeDataList)
            {
                string nodeType = nodeData.NodeType;
                WorkflowNodeViewModel node;
                switch (nodeType)
                {
                    case "Start":
                        node = new StartNodeViewModel();
                        break;
                    case "End":
                        node = new EndNodeViewModel();
                        break;
                    case "Approval":
                        node = new ApprovalNodeViewModel();
                        break;
                    case "Decision":
                        node = new DecisionNodeViewModel();
                        break;
                    case "Task":
                        node = new TaskNodeViewModel();
                        break;
                    case "Notification":
                        node = new NotificationNodeViewModel();
                        break;
                    default:
                        throw new NotSupportedException($"不支持的节点类型: {nodeType}");
                }

                node.DeserializeNodeData(nodeData.ToString());
                nodes.Add(node);
            }

            return nodes;
        }

        private List<WorkflowConnection> DeserializeConnections(string connectionsJson)
        {
            if (string.IsNullOrEmpty(connectionsJson))
                return new List<WorkflowConnection>();

            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<WorkflowConnection>>(connectionsJson);
        }

        private string FindNextNode(List<WorkflowConnection> connections, string currentNodeId, string outputPort)
        {
            var connection = connections.FirstOrDefault(c =>
                c.SourceNodeId == currentNodeId &&
                (string.IsNullOrEmpty(outputPort) || c.SourcePortName == outputPort));

            return connection?.TargetNodeId;
        }
    }

    // 工作流上下文
    public class WorkflowContext
    {
        public WorkflowInstance Instance { get; set; }
        public WorkflowDefinition Definition { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        public T GetService<T>()
        {
            return (T)ServiceProvider.GetService(typeof(T));
        }
    }

    // 工作流节点执行结果
    public class WorkflowNodeResult
    {
        public bool Success { get; set; }
        public string OutputPort { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    // 验证结果
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public static ValidationResult Success => new ValidationResult { IsValid = true };

        public static ValidationResult Error(List<string> errors)
        {
            return new ValidationResult { IsValid = false, Errors = errors };
        }
    }

    // 条件表达式计算器
    public class ConditionEvaluator
    {
        public bool Evaluate(string expression, Dictionary<string, object> data)
        {
            // 简单的表达式计算器实现
            // 在实际项目中可以使用更复杂的表达式引擎，如Dynamic LINQ

            if (string.IsNullOrWhiteSpace(expression))
                return true;

            try
            {
                // 替换变量
                var evaluatedExpression = expression;
                foreach (var kvp in data)
                {
                    var placeholder = $"{{{kvp.Key}}}";
                    if (evaluatedExpression.Contains(placeholder))
                    {
                        var value = kvp.Value?.ToString() ?? "null";
                        if (kvp.Value is string)
                        {
                            value = $"\"{value}\"";
                        }
                        evaluatedExpression = evaluatedExpression.Replace(placeholder, value);
                    }
                }

                // 简单的条件判断
                if (evaluatedExpression.Contains("=="))
                {
                    var parts = evaluatedExpression.Split(new[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        return parts[0].Trim().Trim('"') == parts[1].Trim().Trim('"');
                    }
                }
                else if (evaluatedExpression.Contains("!="))
                {
                    var parts = evaluatedExpression.Split(new[] { "!=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        return parts[0].Trim().Trim('"') != parts[1].Trim().Trim('"');
                    }
                }
                else if (evaluatedExpression.Contains(">"))
                {
                    var parts = evaluatedExpression.Split('>');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0].Trim(), out var left) &&
                        double.TryParse(parts[1].Trim(), out var right))
                    {
                        return left > right;
                    }
                }
                else if (evaluatedExpression.Contains("<"))
                {
                    var parts = evaluatedExpression.Split('<');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0].Trim(), out var left) &&
                        double.TryParse(parts[1].Trim(), out var right))
                    {
                        return left < right;
                    }
                }

                // 如果是布尔值
                if (bool.TryParse(evaluatedExpression, out var boolResult))
                {
                    return boolResult;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}