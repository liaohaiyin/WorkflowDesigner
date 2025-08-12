using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WorkflowDesigner.Infrastructure.Data;

namespace WorkflowDesigner.Infrastructure.Database
{
    /// <summary>
    /// 负责数据库连接、初始化和维护
    /// </summary>
    public class EnhancedDatabaseManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;
        private readonly string _environment;

        public EnhancedDatabaseManager()
        {
            _environment = ConfigurationManager.AppSettings["DatabaseEnvironment"] ?? "Development";
            _connectionString = GetConnectionString();
        }

        public EnhancedDatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        private string GetConnectionString()
        {
            string connectionName;
            switch (_environment)
            {
                case "Production":
                    connectionName = "WorkflowDesignerMySqlProd";
                    break;
                case "Test":
                    connectionName = "WorkflowDesignerMySqlTest";
                    break;
                default:
                    connectionName = "WorkflowDesignerMySqlConnection";
                    break;
            }

            var connectionString = ConfigurationManager.ConnectionStrings[connectionName]?.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ConfigurationErrorsException($"未找到连接字符串配置: {connectionName}");
            }

            return connectionString;
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Logger.Info("数据库连接测试成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "数据库连接测试失败");
                return false;
            }
        }

        /// <summary>
        /// 完整初始化数据库
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                Logger.Info("开始初始化数据库...");

                // 1. 检查数据库是否存在，如果不存在则创建
                await EnsureDatabaseExistsAsync();

                // 2. 验证数据库连接
                if (!await TestConnectionAsync())
                {
                    Logger.Error("数据库连接验证失败");
                    return false;
                }

                // 3. 初始化Entity Framework上下文
                using (var context = new WorkflowDbContext(_connectionString))
                {
                    // 设置命令超时时间
                    context.Database.CommandTimeout = 120;

                    // 检查数据库是否存在，如果不存在则创建
                    if (!context.Database.Exists())
                    {
                        Logger.Info("数据库不存在，正在创建...");
                        context.Database.Create();
                        Logger.Info("数据库创建成功");
                    }
                    else
                    {
                        Logger.Info("数据库已存在，检查表结构...");

                        // 验证表结构
                        await ValidateTableStructureAsync(context);
                    }

                    // 4. 确保数据库迁移到最新版本
                    try
                    {
                        context.Database.Initialize(force: false);
                        Logger.Info("数据库初始化完成");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "数据库初始化过程中出现警告，但继续执行");
                    }

                    // 5. 验证基本表结构
                    await VerifyEssentialTablesAsync();
                }

                Logger.Info("数据库完整初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "数据库初始化失败");
                return false;
            }
        }

        /// <summary>
        /// 确保数据库存在
        /// </summary>
        private async Task EnsureDatabaseExistsAsync()
        {
            var builder = new MySqlConnectionStringBuilder(_connectionString);
            var databaseName = builder.Database;
            var serverConnectionString = builder.ConnectionString.Replace($"Database={databaseName};", "");

            using (var connection = new MySqlConnection(serverConnectionString))
            {
                await connection.OpenAsync();
                Logger.Info($"已连接到MySQL服务器: {builder.Server}:{builder.Port}");

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                    await command.ExecuteNonQueryAsync();
                    Logger.Info($"确保数据库 {databaseName} 存在");
                }
            }
        }

        /// <summary>
        /// 验证表结构
        /// </summary>
        private async Task ValidateTableStructureAsync(WorkflowDbContext context)
        {
            try
            {
                var requiredTables = new[] {
                    "workflow_definitions",
                    "workflow_instances",
                    "workflow_node_executions",
                    "approval_tasks"
                };

                foreach (var tableName in requiredTables)
                {
                    var tableExists = await context.Database.SqlQuery<int>(
                        $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'"
                    ).FirstOrDefaultAsync();

                    if (tableExists == 0)
                    {
                        Logger.Warn($"表 {tableName} 不存在，将通过EF创建");
                    }
                    else
                    {
                        Logger.Info($"表 {tableName} 已存在");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "验证表结构时发生错误");
            }
        }

        /// <summary>
        /// 验证核心表是否存在
        /// </summary>
        private async Task VerifyEssentialTablesAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var requiredTables = new[] {
                        "workflow_definitions",
                        "workflow_instances",
                        "workflow_node_executions",
                        "approval_tasks"
                    };

                    foreach (var tableName in requiredTables)
                    {
                        using (var command = new MySqlCommand(
                            $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'",
                            connection))
                        {
                            var exists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
                            if (exists)
                            {
                                Logger.Info($"验证表 {tableName} 存在");
                            }
                            else
                            {
                                Logger.Error($"核心表 {tableName} 不存在！");
                                throw new InvalidOperationException($"数据库缺少必需的表: {tableName}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "验证核心表时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 创建缺失的表
        /// </summary>
        public async Task<bool> CreateMissingTablesAsync()
        {
            try
            {
                Logger.Info("开始创建缺失的表...");

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 创建表的SQL脚本
                    var createTableScripts = new Dictionary<string, string>
                    {
                        ["workflow_definitions"] = @"
                            CREATE TABLE IF NOT EXISTS `workflow_definitions` (
                              `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
                              `Name` VARCHAR(200) NOT NULL,
                              `Description` VARCHAR(1000) NULL,
                              `Version` VARCHAR(50) NOT NULL DEFAULT '1.0',
                              `Category` VARCHAR(100) NULL,
                              `NodesJson` LONGTEXT NULL,
                              `ConnectionsJson` LONGTEXT NULL,
                              `StartNodeId` VARCHAR(36) NULL,
                              `CreatedTime` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                              `UpdatedTime` DATETIME NULL,
                              `CreatedBy` VARCHAR(100) NULL,
                              `IsActive` BOOLEAN NOT NULL DEFAULT TRUE,
                              INDEX `idx_name` (`Name`),
                              INDEX `idx_category` (`Category`),
                              INDEX `idx_created_time` (`CreatedTime`)
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",

                        ["workflow_instances"] = @"
                            CREATE TABLE IF NOT EXISTS `workflow_instances` (
                              `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
                              `DefinitionId` VARCHAR(36) NOT NULL,
                              `Status` INT NOT NULL DEFAULT 1,
                              `CurrentNodeId` VARCHAR(36) NULL,
                              `DataJson` LONGTEXT NULL,
                              `StartTime` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                              `EndTime` DATETIME NULL,
                              `StartedBy` VARCHAR(100) NULL,
                              `ErrorMessage` VARCHAR(1000) NULL,
                              INDEX `idx_definition_id` (`DefinitionId`),
                              INDEX `idx_status` (`Status`),
                              INDEX `idx_start_time` (`StartTime`),
                              INDEX `idx_started_by` (`StartedBy`)
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",

                        ["workflow_node_executions"] = @"
                            CREATE TABLE IF NOT EXISTS `workflow_node_executions` (
                              `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
                              `InstanceId` VARCHAR(36) NOT NULL,
                              `NodeId` VARCHAR(36) NOT NULL,
                              `NodeName` VARCHAR(200) NULL,
                              `Status` INT NOT NULL DEFAULT 1,
                              `StartTime` DATETIME NULL,
                              `EndTime` DATETIME NULL,
                              `ExecutorId` VARCHAR(100) NULL,
                              `ErrorMessage` VARCHAR(1000) NULL,
                              `InputDataJson` TEXT NULL,
                              `OutputDataJson` TEXT NULL,
                              INDEX `idx_instance_id` (`InstanceId`),
                              INDEX `idx_node_id` (`NodeId`),
                              INDEX `idx_status` (`Status`),
                              INDEX `idx_start_time` (`StartTime`)
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",

                        ["approval_tasks"] = @"
                            CREATE TABLE IF NOT EXISTS `approval_tasks` (
                              `Id` VARCHAR(36) NOT NULL PRIMARY KEY,
                              `InstanceId` VARCHAR(36) NOT NULL,
                              `NodeId` VARCHAR(36) NOT NULL,
                              `Title` VARCHAR(200) NOT NULL,
                              `Content` VARCHAR(2000) NULL,
                              `ApproverId` VARCHAR(100) NULL,
                              `Status` INT NOT NULL DEFAULT 1,
                              `CreatedTime` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                              `ApprovedTime` DATETIME NULL,
                              `ApprovalComment` VARCHAR(1000) NULL,
                              `IsApproved` BOOLEAN NOT NULL DEFAULT FALSE,
                              INDEX `idx_instance_id` (`InstanceId`),
                              INDEX `idx_node_id` (`NodeId`),
                              INDEX `idx_approver_id` (`ApproverId`),
                              INDEX `idx_status` (`Status`),
                              INDEX `idx_created_time` (`CreatedTime`)
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;"
                    };

                    foreach (var script in createTableScripts)
                    {
                        try
                        {
                            using (var command = new MySqlCommand(script.Value, connection))
                            {
                                command.CommandTimeout = 60;
                                await command.ExecuteNonQueryAsync();
                                Logger.Info($"表 {script.Key} 创建成功");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"创建表 {script.Key} 失败");
                            throw;
                        }
                    }

                    // 添加外键约束
                    await AddForeignKeyConstraintsAsync(connection);
                }

                Logger.Info("缺失的表创建完成");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建缺失的表时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 添加外键约束
        /// </summary>
        private async Task AddForeignKeyConstraintsAsync(MySqlConnection connection)
        {
            try
            {
                var foreignKeyScripts = new[]
                {
                    @"ALTER TABLE `workflow_instances` 
                      ADD CONSTRAINT `fk_instances_definitions` 
                      FOREIGN KEY (`DefinitionId`) REFERENCES `workflow_definitions`(`Id`) 
                      ON DELETE RESTRICT;",

                    @"ALTER TABLE `workflow_node_executions` 
                      ADD CONSTRAINT `fk_executions_instances` 
                      FOREIGN KEY (`InstanceId`) REFERENCES `workflow_instances`(`Id`) 
                      ON DELETE CASCADE;",

                    @"ALTER TABLE `approval_tasks` 
                      ADD CONSTRAINT `fk_tasks_instances` 
                      FOREIGN KEY (`InstanceId`) REFERENCES `workflow_instances`(`Id`) 
                      ON DELETE CASCADE;"
                };

                foreach (var script in foreignKeyScripts)
                {
                    try
                    {
                        using (var command = new MySqlCommand(script, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    catch (MySqlException ex) when (ex.Number == 1061) // Duplicate key name
                    {
                        // 外键约束已存在，忽略此错误
                        Logger.Info("外键约束已存在，跳过创建");
                    }
                }

                Logger.Info("外键约束添加完成");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "添加外键约束时发生警告，但不影响主要功能");
            }
        }

        /// <summary>
        /// 插入示例数据
        /// </summary>
        public async Task<bool> InsertSampleDataAsync()
        {
            try
            {
                Logger.Info("开始插入示例数据...");

                using (var context = new WorkflowDbContext(_connectionString))
                {
                    // 检查是否已有数据
                    if (await context.WorkflowDefinitions.AnyAsync())
                    {
                        Logger.Info("数据库已有数据，跳过示例数据插入");
                        return true;
                    }

                    // 触发种子数据创建
                    context.Database.Initialize(force: false);
                }

                Logger.Info("示例数据插入完成");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "插入示例数据失败");
                return false;
            }
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        public async Task<bool> BackupDatabaseAsync(string backupPath)
        {
            try
            {
                Logger.Info($"开始备份数据库到: {backupPath}");

                var builder = new MySqlConnectionStringBuilder(_connectionString);
                var backupCommand = $"mysqldump --host={builder.Server} --port={builder.Port} --user={builder.UserID} --password={builder.Password} --single-transaction --routines --triggers {builder.Database} > \"{backupPath}\"";

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {backupCommand}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        Logger.Info("数据库备份成功");
                        return true;
                    }
                    else
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        Logger.Error($"数据库备份失败: {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "数据库备份过程中发生错误");
                return false;
            }
        }

        /// <summary>
        /// 获取数据库信息
        /// </summary>
        public async Task<DatabaseInfo> GetDatabaseInfoAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var info = new DatabaseInfo();

                    // 获取数据库版本
                    using (var command = new MySqlCommand("SELECT VERSION()", connection))
                    {
                        info.Version = (await command.ExecuteScalarAsync())?.ToString();
                    }

                    // 获取数据库大小
                    var builder = new MySqlConnectionStringBuilder(_connectionString);
                    using (var command = new MySqlCommand($@"
                        SELECT 
                            ROUND(SUM(data_length + index_length) / 1024 / 1024, 2) AS DbSizeMB
                        FROM information_schema.tables 
                        WHERE table_schema = '{builder.Database}'", connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        info.SizeMB = Convert.ToDouble(result ?? 0);
                    }

                    // 获取表数量
                    using (var command = new MySqlCommand($@"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = '{builder.Database}'", connection))
                    {
                        info.TableCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }

                    // 获取连接数
                    using (var command = new MySqlCommand("SHOW STATUS LIKE 'Threads_connected'", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                info.ActiveConnections = Convert.ToInt32(reader["Value"]);
                            }
                        }
                    }

                    info.ServerName = builder.Server;
                    info.DatabaseName = builder.Database;
                    info.Port = (int)builder.Port;

                    return info;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取数据库信息失败");
                throw;
            }
        }

        /// <summary>
        /// 执行健康检查
        /// </summary>
        public async Task<HealthCheckResult> PerformHealthCheckAsync()
        {
            var result = new HealthCheckResult
            {
                CheckTime = DateTime.Now,
                IsHealthy = true,
                Details = new Dictionary<string, object>()
            };

            try
            {
                // 测试连接
                var connectionTest = await TestConnectionAsync();
                result.Details["ConnectionTest"] = connectionTest;

                if (!connectionTest)
                {
                    result.IsHealthy = false;
                    result.ErrorMessage = "数据库连接失败";
                    return result;
                }

                // 获取数据库信息
                var dbInfo = await GetDatabaseInfoAsync();
                result.Details["DatabaseInfo"] = dbInfo;

                // 检查表是否存在
                using (var context = new WorkflowDbContext(_connectionString))
                {
                    var tableExists = await context.Database.SqlQuery<int>(@"
                        SELECT COUNT(*) FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_name IN ('workflow_definitions', 'workflow_instances', 'workflow_node_executions', 'approval_tasks')
                    ").FirstOrDefaultAsync();

                    result.Details["RequiredTablesExist"] = tableExists == 4;

                    if (tableExists != 4)
                    {
                        result.IsHealthy = false;
                        result.ErrorMessage = $"缺少必需的数据库表，当前只有 {tableExists} 个表";

                        // 尝试自动修复
                        Logger.Warn("检测到缺少数据库表，尝试自动创建...");
                        if (await CreateMissingTablesAsync())
                        {
                            result.Warnings.Add("已自动创建缺失的数据库表");
                            result.IsHealthy = true;
                            result.ErrorMessage = null;
                        }
                    }
                }

                // 检查数据库大小
                if (dbInfo.SizeMB > 1000) // 如果数据库超过1GB
                {
                    result.Warnings.Add("数据库大小超过1GB，建议进行数据清理");
                }

                // 检查活动连接数
                if (dbInfo.ActiveConnections > 50)
                {
                    result.Warnings.Add("活动连接数较高，请检查连接池配置");
                }

                Logger.Info("数据库健康检查完成");
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.ErrorMessage = ex.Message;
                Logger.Error(ex, "数据库健康检查失败");
            }

            return result;
        }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        public WorkflowDbContext CreateDbContext()
        {
            return new WorkflowDbContext(_connectionString);
        }
    }

    /// <summary>
    /// 数据库信息
    /// </summary>
    public class DatabaseInfo
    {
        public string Version { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public int Port { get; set; }
        public double SizeMB { get; set; }
        public int TableCount { get; set; }
        public int ActiveConnections { get; set; }
    }

    /// <summary>
    /// 健康检查结果
    /// </summary>
    public class HealthCheckResult
    {
        public DateTime CheckTime { get; set; }
        public bool IsHealthy { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }
}