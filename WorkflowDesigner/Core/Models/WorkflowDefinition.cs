using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowDesigner.Core.Models
{
    // 工作流定义
    [Table("workflow_definitions")]
    public class WorkflowDefinition
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string Version { get; set; } = "1.0";

        [MaxLength(100)]
        public string Category { get; set; }

        public string NodesJson { get; set; }  // JSON格式存储节点信息
        public string ConnectionsJson { get; set; }  // JSON格式存储连线信息
        public string StartNodeId { get; set; }

        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime? UpdatedTime { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // 工作流实例
    [Table("workflow_instances")]
    public class WorkflowInstance
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string DefinitionId { get; set; }

        public WorkflowInstanceStatus Status { get; set; }
        public string CurrentNodeId { get; set; }
        public string DataJson { get; set; }  // 工作流数据

        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }

        [MaxLength(100)]
        public string StartedBy { get; set; }

        [MaxLength(1000)]
        public string ErrorMessage { get; set; }

        // 导航属性
        [ForeignKey("DefinitionId")]
        public virtual WorkflowDefinition Definition { get; set; }

        public virtual ICollection<WorkflowNodeExecution> NodeExecutions { get; set; } = new List<WorkflowNodeExecution>();

        // 计算属性 - 不映射到数据库
        [NotMapped]
        public string Duration
        {
            get
            {
                var endTime = EndTime ?? DateTime.Now;
                var duration = endTime - StartTime;

                if (duration.TotalDays >= 1)
                    return $"{(int)duration.TotalDays}天 {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                else if (duration.TotalHours >= 1)
                    return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                else if (duration.TotalMinutes >= 1)
                    return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
                else
                    return $"{duration.Seconds}秒";
            }
        }

        // 计算属性 - 不映射到数据库
        [NotMapped]
        public bool CanPause => Status == WorkflowInstanceStatus.Running;

        [NotMapped]
        public bool CanResume => Status == WorkflowInstanceStatus.Paused;

        [NotMapped]
        public bool CanTerminate => Status == WorkflowInstanceStatus.Running || Status == WorkflowInstanceStatus.Paused;

        [NotMapped]
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // 实际运行时长（以分钟为单位）- 不映射到数据库
        [NotMapped]
        public double DurationInMinutes
        {
            get
            {
                var endTime = EndTime ?? DateTime.Now;
                return (endTime - StartTime).TotalMinutes;
            }
        }

        // 实际运行时长（以小时为单位）- 不映射到数据库
        [NotMapped]
        public double DurationInHours
        {
            get
            {
                var endTime = EndTime ?? DateTime.Now;
                return (endTime - StartTime).TotalHours;
            }
        }
    }

    // 节点执行记录
    [Table("workflow_node_executions")]
    public class WorkflowNodeExecution
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string InstanceId { get; set; }

        [Required]
        public string NodeId { get; set; }

        [MaxLength(200)]
        public string NodeName { get; set; }

        public WorkflowNodeStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [MaxLength(100)]
        public string ExecutorId { get; set; }

        [MaxLength(1000)]
        public string ErrorMessage { get; set; }

        public string InputDataJson { get; set; }
        public string OutputDataJson { get; set; }

        // 导航属性
        [ForeignKey("InstanceId")]
        public virtual WorkflowInstance Instance { get; set; }

        // 计算属性 - 执行时长
        [NotMapped]
        public string ExecutionDuration
        {
            get
            {
                if (!StartTime.HasValue) return "未开始";
                if (!EndTime.HasValue) return "执行中";

                var duration = EndTime.Value - StartTime.Value;
                if (duration.TotalMinutes >= 1)
                    return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
                else
                    return $"{duration.TotalSeconds:F1}秒";
            }
        }

        // 执行时长（毫秒）
        [NotMapped]
        public double ExecutionTimeInMilliseconds
        {
            get
            {
                if (!StartTime.HasValue || !EndTime.HasValue) return 0;
                return (EndTime.Value - StartTime.Value).TotalMilliseconds;
            }
        }
    }

    // 审批任务
    [Table("approval_tasks")]
    public class ApprovalTask
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string InstanceId { get; set; }

        [Required]
        public string NodeId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(2000)]
        public string Content { get; set; }

        [MaxLength(100)]
        public string ApproverId { get; set; }

        public ApprovalTaskStatus Status { get; set; } = ApprovalTaskStatus.Pending;

        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime? ApprovedTime { get; set; }

        [MaxLength(1000)]
        public string ApprovalComment { get; set; }

        public bool IsApproved { get; set; }

        // 计算属性 - 待审批时长
        [NotMapped]
        public string PendingDuration
        {
            get
            {
                if (Status != ApprovalTaskStatus.Pending) return "";

                var duration = DateTime.Now - CreatedTime;
                if (duration.TotalDays >= 1)
                    return $"{(int)duration.TotalDays}天";
                else if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours}小时";
                else
                    return $"{(int)duration.TotalMinutes}分钟";
            }
        }

        // 是否超时（假设超过24小时为超时）
        [NotMapped]
        public bool IsTimeout
        {
            get
            {
                if (Status != ApprovalTaskStatus.Pending) return false;
                return (DateTime.Now - CreatedTime).TotalHours > 24;
            }
        }
    }

    // 枚举定义
    public enum WorkflowInstanceStatus
    {
        Running = 1,
        Completed = 2,
        Failed = 3,
        Paused = 4,
        Terminated = 5
    }

    public enum WorkflowNodeStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Failed = 4,
        Skipped = 5,
        Timeout = 6
    }

    public enum WorkflowNodeType
    {
        Start = 1,
        End = 2,
        Approval = 3,
        Decision = 4,
        Task = 5,
        Notification = 6,
        DataOperation = 7
    }

    public enum ApprovalTaskStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Timeout = 4
    }

    // 工作流性能统计
    public class WorkflowPerformanceStatistics
    {
        public string DefinitionId { get; set; }
        public string DefinitionName { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double AverageExecutionTimeMinutes { get; set; }
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;
        public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions * 100 : 0;
    }

    // 节点性能统计
    public class NodePerformanceStatistics
    {
        public string NodeId { get; set; }
        public string NodeName { get; set; }
        public string NodeType { get; set; }
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double AverageExecutionTimeSeconds { get; set; }
        public double MaxExecutionTimeSeconds { get; set; }
        public double MinExecutionTimeSeconds { get; set; }
    }
}