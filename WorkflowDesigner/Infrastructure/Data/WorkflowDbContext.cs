using System.Data.Entity;
using WorkflowDesigner.Core.Models;

namespace WorkflowDesigner.Infrastructure.Data
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext() : base("WorkflowDesignerConnection")
        {
            Database.SetInitializer(new WorkflowDbInitializer());
        }

        public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; }
        public DbSet<WorkflowNodeExecution> WorkflowNodeExecutions { get; set; }
        public DbSet<ApprovalTask> ApprovalTasks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // 配置实体关系
            modelBuilder.Entity<WorkflowInstance>()
                .HasRequired(wi => wi.Definition)
                .WithMany()
                .HasForeignKey(wi => wi.DefinitionId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<WorkflowNodeExecution>()
                .HasRequired(wne => wne.Instance)
                .WithMany(wi => wi.NodeExecutions)
                .HasForeignKey(wne => wne.InstanceId)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class WorkflowDbInitializer : CreateDatabaseIfNotExists<WorkflowDbContext>
    {
        protected override void Seed(WorkflowDbContext context)
        {
            // 种子数据
            base.Seed(context);
        }
    }
}