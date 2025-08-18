// 修复WorkflowDesignerView.xaml.cs - 确保NetworkView正确显示连接线

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WorkflowDesigner.UI.ViewModels;
using WorkflowDesigner.UI.Utilities;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.Views
{
    public partial class WorkflowDesignerView : UserControl
    {
        private WorkflowDesignerViewModel ViewModel => DataContext as WorkflowDesignerViewModel;
        private bool _isNodeDragging;
        private WorkflowNodeViewModel _draggingNode;
        private const double NODE_SELECTION_TOLERANCE = 75;

        // 端口连接处理器
        private PortConnectionHandler _portConnectionHandler;
        private ConnectionManager _connectionManager;

        public WorkflowDesignerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            // 设置鼠标事件处理
            MouseMove += OnMouseMove;

            // 设置键盘事件处理
            KeyDown += OnKeyDown;

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
            UpdateStatusText("设计器已就绪");

            // 设置视图模型事件处理
            if (ViewModel != null)
            {
                ViewModel.NodeSelectionChanged += OnNodeSelectionChanged;
                ViewModel.NodeMoved += OnNodeMoved;

                // 初始化端口连接功能
                InitializePortConnection();

                // 确保NetworkView正确设置
                EnsureNetworkViewSetup();
            }
        }

        /// <summary>
        /// 确保NetworkView正确设置
        /// </summary>
        private void EnsureNetworkViewSetup()
        {
            try
            {
                if (networkView != null && ViewModel?.Network != null)
                {
                    // 确保ViewModel正确绑定
                    if (networkView.ViewModel != ViewModel.Network)
                    {
                        networkView.ViewModel = ViewModel.Network;
                    }

                    // 设置NetworkView的渲染属性
                    networkView.ClipToBounds = false; // 允许连接线渲染到边界外
                    networkView.Background = Brushes.Transparent;

                    // 确保NetworkView能够正确处理连接
                    networkView.IsEnabled = true;
                    networkView.Visibility = Visibility.Visible;

                    UpdateStatusText("NetworkView设置完成");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"NetworkView设置失败: {ex.Message}");
            }
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

                    // 监听连接变化 - 关键调试点
                    ViewModel.Network.Connections.Connect()
                        .Subscribe(changes =>
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                var connectionCount = ViewModel.Network.Connections.Count;
                                UpdateStatusText($"连接数变化: {connectionCount}");

                                // 打印所有连接信息用于调试
                                foreach (var conn in ViewModel.Network.Connections.Items)
                                {
                                    var sourceNode = conn.Output?.Parent?.Name ?? "Unknown";
                                    var targetNode = conn.Input?.Parent?.Name ?? "Unknown";
                                    var sourcePort = conn.Output?.Name ?? "Unknown";
                                    var targetPort = conn.Input?.Name ?? "Unknown";

                                    System.Diagnostics.Debug.WriteLine($"连接: {sourceNode}.{sourcePort} -> {targetNode}.{targetPort}");
                                }

                                // 强制刷新
                                networkView.InvalidateVisual();
                                networkView.UpdateLayout();

                            }), System.Windows.Threading.DispatcherPriority.DataBind);
                        });

                    UpdateStatusText("端口连接功能已初始化");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"初始化端口连接功能失败: {ex.Message}");
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

                // 触发NetworkView重新渲染连接线
                InvalidateNetworkView();
            }
            catch (Exception ex)
            {
                UpdateStatusText($"节点移动处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 强制NetworkView重新渲染
        /// </summary>
        private void InvalidateNetworkView()
        {
            try
            {
                networkView?.InvalidateVisual();
                networkView?.UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NetworkView重新渲染失败: {ex.Message}");
            }
        }

        // 检查是否为端口相关元素
        private bool IsPortRelatedElement(Visual visual)
        {
            if (visual == null) return false;

            var current = visual as DependencyObject;
            while (current != null)
            {
                var typeName = current.GetType().Name;
                if (typeName.Contains("PortView") ||
                    typeName.Contains("NodeInputView") ||
                    typeName.Contains("NodeOutputView") ||
                    typeName.Contains("Connector"))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        // 获取命中目标的辅助方法
        private Visual GetHitTarget(Point position)
        {
            try
            {
                var hitTest = VisualTreeHelper.HitTest(networkView, position);
                return hitTest?.VisualHit as Visual;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取命中目标失败: {ex.Message}");
                return null;
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
                    var position = e.GetPosition(networkView);

                    // 网格对齐
                    if (SnapToGridCheckBox.IsChecked == true)
                    {
                        var gridSize = 20;
                        position.X = Math.Round(position.X / gridSize) * gridSize;
                        position.Y = Math.Round(position.Y / gridSize) * gridSize;
                    }

                    ViewModel?.AddNode(toolboxItem.NodeType, position);
                    UpdateStatusText($"已添加节点: {toolboxItem.Name}");

                    // 刷新NetworkView
                    InvalidateNetworkView();
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText($"添加节点失败: {ex.Message}");
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

        private void NetworkView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var hitTarget = GetHitTarget(e.GetPosition(networkView));

                // 检查是否点击了端口
                if (IsPortRelatedElement(hitTarget))
                {
                    // 如果是端口相关元素，标记事件已处理，防止节点拖拽
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NetworkView预览事件处理失败: {ex.Message}");
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var currentPosition = e.GetPosition(networkView);

                // 更新鼠标位置显示
                if (MousePositionText != null)
                {
                    MousePositionText.Text = $"X: {currentPosition.X:F0}, Y: {currentPosition.Y:F0}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"鼠标移动处理失败: {ex.Message}");
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var position = e.GetPosition(networkView);
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
                                InvalidateNetworkView();
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
                            InvalidateNetworkView();
                            e.Handled = true;
                        }
                        break;

                    case Key.Y:
                        // Ctrl+Y 重做
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            ViewModel?.Redo();
                            UpdateStatusText("已重做操作");
                            InvalidateNetworkView();
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
            InvalidateNetworkView();
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
                    InvalidateNetworkView();
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

        #region 视图控制

        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel?.Network?.Nodes?.Items?.Any() == true)
                {
                    UpdateStatusText("适合窗口功能需要进一步实现");
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
                UpdateStatusText("居中视图功能需要进一步实现");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"居中视图失败: {ex.Message}");
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (networkView != null)
            {
                var scaleTransform = new ScaleTransform(e.NewValue, e.NewValue);
                networkView.RenderTransform = scaleTransform;
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
                InvalidateNetworkView();
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
                    InvalidateNetworkView();
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