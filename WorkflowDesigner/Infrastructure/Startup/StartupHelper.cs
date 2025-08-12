using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using WorkflowDesigner.Infrastructure.Database;

namespace WorkflowDesigner.Infrastructure.Startup
{
    /// <summary>
    /// 应用程序启动助手
    /// </summary>
    public static class StartupHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 执行应用程序启动前的预检查和初始化
        /// </summary>
        public static async Task<StartupResult> PrepareApplicationAsync()
        {
            var result = new StartupResult();

            try
            {
                Logger.Info("开始应用程序启动预检查");

                // 1. 检查运行环境
                CheckRuntimeEnvironment(result);

                // 2. 创建必要目录
                CreateRequiredDirectories(result);

                // 3. 检查配置文件
                CheckConfigurationFiles(result);

                // 4. 初始化数据库
                await InitializeDatabaseAsync(result);

                // 5. 验证必要的DLL
                CheckRequiredAssemblies(result);

                Logger.Info($"应用程序启动预检查完成，结果: {(result.IsSuccess ? "成功" : "失败")}");

                if (!result.IsSuccess)
                {
                    foreach (var error in result.Errors)
                    {
                        Logger.Error($"启动检查错误: {error}");
                    }
                }

                if (result.Warnings.Count > 0)
                {
                    foreach (var warning                                                                                                                                                                                                                                                                                                                in result.Warnings)
                    {
                        Logger.Warn($"启动检查警告: {warning}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "应用程序启动预检查时发生异常");
                result.IsSuccess = false;
                result.Errors.Add($"启动预检查异常: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 检查运行环境
        /// </summary>
        private static void CheckRuntimeEnvironment(StartupResult result)
        {
            try
            {
                // 检查.NET Framework版本
                var frameworkVersion = Environment.Version;
                Logger.Info($"当前.NET Framework版本: {frameworkVersion}");

                if (frameworkVersion.Major < 4 || (frameworkVersion.Major == 4 && frameworkVersion.Minor < 8))
                {
                    result.Warnings.Add("建议使用.NET Framework 4.8或更高版本");
                }

                // 检查操作系统版本
                var osVersion = Environment.OSVersion;
                Logger.Info($"操作系统版本: {osVersion}");

                // 检查内存
                var workingSet = Environment.WorkingSet;
                Logger.Info($"当前工作集内存: {workingSet / (1024 * 1024)} MB");

                // 检查磁盘空间
                var currentDirectory = Environment.CurrentDirectory;
                var drive = new DriveInfo(Path.GetPathRoot(currentDirectory));
                var freeSpace = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                Logger.Info($"可用磁盘空间: {freeSpace} GB");

                if (freeSpace < 1)
                {
                    result.Warnings.Add("磁盘可用空间不足1GB，可能影响日志记录和数据存储");
                }

                result.Details.Add("RuntimeCheck", "运行环境检查完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "检查运行环境时发生错误");
                result.Warnings.Add($"运行环境检查失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建必要目录
        /// </summary>
        private static void CreateRequiredDirectories(StartupResult result)
        {
            try
            {
                var directories = new[]
                {
                    "logs",
                    "logs\\archive",
                    "Storage\\Files",
                    "temp",
                    "backup"
                };

                foreach (var dir in directories)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        Logger.Info($"创建目录: {dir}");
                    }
                }

                result.Details.Add("DirectoryCheck", "必要目录创建完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建必要目录时发生错误");
                result.Errors.Add($"目录创建失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查配置文件
        /// </summary>
        private static void CheckConfigurationFiles(StartupResult result)
        {
            try
            {
                // 检查App.config
                var appConfigPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                if (File.Exists(appConfigPath))
                {
                    Logger.Info($"App.config文件存在: {appConfigPath}");
                }
                else
                {
                    result.Errors.Add("App.config文件不存在");
                    return;
                }

                // 检查NLog.config
                var nlogConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config");
                if (File.Exists(nlogConfigPath))
                {
                    Logger.Info($"NLog.config文件存在: {nlogConfigPath}");
                }
                else
                {
                    result.Warnings.Add("NLog.config文件不存在，将使用默认日志配置");
                }

                // 验证连接字符串
                try
                {
                    var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["WorkflowDesignerMySqlConnection"]?.ConnectionString;
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        result.Errors.Add("数据库连接字符串未配置");
                    }
                    else
                    {
                        Logger.Info("数据库连接字符串配置正常");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"读取连接字符串失败: {ex.Message}");
                }

                result.Details.Add("ConfigCheck", "配置文件检查完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "检查配置文件时发生错误");
                result.Errors.Add($"配置文件检查失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private static async Task InitializeDatabaseAsync(StartupResult result)
        {
            try
            {
                Logger.Info("开始数据库初始化检查");

                var dbManager = new EnhancedDatabaseManager();

                // 执行健康检查
                var healthCheck = await dbManager.PerformHealthCheckAsync();

                if (healthCheck.IsHealthy)
                {
                    Logger.Info("数据库健康检查通过");
                    result.Details.Add("DatabaseCheck", "数据库正常");

                    // 尝试插入示例数据
                    try
                    {
                        await dbManager.InsertSampleDataAsync();
                        result.Details.Add("SampleData", "示例数据检查完成");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "插入示例数据时发生警告");
                        result.Warnings.Add($"示例数据检查警告: {ex.Message}");
                    }
                }
                else
                {
                    Logger.Warn($"数据库健康检查失败: {healthCheck.ErrorMessage}");

                    // 尝试初始化数据库
                    if (await dbManager.InitializeDatabaseAsync())
                    {
                        Logger.Info("数据库初始化成功");
                        result.Details.Add("DatabaseCheck", "数据库初始化成功");

                        // 再次尝试插入示例数据
                        await dbManager.InsertSampleDataAsync();
                    }
                    else
                    {
                        Logger.Error("数据库初始化失败");
                        result.Warnings.Add("数据库初始化失败，应用程序将以离线模式运行");
                        result.Details.Add("DatabaseCheck", "数据库不可用");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "数据库初始化时发生错误");
                result.Warnings.Add($"数据库初始化错误: {ex.Message}");
                result.Details.Add("DatabaseCheck", "数据库初始化失败");
            }
        }

        /// <summary>
        /// 检查必要的程序集
        /// </summary>
        private static void CheckRequiredAssemblies(StartupResult result)
        {
            try
            {
                var requiredAssemblies = new[]
                {
                    "MySql.Data",
                    "EntityFramework",
                    "Newtonsoft.Json",
                    "NLog",
                    "NodeNetwork",
                    "ReactiveUI"
                };

                foreach (var assemblyName in requiredAssemblies)
                {
                    try
                    {
                        var assembly = System.Reflection.Assembly.Load(assemblyName);
                        Logger.Debug($"程序集 {assemblyName} 加载成功: {assembly.FullName}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, $"程序集 {assemblyName} 加载失败");
                        result.Warnings.Add($"程序集 {assemblyName} 不可用，某些功能可能受限");
                    }
                }

                result.Details.Add("AssemblyCheck", "程序集检查完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "检查程序集时发生错误");
                result.Warnings.Add($"程序集检查失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示启动结果对话框
        /// </summary>
        public static bool ShowStartupResultDialog(StartupResult result)
        {
            try
            {
                if (result.IsSuccess && result.Warnings.Count == 0)
                {
                    // 完全成功，不显示对话框
                    return true;
                }

                var message = "应用程序启动检查完成：\n\n";

                if (!result.IsSuccess)
                {
                    message += "❌ 发现以下错误：\n";
                    foreach (var error in result.Errors)
                    {
                        message += $"  • {error}\n";
                    }
                    message += "\n";
                }

                if (result.Warnings.Count > 0)
                {
                    message += "⚠️ 发现以下警告：\n";
                    foreach (var warning in result.Warnings)
                    {
                        message += $"  • {warning}\n";
                    }
                    message += "\n";
                }

                if (!result.IsSuccess)
                {
                    message += "是否继续启动应用程序？某些功能可能不可用。";
                    var dialogResult = MessageBox.Show(message, "启动检查",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    return dialogResult == MessageBoxResult.Yes;
                }
                else
                {
                    message += "应用程序可以正常启动，但建议处理上述警告。";
                    //MessageBox.Show(message, "启动检查",
                    //    MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "显示启动结果对话框时发生错误");
                return true; // 默认继续启动
            }
        }
    }

    /// <summary>
    /// 启动结果
    /// </summary>
    public class StartupResult
    {
        public bool IsSuccess { get; set; } = true;
        public System.Collections.Generic.List<string> Errors { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> Warnings { get; set; } = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.Dictionary<string, object> Details { get; set; } = new System.Collections.Generic.Dictionary<string, object>();
    }
}