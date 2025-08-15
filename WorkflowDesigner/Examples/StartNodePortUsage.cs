using System;
using System.Windows;
using System.Windows.Media;
using WorkflowDesigner.UI.Views.Nodes;
using WorkflowDesigner.Nodes;
using WorkflowDesigner.UI.Utilities;

namespace WorkflowDesigner.Examples
{
    /// <summary>
    /// StartNodeView端口功能使用示例
    /// </summary>
    public class StartNodePortUsage
    {
        /// <summary>
        /// 示例：创建StartNodeView并设置端口高亮
        /// </summary>
        public static void ExamplePortHighlight()
        {
            try
            {
                // 创建StartNodeView
                var startNodeView = new StartNodeView();
                
                // 设置DataContext（通常由ViewModel提供）
                var startNodeViewModel = new StartNodeViewModel();
                startNodeView.ViewModel = startNodeViewModel;
                
                // 设置端口高亮状态
                startNodeView.SetPortHighlight(true, true); // 高亮，有效连接
                
                // 监听端口连接事件
                startNodeView.PortConnectionStarted += OnPortConnectionStarted;
                
                Console.WriteLine("StartNodeView端口功能示例已设置");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置StartNodeView端口功能失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 示例：获取端口位置
        /// </summary>
        public static void ExampleGetPortPosition()
        {
            try
            {
                var startNodeView = new StartNodeView();
                var startNodeViewModel = new StartNodeViewModel();
                startNodeView.ViewModel = startNodeViewModel;
                
                // 获取输出端口位置
                var portPosition = startNodeView.GetOutputPortPosition();
                Console.WriteLine($"输出端口位置: X={portPosition.X}, Y={portPosition.Y}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取端口位置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 示例：端口状态管理
        /// </summary>
        public static void ExamplePortStateManagement()
        {
            try
            {
                var startNodeView = new StartNodeView();
                var startNodeViewModel = new StartNodeViewModel();
                startNodeView.ViewModel = startNodeViewModel;
                
                // 模拟不同的端口状态
                Console.WriteLine("设置端口为高亮状态...");
                startNodeView.SetPortHighlight(true, true);
                
                System.Threading.Thread.Sleep(1000);
                
                Console.WriteLine("取消端口高亮...");
                startNodeView.SetPortHighlight(false);
                
                Console.WriteLine("端口状态管理示例完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"端口状态管理失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 示例：与PortConnectionHandler集成
        /// </summary>
        public static void ExamplePortConnectionHandlerIntegration()
        {
            try
            {
                // 创建StartNodeView
                var startNodeView = new StartNodeView();
                var startNodeViewModel = new StartNodeViewModel();
                startNodeView.ViewModel = startNodeViewModel;
                
                // 创建ConnectionManager（需要NetworkViewModel）
                // var networkViewModel = new NetworkViewModel();
                // var connectionManager = new ConnectionManager(networkViewModel);
                
                // 创建PortConnectionHandler（需要NetworkView）
                // var networkView = new NetworkView();
                // var portConnectionHandler = new PortConnectionHandler(networkView, networkViewModel, connectionManager);
                
                Console.WriteLine("PortConnectionHandler集成示例已设置");
                Console.WriteLine("注意：需要实际的NetworkView和NetworkViewModel才能完全测试");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PortConnectionHandler集成失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 端口连接事件处理器示例
        /// </summary>
        private static void OnPortConnectionStarted(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is StartNodeView startNodeView)
                {
                    Console.WriteLine("StartNodeView端口连接已开始");
                    
                    // 获取端口位置
                    var portPosition = startNodeView.GetOutputPortPosition();
                    Console.WriteLine($"连接开始位置: X={portPosition.X}, Y={portPosition.Y}");
                    
                    // 这里可以添加自定义的连接逻辑
                    // 例如：显示连接预览、高亮兼容的输入端口等
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理端口连接事件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 运行所有示例
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("=== StartNodeView端口功能使用示例 ===\n");
            
            ExamplePortHighlight();
            Console.WriteLine();
            
            ExampleGetPortPosition();
            Console.WriteLine();
            
            ExamplePortStateManagement();
            Console.WriteLine();
            
            ExamplePortConnectionHandlerIntegration();
            Console.WriteLine();
            
            Console.WriteLine("所有示例已完成");
        }
    }
}