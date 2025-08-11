using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Infrastructure.Data;
using System.Data.Entity;
using WorkflowDesigner.Core.Services;

namespace WorkflowDesigner.Core.Interfaces
{
    // 工作流仓储接口
    public interface IWorkflowRepository
    {
        Task<WorkflowDefinition> GetWorkflowDefinitionAsync(string id);
        Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync();
        Task SaveWorkflowDefinitionAsync(WorkflowDefinition definition);
        Task UpdateWorkflowDefinitionAsync(WorkflowDefinition definition);
        Task DeleteWorkflowDefinitionAsync(string id);

        Task<WorkflowInstance> GetWorkflowInstanceAsync(string id);
        Task<List<WorkflowInstance>> GetActiveWorkflowsAsync();
        Task<List<WorkflowInstance>> GetWorkflowsByUserAsync(string userId);
        Task SaveWorkflowInstanceAsync(WorkflowInstance instance);
        Task UpdateWorkflowInstanceAsync(WorkflowInstance instance);

        Task SaveNodeExecutionAsync(WorkflowNodeExecution execution);
        Task UpdateNodeExecutionAsync(WorkflowNodeExecution execution);
        Task<List<WorkflowNodeExecution>> GetNodeExecutionsAsync(string instanceId);
    }

    // 审批服务接口
    public interface IApprovalService
    {
        Task SubmitApprovalTasksAsync(List<ApprovalTask> tasks);
        Task<List<ApprovalResult>> WaitForApprovalResultsAsync(List<string> taskIds, TimeSpan? timeout = null);
        Task<List<ApprovalTask>> GetPendingApprovalsAsync(string userId);
        Task ApproveTaskAsync(string taskId, string userId, bool isApproved, string comment);
    }

    // 通知服务接口
    public interface INotificationService
    {
        Task SendNotificationAsync(NotificationRequest request);
        Task SendEmailAsync(string to, string subject, string body);
        Task SendSmsAsync(string phoneNumber, string message);
    }

    // 用户服务接口
    public interface IUserService
    {
        Task<User> GetUserAsync(string userId);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetUsersByRoleAsync(string role);
    }
}