using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using WorkflowDesigner.Core.Models;

namespace WorkflowDesigner.Infrastructure.Data
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class WorkflowDbContext : DbContext
    {
        static WorkflowDbContext()
        {
            // 使用静态构造函数设置初始化器
            System.Data.Entity.Database.SetInitializer(new WorkflowDbInitializer());
        }

        public WorkflowDbContext() : base("WorkflowDesignerMySqlConnection")
        {
            // 配置Entity Framework使用MySQL
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.ValidateOnSaveEnabled = false;
        }

        public WorkflowDbContext(string connectionString) : base(connectionString)
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.ValidateOnSaveEnabled = false;
        }

        public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
        public DbSet<WorkflowNodeExecution> WorkflowNodeExecutions { get; set; }
        public DbSet<ApprovalTask> ApprovalTasks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // 移除EF的默认表名复数化约定
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // 配置WorkflowDefinition
            modelBuilder.Entity<WorkflowDefinition>()
                .ToTable("workflow_definitions")
                .HasKey(e => e.Id);

            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.Id).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.Name).HasMaxLength(200).IsRequired();
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.Description).HasMaxLength(1000);
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.Version).HasMaxLength(50);
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.Category).HasMaxLength(100);
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.NodesJson).HasColumnType("LONGTEXT");
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.ConnectionsJson).HasColumnType("LONGTEXT");
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.StartNodeId).HasMaxLength(36);
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.CreatedBy).HasMaxLength(100);
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.CreatedTime).HasColumnType("DATETIME");
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.UpdatedTime).HasColumnType("DATETIME");
            modelBuilder.Entity<WorkflowDefinition>()
                .Property(e => e.IsActive).IsRequired();

            // 配置WorkflowInstance
            modelBuilder.Entity<WorkflowInstance>()
                .ToTable("workflow_instances")
                .HasKey(e => e.Id);

            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.Id).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.DefinitionId).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.Status).IsRequired();
            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.CurrentNodeId).HasMaxLength(36);
            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.DataJson).HasColumnType("LONGTEXT");
            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.StartTime).HasColumnType("DATETIME").IsRequired();
            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.EndTime).HasColumnType("DATETIME");
            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.StartedBy).HasMaxLength(100);
            modelBuilder.Entity<WorkflowInstance>()
                .Property(e => e.ErrorMessage).HasMaxLength(1000);

            // 配置外键关系
            modelBuilder.Entity<WorkflowInstance>()
                .HasRequired(wi => wi.Definition)
                .WithMany()
                .HasForeignKey(wi => wi.DefinitionId)
                .WillCascadeOnDelete(false);

            // 配置WorkflowNodeExecution
            modelBuilder.Entity<WorkflowNodeExecution>()
                .ToTable("workflow_node_executions")
                .HasKey(e => e.Id);

            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.Id).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.InstanceId).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.NodeId).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.NodeName).HasMaxLength(200);
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.Status).IsRequired();
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.StartTime).HasColumnType("DATETIME");
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.EndTime).HasColumnType("DATETIME");
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.ExecutorId).HasMaxLength(100);
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.ErrorMessage).HasMaxLength(1000);
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.InputDataJson).HasColumnType("TEXT");
            modelBuilder.Entity<WorkflowNodeExecution>()
                .Property(e => e.OutputDataJson).HasColumnType("TEXT");

            // 配置外键关系
            modelBuilder.Entity<WorkflowNodeExecution>()
                .HasRequired(wne => wne.Instance)
                .WithMany(wi => wi.NodeExecutions)
                .HasForeignKey(wne => wne.InstanceId)
                .WillCascadeOnDelete(true);

            // 配置ApprovalTask
            modelBuilder.Entity<ApprovalTask>()
                .ToTable("approval_tasks")
                .HasKey(e => e.Id);

            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.Id).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.InstanceId).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.NodeId).HasMaxLength(36).IsRequired();
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.Title).HasMaxLength(200).IsRequired();
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.Content).HasMaxLength(2000);
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.ApproverId).HasMaxLength(100);
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.Status).IsRequired();
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.CreatedTime).HasColumnType("DATETIME").IsRequired();
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.ApprovedTime).HasColumnType("DATETIME");
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.ApprovalComment).HasMaxLength(1000);
            modelBuilder.Entity<ApprovalTask>()
                .Property(e => e.IsApproved).IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }

    public class WorkflowDbInitializer : CreateDatabaseIfNotExists<WorkflowDbContext>
    {
        protected override void Seed(WorkflowDbContext context)
        {
            try
            {
                // 检查数据库是否已经有数据
                if (context.WorkflowDefinitions.Any())
                {
                    return; // 数据库已经有数据，不需要重新初始化
                }

                // 创建示例工作流定义 - 请假审批流程
                var leaveWorkflow = new WorkflowDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "请假审批流程",
                    Description = "员工请假申请审批工作流，包含部门经理审批环节",
                    Version = "1.0",
                    Category = "人事管理",
                    CreatedBy = "system",
                    CreatedTime = DateTime.Now,
                    IsActive = true,
                    NodesJson = @"[
                        {
                            ""NodeId"": ""start_001"",
                            ""NodeName"": ""开始"",
                            ""NodeType"": ""Start"",
                            ""Position"": { ""X"": 100, ""Y"": 100 }
                        },
                        {
                            ""NodeId"": ""approval_001"",
                            ""NodeName"": ""部门经理审批"",
                            ""NodeType"": ""Approval"",
                            ""Position"": { ""X"": 300, ""Y"": 100 },
                            ""ApprovalTitle"": ""请假申请审批"",
                            ""ApprovalContent"": ""请审批员工请假申请"",
                            ""RequireAllApproval"": false,
                            ""Approvers"": [""1""]
                        },
                        {
                            ""NodeId"": ""notification_001"",
                            ""NodeName"": ""通知申请人"",
                            ""NodeType"": ""Notification"",
                            ""Position"": { ""X"": 500, ""Y"": 100 },
                            ""NotificationTitle"": ""请假审批结果通知"",
                            ""NotificationContent"": ""您的请假申请已处理完成"",
                            ""NotificationType"": ""Email""
                        },
                        {
                            ""NodeId"": ""end_001"",
                            ""NodeName"": ""结束"",
                            ""NodeType"": ""End"",
                            ""Position"": { ""X"": 700, ""Y"": 100 }
                        }
                    ]",
                    ConnectionsJson = @"[
                        {
                            ""Id"": ""conn_001"",
                            ""SourceNodeId"": ""start_001"",
                            ""SourcePortName"": ""输出"",
                            ""TargetNodeId"": ""approval_001"",
                            ""TargetPortName"": ""输入""
                        },
                        {
                            ""Id"": ""conn_002"",
                            ""SourceNodeId"": ""approval_001"",
                            ""SourcePortName"": ""同意"",
                            ""TargetNodeId"": ""notification_001"",
                            ""TargetPortName"": ""输入""
                        },
                        {
                            ""Id"": ""conn_003"",
                            ""SourceNodeId"": ""notification_001"",
                            ""SourcePortName"": ""完成"",
                            ""TargetNodeId"": ""end_001"",
                            ""TargetPortName"": ""输入""
                        }
                    ]",
                    StartNodeId = "start_001"
                };

                // 创建示例工作流定义 - 费用报销流程
                var expenseWorkflow = new WorkflowDefinition
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "费用报销流程",
                    Description = "员工费用报销审批工作流，包含金额判断和多级审批",
                    Version = "1.0",
                    Category = "财务管理",
                    CreatedBy = "system",
                    CreatedTime = DateTime.Now,
                    IsActive = true,
                    NodesJson = @"[
                        {
                            ""NodeId"": ""start_002"",
                            ""NodeName"": ""开始"",
                            ""NodeType"": ""Start"",
                            ""Position"": { ""X"": 100, ""Y"": 100 }
                        },
                        {
                            ""NodeId"": ""decision_001"",
                            ""NodeName"": ""金额判断"",
                            ""NodeType"": ""Decision"",
                            ""Position"": { ""X"": 300, ""Y"": 100 },
                            ""ConditionExpression"": ""{Amount} > 1000""
                        },
                        {
                            ""NodeId"": ""approval_002"",
                            ""NodeName"": ""部门经理审批"",
                            ""NodeType"": ""Approval"",
                            ""Position"": { ""X"": 500, ""Y"": 50 },
                            ""ApprovalTitle"": ""费用报销审批（小额）"",
                            ""ApprovalContent"": ""请审批费用报销申请"",
                            ""RequireAllApproval"": false,
                            ""Approvers"": [""1""]
                        },
                        {
                            ""NodeId"": ""approval_003"",
                            ""NodeName"": ""财务总监审批"",
                            ""NodeType"": ""Approval"",
                            ""Position"": { ""X"": 500, ""Y"": 150 },
                            ""ApprovalTitle"": ""费用报销审批（大额）"",
                            ""ApprovalContent"": ""请审批大额费用报销申请"",
                            ""RequireAllApproval"": false,
                            ""Approvers"": [""5""]
                        },
                        {
                            ""NodeId"": ""end_002"",
                            ""NodeName"": ""结束"",
                            ""NodeType"": ""End"",
                            ""Position"": { ""X"": 700, ""Y"": 100 }
                        }
                    ]",
                    ConnectionsJson = @"[
                        {
                            ""Id"": ""conn_004"",
                            ""SourceNodeId"": ""start_002"",
                            ""SourcePortName"": ""输出"",
                            ""TargetNodeId"": ""decision_001"",
                            ""TargetPortName"": ""输入""
                        },
                        {
                            ""Id"": ""conn_005"",
                            ""SourceNodeId"": ""decision_001"",
                            ""SourcePortName"": ""否"",
                            ""TargetNodeId"": ""approval_002"",
                            ""TargetPortName"": ""输入""
                        },
                        {
                            ""Id"": ""conn_006"",
                            ""SourceNodeId"": ""decision_001"",
                            ""SourcePortName"": ""是"",
                            ""TargetNodeId"": ""approval_003"",
                            ""TargetPortName"": ""输入""
                        },
                        {
                            ""Id"": ""conn_007"",
                            ""SourceNodeId"": ""approval_002"",
                            ""SourcePortName"": ""同意"",
                            ""TargetNodeId"": ""end_002"",
                            ""TargetPortName"": ""输入""
                        },
                        {
                            ""Id"": ""conn_008"",
                            ""SourceNodeId"": ""approval_003"",
                            ""SourcePortName"": ""同意"",
                            ""TargetNodeId"": ""end_002"",
                            ""TargetPortName"": ""输入""
                        }
                    ]",
                    StartNodeId = "start_002"
                };

                context.WorkflowDefinitions.Add(leaveWorkflow);
                context.WorkflowDefinitions.Add(expenseWorkflow);

                context.SaveChanges();

                System.Diagnostics.Debug.WriteLine("数据库种子数据创建成功");
            }
            catch (Exception ex)
            {
                // 记录种子数据创建失败的异常
                System.Diagnostics.Debug.WriteLine($"种子数据创建失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }

            base.Seed(context);
        }
    }

    // MySQL Entity Framework 配置
    public class MySqlConfiguration : DbConfiguration
    {
        public MySqlConfiguration()
        {
            SetExecutionStrategy("MySql.Data.MySqlClient", () => new MySqlExecutionStrategy());
            SetDefaultConnectionFactory(new MySqlConnectionFactory());
        }
    }

    // MySQL连接工厂
    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        public System.Data.Common.DbConnection CreateConnection(string nameOrConnectionString)
        {
            return new MySql.Data.MySqlClient.MySqlConnection(nameOrConnectionString);
        }
    }

    // MySQL执行策略
    public class MySqlExecutionStrategy : DbExecutionStrategy
    {
        public MySqlExecutionStrategy() : base()
        {
        }

        protected override bool ShouldRetryOn(Exception exception)
        {
            if (exception is MySql.Data.MySqlClient.MySqlException mysqlException)
            {
                switch (mysqlException.Number)
                {
                    case 1042: // ER_BAD_HOST_ERROR
                    case 1043: // ER_HANDSHAKE_ERROR
                    case 1047: // ER_UNKNOWN_COM_ERROR
                    case 1053: // ER_SERVER_SHUTDOWN
                    case 1205: // ER_LOCK_WAIT_TIMEOUT
                    case 1213: // ER_LOCK_DEADLOCK
                    case 2003: // CR_CONN_HOST_ERROR
                    case 2006: // CR_SERVER_GONE_ERROR
                    case 2013: // CR_SERVER_LOST
                        return true;
                }
            }
            return false;
        }
    }
}