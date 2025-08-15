using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WorkflowDesigner.Nodes;
using NodeNetwork.ViewModels;

namespace WorkflowDesigner.UI.Views.Nodes
{
    /// <summary>
    /// StartNodeView.xaml 的交互逻辑
    /// </summary>
    public partial class StartNodeView : DraggableNodeViewBase<StartNodeViewModel>
    {
        private bool _isPortHovered = false;
        private bool _isPortDragging = false;
        private Point _portDragStartPoint;

        public StartNodeView()
        {
            InitializeComponent();
            SetupPortEvents();
        }

        protected override Brush GetDefaultBorderBrush()
        {
            return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 绿色
        }

        /// <summary>
        /// 设置端口事件处理
        /// </summary>
        private void SetupPortEvents()
        {
            // 输出端口鼠标事件
            OutputPort.MouseEnter += OnOutputPortMouseEnter;
            OutputPort.MouseLeave += OnOutputPortMouseLeave;
            OutputPort.MouseLeftButtonDown += OnOutputPortMouseLeftButtonDown;
            OutputPort.MouseLeftButtonUp += OnOutputPortMouseLeftButtonUp;
            OutputPort.MouseMove += OnOutputPortMouseMove;
        }

        /// <summary>
        /// 输出端口鼠标进入事件
        /// </summary>
        private void OnOutputPortMouseEnter(object sender, MouseEventArgs e)
        {
            _isPortHovered = true;
            UpdatePortVisualState();
        }

        /// <summary>
        /// 输出端口鼠标离开事件
        /// </summary>
        private void OnOutputPortMouseLeave(object sender, MouseEventArgs e)
        {
            _isPortHovered = false;
            UpdatePortVisualState();
        }

        /// <summary>
        /// 输出端口鼠标左键按下事件
        /// </summary>
        private void OnOutputPortMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                _isPortDragging = true;
                _portDragStartPoint = e.GetPosition(this);
                OutputPort.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 输出端口鼠标左键释放事件
        /// </summary>
        private void OnOutputPortMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPortDragging)
            {
                _isPortDragging = false;
                OutputPort.ReleaseMouseCapture();
                UpdatePortVisualState();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 输出端口鼠标移动事件
        /// </summary>
        private void OnOutputPortMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPortDragging)
            {
                var currentPosition = e.GetPosition(this);
                var delta = currentPosition - _portDragStartPoint;
                
                // 如果移动距离足够大，开始拖拽连接
                if (Math.Abs(delta.X) > 5 || Math.Abs(delta.Y) > 5)
                {
                    StartPortConnection(e);
                }
            }
        }

        /// <summary>
        /// 开始端口连接
        /// </summary>
        private void StartPortConnection(MouseEventArgs e)
        {
            try
            {
                // 通知父级开始端口连接
                var args = new RoutedEventArgs(PortConnectionStartedEvent, this);
                RaiseEvent(args);
                
                // 更新端口视觉状态
                UpdatePortVisualState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"开始端口连接失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新端口视觉状态
        /// </summary>
        private void UpdatePortVisualState()
        {
            if (_isPortDragging)
            {
                // 拖拽状态 - 高亮显示
                OutputPort.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                OutputPort.Stroke = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                OutputPort.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(76, 175, 80),
                    BlurRadius = 8,
                    ShadowDepth = 3,
                    Opacity = 0.8
                };
            }
            else if (_isPortHovered)
            {
                // 悬停状态 - 轻微高亮
                OutputPort.Fill = new SolidColorBrush(Color.FromRgb(102, 187, 106));
                OutputPort.Stroke = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                OutputPort.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(76, 175, 80),
                    BlurRadius = 6,
                    ShadowDepth = 2,
                    Opacity = 0.7
                };
            }
            else
            {
                // 正常状态
                OutputPort.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                OutputPort.Stroke = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                OutputPort.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(76, 175, 80),
                    BlurRadius = 4,
                    ShadowDepth = 2,
                    Opacity = 0.6
                };
            }
        }

        /// <summary>
        /// 端口连接开始事件
        /// </summary>
        public static readonly RoutedEvent PortConnectionStartedEvent = 
            EventManager.RegisterRoutedEvent("PortConnectionStarted", RoutingStrategy.Bubble, 
                typeof(RoutedEventHandler), typeof(StartNodeView));

        /// <summary>
        /// 端口连接开始事件处理器
        /// </summary>
        public event RoutedEventHandler PortConnectionStarted
        {
            add { AddHandler(PortConnectionStartedEvent, value); }
            remove { RemoveHandler(PortConnectionStartedEvent, value); }
        }

        /// <summary>
        /// 获取输出端口位置（相对于NetworkView）
        /// </summary>
        public Point GetOutputPortPosition()
        {
            try
            {
                var transform = OutputPort.TransformToAncestor(this);
                var portCenter = new Point(OutputPort.ActualWidth / 2, OutputPort.ActualHeight / 2);
                var portPosition = transform.Transform(portCenter);
                
                // 转换为相对于NetworkView的位置
                var nodeTransform = this.TransformToAncestor(this.Parent as FrameworkElement);
                if (nodeTransform != null)
                {
                    var nodePosition = nodeTransform.Transform(portPosition);
                    return nodePosition;
                }
                
                return portPosition;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取输出端口位置失败: {ex.Message}");
                return new Point(0, 0);
            }
        }

        /// <summary>
        /// 设置端口高亮状态
        /// </summary>
        public void SetPortHighlight(bool isHighlighted, bool isValid = true)
        {
            if (isHighlighted)
            {
                var color = isValid ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54);
                OutputPort.Fill = new SolidColorBrush(color);
                OutputPort.Stroke = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                OutputPort.Effect = new DropShadowEffect
                {
                    Color = color,
                    BlurRadius = 10,
                    ShadowDepth = 3,
                    Opacity = 0.9
                };
            }
            else
            {
                UpdatePortVisualState();
            }
        }
    }
}
