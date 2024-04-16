### ProtocolGeneratorWindow

```
描述：
	该类用于绘制ProtocolGenerator窗口。
```

------

##### ShowWindow()

`private static void ShowWindow()`

```
描述：
	该静态方法使用MenuItem特性修饰，Unity顶部菜单点击Protocol Generator按钮时，调用该方法弹出窗口。
```

------

##### SetFileLayout()

`private void SetFileLayout()`

```
描述：
	按顺序绘制窗口顶部工具栏，proto编译器选择栏，协议文件、批处理文件、协议脚本导出路径选择栏。
```

------

##### SetProtocolInfoLayout()

`private void SetProtocolInfoLayout()`

```
描述：
	按顺序绘制协议文件名输入框，命名空间输入框，依赖文件添加按钮，依赖文件名及其移除按钮。
```

------

##### SetAddMessageButton()

`private void SetAddMessageButton()`

```
描述：
	绘制添加message的按钮。
```

------

##### CheckError()

`private bool CheckError()`

```
描述：
	检查错误。
```

------

##### ShowError(Rect)

`private void ShowError(Rect rect)`

```
描述：
	在指定区域展示错误提示。
参数：
	rect：绘制错误提示的矩形区域。
```

------

##### SetButtonGroupLayout()

`private void SetButtonGroupLayout()`

```
描述：
	绘制按钮组，包括GitHub链接按钮、Clear按钮、Preview按钮、Generate按钮。
```

------

##### Clear()

`private void Clear()`

```
描述：
	按下Clear按钮调用该方法，清除数据。
```

------

##### CreateHeader()

`private MultiColumnHeader CreateHeader()`

```
描述：
	实例化一个MultiColumnHeader
返回值：
	返回一个用于创建具有多列头部的表格控件。
```



### ProtocolDataView

```
描述：
	该类继承TreeView抽象类，用于绘制多列层级列表。
```

------

##### ProtocolDataView(TreeViewState, MultiColumnHeader)

`public ProtocolDataView(TreeViewState state, MultiColumnHeader header) : base(state, header)`

```
描述：
	构造函数，负责初始化列表并设置一些样式参数。
参数：
	state：TeeeView树的状态管理器，负责跟踪TreeView的状态信息。
	header：用于创建多列头部的控件。
```

------

##### BuildRoot()

`protected override TreeViewItem BuildRoot()`

```
描述：
	继承抽象类TreeView必须实现的抽象方法，默认情况下该方法构建整个列表树并返回根项，在重写了BuildRows方法时该方法仅返回树的根项。
返回值：
	返回列表的根项。
```

------

##### BuildRows(TreeViewItem)

`protected override IList<TreeViewItem> BuildRows(TreeViewItem root)`

```
描述：
	重写此方法手动构建除了根项的其他项，当调用Reload方法以及每次展开或折叠项时，将调用此方法。
参数：
	root：BuildRoot方法返回的根项。
返回值：
	构建后的所有项的列表。
```

------

##### BuildItemList(TreeViewItem, TreeViewItem)

`private void BuildItemList(TreeViewItem parent, TreeViewItem parentCopy = null)`

```
描述：
	递归构建列表树，将除了根项的所有项复制并放入列表中，其中需要绘制的项设置层级关系，不需要绘制的项（未展开的项）使用CreateChildListForCollapsedParent方法创建虚拟列表占位。
参数：
	parent：父项。
	parentCopy：父项在列表中的拷贝，该参数为null则表示parent为根项。
```

------

##### RowGUI(RowGUIArgs)

`protected override void RowGUI(RowGUIArgs args)`

```
描述：
	重写TreeView抽象类的该方法可以在列表项中自定义GUI内容，
	但是不能使用自动布局API，如GUILayout和EditorGUILayout，只能使用GUI和EditorGUI。
参数：
	args：项的数据。
```

------

##### CreateGUI(Rect, int, TreeViewItem)

`private void CreateGUI(Rect rect, int columnIndex, TreeViewItem item)`

```
描述：
	在RowGUI方法中调用，为当前项的可见列自定义GUI类型。
参数：
	rect：该项在该列所占的矩形区域。
	columnIndex：当前可见列的索引。
	item：当前项。
```

------

##### IsMessageType(int)

`private bool IsMessageType(int id)`

```
描述：
	在CreateGUI方法中调用，用于判断当前项是否为message类型。
参数：
	id：当前项的id。
返回值：
	是message类型返回true，反之返回false。
```

------

##### AddMessage()

`public void AddMessage()`

```
描述：
	点击Add Message按钮时调用，向列表中添加message项。
```

------

##### RemoveMessage(int)

`private void RemoveMessage(int id)`

```
描述：
	从列表中移除指定message项。
参数：
	id：需要移除的项的id。
```

------

##### AddField(int)

`private void AddField(int parentID)`

```
描述：
	点击message项的添加按钮时调用，向指定message项中添加一个filed子项。
参数：
	parentID：需要添加子项的message项的id。
```

------

##### RemoveField(int, int)

`private void RemoveField(int id, int parentID)`

```
描述：
	点击field项的移除按钮时调用，从指定的message项中移除指定的file项。
参数：
	id：需要移除的filed项的id。
	parentID：父项message项的id。
```

------

##### Clear()

`public void Clear()`

```
描述：
	清除所有列表项。
```

------

##### GetProtocolData()

`public List<MessageData> GetProtocolData()`

```
描述：
	将所有项中的数据收集起来。
返回值：
	数据列表。
```



### ProtocolHelper

```
描述：
	生成文件的工具类，静态类。
```

------

##### GenerateProtocolScript(string, HashSet\<string\>, List\<MessageData\>, string, string, string)

`public static void GenerateProtocolScript(string package, HashSet<string> depFileSet, List<MessageData> messageDataList, string protoFileName, string exePath, string exportPath)`

```
描述：
	当集中设置文件导出路径时调用该方法，按顺序调用GenerateProtocolText、GenerateProtoFile、GenerateBatchFile、GenerateSciptFile方法，先生成协议文本，再将文本写入协议文件，在生成批处理文件，最后运行批处理文件生成脚本。
参数：
	package：命名空间。
	depFileSet：依赖文件的路径集合。
	messageDataList：协议中定义的message包括其字段的数据列表。
	protoFileName：协议文件名称。
	exePath：protobuf的协议编译器的路径。
	exportPath：所有生成文件的导出路径。
```

------

##### GenerateProtocolScript(string, HashSet\<string\>, List\<MessageData\>, string, string, string[])

`public static void GenerateProtocolScript(string package, HashSet<string> depFileSet, List<MessageData> messageDataList, string protoFileName, string exePath, string[] exportPaths)`

```
描述：
	当分别设置文件导出路径时调用该方法，重载。
参数：
	package：命名空间。
	depFileSet：依赖文件的路径集合。
	messageDataList：协议中定义的message包括其字段的数据列表。
	protoFileName：协议文件名称。
	exePath：protobuf的协议编译器的路径。
	exportPaths：生成的文件的导出路径数组，按顺序分别为协议文件、批处理文件、脚本文件。
```

------

##### GenerateProtocolText(string, HashSet\<string\>, List\<MessageData\>)

`public static string GenerateProtocolText(string package, HashSet<string> depFileSet, List<MessageData> messageDataList)`

```
描述：
	根据填写的数据生成协议文件的文本。
参数：
	package：命名空间。
	depFileSet：依赖文件的路径集合。
	messageDataList：协议中定义的message包括其字段的数据列表。
返回值：
	生成的协议文件的文本。
```

------

##### AppendDepFile(StringBuilder, HashSet\<string\>)

`private static void AppendDepFile(StringBuilder protoFileText, HashSet<string> depFileSet)`

```
描述：
	在GenerateProtocolText方法中调用，分割协议文件路径为文件目录及文件名称两部分，将文件名称部分加入协议文本中。
	如：D:\Protobuf\Proto\Test1.proto分割为D:\Protobuf\Proto及Test1.proto
参数：
	protoFileText：协议文本字符串。
	depFileSet：依赖文件的路径集合。
```

------

##### GetDepFileRootPath(HashSet\<string\>)

`private static HashSet<string> GetDepFileRootPath(HashSet<string> depFileSet)`

```
描述：
	分割协议文件路径为文件目录及文件名称两部分，返回文件目录。
参数：
	depFileSet：依赖文件的路径集合。
返回值：
	所有依赖文件的目录的集合。
```

------

##### GenerateProtoFile(string, string, string)

`private static void GenerateProtoFile(string protoExportPath, string protoFileName, string protoFileText)`

```
描述：
	将协议文本写入文件中。
参数：
	protoExportPath：协议文件导出目录。
	protoFileName：协议文件名称。
	protoFileText：待写入的文本。
```

------

##### GenerateBatchFile(string, string, string, HashSet\<string\>)

`private static string GenerateBatchFile(string exportPath, string protoFileName, string exePath, HashSet<string> depFileSet)`

```
描述：
	当集中设置文件导出路径时调用该方法，生成批处理文件。
参数：
	exportPath：所有文件的导出目录。
	protoFileName：协议文件名称。
	exePath：协议编译器文件路径。
	depFileSet：依赖文件的路径集合。
返回值：
	批处理文件路径。
```

------

##### GenerateBatchFile(string, string, string, string, string, HashSet\<string\>)

`private static string GenerateBatchFile(string batExportPath, string protoFileName, string exePath, string protoExportPath, string scriptExportPath, HashSet<string> depFileSet)`

```
描述：
	当分别设置文件导出路径时调用该方法，生成批处理文件，重载。
参数：
	batExportPath：批处理文件导出目录。
	protoFileName：协议文件名称。
	exePath：协议编译器导出目录。
	protoExportPath：协议文件导出目录。
	scriptExportPath：脚本文件导出目录。
	depFileSet：依赖文件的路径集合。
返回值：
	批处理文件路径。
```

------

##### GenerateSciptFile(string)

`private static void GenerateSciptFile(string batchFilePath)`

```
描述：
	运行批处理文件生成脚本文件。
参数：
	batchFilePath：批处理文件路径。
```



### PreviewWindow

```
描述：
	该类用于绘制协议文本预览窗口。
```

------

##### ShowWindow(string)

`public static void ShowWindow(string text)`

```
描述：
	点击Preview按钮时，调用该方法弹出协议文本预览窗口。
参数：
	text：协议文件文本。
```

