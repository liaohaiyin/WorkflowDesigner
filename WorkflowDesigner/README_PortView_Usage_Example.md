# PortView连接点功能使用示例

## 基本使用

### 1. 创建工作流

```csharp
// 创建网络视图模型
var network = new NetworkViewModel();

// 创建开始节点
var startNode = new StartNodeViewModel
{
    NodeName = "工作流开始",
    Position = new Point(100, 100)
};

// 创建任务节点
var taskNode = new TaskNodeViewModel
{
    NodeName = "执行任务",
    Position = new Point(300, 100),
    TaskName = "数据处理",
    TaskType = "Auto"
};

// 创建判断节点
var decisionNode = new DecisionNodeViewModel
{
    NodeName = "结果判断",
    Position = new Point(500, 100),
    ConditionExpression = "result > 0"
};

// 创建结束节点
var endNode = new EndNodeViewModel
{
    NodeName = "工作流结束",
    Position = new Point(700, 100)
};

// 添加节点到网络
network.Nodes.Add(startNode);
network.Nodes.Add(taskNode);
network.Nodes.Add(decisionNode);
network.Nodes.Add(endNode);
```

### 2. 端口连接

```csharp
// 创建连接管理器
var connectionManager = new ConnectionManager(network);

// 连接开始节点到任务节点
var startToTask = connectionManager.CreateConnection(startNode, taskNode);
if (startToTask)
{
    Console.WriteLine("开始节点 -> 任务节点 连接成功");
}

// 连接任务节点到判断节点
var taskToDecision = connectionManager.CreateConnection(taskNode, decisionNode);
if (taskToDecision)
{
    Console.WriteLine("任务节点 -> 判断节点 连接成功");
}

// 连接判断节点到结束节点（通过"是"输出端口）
var decisionToEnd = connectionManager.CreatePortConnection(
    decisionNode.Outputs[0], // "是"输出端口
    endNode.Inputs[0]        // 输入端口
);
if (decisionToEnd)
{
    Console.WriteLine("判断节点 -> 结束节点 连接成功");
}
```

### 3. 端口验证

```csharp
// 检查端口连接是否有效
bool isValid = connectionManager.IsValidPortConnection(
    startNode.Outputs[0],    // 开始节点的输出端口
    taskNode.Inputs[0]       // 任务节点的输入端口
);

if (isValid)
{
    Console.WriteLine("端口连接有效");
}
else
{
    Console.WriteLine("端口连接无效");
}

// 检查节点连接是否有效
bool isNodeConnectionValid = connectionManager.IsValidConnection(startNode, taskNode);
if (isNodeConnectionValid)
{
    Console.WriteLine("节点连接有效");
}
```

## 高级功能

### 1. 动态端口管理

```csharp
// 动态添加输入端口
var newInputPort = new ValueNodeInputViewModel<object> { Name = "额外输入" };
taskNode.Inputs.Add(newInputPort);

// 动态添加输出端口
var newOutputPort = new ValueNodeOutputViewModel<object> { Name = "额外输出" };
taskNode.Outputs.Add(newOutputPort);

// 重新验证连接
connectionManager.ValidateAllConnections();
```

### 2. 端口类型检查

```csharp
// 创建类型化的端口
var stringInputPort = new ValueNodeInputViewModel<string> { Name = "字符串输入" };
var stringOutputPort = new ValueNodeOutputViewModel<string> { Name = "字符串输出" };

// 类型化端口只能连接相同类型的端口
bool typeCompatible = connectionManager.IsValidPortConnection(stringOutputPort, stringInputPort);
```

### 3. 连接事件处理

```csharp
// 监听连接创建事件
connectionManager.ConnectionCreated += (sender, e) =>
{
    Console.WriteLine($"创建连接: {e.SourceNode.NodeName} -> {e.TargetNode.NodeName}");
};

// 监听连接删除事件
connectionManager.ConnectionRemoved += (sender, e) =>
{
    Console.WriteLine($"删除连接: {e.SourceNode.NodeName} -> {e.TargetNode.NodeName}");
};

// 监听端口连接事件
connectionManager.PortConnectionCreated += (sender, e) =>
{
    Console.WriteLine($"创建端口连接: {e.SourcePort.Name} -> {e.TargetPort.Name}");
};
```

## 错误处理

### 1. 连接验证错误

```csharp
try
{
    // 尝试创建无效连接
    var invalidConnection = connectionManager.CreateConnection(startNode, startNode);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"连接创建失败: {ex.Message}");
}
```

### 2. 端口类型不匹配

```csharp
try
{
    // 尝试连接不同类型的端口
    var stringPort = new ValueNodeInputViewModel<string> { Name = "字符串" };
    var intPort = new ValueNodeOutputViewModel<int> { Name = "整数" };
    
    var connection = connectionManager.CreatePortConnection(intPort, stringPort);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"端口类型不匹配: {ex.Message}");
}
```

### 3. 循环依赖检测

```csharp
try
{
    // 尝试创建循环依赖
    var connection1 = connectionManager.CreateConnection(startNode, taskNode);
    var connection2 = connectionManager.CreateConnection(taskNode, startNode);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"检测到循环依赖: {ex.Message}");
}
```

## 性能优化

### 1. 批量操作

```csharp
// 批量创建连接
var connections = new List<(WorkflowNodeViewModel, WorkflowNodeViewModel)>
{
    (startNode, taskNode),
    (taskNode, decisionNode),
    (decisionNode, endNode)
};

// 使用批量操作提高性能
connectionManager.CreateConnections(connections);
```

### 2. 延迟验证

```csharp
// 禁用自动验证以提高性能
connectionManager.AutoValidate = false;

// 批量创建连接
foreach (var connection in connections)
{
    connectionManager.CreateConnection(connection.Item1, connection.Item2);
}

// 手动验证所有连接
connectionManager.ValidateAllConnections();
```

### 3. 缓存优化

```csharp
// 启用连接缓存
connectionManager.EnableCaching = true;

// 预计算端口位置
connectionManager.PrecomputePortPositions();

// 清理缓存
connectionManager.ClearCache();
```

## 自定义扩展

### 1. 自定义端口样式

```xml
<!-- 自定义端口样式 -->
<Style TargetType="nodenetwork:NodeInputView">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="nodenetwork:NodeInputView">
                <Ellipse Fill="LightBlue" 
                         Stroke="Blue" 
                         StrokeThickness="2"
                         Width="20" Height="20"/>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### 2. 自定义连接验证规则

```csharp
// 继承ConnectionManager并重写验证方法
public class CustomConnectionManager : ConnectionManager
{
    protected override bool ArePortTypesCompatible(NodeOutputViewModel output, NodeInputViewModel input)
    {
        // 自定义类型兼容性检查
        if (output is ValueNodeOutputViewModel<string> && input is ValueNodeInputViewModel<string>)
        {
            return true;
        }
        
        if (output is ValueNodeOutputViewModel<int> && input is ValueNodeInputViewModel<int>)
        {
            return true;
        }
        
        return false;
    }
}
```

### 3. 自定义端口行为

```csharp
// 创建自定义端口视图模型
public class CustomPortViewModel : ValueNodeInputViewModel<object>
{
    public CustomPortViewModel()
    {
        // 自定义端口行为
        this.WhenAnyValue(x => x.IsConnected)
            .Subscribe(isConnected =>
            {
                if (isConnected)
                {
                    Console.WriteLine("端口已连接");
                }
                else
                {
                    Console.WriteLine("端口已断开");
                }
            });
    }
}
```

## 测试建议

### 1. 单元测试

```csharp
[Test]
public void TestPortConnection()
{
    // 创建测试节点
    var startNode = new StartNodeViewModel();
    var endNode = new EndNodeViewModel();
    
    // 测试端口连接
    var connectionManager = new ConnectionManager(new NetworkViewModel());
    var result = connectionManager.CreatePortConnection(
        startNode.Outputs[0], 
        endNode.Inputs[0]
    );
    
    Assert.IsTrue(result);
}
```

### 2. 集成测试

```csharp
[Test]
public void TestWorkflowExecution()
{
    // 创建完整工作流
    var workflow = CreateTestWorkflow();
    
    // 验证所有连接
    var connectionManager = new ConnectionManager(workflow.Network);
    var isValid = connectionManager.ValidateAllConnections();
    
    Assert.IsTrue(isValid);
    
    // 执行工作流
    var result = workflow.Execute();
    Assert.IsTrue(result.Success);
}
```

### 3. 性能测试

```csharp
[Test]
public void TestPerformance()
{
    var stopwatch = Stopwatch.StartNew();
    
    // 创建大量节点和连接
    var network = CreateLargeNetwork(1000);
    var connectionManager = new ConnectionManager(network);
    
    stopwatch.Stop();
    Console.WriteLine($"创建1000个节点耗时: {stopwatch.ElapsedMilliseconds}ms");
    
    Assert.Less(stopwatch.ElapsedMilliseconds, 5000); // 5秒内完成
}
```

## 总结

通过使用PortView连接点功能，你可以：

1. **轻松创建工作流**：通过拖拽连接节点
2. **管理复杂连接**：支持多输入/多输出端口
3. **验证连接有效性**：自动检测错误和循环依赖
4. **扩展功能**：支持自定义端口样式和行为
5. **优化性能**：批量操作和缓存机制

这些功能为工作流设计器提供了强大而灵活的节点连接能力。