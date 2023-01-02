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

## Log 输出及弹窗使用规范

### 标准信息使用默认输出

```csharp
// 等价于STLog.Instance.WriteLine(message, STLogLevel.INFO);
STLog.Instance.WriteLine(message);
// 等价于ST.ShowMessageBox(message, MessageBoxImage.Information);
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
    ST.ShowMessageBox(message, STLogLevel.WARNING);
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
    STLog.Instance.WriteLine(message, ex);
    ST.ShowMessageBox(message, MessageBoxImage.Error);
}
```
