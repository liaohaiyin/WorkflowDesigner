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

        public WorkflowDesignerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            MouseMove += OnMouseMove;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateStatusText("设计器已就绪");
        }

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

        #region 视图控制

        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel?.Network?.Nodes?.Items?.Any() == true)
                {
                    var bounds = CalculateNodesBounds();
                    if (bounds.Width > 0 && bounds.Height > 0)
                    {
                        var viewportWidth = DesignerScrollViewer.ViewportWidth - 40; // 留边距
                        var viewportHeight = DesignerScrollViewer.ViewportHeight - 40;

                        var scaleX = viewportWidth / bounds.Width;
                        var scaleY = viewportHeight / bounds.Height;
                        var scale = Math.Min(scaleX, scaleY) * 0.9; // 留10%边距

                        ZoomSlider.Value = Math.Max(0.1, Math.Min(3.0, scale));

                        // 居中显示
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
                    // 居中到画布中心
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

        #endregion

        #region 操作功能

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现全选功能
            UpdateStatusText("全选功能待实现");
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

        #region 鼠标事件

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // 更新鼠标位置显示
            var position = e.GetPosition(NetworkView);
            MousePositionText.Text = $"X: {position.X:F0}, Y: {position.Y:F0}";
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            _isDragging = false;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
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
                maxX = Math.Max(maxX, x + 200); // 假设节点宽度200
                maxY = Math.Max(maxY, y + 100); // 假设节点高度100
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private void AutoLayoutNodes()
        {
            var nodes = ViewModel.Network.Nodes.Items.OfType<WorkflowNodeViewModel>().ToList();
            if (!nodes.Any()) return;

            // 简单的网格布局
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