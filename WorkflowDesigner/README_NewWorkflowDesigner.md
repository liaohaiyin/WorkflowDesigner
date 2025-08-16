# 新工作流设计器 (NewWorkflowDesigner)

## 概述

新的工作流设计器基于 `NodeNetwork.Toolkit.NodeList` 和 `NodeNetwork` 实现，替代了原有的 AvalonDock 架构，提供了更现代化的节点编辑体验。

## 主要特性

### 1. 左侧节点工具箱
- **NodeList 集成**：使用 `NodeNetwork.Toolkit.NodeList` 显示可用节点类型
- **分类显示**：节点按类别分组（流程控制、业务处理、审批流程、消息通知）
- **图标支持**：每个节点类型都有独特的图标和颜色标识
- **拖拽创建**：从工具箱拖拽节点到设计器画布创建新节点

### 2. 节点类型支持
- **开始节点** (StartNode)：工作流入口点，绿色三角形图标
- **任务节点** (TaskNode)：执行具体业务任务，紫色齿轮图标
- **判断节点** (DecisionNode)：条件分支判断，橙色问号图标
- **审批节点** (ApprovalNode)：多人审批流程，蓝色对勾图标
- **通知节点** (NotificationNode)：消息推送通知，青色邮件图标
- **结束节点** (EndNode)：工作流终止点，红色方块图标

### 3. 交互功能
- **节点选择**：单击选择单个节点，Ctrl+单击多选
- **选择框**：拖拽创建选择框，选择框内的节点自动选中
- **节点移动**：拖拽节点到新位置，支持网格对齐
- **键盘快捷键**：
  - `Delete`：删除选中的节点
  - `Ctrl+A`：选择所有节点
  - `Escape`：清除选择

### 4. 视图控制
- **缩放控制**：0.1x 到 3.0x 缩放，支持适合窗口、实际大小、居中显示
- **网格显示**：可切换网格背景显示，支持网格对齐
- **自动布局**：一键自动排列所有节点，避免重叠

### 5. PortView 支持
- **端口连接**：完整的输入/输出端口支持
- **拖拽连线**：从输出端口拖拽到输入端口创建连接
- **连接预览**：拖拽过程中显示连接预览线
- **端口高亮**：兼容的端口在拖拽过程中高亮显示

## 技术架构

### 核心组件

#### NewWorkflowDesignerView.xaml
```xml
<!-- 左侧节点工具箱 -->
<toolkit:NodeList x:Name="nodeList" 
                 Network="{Binding Network}"
                 NodeFactory="{Binding NodeFactory}"
                 ItemsSource="{Binding NodeFactory.AvailableNodes}">
    <toolkit:NodeList.ItemTemplate>
        <DataTemplate>
            <!-- 自定义节点项模板 -->
        </DataTemplate>
    </toolkit:NodeList.ItemTemplate>
</toolkit:NodeList>

<!-- 右侧设计器主区域 -->
<nodenetwork:NetworkView x:Name="networkView" 
                       ViewModel="{Binding Network}" />
```

#### NewWorkflowDesignerView.xaml.cs
```csharp
public partial class NewWorkflowDesignerView : UserControl
{
    // 选择相关状态
    private Point _selectionStartPoint;
    private bool _isSelecting;
    private bool _isDragging;
    
    // 事件处理
    private void NetworkView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    private void NetworkView_MouseMove(object sender, MouseEventArgs e)
    private void NetworkView_KeyDown(object sender, KeyEventArgs e)
    
    // 节点操作
    private void HandleNodeDragging(Point position)
    private void ProcessSelectionBox()
    private void DeleteSelectedNodes()
}
```

#### NodeFactory.cs
```csharp
public class NodeFactory : INodeFactory
{
    // 节点创建器字典
    private readonly Dictionary<string, Func<NodeViewModel>> _nodeCreators;
    
    // 获取可用节点列表
    public IEnumerable<NodeFactoryItem> GetAvailableNodes()
    
    // 创建指定类型节点
    public NodeViewModel CreateNode(string nodeType)
    public NodeViewModel CreateNode(string nodeType, Point position)
}
```

### 数据绑定

#### ViewModel 集成
```csharp
public class WorkflowDesignerViewModel : ReactiveObject
{
    // 节点工厂属性
    public NodeFactory NodeFactory { get; set; }
    
    // 网络视图模型
    public NetworkViewModel Network { get; set; }
}
```

#### 节点工厂项
```csharp
public class NodeFactoryItem
{
    public string Name { get; set; }        // 节点名称
    public string Category { get; set; }    // 节点类别
    public string Description { get; set; } // 节点描述
    public string Icon { get; set; }        // 节点图标
    public string IconColor { get; set; }   // 图标颜色
    public string NodeType { get; set; }    // 节点类型标识
}
```

## 使用方法

### 1. 创建新节点
1. 在左侧工具箱中找到需要的节点类型
2. 拖拽节点到右侧设计器画布
3. 节点自动创建并放置在拖拽位置

### 2. 选择节点
- **单选**：单击节点
- **多选**：Ctrl+单击多个节点
- **框选**：拖拽创建选择框，选择框内的节点自动选中

### 3. 移动节点
1. 选中要移动的节点
2. 拖拽节点到新位置
3. 启用网格对齐时，节点会自动对齐到网格

### 4. 创建连接
1. 从输出端口开始拖拽
2. 拖拽过程中显示预览线
3. 拖拽到目标输入端口释放
4. 自动创建连接

### 5. 删除节点
1. 选中要删除的节点
2. 按 `Delete` 键或使用工具栏按钮
3. 节点及其连接自动删除

## 与原有设计的区别

### 架构变化
- **从 AvalonDock 到 NodeNetwork.Toolkit**：更现代的节点网络架构
- **集成式设计**：工具箱和设计器在同一视图中，操作更流畅
- **标准化接口**：使用标准的 INodeFactory 接口

### 功能增强
- **更好的选择体验**：支持框选、多选等现代交互
- **增强的拖拽**：更流畅的节点创建和移动
- **完整的键盘支持**：标准快捷键操作
- **实时反馈**：状态栏实时显示操作信息

### 性能优化
- **事件处理优化**：更高效的鼠标和键盘事件处理
- **内存管理**：更好的资源清理和内存管理
- **渲染优化**：优化的节点渲染和更新机制

## 扩展开发

### 添加新节点类型
```csharp
public class CustomNodeFactory : NodeFactory
{
    public CustomNodeFactory()
    {
        _nodeCreators["CustomNode"] = () => new CustomNodeViewModel();
    }
    
    public override IEnumerable<NodeFactoryItem> GetAvailableNodes()
    {
        var baseNodes = base.GetAvailableNodes().ToList();
        baseNodes.Add(new NodeFactoryItem
        {
            Name = "自定义节点",
            Category = "自定义",
            Description = "自定义节点描述",
            Icon = "🔧",
            IconColor = "#FF5722",
            NodeType = "CustomNode"
        });
        return baseNodes;
    }
}
```

### 自定义节点项模板
```xml
<toolkit:NodeList.ItemTemplate>
    <DataTemplate>
        <Border Style="{StaticResource CustomNodeItemStyle}">
            <!-- 自定义节点项内容 -->
            <StackPanel>
                <TextBlock Text="{Binding Icon}" FontSize="24"/>
                <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                <TextBlock Text="{Binding Description}" TextWrapping="Wrap"/>
            </StackPanel>
        </Border>
    </DataTemplate>
</toolkit:NodeList.ItemTemplate>
```

### 自定义交互行为
```csharp
// 重写鼠标事件处理
protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
{
    // 自定义左键按下行为
    base.OnMouseLeftButtonDown(e);
}

// 添加自定义快捷键
protected override void OnKeyDown(KeyEventArgs e)
{
    if (e.Key == Key.F5)
    {
        // 自定义F5快捷键行为
        e.Handled = true;
    }
    base.OnKeyDown(e);
}
```

## 配置说明

### 节点位置设置
```csharp
// 设置节点默认位置范围
node.Position = new Point(
    random.Next(100, 500),  // X坐标范围
    random.Next(100, 400)   // Y坐标范围
);
```

### 网格对齐设置
```csharp
// 网格大小设置
if (SnapToGridCheckBox.IsChecked == true)
{
    newX = Math.Round(newX / 20) * 20;  // 20像素网格
    newY = Math.Round(newY / 20) * 20;
}
```

### 缩放范围设置
```xml
<Slider x:Name="ZoomSlider" 
        Minimum="0.1" Maximum="3.0" 
        Value="1.0" />
```

## 故障排除

### 常见问题

#### 1. 节点无法创建
- 检查 NodeFactory 是否正确初始化
- 验证节点类型是否在 _nodeCreators 字典中
- 查看异常日志

#### 2. 拖拽不响应
- 确认鼠标事件绑定正确
- 检查是否有其他控件拦截事件
- 验证拖拽状态变量设置

#### 3. 端口连接失败
- 检查 PortConnectionHandler 初始化
- 验证端口类型兼容性
- 查看连接管理器状态

### 调试技巧
```csharp
// 启用详细日志
Logger.SetLogLevel(LogLevel.Debug);

// 添加状态更新
UpdateStatusText($"操作: {operation}, 状态: {status}");

// 异常捕获
try
{
    // 操作代码
}
catch (Exception ex)
{
    UpdateStatusText($"操作失败: {ex.Message}");
    Logger.Error(ex, "操作执行失败");
}
```

## 总结

新的工作流设计器基于 NodeNetwork.Toolkit.NodeList 实现，提供了：

1. **现代化的用户界面**：左侧工具箱 + 右侧设计器的布局
2. **完整的交互支持**：选择、移动、连接、删除等操作
3. **灵活的扩展性**：支持自定义节点类型和交互行为
4. **优秀的性能**：优化的事件处理和渲染机制
5. **标准化的接口**：基于 INodeFactory 的节点创建机制

这个新设计器为工作流设计提供了更好的用户体验和更强的功能扩展性，是企业级工作流管理系统的理想选择。