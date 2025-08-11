using Prism.Ioc;
using Prism.Unity;
using System;
using System.Windows;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Services;
using WorkflowDesigner.Engine;
using WorkflowDesigner.Infrastructure.Data;
using WorkflowDesigner.UI.ViewModels;
using WorkflowDesigner.UI.Views;

namespace WorkflowDesigner
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册服务
            containerRegistry.RegisterSingleton<IWorkflowEngine, WorkflowEngine>();
            containerRegistry.RegisterSingleton<IWorkflowRepository, WorkflowRepository>();
            containerRegistry.RegisterSingleton<IApprovalService, ApprovalService>();
            containerRegistry.RegisterSingleton<INotificationService, NotificationService>();
            containerRegistry.RegisterSingleton<IUserService, UserService>();

            // 注册视图和视图模型
            containerRegistry.Register<MainWindow>();
            containerRegistry.Register<MainWindowViewModel>();
            containerRegistry.Register<WorkflowDesignerView>();
            containerRegistry.Register<WorkflowDesignerViewModel>();
            containerRegistry.Register<PropertyPanelView>();
            containerRegistry.Register<PropertyPanelViewModel>();
            containerRegistry.Register<ToolboxView>();
            containerRegistry.Register<ToolboxViewModel>();

            // 注册数据库上下文
            containerRegistry.Register<WorkflowDbContext>();
        }
    }
}