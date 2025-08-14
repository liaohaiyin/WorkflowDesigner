using NLog;
using Prism.Ioc;
using Prism.Unity;
using ReactiveUI;
using Splat;
using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Services;
using WorkflowDesigner.Engine;
using WorkflowDesigner.Infrastructure.Data;
using WorkflowDesigner.Infrastructure.Database;
using WorkflowDesigner.Infrastructure.Services;
using WorkflowDesigner.Infrastructure.Startup;
using WorkflowDesigner.Nodes;
using WorkflowDesigner.UI.ViewLocators;
using WorkflowDesigner.UI.ViewModels;
using WorkflowDesigner.UI.Views;
using WorkflowDesigner.UI.Views.Nodes;

namespace WorkflowDesigner
{
    public partial class App : PrismApplication
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                Logger.Info("=== 工作流设计器启动 ===");

                // 首先设置 ReactiveUI 的视图定位器
                SetupReactiveUIViewLocator();

                // 设置未处理异常处理器
                SetupExceptionHandling();

                SetupReactiveUIExceptionHandling();

                // 执行启动前检查
                var startupResult = await StartupHelper.PrepareApplicationAsync();

                // 显示启动检查结果，让用户决定是否继续
                if (!StartupHelper.ShowStartupResultDialog(startupResult))
                {
                    Logger.Info("用户选择不继续启动");
                    Environment.Exit(1);
                    return;
                }

                base.OnStartup(e);

                Logger.Info("应用程序启动完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "应用程序启动失败");
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "启动错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void SetupReactiveUIExceptionHandling()
        {
            try
            {
                // 设置ReactiveUI的全局异常处理器
                RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
                {
                    Logger.Error(ex, "ReactiveUI管道异常");

                    // 在UI线程上显示错误（可选）
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            // 可以选择显示错误信息或记录到输出面板
                            System.Diagnostics.Debug.WriteLine($"ReactiveUI异常: {ex.Message}");
                        }
                        catch
                        {
                            // 忽略显示错误时的异常
                        }
                    }));
                });

                Logger.Info("ReactiveUI异常处理器设置完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "设置ReactiveUI异常处理器失败");
            }
        }

        /// <summary>
        /// 设置 ReactiveUI 的视图定位器
        /// </summary>
        private void SetupReactiveUIViewLocator()
        {
            try
            {
                // 注册自定义的视图定位器
                Locator.CurrentMutable.RegisterConstant(new NodeViewLocator(), typeof(IViewLocator));

                // 或者使用 ReactiveUI 的默认视图定位器并注册视图
                Locator.CurrentMutable.Register(() => new StartNodeView(), typeof(IViewFor<StartNodeViewModel>));
                Locator.CurrentMutable.Register(() => new EndNodeView(), typeof(IViewFor<EndNodeViewModel>));
                Locator.CurrentMutable.Register(() => new ApprovalNodeView(), typeof(IViewFor<ApprovalNodeViewModel>));
                Locator.CurrentMutable.Register(() => new DecisionNodeView(), typeof(IViewFor<DecisionNodeViewModel>));
                Locator.CurrentMutable.Register(() => new TaskNodeView(), typeof(IViewFor<TaskNodeViewModel>));
                Locator.CurrentMutable.Register(() => new NotificationNodeView(), typeof(IViewFor<NotificationNodeViewModel>));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "设置 ReactiveUI 视图定位器失败");
            }
        }

        protected override Window CreateShell()
        {
            try
            {
                // 尝试从容器获取服务
                var mainWindow = Container.Resolve<MainWindow>();
                return mainWindow;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "从容器创建主窗口失败，使用备用方案");

                try
                {
                    // 备用方案：直接创建窗口和视图模型
                    var mainWindow = new MainWindow();
                    var viewModel = new MainWindowViewModel();
                    mainWindow.DataContext = viewModel;

                    Logger.Info("使用备用方案创建主窗口成功");
                    return mainWindow;
                }
                catch (Exception innerEx)
                {
                    Logger.Error(innerEx, "备用方案也失败，创建最简单的窗口");
                    return CreateFallbackWindow(innerEx);
                }
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            try
            {
                Logger.Info("开始注册服务类型");

                // 首先注册服务提供者适配器
                containerRegistry.RegisterSingleton<IServiceProvider>(provider =>
                    new PrismServiceProviderAdapter(provider));

                // 注册数据库相关服务
                RegisterDatabaseServices(containerRegistry);

                // 注册核心业务服务
                RegisterCoreServices(containerRegistry);

                // 注册UI相关服务
                RegisterUIServices(containerRegistry);

                Logger.Info("服务类型注册完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "服务注册失败，但继续启动");
            }
        }

        /// <summary>
        /// 注册数据库相关服务
        /// </summary>
        private void RegisterDatabaseServices(IContainerRegistry containerRegistry)
        {
            try
            {
                // 注册数据库管理器
                containerRegistry.RegisterSingleton<EnhancedDatabaseManager>();

                // 注册数据库上下文工厂
                containerRegistry.Register<Func<WorkflowDbContext>>(() =>
                {
                    try
                    {
                        return new WorkflowDbContext();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "创建数据库上下文失败");
                        return null;
                    }
                });

                // 注册工作流仓储
                containerRegistry.Register<IWorkflowRepository>(provider =>
                {
                    try
                    {
                        var dbManager = provider.Resolve<EnhancedDatabaseManager>();
                        var context = dbManager.CreateDbContext();
                        return new WorkflowRepository(context);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "创建工作流仓储失败");
                        return null;
                    }
                });

                Logger.Info("数据库服务注册成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "注册数据库服务失败");
            }
        }

        /// <summary>
        /// 注册核心业务服务
        /// </summary>
        private void RegisterCoreServices(IContainerRegistry containerRegistry)
        {
            try
            {
                // 注册业务服务
                containerRegistry.Register<IApprovalService>(provider =>
                {
                    try
                    {
                        var dbManager = provider.Resolve<EnhancedDatabaseManager>();
                        var context = dbManager.CreateDbContext();
                        return new ApprovalService(context);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "创建审批服务失败");
                        return null;
                    }
                });

                containerRegistry.Register<INotificationService, NotificationService>();
                containerRegistry.Register<IUserService, UserService>();

                // 注册工作流引擎 - 修复后的版本
                containerRegistry.Register<IWorkflowEngine>(provider =>
                {
                    try
                    {
                        var repository = provider.Resolve<IWorkflowRepository>();
                        var serviceProvider = provider.Resolve<IServiceProvider>();
                        return new WorkflowEngine(repository, serviceProvider);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "创建工作流引擎失败");
                        return null;
                    }
                });

                Logger.Info("核心业务服务注册成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "注册核心业务服务失败");
            }
        }

        /// <summary>
        /// 注册UI相关服务
        /// </summary>
        private void RegisterUIServices(IContainerRegistry containerRegistry)
        {
            try
            {
                // 注册视图
                containerRegistry.Register<MainWindow>();

                // 注册视图模型
                containerRegistry.Register<MainWindowViewModel>(provider =>
                {
                    try
                    {
                        var engine = provider.Resolve<IWorkflowEngine>();
                        var repository = provider.Resolve<IWorkflowRepository>();
                        var toolboxVM = provider.Resolve<ToolboxViewModel>();
                        var designerVM = provider.Resolve<WorkflowDesignerViewModel>();
                        var propertyVM = provider.Resolve<PropertyPanelViewModel>();
                        var monitorVM = provider.Resolve<WorkflowMonitorViewModel>();
                        var outputVM = provider.Resolve<OutputPanelViewModel>();

                        return new MainWindowViewModel(engine, repository, toolboxVM, designerVM, propertyVM, monitorVM, outputVM);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "创建主窗口视图模型失败，使用简单构造函数");
                        return new MainWindowViewModel();
                    }
                });

                containerRegistry.Register<ToolboxViewModel>();
                containerRegistry.Register<WorkflowDesignerViewModel>();
                containerRegistry.Register<OutputPanelViewModel>();

                // 注册属性面板视图模型
                containerRegistry.Register<PropertyPanelViewModel>(provider =>
                {
                    try
                    {
                        var userService = provider.Resolve<IUserService>();
                        return new PropertyPanelViewModel(userService);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "创建属性面板视图模型失败");
                        return new PropertyPanelViewModel();
                    }
                });

                // 注册监控视图模型
                containerRegistry.Register<WorkflowMonitorViewModel>(provider =>
                {
                    try
                    {
                        var engine = provider.Resolve<IWorkflowEngine>();
                        return engine != null ? new WorkflowMonitorViewModel(engine) : new WorkflowMonitorViewModel();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "创建监控视图模型失败");
                        return new WorkflowMonitorViewModel();
                    }
                });

                Logger.Info("UI服务注册成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "注册UI服务失败");
            }
        }

        /// <summary>
        /// 创建备用窗口
        /// </summary>
        private Window CreateFallbackWindow(Exception ex)
        {
            return new Window
            {
                Title = "WorkflowDesigner - 安全模式",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new System.Windows.Controls.StackPanel
                {
                    Margin = new Thickness(20),
                    Children =
                    {
                        new System.Windows.Controls.TextBlock
                        {
                            Text = "工作流设计器 - 安全模式",
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 0, 20),
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text = "应用程序在安全模式下运行，部分功能可能不可用。",
                            FontSize = 14,
                            Margin = new Thickness(0, 0, 0, 10),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text = "可能的原因：",
                            FontSize = 12,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 10, 0, 5)
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text = "• 数据库连接失败\n• 依赖项缺失\n• 配置文件错误",
                            FontSize = 12,
                            Margin = new Thickness(20, 0, 0, 10)
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text = $"错误详情：{ex.Message}",
                            FontSize = 10,
                            Foreground = System.Windows.Media.Brushes.Red,
                            Margin = new Thickness(0, 10, 0, 0),
                            TextWrapping = TextWrapping.Wrap
                        },
                        new System.Windows.Controls.Button
                        {
                            Content = "重试启动",
                            Width = 120,
                            Height = 30,
                            Margin = new Thickness(0, 20, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 设置异常处理
        /// </summary>
        private void SetupExceptionHandling()
        {
            // 处理UI线程未捕获的异常
            DispatcherUnhandledException += (s, e) =>
            {
                Logger.Error(e.Exception, "UI线程未处理异常");

                var result = MessageBox.Show(
                    $"发生未处理的异常：\n{e.Exception.Message}\n\n是否继续运行应用程序？",
                    "错误",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    e.Handled = true; // 继续运行
                }
                else
                {
                    Logger.Info("用户选择退出应用程序");
                    Current.Shutdown();
                }
            };

            // 处理非UI线程未捕获的异常
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                Logger.Fatal(exception, "应用程序域未处理异常");

                if (e.IsTerminating)
                {
                    Logger.Fatal("应用程序即将终止");

                    try
                    {
                        MessageBox.Show(
                            $"应用程序遇到严重错误，即将退出：\n{exception?.Message}",
                            "严重错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Stop);
                    }
                    catch
                    {
                        // 如果连MessageBox都无法显示，就只能记录日志了
                    }
                }
            };

            // 处理Task未捕获的异常
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logger.Error(e.Exception, "Task未观察到的异常");
                e.SetObserved(); // 防止应用程序崩溃
            };

            Logger.Info("异常处理器设置完成");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Logger.Info("应用程序正在退出");

                // 清理资源
                try
                {
                    // 停止任何正在运行的工作流
                    // 关闭数据库连接
                    // 保存用户设置等
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "清理资源时发生错误");
                }

                Logger.Info("=== 应用程序退出完成 ===");
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }

    /// <summary>
    /// 应用程序扩展方法
    /// </summary>
    public static class ApplicationExtensions
    {
        /// <summary>
        /// 重启应用程序
        /// </summary>
        public static void Restart(this Application app)
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            System.Diagnostics.Process.Start(exePath);
            app.Shutdown();
        }
    }
}