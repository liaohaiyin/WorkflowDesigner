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
    /// 数据库管理器，负责数据库连接、初始化和维护
    /// </summary>
    public class DatabaseManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;
        private readonly string _environment;

        public DatabaseManager()
        {
            _environment = ConfigurationManager.AppSettings["DatabaseEnvironment"] ?? "Development";
            _connectionString = GetConnectionString();
        }

        public DatabaseManager(string connectionString)
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
        /// 初始化数据库
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                Logger.Info("开始初始化数据库...");

                // 检查数据库是否存在，如果不存在则创建
                await EnsureDatabaseExistsAsync();

                // 初始化Entity Framework上下文
                using (var context = new WorkflowDbContext(_connectionString))
                {
                    // 如果数据库不存在，则创建数据库
                    if (!context.Database.Exists())
                    {
                        Logger.Info("数据库不存在，正在创建...");
                        context.Database.Create();
                        Logger.Info("数据库创建成功");
                    }
                    else
                    {
                        Logger.Info("数据库已存在");
                    }
                }

                Logger.Info("数据库初始化完成");
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
            builder.Database = ""; // 连接到MySQL服务器而不是特定数据库

            using (var connection = new MySqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                    await command.ExecuteNonQueryAsync();
                    Logger.Info($"确保数据库 {databaseName} 存在");
                }
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
        /// 还原数据库
        /// </summary>
        public async Task<bool> RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                Logger.Info($"开始从备份文件还原数据库: {backupPath}");

                if (!System.IO.File.Exists(backupPath))
                {
                    Logger.Error($"备份文件不存在: {backupPath}");
                    return false;
                }

                var builder = new MySqlConnectionStringBuilder(_connectionString);
                var restoreCommand = $"mysql --host={builder.Server} --port={builder.Port} --user={builder.UserID} --password={builder.Password} {builder.Database} < \"{backupPath}\"";

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {restoreCommand}",
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
                        Logger.Info("数据库还原成功");
                        return true;
                    }
                    else
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        Logger.Error($"数据库还原失败: {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "数据库还原过程中发生错误");
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
        /// 清理数据库（删除过期数据）
        /// </summary>
        public async Task<bool> CleanupDatabaseAsync(int retentionDays = 90)
        {
            try
            {
                Logger.Info($"开始清理数据库，保留 {retentionDays} 天内的数据");

                using (var context = new WorkflowDbContext(_connectionString))
                {
                    var cutoffDate = DateTime.Now.AddDays(-retentionDays);

                    // 删除过期的工作流实例
                    var expiredInstances = await context.WorkflowInstances
                        .Where(wi => wi.EndTime.HasValue && wi.EndTime < cutoffDate)
                        .ToListAsync();

                    if (expiredInstances.Any())
                    {
                        Logger.Info($"找到 {expiredInstances.Count} 个过期的工作流实例");

                        // 删除相关的节点执行记录
                        var instanceIds = expiredInstances.Select(wi => wi.Id).ToList();
                        var expiredExecutions = await context.WorkflowNodeExecutions
                            .Where(wne => instanceIds.Contains(wne.InstanceId))
                            .ToListAsync();

                        context.WorkflowNodeExecutions.RemoveRange(expiredExecutions);

                        // 删除相关的审批任务
                        var expiredTasks = await context.ApprovalTasks
                            .Where(at => instanceIds.Contains(at.InstanceId))
                            .ToListAsync();

                        context.ApprovalTasks.RemoveRange(expiredTasks);

                        // 删除工作流实例
                        context.WorkflowInstances.RemoveRange(expiredInstances);

                        await context.SaveChangesAsync();

                        Logger.Info($"已清理 {expiredInstances.Count} 个过期工作流实例及相关数据");
                    }
                    else
                    {
                        Logger.Info("没有找到需要清理的过期数据");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "数据库清理失败");
                return false;
            }
        }

        /// <summary>
        /// 优化数据库表
        /// </summary>
        public async Task<bool> OptimizeTablesAsync()
        {
            try
            {
                Logger.Info("开始优化数据库表");

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var builder = new MySqlConnectionStringBuilder(_connectionString);
                    var tables = new[] { "workflow_definitions", "workflow_instances", "workflow_node_executions", "approval_tasks" };

                    foreach (var table in tables)
                    {
                        using (var command = new MySqlCommand($"OPTIMIZE TABLE `{builder.Database}`.`{table}`", connection))
                        {
                            await command.ExecuteNonQueryAsync();
                            Logger.Info($"表 {table} 优化完成");
                        }
                    }
                }

                Logger.Info("数据库表优化完成");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "数据库表优化失败");
                return false;
            }
        }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        public WorkflowDbContext CreateDbContext()
        {
            return new WorkflowDbContext(_connectionString);
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
                        result.ErrorMessage = "缺少必需的数据库表";
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