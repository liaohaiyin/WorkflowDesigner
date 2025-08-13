// 增强的 NodeViewBase.cs - 支持拖拽功能
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
    /// 支持拖拽的工作流节点视图基类
    /// </summary>
    public class DraggableNodeViewBase : UserControl, IViewFor, IActivatableView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel),typeof(object),typeof(DraggableNodeViewBase), new PropertyMetadata(null, OnViewModelChanged));

        private bool _isDragging;
        private Point _dragStartPoint;
        private Point _nodeStartPosition;
        private Canvas _parentCanvas;
        private Border _mainBorder;

        public object ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public DraggableNodeViewBase()
        {
            InitializeNodeView();
            SetupEventHandlers();
            SetupActivation();
        }

        #region 初始化

        private void InitializeNodeView()
        {
            // 设置基本属性
            Cursor = Cursors.Hand;
            Focusable = true;

            // 设置默认尺寸
            Width = 140;
            Height = 80;

            // 设置Canvas定位属性
            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, 0);
        }

        private void SetupEventHandlers()
        {
            // 鼠标事件
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;

            // 键盘事件
            KeyDown += OnKeyDown;

            // 焦点事件
            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;

            // 加载事件
            Loaded += OnLoaded;
        }

        private void SetupActivation()
        {
            this.WhenActivated(disposables =>
            {
                if (ViewModel != null)
                {
                    DataContext = ViewModel;
                    SetupNodeBindings();
                }

                Disposable.Create(() =>
                {
                    // 清理资源
                }).DisposeWith(disposables);
            });

            Loaded += (s, e) => Activator.Activate();
            Unloaded += (s, e) => Activator.Deactivate();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 查找父Canvas
            _parentCanvas = FindParentCanvas();

            // 查找主边框
            _mainBorder = FindMainBorder();

            // 确保DataContext正确设置
            if (ViewModel != null && DataContext != ViewModel)
            {
                DataContext = ViewModel;
                SetupNodeBindings();
            }
        }

        #endregion

        #region 拖拽功能

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    // 记录拖拽开始位置
                    _dragStartPoint = e.GetPosition(_parentCanvas);
                    _nodeStartPosition = new Point(
                        Canvas.GetLeft(this),
                        Canvas.GetTop(this)
                    );

                    // 处理节点选择
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        // Ctrl+点击：切换选择状态
                        nodeViewModel.IsChecked = !nodeViewModel.IsChecked;
                    }
                    else
                    {
                        // 普通点击：选择节点
                        nodeViewModel.OnMouseClick();
                    }

                    // 开始拖拽
                    StartDrag();

                    // 捕获鼠标
                    CaptureMouse();

                    // 设置焦点
                    Focus();
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"节点鼠标按下处理失败: {ex.Message}");
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDragging && IsMouseCaptured && _parentCanvas != null)
                {
                    var currentPosition = e.GetPosition(_parentCanvas);
                    var deltaX = currentPosition.X - _dragStartPoint.X;
                    var deltaY = currentPosition.Y - _dragStartPoint.Y;

                    // 计算新位置
                    var newX = _nodeStartPosition.X + deltaX;
                    var newY = _nodeStartPosition.Y + deltaY;

                    //// 边界限制
                    //newX = Math.Max(0, Math.Min(_parentCanvas.ActualWidth - ActualWidth, newX));
                    //newY = Math.Max(0, Math.Min(_parentCanvas.ActualHeight - ActualHeight, newY));

                    // 网格对齐（可选）
                    if (ShouldSnapToGrid())
                    {
                        var gridSize = GetGridSize();
                        newX = Math.Round(newX / gridSize) * gridSize;
                        newY = Math.Round(newY / gridSize) * gridSize;
                    }

                    // 更新位置
                    Canvas.SetLeft(this, newX);
                    Canvas.SetTop(this, newY);

                    // 更新视图模型位置
                    if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                    {
                        nodeViewModel.Position = new Point(newX, newY);
                    }

                    // 触发拖拽事件
                    OnNodeDragged?.Invoke(this, new NodeDraggedEventArgs
                    {
                        Node = ViewModel as WorkflowNodeViewModel,
                        OldPosition = _nodeStartPosition,
                        NewPosition = new Point(newX, newY)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"节点拖拽移动失败: {ex.Message}");
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDragging)
                {
                    EndDrag();

                    // 释放鼠标捕获
                    ReleaseMouseCapture();

                    // 触发拖拽完成事件
                    OnNodeDragCompleted?.Invoke(this, new NodeDragCompletedEventArgs
                    {
                        Node = ViewModel as WorkflowNodeViewModel,
                        StartPosition = _nodeStartPosition,
                        EndPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this))
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"节点拖拽结束失败: {ex.Message}");
            }
        }

        private void StartDrag()
        {
            _isDragging = true;

            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.StartDrag();
            }

            // 更新视觉状态
            UpdateDragVisual(true);
            Cursor = Cursors.SizeAll;
        }

        private void EndDrag()
        {
            _isDragging = false;

            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.EndDrag();
            }

            // 恢复视觉状态
            UpdateDragVisual(false);
            Cursor = Cursors.Hand;
        }

        #endregion

        #region 鼠标悬停和选择

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.OnMouseEnter();
            }
            UpdateHoverVisual(true);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.OnMouseLeave();
            }
            UpdateHoverVisual(false);
        }

        #endregion

        #region 键盘事件

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    switch (e.Key)
                    {
                        case Key.Delete:
                            var result = MessageBox.Show(
                                $"确定要删除节点 '{nodeViewModel.NodeName}' 吗？",
                                "确认删除",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                OnNodeDeleteRequested?.Invoke(this, new NodeEventArgs(nodeViewModel));
                            }
                            e.Handled = true;
                            break;

                        case Key.F2:
                            OnNodeNameEditRequested?.Invoke(this, new NodeEventArgs(nodeViewModel));
                            e.Handled = true;
                            break;

                        case Key.Enter:
                            OnNodePropertiesEditRequested?.Invoke(this, new NodeEventArgs(nodeViewModel));
                            e.Handled = true;
                            break;

                        // 方向键移动节点
                        case Key.Up:
                        case Key.Down:
                        case Key.Left:
                        case Key.Right:
                            MoveNodeWithArrowKey(e.Key);
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

        private void MoveNodeWithArrowKey(Key key)
        {
            if (_parentCanvas == null) return;

            var moveDistance = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
            var currentX = Canvas.GetLeft(this);
            var currentY = Canvas.GetTop(this);
            var newX = currentX;
            var newY = currentY;

            switch (key)
            {
                case Key.Up:
                    newY = Math.Max(0, currentY - moveDistance);
                    break;
                case Key.Down:
                    newY = Math.Min(_parentCanvas.ActualHeight - ActualHeight, currentY + moveDistance);
                    break;
                case Key.Left:
                    newX = Math.Max(0, currentX - moveDistance);
                    break;
                case Key.Right:
                    newX = Math.Min(_parentCanvas.ActualWidth - ActualWidth, currentX + moveDistance);
                    break;
            }

            Canvas.SetLeft(this, newX);
            Canvas.SetTop(this, newY);

            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.Position = new Point(newX, newY);
            }
        }

        #endregion

        #region 焦点事件

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                nodeViewModel.IsChecked = true;
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            // 可以选择在失去焦点时保持选择状态
        }

        #endregion

        #region 视觉状态更新

        private void SetupNodeBindings()
        {
            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                // 绑定选择状态变化
                nodeViewModel.WhenAnyValue(x => x.IsChecked)
                    .Subscribe(isSelected => UpdateSelectionVisual(isSelected));

                // 绑定悬停状态变化
                nodeViewModel.WhenAnyValue(x => x.IsHovered)
                    .Subscribe(isHovered => UpdateHoverVisual(isHovered));

                // 绑定拖拽状态变化
                nodeViewModel.WhenAnyValue(x => x.IsDragging)
                    .Subscribe(isDragging => UpdateDragVisual(isDragging));

                // 绑定位置变化
                nodeViewModel.WhenAnyValue(x => x.Position)
                    .Subscribe(position => UpdatePosition(position));
            }
        }

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
                        _mainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                        _mainBorder.BorderThickness = new Thickness(3);

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
                        RestoreDefaultBorder();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新选择视觉效果失败: {ex.Message}");
            }
        }

        private void UpdateHoverVisual(bool isHovered)
        {
            try
            {
                if (_mainBorder == null)
                    _mainBorder = FindMainBorder();

                if (_mainBorder != null && ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    if (isHovered && !nodeViewModel.IsChecked)
                    {
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
                    else if (!isHovered && !nodeViewModel.IsChecked)
                    {
                        RestoreDefaultBorder();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新悬停视觉效果失败: {ex.Message}");
            }
        }

        private void UpdateDragVisual(bool isDragging)
        {
            try
            {
                if (isDragging)
                {
                    Opacity = 0.7;
                    Panel.SetZIndex(this, 1000); // 拖拽时提升层级
                }
                else
                {
                    Opacity = 1.0;
                    Panel.SetZIndex(this, 0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新拖拽视觉效果失败: {ex.Message}");
            }
        }

        private void UpdatePosition(Point position)
        {
            try
            {
                Canvas.SetLeft(this, position.X);
                Canvas.SetTop(this, position.Y);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新位置失败: {ex.Message}");
            }
        }

        private void RestoreDefaultBorder()
        {
            if (_mainBorder != null)
            {
                _mainBorder.BorderBrush = GetDefaultBorderBrush();
                _mainBorder.BorderThickness = new Thickness(2);
                _mainBorder.Effect = null;
            }
        }

        protected virtual Brush GetDefaultBorderBrush()
        {
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        #endregion

        #region 辅助方法

        private Canvas FindParentCanvas()
        {
            DependencyObject parent = this;
            while (parent != null)
            {
                parent = VisualTreeHelper.GetParent(parent);
                if (parent is Canvas canvas)
                    return canvas;
            }
            return null;
        }

        private Border FindMainBorder()
        {
            return FindVisualChild<Border>(this);
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private bool ShouldSnapToGrid()
        {
            // 可以从设置或父容器获取网格对齐选项
            return true; // 默认启用网格对齐
        }

        private double GetGridSize()
        {
            return 20; // 默认网格大小
        }

        #endregion

        #region 事件

        public event EventHandler<NodeEventArgs> OnNodeDeleteRequested;
        public event EventHandler<NodeEventArgs> OnNodeNameEditRequested;
        public event EventHandler<NodeEventArgs> OnNodePropertiesEditRequested;
        public event EventHandler<NodeDraggedEventArgs> OnNodeDragged;
        public event EventHandler<NodeDragCompletedEventArgs> OnNodeDragCompleted;

        #endregion

        #region 静态方法

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DraggableNodeViewBase view)
            {
                view.DataContext = e.NewValue;

                if (e.NewValue is WorkflowNodeViewModel nodeViewModel)
                {
                    view.SetupNodeBindings();
                }
            }
        }

        #endregion
    }

    #region 事件参数类
    public class NodeEventArgs : EventArgs
    {
        public WorkflowNodeViewModel Node { get; }

        public NodeEventArgs(WorkflowNodeViewModel node)
        {
            Node = node;
        }
    }

    public class NodeDraggedEventArgs : EventArgs
    {
        public WorkflowNodeViewModel Node { get; set; }
        public Point OldPosition { get; set; }
        public Point NewPosition { get; set; }
    }

    public class NodeDragCompletedEventArgs : EventArgs
    {
        public WorkflowNodeViewModel Node { get; set; }
        public Point StartPosition { get; set; }
        public Point EndPosition { get; set; }
    }

    #endregion

    public class DraggableNodeViewBase<TViewModel> : DraggableNodeViewBase, IViewFor<TViewModel>
       where TViewModel : class
    {
        public new TViewModel ViewModel
        {
            get => (TViewModel)base.ViewModel;
            set => base.ViewModel = value;
        }

        TViewModel IViewFor<TViewModel>.ViewModel
        {
            get => ViewModel;
            set => ViewModel = value;
        }

        public DraggableNodeViewBase()
        {
            Loaded += (s, e) =>
            {
                if (ViewModel != null && DataContext != ViewModel)
                {
                    DataContext = ViewModel;
                }
            };
        }
    }
}