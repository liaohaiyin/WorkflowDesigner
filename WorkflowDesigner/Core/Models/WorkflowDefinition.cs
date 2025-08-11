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
    [Table("WorkflowDefinitions")]
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
    [Table("WorkflowInstances")]
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

        public string Duration { get; set; }
        public bool CanPause { get; set; }
        public bool CanResume { get; set; }
        public bool CanTerminate { get; set; }
        public bool HasError { get; set; }
    }

    // 节点执行记录
    [Table("WorkflowNodeExecutions")]
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
    }

    // 审批任务
    [Table("ApprovalTasks")]
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
}
