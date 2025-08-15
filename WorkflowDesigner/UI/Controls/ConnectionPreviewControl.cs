using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WorkflowDesigner.UI.Controls
{
    /// <summary>
    /// 连接预览控件 - 在端口连接过程中显示预览线
    /// </summary>
    public class ConnectionPreviewControl : Canvas
    {
        private Line _previewLine;
        private Ellipse _startPoint;
        private Ellipse _endPoint;
        
        public ConnectionPreviewControl()
        {
            IsHitTestVisible = false; // 不拦截鼠标事件
            CreatePreviewElements();
        }

        private void CreatePreviewElements()
        {
            // 创建预览线
            _previewLine = new Line
            {
                Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // 蓝色
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 }, // 虚线
                Opacity = 0.8
            };
            
            // 创建起始点
            _startPoint = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // 绿色
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            
            // 创建结束点
            _endPoint = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)), // 橙色
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            
            Children.Add(_previewLine);
            Children.Add(_startPoint);
            Children.Add(_endPoint);
            
            Hide();
        }

        /// <summary>
        /// 显示连接预览
        /// </summary>
        /// <param name="startPoint">起始点位置</param>
        /// <param name="endPoint">结束点位置</param>
        public void ShowPreview(Point startPoint, Point endPoint)
        {
            UpdatePreview(startPoint, endPoint);
            Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 更新连接预览
        /// </summary>
        /// <param name="startPoint">起始点位置</param>
        /// <param name="endPoint">结束点位置</param>
        public void UpdatePreview(Point startPoint, Point endPoint)
        {
            // 更新预览线
            _previewLine.X1 = startPoint.X;
            _previewLine.Y1 = startPoint.Y;
            _previewLine.X2 = endPoint.X;
            _previewLine.Y2 = endPoint.Y;
            
            // 更新起始点位置
            Canvas.SetLeft(_startPoint, startPoint.X - _startPoint.Width / 2);
            Canvas.SetTop(_startPoint, startPoint.Y - _startPoint.Height / 2);
            
            // 更新结束点位置
            Canvas.SetLeft(_endPoint, endPoint.X - _endPoint.Width / 2);
            Canvas.SetTop(_endPoint, endPoint.Y - _endPoint.Height / 2);
        }

        /// <summary>
        /// 隐藏连接预览
        /// </summary>
        public void Hide()
        {
            Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 设置预览线颜色（根据连接有效性）
        /// </summary>
        /// <param name="isValid">连接是否有效</param>
        public void SetValidationState(bool isValid)
        {
            var color = isValid ? 
                Color.FromRgb(76, 175, 80) :  // 绿色 - 有效
                Color.FromRgb(244, 67, 54);   // 红色 - 无效
                
            _previewLine.Stroke = new SolidColorBrush(color);
            _endPoint.Fill = new SolidColorBrush(color);
        }

        /// <summary>
        /// 使用贝塞尔曲线更新预览（更美观的连接线）
        /// </summary>
        /// <param name="startPoint">起始点位置</param>
        /// <param name="endPoint">结束点位置</param>
        public void UpdateBezierPreview(Point startPoint, Point endPoint)
        {
            // 移除直线
            if (Children.Contains(_previewLine))
            {
                Children.Remove(_previewLine);
            }

            // 创建贝塞尔曲线路径
            var path = new Path
            {
                Stroke = _previewLine.Stroke,
                StrokeThickness = _previewLine.StrokeThickness,
                StrokeDashArray = _previewLine.StrokeDashArray,
                Opacity = _previewLine.Opacity
            };

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = startPoint
            };

            // 计算控制点以创建平滑的曲线
            var controlOffset = Math.Abs(endPoint.X - startPoint.X) * 0.5;
            var controlPoint1 = new Point(startPoint.X + controlOffset, startPoint.Y);
            var controlPoint2 = new Point(endPoint.X - controlOffset, endPoint.Y);

            var bezierSegment = new BezierSegment
            {
                Point1 = controlPoint1,
                Point2 = controlPoint2,
                Point3 = endPoint
            };

            pathFigure.Segments.Add(bezierSegment);
            pathGeometry.Figures.Add(pathFigure);
            path.Data = pathGeometry;

            // 插入到第一个位置（在点的后面）
            Children.Insert(0, path);
            
            // 更新点的位置
            Canvas.SetLeft(_startPoint, startPoint.X - _startPoint.Width / 2);
            Canvas.SetTop(_startPoint, startPoint.Y - _startPoint.Height / 2);
            Canvas.SetLeft(_endPoint, endPoint.X - _endPoint.Width / 2);
            Canvas.SetTop(_endPoint, endPoint.Y - _endPoint.Height / 2);
        }
    }
}