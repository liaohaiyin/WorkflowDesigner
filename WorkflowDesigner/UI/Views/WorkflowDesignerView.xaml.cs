using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WorkflowDesigner.UI.ViewModels;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.Views
{
    public partial class WorkflowDesignerView : UserControl
    {
        private WorkflowDesignerViewModel ViewModel => DataContext as WorkflowDesignerViewModel;
        private Point _dragStartPoint;
        private bool _isDragging;
        private bool _isNodeDragging;
        private WorkflowNodeViewModel _draggingNode;
        private const double NODE_SELECTION_TOLERANCE = 75; // 节点选择容差（像素）

        public WorkflowDesignerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;

            // 设置鼠标事件处理
            MouseMove += OnMouseMove;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseRightButtonDown += OnMouseRightButtonDown;

            // 设置键盘事件处理
            KeyDown += OnKeyDown;

            // 确保控件可以接收键盘焦点
            Focusable = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateStatusText("设计器已就绪");

            // 设置视图模型事件处理
            if (ViewModel != null)
            {
                ViewModel.NodeSelectionChanged += OnNodeSelectionChanged;
                ViewModel.NodeMoved += OnNodeMoved;
            }
        }

        #region 节点选择和交互

        private void OnNodeSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (ViewModel?.SelectedNode != null)
                {
                    UpdateStatusText($"已选择节点: {ViewModel.SelectedNode.NodeName}");

                    // 确保选中的节点可见
                    EnsureNodeVisible(ViewModel.SelectedNode);
                }
                else
                {
                    UpdateStatusText("未选择节点");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"节点选择处理失败: {ex.Message}");
            }
        }

        private void OnNodeMoved(object sender, NodeMovedEventArgs e)
        {
            try
            {
                UpdateStatusText($"节点 {e.Node.NodeName} 已移动到 ({e.NewPosition.X:F0}, {e.NewPosition.Y:F0})");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"节点移动处理失败: {ex.Message}");
            }
        }

        private void EnsureNodeVisible(WorkflowNodeViewModel node)
        {
            if (node == null) return;

            try
            {
                var nodePosition = node.Position;
                var viewportBounds = new Rect(
                    DesignerScrollViewer.HorizontalOffset,
                    DesignerScrollViewer.VerticalOffset,
                    DesignerScrollViewer.ViewportWidth,
                    DesignerScrollViewer.ViewportHeight
                );

                // 如果节点不在可视区域内，滚动到节点位置
                if (!viewportBounds.Contains(nodePosition))
                {
                    var centerX = nodePosition.X - DesignerScrollViewer.ViewportWidth / 2;
                    var centerY = nodePosition.Y - DesignerScrollViewer.ViewportHeight / 2;

                    DesignerScrollViewer.ScrollToHorizontalOffset(Math.Max(0, centerX));
                    DesignerScrollViewer.ScrollToVerticalOffset(Math.Max(0, centerY));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"确保节点可见失败: {ex.Message}");
            }
        }

        #endregion

        #region 拖拽功能

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(ToolboxItemViewModel)))
                {
                    var toolboxItem = (ToolboxItemViewModel)e.Data.GetData(typeof(ToolboxItemViewModel));
                    var position = e.GetPosition(NetworkView);

                    // 网格对齐
                    if (SnapToGridCheckBox.IsChecked == true)
                    {
                        var gridSize = 20;
                        position.X = Math.Round(position.X / gridSize) * gridSize;
                        position.Y = Math.Round(position.Y / gridSize) * gridSize;
                    }

                    ViewModel?.AddNode(toolboxItem.NodeType, position);
                    UpdateStatusText($"已添加节点: {toolboxItem.Name}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"添加节点失败: {ex.Message}");
                MessageBox.Show($"添加节点失败: {ex.Message}", "错误",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ToolboxItemViewModel)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        #endregion

        #region 鼠标事件处理

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 获取鼠标在NetworkView中的位置
                var position = e.GetPosition(NetworkView);
                _dragStartPoint = position;
                _isDragging = false;
                _isNodeDragging = false;

                // 查找点击位置的节点
                var clickedNode = GetNodeAtPosition(position);

                if (clickedNode != null)
                {
                    // 点击了节点
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        // Ctrl+点击：切换选择状态
                        clickedNode.IsSelected = !clickedNode.IsSelected;
                        if (clickedNode.IsSelected)
                        {
                            ViewModel.SelectedNode = clickedNode;
                        }
                    }
                    else
                    {
                        // 普通点击：选择节点
                        ViewModel?.SelectNode(clickedNode);
                    }

                    // 准备开始拖拽节点
                    _draggingNode = clickedNode;
                    clickedNode.StartDrag();

                    // 捕获鼠标
                    CaptureMouse();
                }
                else
                {
                    // 点击了空白区域，取消选择
                    if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        ViewModel.SelectedNode = null;
                    }
                }

                // 设置焦点以接收键盘事件
                Focus();
            }
            catch (Exception ex)
            {
                UpdateStatusText($"鼠标点击处理失败: {ex.Message}");
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var currentPosition = e.GetPosition(NetworkView);

                // 更新鼠标位置显示
                if (MousePositionText != null)
                {
                    MousePositionText.Text = $"X: {currentPosition.X:F0}, Y: {currentPosition.Y:F0}";
                }

                // 处理节点拖拽
                if (e.LeftButton == MouseButtonState.Pressed && _draggingNode != null)
                {
                    var dragVector = currentPosition - _dragStartPoint;
                    var dragDistance = Math.Sqrt(dragVector.X * dragVector.X + dragVector.Y * dragVector.Y);

                    // 开始拖拽（鼠标移动超过阈值）
                    if (!_isNodeDragging && dragDistance > 5)
                    {
                        _isNodeDragging = true;
                        ViewModel?.StartDrag(_dragStartPoint);
                        UpdateStatusText($"开始拖拽节点: {_draggingNode.NodeName}");
                    }

                    // 更新拖拽
                    if (_isNodeDragging)
                    {
                        // 网格对齐
                        var newPosition = currentPosition;
                        if (SnapToGridCheckBox?.IsChecked == true)
                        {
                            var gridSize = 20;
                            newPosition.X = Math.Round(newPosition.X / gridSize) * gridSize;
                            newPosition.Y = Math.Round(newPosition.Y / gridSize) * gridSize;
                        }

                        // 确保节点不会被拖到负坐标
                        newPosition.X = Math.Max(0, newPosition.X);
                        newPosition.Y = Math.Max(0, newPosition.Y);

                        ViewModel?.MoveNode(_draggingNode, newPosition);
                    }
                }
                else
                {
                    // 更新节点悬停状态
                    UpdateNodeHoverState(currentPosition);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"鼠标移动处理失败: {ex.Message}");
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var endPosition = e.GetPosition(NetworkView);

                if (_isNodeDragging && _draggingNode != null)
                {
                    // 结束节点拖拽
                    ViewModel?.EndDrag(endPosition);
                    _draggingNode.EndDrag();
                    UpdateStatusText($"节点拖拽完成: {_draggingNode.NodeName}");
                }

                // 清理拖拽状态
                _isDragging = false;
                _isNodeDragging = false;
                _draggingNode = null;

                // 释放鼠标捕获
                ReleaseMouseCapture();
            }
            catch (Exception ex)
            {
                UpdateStatusText($"鼠标释放处理失败: {ex.Message}");
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var position = e.GetPosition(NetworkView);
                var rightClickedNode = GetNodeAtPosition(position);

                if (rightClickedNode != null)
                {
                    // 右键点击节点，显示上下文菜单
                    ShowNodeContextMenu(rightClickedNode, e.GetPosition(this));
                }
                else
                {
                    // 右键点击空白区域，显示画布上下文菜单
                    ShowCanvasContextMenu(e.GetPosition(this));
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"右键点击处理失败: {ex.Message}");
            }
        }

        private WorkflowNodeViewModel GetNodeAtPosition(Point position)
        {
            try
            {
                if (ViewModel?.Network?.Nodes == null) return null;

                // 查找位置附近的节点
                return ViewModel.Network.Nodes.Items.OfType<WorkflowNodeViewModel>()
                    .FirstOrDefault(node =>
                    {
                        var nodePos = node.Position;
                        var distance = Math.Sqrt(
                            Math.Pow(nodePos.X - position.X, 2) +
                            Math.Pow(nodePos.Y - position.Y, 2));
                        return distance <= NODE_SELECTION_TOLERANCE;
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取节点位置失败: {ex.Message}");
                return null;
            }
        }

        private void UpdateNodeHoverState(Point mousePosition)
        {
            try
            {
                if (ViewModel?.Network?.Nodes == null) return;

                foreach (var node in ViewModel.Network.Nodes.Items.OfType<WorkflowNodeViewModel>())
                {
                    var nodePos = node.Position;
                    var distance = Math.Sqrt(
                        Math.Pow(nodePos.X - mousePosition.X, 2) +
                        Math.Pow(nodePos.Y - mousePosition.Y, 2));

                    bool shouldHover = distance <= NODE_SELECTION_TOLERANCE;

                    if (node.IsHovered != shouldHover)
                    {
                        if (shouldHover)
                        {
                            node.OnMouseEnter();
                        }
                        else
                        {
                            node.OnMouseLeave();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新节点悬停状态失败: {ex.Message}");
            }
        }

        #endregion

        #region 键盘事件处理

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.Delete:
                        // 删除选中的节点
                        if (ViewModel?.SelectedNode != null)
                        {
                            var result = MessageBox.Show(
                                $"确定要删除节点 '{ViewModel.SelectedNode.NodeName}' 吗？",
                                "确认删除",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                ViewModel.DeleteSelectedNodes();
                                UpdateStatusText("已删除选中节点");
                            }
                        }
                        e.Handled = true;
                        break;

                    case Key.Escape:
                        // 取消当前操作
                        if (_isNodeDragging)
                        {
                            ViewModel?.CancelDrag();
                            _draggingNode?.EndDrag();
                            _isNodeDragging = false;
                            _draggingNode = null;
                            ReleaseMouseCapture();
                            UpdateStatusText("已取消拖拽操作");
                        }
                        else
                        {
                            // 取消选择
                            ViewModel.SelectedNode = null;
                            UpdateStatusText("已取消选择");
                        }
                        e.Handled = true;
                        break;

                    case Key.A:
                        // Ctrl+A 全选
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            SelectAllNodes();
                            e.Handled = true;
                        }
                        break;

                    case Key.C:
                        // Ctrl+C 复制
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            CopySelectedNode();
                            e.Handled = true;
                        }
                        break;

                    case Key.V:
                        // Ctrl+V 粘贴
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            PasteNode();
                            e.Handled = true;
                        }
                        break;

                    case Key.Z:
                        // Ctrl+Z 撤销
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            ViewModel?.Undo();
                            UpdateStatusText("已撤销操作");
                            e.Handled = true;
                        }
                        break;

                    case Key.Y:
                        // Ctrl+Y 重做
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            ViewModel?.Redo();
                            UpdateStatusText("已重做操作");
                            e.Handled = true;
                        }
                        break;

                    // 方向键移动选中节点
                    case Key.Up:
                    case Key.Down:
                    case Key.Left:
                    case Key.Right:
                        if (ViewModel?.SelectedNode != null)
                        {
                            MoveSelectedNodeWithArrowKey(e.Key);
                            e.Handled = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"键盘事件处理失败: {ex.Message}");
            }
        }

        private void MoveSelectedNodeWithArrowKey(Key key)
        {
            if (ViewModel?.SelectedNode == null) return;

            var moveDistance = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
            var offset = new Vector();

            switch (key)
            {
                case Key.Up:
                    offset.Y = -moveDistance;
                    break;
                case Key.Down:
                    offset.Y = moveDistance;
                    break;
                case Key.Left:
                    offset.X = -moveDistance;
                    break;
                case Key.Right:
                    offset.X = moveDistance;
                    break;
            }

            ViewModel.MoveSelectedNode(offset);
            UpdateStatusText($"已移动节点: {ViewModel.SelectedNode.NodeName}");
        }

        #endregion

        #region 上下文菜单

        private void ShowNodeContextMenu(WorkflowNodeViewModel node, Point position)
        {
            try
            {
                var contextMenu = new ContextMenu();

                // 添加菜单项
                var selectItem = new MenuItem { Header = "选择" };
                selectItem.Click += (s, e) => ViewModel?.SelectNode(node);
                contextMenu.Items.Add(selectItem);

                var deleteItem = new MenuItem { Header = "删除" };
                deleteItem.Click += (s, e) => DeleteNode(node);
                contextMenu.Items.Add(deleteItem);

                contextMenu.Items.Add(new Separator());

                var copyItem = new MenuItem { Header = "复制" };
                copyItem.Click += (s, e) => CopyNode(node);
                contextMenu.Items.Add(copyItem);

                var propertiesItem = new MenuItem { Header = "属性" };
                propertiesItem.Click += (s, e) => ShowNodeProperties(node);
                contextMenu.Items.Add(propertiesItem);

                // 显示上下文菜单
                contextMenu.PlacementTarget = this;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                UpdateStatusText($"显示节点上下文菜单失败: {ex.Message}");
            }
        }

        private void ShowCanvasContextMenu(Point position)
        {
            try
            {
                var contextMenu = new ContextMenu();

                var pasteItem = new MenuItem { Header = "粘贴" };
                pasteItem.Click += (s, e) => PasteNode();
                contextMenu.Items.Add(pasteItem);

                var selectAllItem = new MenuItem { Header = "全选" };
                selectAllItem.Click += (s, e) => SelectAllNodes();
                contextMenu.Items.Add(selectAllItem);

                contextMenu.Items.Add(new Separator());

                var layoutItem = new MenuItem { Header = "自动布局" };
                layoutItem.Click += (s, e) => AutoLayout_Click(null, null);
                contextMenu.Items.Add(layoutItem);

                // 显示上下文菜单
                contextMenu.PlacementTarget = this;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                UpdateStatusText($"显示画布上下文菜单失败: {ex.Message}");
            }
        }

        #endregion

        #region 节点操作方法

        private void DeleteNode(WorkflowNodeViewModel node)
        {
            try
            {
                var result = MessageBox.Show(
                    $"确定要删除节点 '{node.NodeName}' 吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ViewModel?.DeleteNode(node);
                    UpdateStatusText($"已删除节点: {node.NodeName}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"删除节点失败: {ex.Message}");
            }
        }

        private void CopyNode(WorkflowNodeViewModel node)
        {
            try
            {
                // 这里可以实现节点复制功能
                // 将节点数据序列化并保存到剪贴板
                var nodeData = node.SerializeNodeData();
                Clipboard.SetText(nodeData);
                UpdateStatusText($"已复制节点: {node.NodeName}");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"复制节点失败: {ex.Message}");
            }
        }

        private void CopySelectedNode()
        {
            if (ViewModel?.SelectedNode != null)
            {
                CopyNode(ViewModel.SelectedNode);
            }
        }

        private void PasteNode()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var nodeData = Clipboard.GetText();
                    // 这里可以实现节点粘贴功能
                    // 反序列化节点数据并创建新节点
                    UpdateStatusText("粘贴功能待实现");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"粘贴节点失败: {ex.Message}");
            }
        }

        private void SelectAllNodes()
        {
            try
            {
                // 选择所有节点（这里只选择第一个节点作为示例）
                var firstNode = ViewModel?.Network?.Nodes?.Items?.OfType<WorkflowNodeViewModel>()?.FirstOrDefault();
                if (firstNode != null)
                {
                    ViewModel.SelectedNode = firstNode;
                    UpdateStatusText("已选择第一个节点");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"全选节点失败: {ex.Message}");
            }
        }

        private void ShowNodeProperties(WorkflowNodeViewModel node)
        {
            try
            {
                // 选择节点以在属性面板中显示其属性
                ViewModel?.SelectNode(node);
                UpdateStatusText($"显示节点属性: {node.NodeName}");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"显示节点属性失败: {ex.Message}");
            }
        }

        #endregion

        #region 视图控制 (保持原有实现)

        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel?.Network?.Nodes?.Items?.Any() == true)
                {
                    var bounds = CalculateNodesBounds();
                    if (bounds.Width > 0 && bounds.Height > 0)
                    {
                        var viewportWidth = DesignerScrollViewer.ViewportWidth - 40;
                        var viewportHeight = DesignerScrollViewer.ViewportHeight - 40;

                        var scaleX = viewportWidth / bounds.Width;
                        var scaleY = viewportHeight / bounds.Height;
                        var scale = Math.Min(scaleX, scaleY) * 0.9;

                        ZoomSlider.Value = Math.Max(0.1, Math.Min(3.0, scale));

                        DesignerScrollViewer.ScrollToHorizontalOffset(bounds.X - 20);
                        DesignerScrollViewer.ScrollToVerticalOffset(bounds.Y - 20);

                        UpdateStatusText("已适合窗口大小");
                    }
                }
                else
                {
                    UpdateStatusText("没有节点可以适合窗口");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"适合窗口失败: {ex.Message}");
            }
        }

        private void ActualSize_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value = 1.0;
            UpdateStatusText("已恢复实际大小");
        }

        private void CenterView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel?.Network?.Nodes?.Items?.Any() == true)
                {
                    var bounds = CalculateNodesBounds();
                    var centerX = bounds.X + bounds.Width / 2;
                    var centerY = bounds.Y + bounds.Height / 2;

                    var viewportCenterX = DesignerScrollViewer.ViewportWidth / 2;
                    var viewportCenterY = DesignerScrollViewer.ViewportHeight / 2;

                    DesignerScrollViewer.ScrollToHorizontalOffset(centerX - viewportCenterX);
                    DesignerScrollViewer.ScrollToVerticalOffset(centerY - viewportCenterY);

                    UpdateStatusText("视图已居中");
                }
                else
                {
                    DesignerScrollViewer.ScrollToHorizontalOffset(1000 - DesignerScrollViewer.ViewportWidth / 2);
                    DesignerScrollViewer.ScrollToVerticalOffset(1000 - DesignerScrollViewer.ViewportHeight / 2);
                    UpdateStatusText("视图已居中");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"居中视图失败: {ex.Message}");
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (NetworkView != null)
            {
                var scaleTransform = new ScaleTransform(e.NewValue, e.NewValue);
                NetworkView.RenderTransform = scaleTransform;
                UpdateStatusText($"缩放: {e.NewValue:P0}");
            }
        }

        private void ShowGrid_Changed(object sender, RoutedEventArgs e)
        {
            var isVisible = ShowGridCheckBox.IsChecked == true;
            UpdateStatusText(isVisible ? "网格已显示" : "网格已隐藏");
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectAllNodes();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要清空所有节点吗？", "确认",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ViewModel?.CreateNewWorkflow();
                UpdateStatusText("已清空所有节点");
            }
        }

        private void AutoLayout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel?.Network?.Nodes?.Items?.Any() == true)
                {
                    AutoLayoutNodes();
                    UpdateStatusText("自动布局完成");
                }
                else
                {
                    UpdateStatusText("没有节点需要布局");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"自动布局失败: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        private Rect CalculateNodesBounds()
        {
            if (ViewModel?.Network?.Nodes?.Items?.Any() != true)
                return new Rect();

            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            foreach (var node in ViewModel.Network.Nodes.Items.OfType<WorkflowNodeViewModel>())
            {
                var x = node.Position.X;
                var y = node.Position.Y;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x + 200);
                maxY = Math.Max(maxY, y + 100);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private void AutoLayoutNodes()
        {
            var nodes = ViewModel.Network.Nodes.Items.OfType<WorkflowNodeViewModel>().ToList();
            if (!nodes.Any()) return;

            var startX = 100;
            var startY = 100;
            var spacingX = 250;
            var spacingY = 150;
            var nodesPerRow = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(nodes.Count)));

            for (int i = 0; i < nodes.Count; i++)
            {
                var row = i / nodesPerRow;
                var col = i % nodesPerRow;

                nodes[i].Position = new Point(
                    startX + col * spacingX,
                    startY + row * spacingY
                );
            }
        }

        private void UpdateStatusText(string message)
        {
            if (StatusText != null)
            {
                StatusText.Text = message;
            }
        }

        #endregion
    }
}