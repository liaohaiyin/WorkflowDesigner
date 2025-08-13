using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
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

        private Border _mainBorder;
        private bool _isMouseOver;

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
            InitializeView();
            SetupEventHandlers();

            // 当视图激活时设置DataContext
            this.WhenActivated(disposables =>
            {
                // 确保DataContext与ViewModel同步
                if (ViewModel != null)
                {
                    DataContext = ViewModel;
                    SetupNodeBindings();
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
                    SetupNodeBindings();
                }
            };
            Unloaded += (s, e) => Activator.Deactivate();
        }

        /// <summary>
        /// 初始化视图
        /// </summary>
        private void InitializeView()
        {
            // 设置基本属性
            Cursor = Cursors.Hand;

            // 确保控件可以接收焦点
            Focusable = true;

            // 设置默认尺寸
            Width = 140;
            Height = 80;
        }

        /// <summary>
        /// 设置事件处理器
        /// </summary>
        private void SetupEventHandlers()
        {
            // 鼠标事件
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseRightButtonDown += OnMouseRightButtonDown;

            // 键盘事件
            KeyDown += OnKeyDown;

            // 焦点事件
            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
        }

        /// <summary>
        /// 设置节点数据绑定
        /// </summary>
        private void SetupNodeBindings()
        {
            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                // 绑定选择状态变化
                nodeViewModel.WhenAnyValue(x => x.IsSelected)
                    .Subscribe(isSelected => UpdateSelectionVisual(isSelected));

                // 绑定悬停状态变化
                nodeViewModel.WhenAnyValue(x => x.IsHovered)
                    .Subscribe(isHovered => UpdateHoverVisual(isHovered));

                // 绑定拖拽状态变化
                nodeViewModel.WhenAnyValue(x => x.IsDragging)
                    .Subscribe(isDragging => UpdateDragVisual(isDragging));

                // 绑定状态变化
                nodeViewModel.WhenAnyValue(x => x.Status)
                    .Subscribe(status => UpdateStatusVisual(status));
            }
        }

        #region 鼠标事件处理

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseOver = true;

            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.OnMouseEnter();
            }

            UpdateHoverVisual(true);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseOver = false;

            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.OnMouseLeave();
            }

            UpdateHoverVisual(false);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    // 处理节点点击
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        // Ctrl+点击：切换选择状态
                        nodeViewModel.IsSelected = !nodeViewModel.IsSelected;
                    }
                    else
                    {
                        // 普通点击：选择节点
                        nodeViewModel.OnMouseClick();
                    }

                    // 设置焦点
                    Focus();
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"节点鼠标点击处理失败: {ex.Message}");
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    // 右键点击时选择节点
                    nodeViewModel.IsSelected = true;

                    // 显示上下文菜单
                    //ShowContextMenu();
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"节点右键点击处理失败: {ex.Message}");
            }
        }

        #endregion

        #region 键盘事件处理

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    switch (e.Key)
                    {
                        case Key.Delete:
                            // 删除节点
                            var result = MessageBox.Show(
                                $"确定要删除节点 '{nodeViewModel.NodeName}' 吗？",
                                "确认删除",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                // 触发删除事件
                                NodeDeleteRequested?.Invoke(this, new NodeEventArgs(nodeViewModel));
                            }
                            e.Handled = true;
                            break;

                        case Key.F2:
                            // 重命名节点
                            StartEditingNodeName();
                            e.Handled = true;
                            break;

                        case Key.Enter:
                            // 编辑节点属性
                            EditNodeProperties();
                            e.Handled = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"节点键盘事件处理失败: {ex.Message}");
            }
        }

        #endregion

        #region 焦点事件处理

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.IsSelected = true;
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            // 焦点丢失时可以选择保持选择状态或取消选择
            // 这里选择保持选择状态
        }

        #endregion

        #region 视觉状态更新

        /// <summary>
        /// 更新选择状态的视觉效果
        /// </summary>
        private void UpdateSelectionVisual(bool isSelected)
        {
            try
            {
                if (_mainBorder == null)
                    _mainBorder = FindMainBorder();

                if (_mainBorder != null)
                {
                    if (isSelected)
                    {
                        // 选中状态：蓝色边框，发光效果
                        _mainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                        _mainBorder.BorderThickness = new Thickness(1);
                        _mainBorder.CornerRadius = new CornerRadius(4);

                        // 添加发光效果
                        var glowEffect = new DropShadowEffect
                        {
                            Color = Color.FromRgb(33, 150, 243),
                            Direction = 0,
                            ShadowDepth = 0,
                            BlurRadius = 10,
                            Opacity = 0.8
                        };
                        _mainBorder.Effect = glowEffect;
                    }
                    else
                    {
                        // 未选中状态：恢复默认边框
                        RestoreDefaultBorder();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新选择视觉效果失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新悬停状态的视觉效果
        /// </summary>
        private void UpdateHoverVisual(bool isHovered)
        {
            try
            {
                if (_mainBorder == null)
                    _mainBorder = FindMainBorder();

                if (_mainBorder != null && ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    if (isHovered && !nodeViewModel.IsSelected)
                    {
                        // 悬停状态：绿色边框，轻微发光
                        _mainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                        _mainBorder.BorderThickness = new Thickness(2);

                        var hoverEffect = new DropShadowEffect
                        {
                            Color = Color.FromRgb(76, 175, 80),
                            Direction = 0,
                            ShadowDepth = 0,
                            BlurRadius = 5,
                            Opacity = 0.6
                        };
                        _mainBorder.Effect = hoverEffect;
                    }
                    else if (!isHovered && !nodeViewModel.IsSelected)
                    {
                        // 非悬停且未选中：恢复默认
                        RestoreDefaultBorder();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新悬停视觉效果失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新拖拽状态的视觉效果
        /// </summary>
        private void UpdateDragVisual(bool isDragging)
        {
            try
            {
                if (isDragging)
                {
                    Opacity = 0.7;
                    Cursor = Cursors.SizeAll;
                }
                else
                {
                    Opacity = 1.0;
                    Cursor = Cursors.Hand;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新拖拽视觉效果失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新状态视觉效果
        /// </summary>
        private void UpdateStatusVisual(WorkflowDesigner.Core.Models.WorkflowNodeStatus status)
        {
            try
            {
                // 这里可以根据状态更新节点的视觉效果
                // 例如改变背景色、添加状态图标等
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新状态视觉效果失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复默认边框样式
        /// </summary>
        private void RestoreDefaultBorder()
        {
            if (_mainBorder != null)
            {
                // 恢复默认边框
                _mainBorder.BorderBrush = GetDefaultBorderBrush();
                _mainBorder.BorderThickness = new Thickness(2);
                _mainBorder.Effect = null;
            }
        }

        /// <summary>
        /// 获取默认边框画刷（由子类重写）
        /// </summary>
        protected virtual Brush GetDefaultBorderBrush()
        {
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        /// <summary>
        /// 查找主边框控件
        /// </summary>
        private Border FindMainBorder()
        {
            return FindVisualChild<Border>(this);
        }

        /// <summary>
        /// 查找可视化子元素
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        #endregion

        #region 上下文菜单和编辑功能

        /// <summary>
        /// 显示上下文菜单
        /// </summary>
        private void ShowContextMenu()
        {
            try
            {
                var contextMenu = new ContextMenu();

                var editItem = new MenuItem { Header = "编辑属性(_E)" };
                editItem.Click += (s, e) => EditNodeProperties();
                contextMenu.Items.Add(editItem);

                var renameItem = new MenuItem { Header = "重命名(_R)" };
                renameItem.Click += (s, e) => StartEditingNodeName();
                contextMenu.Items.Add(renameItem);

                contextMenu.Items.Add(new Separator());

                var deleteItem = new MenuItem { Header = "删除(_D)" };
                deleteItem.Click += (s, e) => DeleteNode();
                contextMenu.Items.Add(deleteItem);

                contextMenu.PlacementTarget = this;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示上下文菜单失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 开始编辑节点名称
        /// </summary>
        private void StartEditingNodeName()
        {
            try
            {
                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    NodeNameEditRequested?.Invoke(this, new NodeEventArgs(nodeViewModel));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"开始编辑节点名称失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 编辑节点属性
        /// </summary>
        private void EditNodeProperties()
        {
            try
            {
                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    NodePropertiesEditRequested?.Invoke(this, new NodeEventArgs(nodeViewModel));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑节点属性失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        private void DeleteNode()
        {
            try
            {
                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    var result = MessageBox.Show(
                        $"确定要删除节点 '{nodeViewModel.NodeName}' 吗？",
                        "确认删除",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        NodeDeleteRequested?.Invoke(this, new NodeEventArgs(nodeViewModel));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除节点失败: {ex.Message}");
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 节点删除请求事件
        /// </summary>
        public event EventHandler<NodeEventArgs> NodeDeleteRequested;

        /// <summary>
        /// 节点名称编辑请求事件
        /// </summary>
        public event EventHandler<NodeEventArgs> NodeNameEditRequested;

        /// <summary>
        /// 节点属性编辑请求事件
        /// </summary>
        public event EventHandler<NodeEventArgs> NodePropertiesEditRequested;

        #endregion

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
                    view.OnNodeViewModelSet(nodeViewModel);
                    view.SetupNodeBindings();
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

    /// <summary>
    /// 节点事件参数
    /// </summary>
    public class NodeEventArgs : EventArgs
    {
        public WorkflowNodeViewModel Node { get; }

        public NodeEventArgs(WorkflowNodeViewModel node)
        {
            Node = node;
        }
    }
}