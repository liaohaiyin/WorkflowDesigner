using ReactiveUI;
using System;
using System.Reflection;
using WorkflowDesigner.Nodes;
using WorkflowDesigner.UI.Views.Nodes;

namespace WorkflowDesigner.UI.ViewLocators
{
    /// <summary>
    /// 自定义视图定位器，用于将视图模型映射到对应的视图
    /// </summary>
    public class NodeViewLocator : IViewLocator
    {
        public IViewFor ResolveView<T>(T viewModel, string contract = null)
        {
            return ResolveView(viewModel as object, contract);
        }

        public IViewFor ResolveView(object viewModel, string contract = null)
        {
            if (viewModel == null) return null;

            var viewModelType = viewModel.GetType();

            // 处理工作流节点视图模型
            if (viewModel is WorkflowNodeViewModel)
            {
                switch (viewModel)
                {
                    case StartNodeViewModel _:
                        return new StartNodeView { ViewModel = (StartNodeViewModel)viewModel };

                    case EndNodeViewModel _:
                        return new EndNodeView { ViewModel = (EndNodeViewModel)viewModel };

                    case ApprovalNodeViewModel _:
                        return new ApprovalNodeView { ViewModel = (ApprovalNodeViewModel)viewModel };

                    case DecisionNodeViewModel _:
                        return new DecisionNodeView { ViewModel = (DecisionNodeViewModel)viewModel };

                    case TaskNodeViewModel _:
                        return new TaskNodeView { ViewModel = (TaskNodeViewModel)viewModel };

                    case NotificationNodeViewModel _:
                        return new NotificationNodeView { ViewModel = (NotificationNodeViewModel)viewModel };
                }
            }

            // 尝试使用约定来查找视图
            var viewTypeName = viewModelType.FullName.Replace("ViewModel", "View");
            var viewType = Type.GetType(viewTypeName);

            if (viewType != null)
            {
                var view = Activator.CreateInstance(viewType) as IViewFor;
                if (view != null)
                {
                    view.ViewModel = viewModel;
                    return view;
                }
            }

            return null;
        }
    }
}