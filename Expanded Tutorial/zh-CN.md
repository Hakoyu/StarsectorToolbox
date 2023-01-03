# Expanded Tutorial zh-CN

## 环境

工具: **[Visual Studio 2022](https://visualstudio.microsoft.com/zh-hans/vs/)**

工具包: **.NET 桌面开发** 和 **通用 Windows 平台开发**

环境: **[.NET6](https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0)**

## 创建项目

1. 新建项目: **WPF 类库**
   注意是选择 **C#** 项目而不是 **Visual Basic** 项目
2. 创建完成后 为项目添加一个页面(`Page`)
   原来的**Class1.cs**可以自行选择删除或保留
3. 为项目添加引用 **StarsectorTools.dll**
   你可以在 **[Releases](https://github.com/Hakoyu/StarsectorTools/releases)** 中下载到

## 设置拓展信息

在 Debug 目录下(通常位于`\bin\Debug\net6.0-windows`)
建立拓展信息文件 **Expansion.toml**
标准格式如下

```toml
# 拓展的ID
Id = "testId"
# 拓展显示的图标
Icon = "💥"
# 拓展的名称
Name = "testName"
# 拓展的作者
Author = "testAuthor"
# 拓展的版本
Version = "114.514"
# 拓展支持的工具箱版本
ToolsVersion = "1919.810"
# 拓展的描述
Description = "这是一个测试案例"
# 拓展的入口
ExpansionId = "WpfLibrary1.Page1"
# 拓展的入口文件
ExpansionFile = "WpfLibrary1.dll"
```

## 测试入口

在 **Page1** 中写入

```csharp
public Page1()
{
    InitializeComponent();
    STLog.WriteLine(GetType().ToString());
}
```

如果你无法使用 `using StarsectorTools.Utils;`
那可能是引用不正确

然后使用 StarsectorTools 的拓展调试功能定位拓展的路径即可载入
如果操作正确,此时 **StarsectorTools.log** 中会输出 `[Page1] INFO WpfLibrary1.Page1`

## 测试断点调试

在`STLog`处打上断点
在 VS2022**调试->附加到进程**中选择**StarsectorTools.exe**
也可以通过选择窗口来指定**StarsectorTools.exe**
完成后在 StarsectorTools 中右键拓展项,点击**刷新页面**
如果操作正确,此时会命中断点

与正常的拓展载入不同,调试拓展会将内容载入到内存
你可以对拓展进行修改与编译,完成后使用`刷新页面`即可

## 日志输出及弹窗使用规范

### 标准信息使用默认输出

```csharp
STLog.WriteLine(message);
ST.ShowMessageBox(message);
```

### 使用 `ifelse` 筛选的信息以 `Warn` 等级输出

```csharp
if(isTrue == true)
{
    ...
}
else
{
    STLog.WriteLine(message, STLogLevel.WARNING);
    ST.ShowMessageBox(message, MessageBoxImage.Warning);
}
```

### 使用 `trycatch` 捕获的信息以 `Error` 等级输出

```csharp
try
{
    ...
}
catch (Exception ex)
{
    STLog.WriteLine(message, ex);
    ST.ShowMessageBox(message, MessageBoxImage.Error);
}
```
