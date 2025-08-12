using DynamicData;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Core.Services;
using WorkflowDesigner.Engine;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.Nodes
{
    // 修复版本的工作流节点基类
    public abstract class WorkflowNodeViewModel : NodeViewModel, INotifyPropertyChanged
    {
        private WorkflowNodeStatus _status = WorkflowNodeStatus.Pending;
        private string _nodeName = "未命名节点";
        private string _description = "";
        private string _executorId = "";
        private string _executorRole = "";
        private TimeSpan? _timeoutDuration;

        public WorkflowNodeViewModel()
        {
            // NodeNetwork 3.0.2 使用的是继承的端口集合
            // 不需要重新初始化 Inputs 和 Outputs
        }

        public string NodeId { get; set; } = Guid.NewGuid().ToString();

        public string NodeName
        {
            get => _nodeName;
            set => this.RaiseAndSetIfChanged(ref _nodeName, value);
        }

        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        public abstract WorkflowNodeType NodeType { get; }

        public WorkflowNodeStatus Status
        {
            get => _status;
            set
            {
                this.RaiseAndSetIfChanged(ref _status, value);
                this.RaisePropertyChanged(nameof(StatusBrush));
            }
        }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public TimeSpan? TimeoutDuration
        {
            get => _timeoutDuration;
            set => this.RaiseAndSetIfChanged(ref _timeoutDuration, value);
        }

        public string ExecutorId
        {
            get => _executorId;
            set => this.RaiseAndSetIfChanged(ref _executorId, value);
        }

        public string ExecutorRole
        {
            get => _executorRole;
            set => this.RaiseAndSetIfChanged(ref _executorRole, value);
        }

        public Dictionary<string, object> NodeData { get; set; } = new Dictionary<string, object>();

        // 节点状态颜色
        public Brush StatusBrush
        {
            get
            {
                switch (Status)
                {
                    case WorkflowNodeStatus.Completed:
                        return Brushes.Green;
                    case WorkflowNodeStatus.InProgress:
                        return Brushes.Orange;
                    case WorkflowNodeStatus.Failed:
                        return Brushes.Red;
                    case WorkflowNodeStatus.Timeout:
                        return Brushes.Purple;
                    default:
                        return Brushes.Gray;
                }
            }
        }

        // 抽象方法 - 执行节点逻辑
        public abstract Task<WorkflowNodeResult> ExecuteAsync(WorkflowContext context);

        // 抽象方法 - 验证节点配置
        public abstract ValidationResult ValidateConfiguration();

        // 将节点数据序列化为JSON
        public virtual string SerializeNodeData()
        {
            var data = new
            {
                NodeId,
                NodeName,
                Description,
                NodeType = NodeType.ToString(),
                ExecutorId,
                ExecutorRole,
                TimeoutDuration = TimeoutDuration?.ToString(),
                NodeData,
                Position = new { X = this.Position.X, Y = this.Position.Y }
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(data);
        }

        // 从JSON反序列化节点数据
        public virtual void DeserializeNodeData(string json)
        {
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            NodeId = data.NodeId;
            NodeName = data.NodeName;
            Description = data.Description ?? "";
            ExecutorId = data.ExecutorId ?? "";
            ExecutorRole = data.ExecutorRole ?? "";

            if (data.TimeoutDuration != null)
            {
                TimeSpan.TryParse(data.TimeoutDuration.ToString(), out TimeSpan timeout);
                TimeoutDuration = timeout;
            }

            if (data.NodeData != null)
            {
                NodeData = ((Newtonsoft.Json.Linq.JObject)data.NodeData).ToObject<Dictionary<string, object>>();
            }

            if (data.Position != null)
            {
                this.Position = new System.Windows.Point((double)data.Position.X, (double)data.Position.Y);
            }
        }
    }
    // 开始节点
    public class StartNodeViewModel : WorkflowNodeViewModel
    {
        public override WorkflowNodeType NodeType => WorkflowNodeType.Start;

        public StartNodeViewModel()
        {
            NodeName = "开始";
            this.Outputs.Add(new ValueNodeOutputViewModel<object>
            {
                Name = "输出"
            });
        }

        public override async Task<WorkflowNodeResult> ExecuteAsync(WorkflowContext context)
        {
            return await Task.FromResult(new WorkflowNodeResult
            {
                Success = true,
                OutputPort = "输出",
                Data = context.Data
            });
        }

        public override ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success;
        }
    }
    // 结束节点
    public class EndNodeViewModel : WorkflowNodeViewModel
    {
        public override WorkflowNodeType NodeType => WorkflowNodeType.End;

        public EndNodeViewModel()
        {
            NodeName = "结束";
            this.Inputs.Add(new ValueNodeInputViewModel<object>
            {
                Name = "输入"
            });
        }

        public override async Task<WorkflowNodeResult> ExecuteAsync(WorkflowContext context)
        {
            return await Task.FromResult(new WorkflowNodeResult
            {
                Success = true,
                Data = context.Data
            });
        }

        public override ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success;
        }
    }
    // 审批节点
    public class ApprovalNodeViewModel : WorkflowNodeViewModel
    {
        private string _approvalTitle = "审批";
        private string _approvalContent = "";
        private List<string> _approvers = new List<string>();
        private bool _requireAllApproval = true;
        public override WorkflowNodeType NodeType => WorkflowNodeType.Approval;
        public string ApprovalTitle
        {
            get => _approvalTitle;
            set => this.RaiseAndSetIfChanged(ref _approvalTitle, value);
        }

        public string ApprovalContent
        {
            get => _approvalContent;
            set => this.RaiseAndSetIfChanged(ref _approvalContent, value);
        }

        public List<string> Approvers
        {
            get => _approvers;
            set => this.RaiseAndSetIfChanged(ref _approvers, value);
        }

        public bool RequireAllApproval
        {
            get => _requireAllApproval;
            set => this.RaiseAndSetIfChanged(ref _requireAllApproval, value);
        }

        public ApprovalNodeViewModel()
        {
            NodeName = "审批节点";

            this.Inputs.Add(new ValueNodeInputViewModel<object>
            {
                Name = "输入"
            });

            this.Outputs.Add(new ValueNodeOutputViewModel<object>
            {
                Name = "同意"
            });

            this.Outputs.Add(new ValueNodeOutputViewModel<object>
            {
                Name = "拒绝"
            });
        }

        public override async Task<WorkflowNodeResult> ExecuteAsync(WorkflowContext context)
        {
            var approvalService = context.GetService<IApprovalService>();

            // 创建审批任务
            var approvalTasks = new List<ApprovalTask>();
            foreach (var approverId in Approvers)
            {
                var task = new ApprovalTask
                {
                    Id = Guid.NewGuid().ToString(),
                    InstanceId = context.Instance.Id,
                    NodeId = NodeId,
                    Title = ApprovalTitle,
                    Content = ApprovalContent,
                    ApproverId = approverId,
                    CreatedTime = DateTime.Now,
                    Status = ApprovalTaskStatus.Pending
                };
                approvalTasks.Add(task);
            }

            // 提交审批任务
            await approvalService.SubmitApprovalTasksAsync(approvalTasks);

            // 等待审批结果
            var results = await approvalService.WaitForApprovalResultsAsync(
                approvalTasks.Select(t => t.Id).ToList(), TimeoutDuration);

            // 判断审批结果
            bool isApproved;
            if (RequireAllApproval)
            {
                isApproved = results.All(r => r.IsApproved);
            }
            else
            {
                isApproved = results.Any(r => r.IsApproved);
            }

            return new WorkflowNodeResult
            {
                Success = true,
                OutputPort = isApproved ? "同意" : "拒绝",
                Data = new Dictionary<string, object> { { "ApprovalResults", results } }
            };
        }

        public override ValidationResult ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ApprovalTitle))
                errors.Add("审批标题不能为空");

            if (!Approvers.Any())
                errors.Add("必须指定至少一个审批人");

            return errors.Any() ? ValidationResult.Error(errors) : ValidationResult.Success;
        }
    }
    // 判断节点
    public class DecisionNodeViewModel : WorkflowNodeViewModel
    {
        private string _conditionExpression = "";

        public override WorkflowNodeType NodeType => WorkflowNodeType.Decision;

        public string ConditionExpression
        {
            get => _conditionExpression;
            set => this.RaiseAndSetIfChanged(ref _conditionExpression, value);
        }

        public DecisionNodeViewModel()
        {
            NodeName = "判断节点";

            this.Inputs.Add(new ValueNodeInputViewModel<object>
            {
                Name = "输入"
            });

            this.Outputs.Add(new ValueNodeOutputViewModel<object>
            {
                Name = "是"
            });

            this.Outputs.Add(new ValueNodeOutputViewModel<object>
            {
                Name = "否"
            });
        }

        public override async Task<WorkflowNodeResult> ExecuteAsync(WorkflowContext context)
        {
            var conditionEvaluator = new ConditionEvaluator();
            var result = await conditionEvaluator.EvaluateAsync(ConditionExpression, context.Data);

            return new WorkflowNodeResult
            {
                Success = true,
                OutputPort = result ? "是" : "否",
                Data = new Dictionary<string, object> { { "ConditionResult", result } }
            };
        }

        public override ValidationResult ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ConditionExpression))
                errors.Add("判断条件不能为空");

            return errors.Any() ? ValidationResult.Error(errors) : ValidationResult.Success;
        }
    }
    // 任务节点
    public class TaskNodeViewModel : WorkflowNodeViewModel
    {
        private string _taskName = "任务";
        private string _taskDescription = "";
        private string _taskType = "Manual";

        public override WorkflowNodeType NodeType => WorkflowNodeType.Task;

        public string TaskName
        {
            get => _taskName;
            set => this.RaiseAndSetIfChanged(ref _taskName, value);
        }

        public string TaskDescription
        {
            get => _taskDescription;
            set => this.RaiseAndSetIfChanged(ref _taskDescription, value);
        }

        public string TaskType
        {
            get => _taskType;
            set => this.RaiseAndSetIfChanged(ref _taskType, value);
        }

        public TaskNodeViewModel()
        {
            NodeName = "任务节点";

            this.Inputs.Add(new ValueNodeInputViewModel<object>
            {
                Name = "输入"
            });

            this.Outputs.Add(new ValueNodeOutputViewModel<object>
            {
                Name = "完成"
            });

            this.Outputs.Add(new ValueNodeOutputViewModel<object>
            {
                Name = "失败"
            });
        }

        public override async Task<WorkflowNodeResult> ExecuteAsync(WorkflowContext context)
        {
            try
            {
                // 根据任务类型执行不同的逻辑
                switch (TaskType)
                {
                    case "Manual":
                        return await ExecuteManualTask(context);
                    case "Auto":
                        return await ExecuteAutoTask(context);
                    default:
                        throw new NotSupportedException($"不支持的任务类型: {TaskType}");
                }
            }
            catch (Exception ex)
            {
                return new WorkflowNodeResult
                {
                    Success = false,
                    OutputPort = "失败",
                    ErrorMessage = ex.Message,
                    Data = new Dictionary<string, object> { { "Error", ex.Message } }
                };
            }
        }

        private async Task<WorkflowNodeResult> ExecuteManualTask(WorkflowContext context)
        {
            await Task.Delay(1000); // 模拟任务执行
            return new WorkflowNodeResult
            {
                Success = true,
                OutputPort = "完成",
                Data = new Dictionary<string, object> { { "TaskResult", "手动任务已完成" } }
            };
        }

        private async Task<WorkflowNodeResult> ExecuteAutoTask(WorkflowContext context)
        {
            await Task.Delay(500); // 模拟自动处理
            return new WorkflowNodeResult
            {
                Success = true,
                OutputPort = "完成",
                Data = new Dictionary<string, object> { { "TaskResult", "自动任务已完成" } }
            };
        }

        public override ValidationResult ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TaskName))
                errors.Add("任务名称不能为空");

            return errors.Any() ? ValidationResult.Error(errors) : ValidationResult.Success;
        }
    }
    // 通知节点
    public class NotificationNodeViewModel : WorkflowNodeViewModel
    {
        private string _notificationTitle = "通知";
        private string _notificationContent = "";
        private List<string> _recipients = new List<string>();
        private string _notificationType = "Email";

        public override WorkflowNodeType NodeType => WorkflowNodeType.Notification;

        public string NotificationTitle
        {
            get => _notificationTitle;
            set => this.RaiseAndSetIfChanged(ref _notificationTitle, value);
        }

        public string NotificationContent
        {
            get => _notificationContent;
            set => this.RaiseAndSetIfChanged(ref _notificationContent, value);
        }

        public List<string> Recipients
        {
            get => _recipients;
            set => this.RaiseAndSetIfChanged(ref _recipients, value);
        }

        public string NotificationType
        {
            get => _notificationType;
            set => this.RaiseAndSetIfChanged(ref _notificationType, value);
        }

        public NotificationNodeViewModel()
        {
            NodeName = "通知节点";

            this.Inputs.Add(new ValueNodeInputViewModel<object>
            {
                Name = "输入"
            });

            this.Outputs.Add(new ValueNodeOutputViewModel<object>
            {
                Name = "完成"
            });
        }

        public override async Task<WorkflowNodeResult> ExecuteAsync(WorkflowContext context)
        {
            var notificationService = context.GetService<INotificationService>();

            try
            {
                await notificationService.SendNotificationAsync(new NotificationRequest
                {
                    Title = NotificationTitle,
                    Content = NotificationContent,
                    Recipients = Recipients,
                    Type = NotificationType
                });

                return new WorkflowNodeResult
                {
                    Success = true,
                    OutputPort = "完成",
                    Data = new Dictionary<string, object> { { "NotificationSent", true } }
                };
            }
            catch (Exception ex)
            {
                return new WorkflowNodeResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Data = new Dictionary<string, object> { { "Error", ex.Message } }
                };
            }
        }

        public override ValidationResult ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(NotificationTitle))
                errors.Add("通知标题不能为空");

            if (!Recipients.Any())
                errors.Add("必须指定至少一个接收人");

            return errors.Any() ? ValidationResult.Error(errors) : ValidationResult.Success;
        }
    }
    public class WorkflowNodeInputViewModel : NodeInputViewModel
    {
        public WorkflowNodeInputViewModel()
        {
            this.Name = "输入";
        }

        public WorkflowNodeInputViewModel(string name)
        {
            this.Name = name;
        }
    }
    public class WorkflowNodeOutputViewModel : NodeOutputViewModel
    {
        public WorkflowNodeOutputViewModel()
        {
            this.Name = "输出";
        }

        public WorkflowNodeOutputViewModel(string name)
        {
            this.Name = name;
        }
    }

}