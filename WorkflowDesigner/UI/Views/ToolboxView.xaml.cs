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
using WorkflowDesigner.UI.ViewModels;

namespace WorkflowDesigner.UI.Views
{
    /// <summary>
    /// ToolboxView.xaml 的交互逻辑
    /// </summary>
    public partial class ToolboxView : UserControl
    {
        public ToolboxView()
        {
            InitializeComponent();
        }

        private void ToolboxItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ToolboxItemViewModel toolboxItem)
            {
                // 开始拖拽操作
                var dragData = new DataObject(typeof(ToolboxItemViewModel), toolboxItem);
                DragDrop.DoDragDrop(this, dragData, DragDropEffects.Copy);
            }
        }
    }
}
