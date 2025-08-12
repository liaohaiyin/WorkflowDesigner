using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.Views.Nodes
{
    /// <summary>
    /// 工作流节点视图的基类，实现IViewFor接口以支持ReactiveUI
    /// </summary>
    public class NodeViewBase : UserControl, IViewFor, IActivatableView
    {
        /// <summary>
        /// ViewModel依赖属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(object),
                typeof(NodeViewBase),
                new PropertyMetadata(null, OnViewModelChanged));

        /// <summary>
        /// 获取或设置视图模型
        /// </summary>
        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 视图激活状态管理
        /// </summary>
        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        /// <summary>
        /// 构造函数
        /// </summary>
        public NodeViewBase()
        {
            // 当视图激活时设置DataContext
            this.WhenActivated(disposables =>
            {
                // 确保DataContext与ViewModel同步
                if (ViewModel != null)
                {
                    DataContext = ViewModel;
                }

                // 清理资源
                Disposable.Create(() =>
                {
                    // 视图停用时的清理逻辑
                }).DisposeWith(disposables);
            });

            // 监听Loaded和Unloaded事件来管理激活状态
            Loaded += (s, e) =>
            {
                Activator.Activate();
                // 确保DataContext正确设置
                if (ViewModel != null && DataContext != ViewModel)
                {
                    DataContext = ViewModel;
                }
            };
            Unloaded += (s, e) => Activator.Deactivate();
        }

        /// <summary>
        /// 当ViewModel属性改变时调用
        /// </summary>
        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NodeViewBase view)
            {
                // 更新DataContext
                view.DataContext = e.NewValue;

                // 如果是工作流节点视图模型，进行额外的设置
                if (e.NewValue is WorkflowNodeViewModel nodeViewModel)
                {
                    // 可以在这里添加节点特定的初始化逻辑
                    view.OnNodeViewModelSet(nodeViewModel);
                }
            }
        }

        /// <summary>
        /// 当节点视图模型被设置时调用，子类可以重写此方法
        /// </summary>
        protected virtual void OnNodeViewModelSet(WorkflowNodeViewModel nodeViewModel)
        {
            // 子类可以重写此方法来处理特定的初始化逻辑
        }
    }

    /// <summary>
    /// 泛型版本的NodeViewBase，提供强类型的ViewModel
    /// </summary>
    /// <typeparam name="TViewModel">视图模型类型</typeparam>
    public class NodeViewBase<TViewModel> : NodeViewBase, IViewFor<TViewModel>
        where TViewModel : class
    {
        /// <summary>
        /// 强类型的ViewModel属性
        /// </summary>
        public new TViewModel ViewModel
        {
            get => (TViewModel)base.ViewModel;
            set => base.ViewModel = value;
        }

        /// <summary>
        /// 显式实现IViewFor<TViewModel>接口
        /// </summary>
        TViewModel IViewFor<TViewModel>.ViewModel
        {
            get => ViewModel;
            set => ViewModel = value;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public NodeViewBase()
        {
            // 确保在加载时正确设置DataContext
            Loaded += (s, e) =>
            {
                if (ViewModel != null && DataContext != ViewModel)
                {
                    DataContext = ViewModel;
                }
            };
        }

        /// <summary>
        /// 重写以提供强类型的节点视图模型设置
        /// </summary>
        protected override void OnNodeViewModelSet(WorkflowNodeViewModel nodeViewModel)
        {
            if (nodeViewModel is TViewModel typedViewModel)
            {
                OnTypedNodeViewModelSet(typedViewModel);
            }
            base.OnNodeViewModelSet(nodeViewModel);
        }

        /// <summary>
        /// 当强类型的节点视图模型被设置时调用，子类可以重写此方法
        /// </summary>
        protected virtual void OnTypedNodeViewModelSet(TViewModel nodeViewModel)
        {
            // 子类可以重写此方法来处理特定类型的初始化逻辑
        }
    }
}