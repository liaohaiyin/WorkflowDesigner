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

namespace WorkflowDesigner.UI.Views.Nodes
{
    /// <summary>
    /// ApprovalNodeView.xaml 的交互逻辑
    /// </summary>
    public partial class ApprovalNodeView : DraggableNodeViewBase<ApprovalNodeViewModel>
    {
        public ApprovalNodeView()
        {
            InitializeComponent();
        }
        protected override Brush GetDefaultBorderBrush()
        {
            return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // 蓝色
        }
    }
}
