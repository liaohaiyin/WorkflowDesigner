using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NodeNetwork.Toolkit;
using WorkflowDesigner.UI.ViewModels;
using WorkflowDesigner.UI.Utilities;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.Views
{
    public partial class NewWorkflowDesignerView : UserControl
    {
        private WorkflowDesignerViewModel ViewModel => DataContext as WorkflowDesignerViewModel;
        
        // 选择相关
        private Point _selectionStartPoint;
        private bool _isSelecting;
        private bool _isDragging;
        private Point _dragStartPoint;
        private Point _dragOffset;
        
        // 端口连接处理器
        private PortConnectionHandler _portConnectionHandler;
        private ConnectionManager _connectionManager;

        public NewWorkflowDesignerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            // 确保控件可以接收键盘焦点
            Focusable = true;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 清理端口连接处理器
                _portConnectionHandler?.Dispose();
                _portConnectionHandler = null;
                _connectionManager = null;

                // 取消事件订阅
                if (ViewModel != null)
                {
                    ViewModel.NodeSelectionChanged -= OnNodeSelectionChanged;
                    ViewModel.NodeMoved -= OnNodeMoved;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"清理资源失败: {ex.Message}");
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateStatusText("新设计器已就绪");

            // 设置视图模型事件处理
            if (ViewModel != null)
            {
                ViewModel.NodeSelectionChanged += OnNodeSelectionChanged;
                ViewModel.NodeMoved += OnNodeMoved;
                
                // 初始化端口连接功能
                InitializePortConnection();
            }

            // 设置焦点
            networkView.Focus();
        }
        
        /// <summary>
        /// 初始化端口连接功能
        /// </summary>
        private void InitializePortConnection()
        {
            try
            {
                if (ViewModel?.Network != null && networkView != null)
                {
                    // 创建连接管理器
                    _connectionManager = new ConnectionManager(ViewModel.Network);
                    
                    // 创建端口连接处理器
                    _portConnectionHandler = new PortConnectionHandler(networkView, ViewModel.Network, _connectionManager);
                    
                    UpdateStatusText("端口连接功能已初始化");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"端口连接初始化失败: {ex.Message}");
            }
        }

        #region 节点工具箱事件

        /// <summary>
        /// 工具箱节点项点击事件
        /// </summary>
        private void NodeListItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is NodeFactoryItem item)
                {
                    // 开始拖拽创建新节点
                    var dragData = new DataObject();
                    dragData.SetData("NodeFactoryItem", item);
                    
                    DragDrop.DoDragDrop(border, dragData, DragDropEffects.Copy);
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"创建节点失败: {ex.Message}");
            }
        }

        #endregion

        #region NetworkView事件处理

        /// <summary>
        /// NetworkView鼠标左键按下事件
        /// </summary>
        private void NetworkView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var position = e.GetPosition(networkView);
                _selectionStartPoint = position;
                
                // 检查是否点击了节点
                var hitResult = VisualTreeHelper.HitTest(networkView, position);
                if (hitResult?.VisualHit != null)
                {
                    var nodeView = FindAncestor<NodeView>(hitResult.VisualHit);
                    if (nodeView != null)
                    {
                        // 点击了节点，开始拖拽
                        _isDragging = true;
                        _dragStartPoint = position;
                        
                        // 获取节点位置
                        var node = nodeView.DataContext as WorkflowNodeViewModel;
                        if (node != null)
                        {
                            _dragOffset = new Point(
                                position.X - node.Position.X,
                                position.Y - node.Position.Y
                            );
                        }
                        
                        e.Handled = true;
                        return;
                    }
                }
                
                // 没有点击节点，开始选择框
                _isSelecting = true;
                SelectionRectangle.Visibility = Visibility.Visible;
                SelectionRectangle.SetValue(Canvas.LeftProperty, position.X);
                SelectionRectangle.SetValue(Canvas.TopProperty, position.Y);
                SelectionRectangle.Width = 0;
                SelectionRectangle.Height = 0;
                
                e.Handled = true;
            }
            catch (Exception ex)
            {
                UpdateStatusText($"鼠标事件处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// NetworkView鼠标左键松开事件
        /// </summary>
        private void NetworkView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDragging)
                {
                    // 结束拖拽
                    _isDragging = false;
                    e.Handled = true;
                }
                else if (_isSelecting)
                {
                    // 结束选择框
                    _isSelecting = false;
                    SelectionRectangle.Visibility = Visibility.Collapsed;
                    
                    // 处理选择框内的节点
                    ProcessSelectionBox();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"鼠标事件处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// NetworkView鼠标移动事件
        /// </summary>
        private void NetworkView_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var position = e.GetPosition(networkView);
                UpdateMousePosition(position);
                
                if (_isDragging)
                {
                    // 处理节点拖拽
                    HandleNodeDragging(position);
                }
                else if (_isSelecting)
                {
                    // 处理选择框
                    HandleSelectionBox(position);
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"鼠标移动处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// NetworkView键盘事件
        /// </summary>
        private void NetworkView_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.Delete:
                        DeleteSelectedNodes();
                        e.Handled = true;
                        break;
                    case Key.A:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            SelectAllNodes();
                            e.Handled = true;
                        }
                        break;
                    case Key.Escape:
                        ClearSelection();
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"键盘事件处理失败: {ex.Message}");
            }
        }

        #endregion

        #region 拖拽处理

        /// <summary>
        /// 处理节点拖拽
        /// </summary>
        private void HandleNodeDragging(Point position)
        {
            try
            {
                if (ViewModel?.Network?.SelectedNodes?.Count > 0)
                {
                    var selectedNode = ViewModel.Network.SelectedNodes.FirstOrDefault() as WorkflowNodeViewModel;
                    if (selectedNode != null)
                    {
                        // 计算新位置
                        var newX = position.X - _dragOffset.X;
                        var newY = position.Y - _dragOffset.Y;
                        
                        // 网格对齐
                        if (SnapToGridCheckBox.IsChecked == true)
                        {
                            newX = Math.Round(newX / 20) * 20;
                            newY = Math.Round(newY / 20) * 20;
                        }
                        
                        // 更新节点位置
                        selectedNode.Position = new Point(newX, newY);
                        
                        // 通知节点移动
                        ViewModel.OnNodeMoved(selectedNode, _dragStartPoint, new Point(newX, newY));
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"节点拖拽处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理选择框
        /// </summary>
        private void HandleSelectionBox(Point position)
        {
            try
            {
                var startX = Math.Min(_selectionStartPoint.X, position.X);
                var startY = Math.Min(_selectionStartPoint.Y, position.Y);
                var width = Math.Abs(position.X - _selectionStartPoint.X);
                var height = Math.Abs(position.Y - _selectionStartPoint.Y);
                
                SelectionRectangle.SetValue(Canvas.LeftProperty, startX);
                SelectionRectangle.SetValue(Canvas.TopProperty, startY);
                SelectionRectangle.Width = width;
                SelectionRectangle.Height = height;
            }
            catch (Exception ex)
            {
                UpdateStatusText($"选择框处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理选择框内的节点
        /// </summary>
        private void ProcessSelectionBox()
        {
            try
            {
                var selectionRect = new Rect(
                    Canvas.GetLeft(SelectionRectangle),
                    Canvas.GetTop(SelectionRectangle),
                    SelectionRectangle.Width,
                    SelectionRectangle.Height
                );
                
                // 选择选择框内的所有节点
                if (ViewModel?.Network?.Nodes != null)
                {
                    foreach (var node in ViewModel.Network.Nodes.Items)
                    {
                        if (node is WorkflowNodeViewModel workflowNode)
                        {
                            var nodeRect = new Rect(
                                workflowNode.Position.X,
                                workflowNode.Position.Y,
                                workflowNode.Width,
                                workflowNode.Height
                            );
                            
                            if (selectionRect.IntersectsWith(nodeRect))
                            {
                                workflowNode.IsSelected = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"选择框处理失败: {ex.Message}");
            }
        }

        #endregion

        #region 节点操作

        /// <summary>
        /// 删除选中的节点
        /// </summary>
        private void DeleteSelectedNodes()
        {
            try
            {
                if (ViewModel?.Network?.SelectedNodes != null)
                {
                    var selectedNodes = ViewModel.Network.SelectedNodes.ToList();
                    foreach (var node in selectedNodes)
                    {
                        if (node is WorkflowNodeViewModel workflowNode)
                        {
                            ViewModel.Network.Nodes.Remove(workflowNode);
                        }
                    }
                    
                    UpdateStatusText($"删除了 {selectedNodes.Count} 个节点");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"删除节点失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 选择所有节点
        /// </summary>
        private void SelectAllNodes()
        {
            try
            {
                if (ViewModel?.Network?.Nodes != null)
                {
                    foreach (var node in ViewModel.Network.Nodes.Items)
                    {
                        if (node is WorkflowNodeViewModel workflowNode)
                        {
                            workflowNode.IsSelected = true;
                        }
                    }
                    
                    UpdateStatusText("已选择所有节点");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"选择所有节点失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除选择
        /// </summary>
        private void ClearSelection()
        {
            try
            {
                if (ViewModel?.Network?.SelectedNodes != null)
                {
                    foreach (var node in ViewModel.Network.SelectedNodes.ToList())
                    {
                        if (node is WorkflowNodeViewModel workflowNode)
                        {
                            workflowNode.IsSelected = false;
                        }
                    }
                    
                    UpdateStatusText("已清除选择");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"清除选择失败: {ex.Message}");
            }
        }

        #endregion

        #region 工具栏事件

        /// <summary>
        /// 适合窗口
        /// </summary>
        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 计算适合窗口的缩放比例
                var networkBounds = CalculateNetworkBounds();
                if (networkBounds.HasValue)
                {
                    var scaleX = networkView.ActualWidth / networkBounds.Value.Width;
                    var scaleY = networkView.ActualHeight / networkBounds.Value.Height;
                    var scale = Math.Min(scaleX, scaleY) * 0.9; // 留10%边距
                    
                    ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, Math.Min(ZoomSlider.Maximum, scale));
                }
                
                UpdateStatusText("视图已适合窗口");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"适合窗口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 实际大小
        /// </summary>
        private void ActualSize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ZoomSlider.Value = 1.0;
                UpdateStatusText("视图已设置为实际大小");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"设置实际大小失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 居中显示
        /// </summary>
        private void CenterView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 计算网络边界
                var networkBounds = CalculateNetworkBounds();
                if (networkBounds.HasValue)
                {
                    // 居中显示逻辑
                    UpdateStatusText("视图已居中显示");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"居中显示失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示网格变化
        /// </summary>
        private void ShowGrid_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var isChecked = ShowGridCheckBox.IsChecked ?? false;
                GridBackground.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
                UpdateStatusText($"网格显示: {(isChecked ? "开启" : "关闭")}");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"网格设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 缩放滑块变化
        /// </summary>
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var zoom = e.NewValue;
                // 应用缩放变换
                var transform = new ScaleTransform(zoom, zoom);
                networkView.LayoutTransform = transform;
                
                UpdateStatusText($"缩放: {zoom:P0}");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"缩放设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 全选
        /// </summary>
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectAllNodes();
        }

        /// <summary>
        /// 清空
        /// </summary>
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel?.Network?.Nodes != null)
                {
                    ViewModel.Network.Nodes.Clear();
                    UpdateStatusText("已清空所有节点");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"清空失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 自动布局
        /// </summary>
        private void AutoLayout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel?.Network?.Nodes != null)
                {
                    var nodes = ViewModel.Network.Nodes.Items.ToList();
                    var spacing = 150.0;
                    var startX = 100.0;
                    var startY = 100.0;
                    
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        if (nodes[i] is WorkflowNodeViewModel workflowNode)
                        {
                            var row = i / 3;
                            var col = i % 3;
                            
                            workflowNode.Position = new Point(
                                startX + col * spacing,
                                startY + row * spacing
                            );
                        }
                    }
                    
                    UpdateStatusText("已应用自动布局");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"自动布局失败: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 查找祖先元素
        /// </summary>
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T result)
                    return result;
                
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// 计算网络边界
        /// </summary>
        private Rect? CalculateNetworkBounds()
        {
            try
            {
                if (ViewModel?.Network?.Nodes?.Items == null || !ViewModel.Network.Nodes.Items.Any())
                    return null;
                
                var minX = double.MaxValue;
                var minY = double.MaxValue;
                var maxX = double.MinValue;
                var maxY = double.MinValue;
                
                foreach (var node in ViewModel.Network.Nodes.Items)
                {
                    if (node is WorkflowNodeViewModel workflowNode)
                    {
                        minX = Math.Min(minX, workflowNode.Position.X);
                        minY = Math.Min(minY, workflowNode.Position.Y);
                        maxX = Math.Max(maxX, workflowNode.Position.X + workflowNode.Width);
                        maxY = Math.Max(maxY, workflowNode.Position.Y + workflowNode.Height);
                    }
                }
                
                if (minX == double.MaxValue)
                    return null;
                
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 更新鼠标位置显示
        /// </summary>
        private void UpdateMousePosition(Point position)
        {
            try
            {
                MousePositionText.Text = $"X: {(int)position.X}, Y: {(int)position.Y}";
            }
            catch
            {
                // 忽略错误
            }
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatusText(string text)
        {
            try
            {
                StatusText.Text = text;
            }
            catch
            {
                // 忽略错误
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 节点选择变化事件
        /// </summary>
        private void OnNodeSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateStatusText("节点选择已更新");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"节点选择处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 节点移动事件
        /// </summary>
        private void OnNodeMoved(object sender, NodeMovedEventArgs e)
        {
            try
            {
                UpdateStatusText($"节点 '{e.Node.NodeName}' 已移动到 ({e.NewPosition.X:F0}, {e.NewPosition.Y:F0})");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"节点移动处理失败: {ex.Message}");
            }
        }

        #endregion
    }
}