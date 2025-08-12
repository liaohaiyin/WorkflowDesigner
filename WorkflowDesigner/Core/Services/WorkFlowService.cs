using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.Infrastructure.Data;

namespace WorkflowDesigner.Core.Services
{
    // 工作流仓储实现
    public class WorkflowRepository : IWorkflowRepository, IDisposable
    {
        private readonly WorkflowDbContext _context;
        private readonly bool _disposeContext;

        public WorkflowRepository(WorkflowDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _disposeContext = false; // 外部传入的context，不由本类负责释放
        }

        // 添加一个默认构造函数，用于创建自己的context
        public WorkflowRepository()
        {
            _context = new WorkflowDbContext();
            _disposeContext = true; // 自己创建的context，需要释放
        }

        public async Task<WorkflowDefinition> GetWorkflowDefinitionAsync(string id)
        {
            try
            {
                return await _context.WorkflowDefinitions.FindAsync(id);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"获取工作流定义失败: {ex.Message}", ex);
            }
        }

        public async Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
        {
            try
            {
                return await _context.WorkflowDefinitions.Where(w => w.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"获取所有工作流定义失败: {ex.Message}", ex);
            }
        }

        public async Task SaveWorkflowDefinitionAsync(WorkflowDefinition definition)
        {
            try
            {
                _context.WorkflowDefinitions.Add(definition);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"保存工作流定义失败: {ex.Message}", ex);
            }
        }

        public async Task UpdateWorkflowDefinitionAsync(WorkflowDefinition definition)
        {
            try
            {
                definition.UpdatedTime = DateTime.Now;
                _context.Entry(definition).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"更新工作流定义失败: {ex.Message}", ex);
            }
        }

        public async Task DeleteWorkflowDefinitionAsync(string id)
        {
            try
            {
                var definition = await _context.WorkflowDefinitions.FindAsync(id);
                if (definition != null)
                {
                    definition.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"删除工作流定义失败: {ex.Message}", ex);
            }
        }

        public async Task<WorkflowInstance> GetWorkflowInstanceAsync(string id)
        {
            try
            {
                return await _context.WorkflowInstances
                    .Include(wi => wi.Definition)
                    .Include(wi => wi.NodeExecutions)
                    .FirstOrDefaultAsync(wi => wi.Id == id);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"获取工作流实例失败: {ex.Message}", ex);
            }
        }

        public async Task<List<WorkflowInstance>> GetActiveWorkflowsAsync()
        {
            try
            {
                return await _context.WorkflowInstances
                    .Include(wi => wi.Definition)
                    .Where(wi => wi.Status == WorkflowInstanceStatus.Running || wi.Status == WorkflowInstanceStatus.Paused)
                    .OrderByDescending(wi => wi.StartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"获取活动工作流失败: {ex.Message}", ex);
            }
        }

        public async Task<List<WorkflowInstance>> GetWorkflowsByUserAsync(string userId)
        {
            try
            {
                return await _context.WorkflowInstances
                    .Include(wi => wi.Definition)
                    .Where(wi => wi.StartedBy == userId)
                    .OrderByDescending(wi => wi.StartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"获取用户工作流失败: {ex.Message}", ex);
            }
        }

        public async Task SaveWorkflowInstanceAsync(WorkflowInstance instance)
        {
            try
            {
                _context.WorkflowInstances.Add(instance);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"保存工作流实例失败: {ex.Message}", ex);
            }
        }

        public async Task UpdateWorkflowInstanceAsync(WorkflowInstance instance)
        {
            try
            {
                _context.Entry(instance).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"更新工作流实例失败: {ex.Message}", ex);
            }
        }

        public async Task SaveNodeExecutionAsync(WorkflowNodeExecution execution)
        {
            try
            {
                _context.WorkflowNodeExecutions.Add(execution);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"保存节点执行记录失败: {ex.Message}", ex);
            }
        }

        public async Task UpdateNodeExecutionAsync(WorkflowNodeExecution execution)
        {
            try
            {
                _context.Entry(execution).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"更新节点执行记录失败: {ex.Message}", ex);
            }
        }

        public async Task<List<WorkflowNodeExecution>> GetNodeExecutionsAsync(string instanceId)
        {
            try
            {
                return await _context.WorkflowNodeExecutions
                    .Where(ne => ne.InstanceId == instanceId)
                    .OrderBy(ne => ne.StartTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"获取节点执行记录失败: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (_disposeContext && _context != null)
            {
                _context.Dispose();
            }
        }
    }

    // 审批服务实现
    public class ApprovalService : IApprovalService, IDisposable
    {
        private readonly WorkflowDbContext _context;
        private readonly bool _disposeContext;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ApprovalService(WorkflowDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _disposeContext = false;
        }

        public ApprovalService()
        {
            _context = new WorkflowDbContext();
            _disposeContext = true;
        }

        public async Task SubmitApprovalTasksAsync(List<ApprovalTask> tasks)
        {
            try
            {
                _context.ApprovalTasks.AddRange(tasks);
                await _context.SaveChangesAsync();

                Logger.Info($"提交了 {tasks.Count} 个审批任务");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "提交审批任务失败");
                throw new ApplicationException($"提交审批任务失败: {ex.Message}", ex);
            }
        }

        public async Task<List<ApprovalResult>> WaitForApprovalResultsAsync(List<string> taskIds, TimeSpan? timeout = null)
        {
            try
            {
                var results = new List<ApprovalResult>();
                var startTime = DateTime.Now;
                var timeoutTime = timeout.HasValue ? startTime.Add(timeout.Value) : DateTime.MaxValue;

                while (results.Count < taskIds.Count && DateTime.Now < timeoutTime)
                {
                    var completedTasks = await _context.ApprovalTasks
                        .Where(t => taskIds.Contains(t.Id) && t.Status != ApprovalTaskStatus.Pending)
                        .ToListAsync();

                    foreach (var task in completedTasks)
                    {
                        if (!results.Any(r => r.TaskId == task.Id))
                        {
                            results.Add(new ApprovalResult
                            {
                                TaskId = task.Id,
                                IsApproved = task.Status == ApprovalTaskStatus.Approved,
                                Comment = task.ApprovalComment,
                                ApprovedTime = task.ApprovedTime,
                                ApproverId = task.ApproverId
                            });
                        }
                    }

                    if (results.Count < taskIds.Count)
                    {
                        await Task.Delay(1000); // 等待1秒后重新检查
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "等待审批结果失败");
                throw new ApplicationException($"等待审批结果失败: {ex.Message}", ex);
            }
        }

        public async Task<List<ApprovalTask>> GetPendingApprovalsAsync(string userId)
        {
            try
            {
                return await _context.ApprovalTasks
                    .Where(t => t.ApproverId == userId && t.Status == ApprovalTaskStatus.Pending)
                    .OrderBy(t => t.CreatedTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取待审批任务失败");
                throw new ApplicationException($"获取待审批任务失败: {ex.Message}", ex);
            }
        }

        public async Task ApproveTaskAsync(string taskId, string userId, bool isApproved, string comment)
        {
            try
            {
                var task = await _context.ApprovalTasks.FindAsync(taskId);
                if (task != null && task.ApproverId == userId && task.Status == ApprovalTaskStatus.Pending)
                {
                    task.Status = isApproved ? ApprovalTaskStatus.Approved : ApprovalTaskStatus.Rejected;
                    task.IsApproved = isApproved;
                    task.ApprovalComment = comment;
                    task.ApprovedTime = DateTime.Now;

                    await _context.SaveChangesAsync();
                    Logger.Info($"用户 {userId} {(isApproved ? "同意" : "拒绝")} 了审批任务 {taskId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "审批任务处理失败");
                throw new ApplicationException($"审批任务处理失败: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (_disposeContext && _context != null)
            {
                _context.Dispose();
            }
        }
    }

    // 通知服务实现
    public class NotificationService : INotificationService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public async Task SendNotificationAsync(NotificationRequest request)
        {
            foreach (var recipient in request.Recipients)
            {
                try
                {
                    switch (request.Type.ToLower())
                    {
                        case "email":
                            await SendEmailAsync(recipient, request.Title, request.Content);
                            break;
                        case "sms":
                            await SendSmsAsync(recipient, $"{request.Title}: {request.Content}");
                            break;
                        default:
                            Logger.Warn($"不支持的通知类型: {request.Type}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"发送通知失败: {recipient}");
                }
            }
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // 实际项目中这里应该集成真实的邮件服务
            Logger.Info($"发送邮件 - 收件人: {to}, 主题: {subject}");
            await Task.Delay(100); // 模拟邮件发送
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            // 实际项目中这里应该集成短信服务
            Logger.Info($"发送短信 - 手机号: {phoneNumber}, 内容: {message}");
            await Task.Delay(100); // 模拟短信发送
        }
    }

    // 用户服务实现
    public class UserService : IUserService
    {
        private static readonly List<User> _users = new List<User>
        {
            new User { Id = "1", Name = "张三", Email = "zhangsan@company.com", Role = "Manager" },
            new User { Id = "2", Name = "李四", Email = "lisi@company.com", Role = "Employee" },
            new User { Id = "3", Name = "王五", Email = "wangwu@company.com", Role = "Employee" },
            new User { Id = "4", Name = "赵六", Email = "zhaoliu@company.com", Role = "HR" },
            new User { Id = "5", Name = "孙七", Email = "sunqi@company.com", Role = "Finance" }
        };

        public async Task<User> GetUserAsync(string userId)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await Task.FromResult(_users.ToList());
        }

        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            return await Task.FromResult(_users.Where(u => u.Role == role).ToList());
        }
    }

    // 数据传输对象
    public class ApprovalResult
    {
        public string TaskId { get; set; }
        public bool IsApproved { get; set; }
        public string Comment { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public string ApproverId { get; set; }
    }

    public class NotificationRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public List<string> Recipients { get; set; }
        public string Type { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class WorkflowConnection
    {
        public string Id { get; set; }
        public string SourceNodeId { get; set; }
        public string SourcePortName { get; set; }
        public string TargetNodeId { get; set; }
        public string TargetPortName { get; set; }
        public string Condition { get; set; }
    }
}
