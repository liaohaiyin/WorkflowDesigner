using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.Views.Nodes
{
    public class DraggableNodeViewBase : UserControl, IViewFor, IActivatableView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(object), typeof(DraggableNodeViewBase),
                new PropertyMetadata(null, OnViewModelChanged));

        private bool _isDragging;
        private Point _dragStartPoint;
        private Point _nodeStartPosition;
        private Canvas _parentCanvas;
        private Border _mainBorder;
        private CompositeDisposable _subscriptions = new CompositeDisposable();

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
            Cursor = Cursors.Hand;
            Focusable = true;
            Width = 140;
            Height = 80;

            // 设置Canvas定位属性
            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, 0);
        }

        private void SetupEventHandlers()
        {
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            KeyDown += OnKeyDown;
            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
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
            _parentCanvas = FindParentCanvas();
            _mainBorder = FindMainBorder();
            if (ViewModel != null && DataContext != ViewModel)
            {
                DataContext = ViewModel;
                SetupNodeBindings();
            }

            // 同步位置到Canvas - 重要！
            SyncPositionToCanvas();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _subscriptions?.Dispose();
            _subscriptions = new CompositeDisposable();
        }

        #endregion

        #region 拖拽功能修复

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var clickPosition = e.GetPosition(this);
                var hitElement = e.OriginalSource as DependencyObject;

                // 综合检查：元素类型检查 + 位置检查
                if (IsPortElement(hitElement) || IsInPortArea(clickPosition))
                {
                    // 如果点击的是端口区域，不处理节点拖拽
                    return;
                }

                if (ViewModel is WorkflowNodeViewModel nodeViewModel)
                {
                    // 记录拖拽开始位置
                    _dragStartPoint = e.GetPosition(_parentCanvas);
                    _nodeStartPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));

                    // 处理节点选择
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        nodeViewModel.IsChecked = !nodeViewModel.IsChecked;
                    }
                    else
                    {
                        nodeViewModel.OnMouseClick();
                    }

                    // 开始拖拽
                    StartDrag();
                    CaptureMouse();
                    Focus();
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"节点鼠标按下处理失败: {ex.Message}");
            }
        }

        private bool IsInPortArea(Point clickPosition)
        {
            try
            {
                // 获取节点内的所有端口区域
                var inputPortAreas = FindVisualChildren<ItemsControl>(this)
                    .Where(ic => ic.ItemsSource != null &&
                                (ic.ItemsSource.GetType().Name.Contains("Input") ||
                                 ic.ItemsSource.GetType().Name.Contains("Output")));

                foreach (var portArea in inputPortAreas)
                {
                    var bounds = new Rect(0, 0, portArea.ActualWidth, portArea.ActualHeight);
                    var relativePoint = this.TranslatePoint(clickPosition, portArea);

                    if (bounds.Contains(relativePoint))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // 辅助方法：查找指定类型的子元素
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    yield return t;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
        private bool IsPortElement(DependencyObject element)
        {
            if (element == null) return false;

            // 向上遍历可视化树，检查是否包含端口相关的控件
            var current = element;
            while (current != null && current != this)
            {
                // 检查是否是 NodeNetwork 的端口视图
                var typeName = current.GetType().Name;
                if (typeName.Contains("PortView") ||
                    typeName.Contains("NodeInputView") ||
                    typeName.Contains("NodeOutputView"))
                {
                    return true;
                }

                // 检查 DataContext 是否为端口视图模型
                if (current is FrameworkElement fe && fe.DataContext != null)
                {
                    var dataContextType = fe.DataContext.GetType().Name;
                    if (dataContextType.Contains("NodeInputViewModel") ||
                        dataContextType.Contains("NodeOutputViewModel") ||
                        dataContextType.Contains("PortViewModel"))
                    {
                        return true;
                    }
                }

                // 检查是否是包含端口的容器（通过名称）
                if (current is FrameworkElement frameElement)
                {
                    var name = frameElement.Name;
                    if (!string.IsNullOrEmpty(name) &&
                        (name.Contains("Port") || name.Contains("Input") || name.Contains("Output")))
                    {
                        return true;
                    }
                }

                // 特殊处理：检查是否在端口区域内（通过位置判断）
                if (current is Border border)
                {
                    // 检查Border的父容器是否有端口相关的内容
                    var parent = VisualTreeHelper.GetParent(border);
                    if (parent is ItemsControl itemsControl)
                    {
                        var itemsSource = itemsControl.ItemsSource;
                        if (itemsSource != null)
                        {
                            var itemsSourceType = itemsSource.GetType().Name;
                            if (itemsSourceType.Contains("Input") || itemsSourceType.Contains("Output"))
                            {
                                return true;
                            }
                        }
                    }
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
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
                    var newX = Math.Max(0, _nodeStartPosition.X + deltaX);
                    var newY = Math.Max(0, _nodeStartPosition.Y + deltaY);

                    // 网格对齐（可选）
                    if (ShouldSnapToGrid())
                    {
                        var gridSize = GetGridSize();
                        newX = Math.Round(newX / gridSize) * gridSize;
                        newY = Math.Round(newY / gridSize) * gridSize;
                    }

                    // 更新Canvas位置 - 关键修复！
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

            UpdateDragVisual(false);
            Cursor = Cursors.Hand;
        }

        #endregion

        #region 位置同步修复

        private void SyncPositionToCanvas()
        {
            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                Canvas.SetLeft(this, nodeViewModel.Position.X);
                Canvas.SetTop(this, nodeViewModel.Position.Y);
            }
        }

        private void SetupNodeBindings()
        {
            if (ViewModel is WorkflowNodeViewModel nodeViewModel)
            {
                try
                {
                    // 监听位置变化并同步到Canvas
                    nodeViewModel.WhenAnyValue(x => x.Position)
                        .ObserveOnDispatcher()
                        .Subscribe(
                            onNext: position =>
                            {
                                try
                                {
                                    if (!_isDragging) // 避免拖拽时的循环更新
                                    {
                                        Canvas.SetLeft(this, position.X);
                                        Canvas.SetTop(this, position.Y);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"位置同步失败: {ex.Message}");
                                }
                            },
                            onError: ex =>
                            {
                                System.Diagnostics.Debug.WriteLine($"Position Observable异常: {ex.Message}");
                            })
                        .DisposeWith(_subscriptions);

                    nodeViewModel.WhenAnyValue(x => x.IsChecked)
                        .ObserveOnDispatcher()
                        .Subscribe(
                            onNext: isSelected =>
                            {
                                try
                                {
                                    UpdateSelectionVisual(isSelected);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"更新选择视觉效果失败: {ex.Message}");
                                }
                            },
                            onError: ex =>
                            {
                                System.Diagnostics.Debug.WriteLine($"IsChecked绑定异常: {ex.Message}");
                            })
                        .DisposeWith(_subscriptions);

                    nodeViewModel.WhenAnyValue(x => x.IsHovered)
                        .ObserveOnDispatcher()
                        .Subscribe(
                            onNext: isHovered =>
                            {
                                try
                                {
                                    UpdateHoverVisual(isHovered);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"更新悬停视觉效果失败: {ex.Message}");
                                }
                            },
                            onError: ex =>
                            {
                                System.Diagnostics.Debug.WriteLine($"IsHovered绑定异常: {ex.Message}");
                            })
                        .DisposeWith(_subscriptions);

                    nodeViewModel.WhenAnyValue(x => x.IsDragging)
                        .ObserveOnDispatcher()
                        .Subscribe(
                            onNext: isDragging =>
                            {
                                try
                                {
                                    UpdateDragVisual(isDragging);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"更新拖拽视觉效果失败: {ex.Message}");
                                }
                            },
                            onError: ex =>
                            {
                                System.Diagnostics.Debug.WriteLine($"IsDragging绑定异常: {ex.Message}");
                            })
                        .DisposeWith(_subscriptions);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"设置节点绑定失败: {ex.Message}");
                }
            }
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
                    newY = currentY + moveDistance;
                    break;
                case Key.Left:
                    newX = Math.Max(0, currentX - moveDistance);
                    break;
                case Key.Right:
                    newX = currentX + moveDistance;
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
                    Panel.SetZIndex(this, 1000);
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
                    view.SyncPositionToCanvas(); // 同步位置
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