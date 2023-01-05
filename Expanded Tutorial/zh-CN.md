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

## 基础 API 一览

### [StarsectorTools.Libs.Utils](https://github.com/Hakoyu/StarsectorTools/blob/master/Libs/Utils.cs)


```csharp
/// <summary>模组信息</summary>
class ModInfo
```

```csharp
/// <summary>StarsectorTools日志等级</summary>
enum STLogLevel
```

```csharp
/// <summary>StarsectorTools日志</summary>
class STLog
{
    /// <summary>日志目录</summary>
    const string logFile
    /// <summary>
    /// 字符串转换成日志等级
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>日志等级</returns>
    STLogLevel Str2STLogLevel(string str)
    /// <summary>
    /// 写入日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="logLevel">日志等级</param>
    void WriteLine(string message, STLogLevel logLevel = STLogLevel.INFO)
    /// <summary>
    /// 写入日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="logLevel">日志等级</param>
    /// <param name="keys">插入的对象</param>
    void WriteLine(string message, STLogLevel logLevel = STLogLevel.INFO, params object[] args)
    /// <summary>
    /// 写入捕获的异常
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="ex">错误</param>
    /// <param name="args">插入的对象</param>
    void WriteLine(string message, Exception ex, params object[] args)
     /// <summary>
    /// Exception解析 用来精简异常的堆栈输出
    /// </summary>
    /// <param name="ex">Exception</param>
    /// <returns></returns>
    string ExceptionParse(Exception ex)
}
```

```csharp
/// <summary>StarsectorTools全局工具</summary>
class ST
{
    /// <summary>游戏目录</summary>
    string gameDirectory
    /// <summary>游戏exe文件路径</summary>
    string gameExeFile
    /// <summary>游戏模组文件夹目录</summary>
    string gameModsDirectory
    /// <summary>游戏版本</summary>
    string gameVersion
    /// <summary>游戏保存文件夹目录</summary>
    string gameSaveDirectory
    /// <summary>游戏已启用模组文件目录</summary>
    string enabledModsJsonFile
    /// <summary>游戏日志文件</summary>
    string gameLogFile
    /// <summary>
    /// 格式化Json数据,去除掉注释以及不合规的逗号
    /// </summary>
    /// <param name="jsonData">Json数据</param>
    /// <returns>格式化后的数据</returns>
    string JsonParse(string jsonData)
    /// <summary>
    /// 复制文件夹至目标文件夹
    /// </summary>
    /// <param name="sourceDirectoryName">原始路径</param>
    /// <param name="destinationDirectoryName">目标路径</param>
    /// <returns>复制成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool CopyDirectory(string sourceDirectoryName, string destinationDirectoryName)
    /// <summary>
    /// 删除文件至回收站
    /// </summary>
    /// <param name="file"></param>
    /// <returns>删除成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool DeleteFileToRecycleBin(string file)
    /// <summary>
    /// 删除文件夹至回收站
    /// </summary>
    /// <param name="directory">文件夹</param>
    /// <returns>删除成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool DeleteDirToRecycleBin(string directory)
    /// <summary>
    /// 使用系统默认打开方式打开链接,文件或文件夹
    /// </summary>
    /// <param name="link">链接</param>
    /// <returns>打开成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool OpenLink(string link)
    /// <summary>
    /// <para>压缩文件夹至Zip文件并输出到目录</para>
    /// <para>若不输入压缩文件名,则以原始目录的文件夹名称来命名</para>
    /// </summary>
    /// <param name="sourceDirectoryName">原始目录</param>
    /// <param name="destinationDirectoryName">输出目录</param>
    /// <param name="archiveName">压缩文件名</param>
    /// <returns>压缩成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool ArchiveDirToDir(string sourceDirectoryName, string destinationDirectoryName, string? archiveName = null)
    /// <summary>
    /// <para>解压压缩文件至目录</para>
    /// <para>支持: <see langword="Zip"/> <see langword="Rar"/> <see langword="7z"/></para>
    /// </summary>
    /// <param name="sourceFileName">原始文件</param>
    /// <param name="destinationDirectoryName">输出目录</param>
    /// <returns>解压成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool UnArchiveFileToDir(string sourceFileName, string destinationDirectoryName)
    /// <summary>
    /// 弹出消息窗口
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="image">显示的图标</param>
    /// <returns>按钮结果: <see cref="MessageBoxResult"/></returns>
    MessageBoxResult ShowMessageBox(string message, MessageBoxImage image = MessageBoxImage.Information, bool setBlurEffect = true)
    /// <summary>
    /// 弹出消息窗口
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="button">显示的按钮</param>
    /// <param name="image">显示的图标</param>
    /// <returns>按钮结果: <see cref="MessageBoxResult"/></returns>
    MessageBoxResult ShowMessageBox(string message, MessageBoxButton button, MessageBoxImage image, bool setBlurEffect = true)
}

```

### [StarsectorTools.Tools.ModManager](https://github.com/Hakoyu/StarsectorTools/blob/master/Tools/ModManager/ModManager.cs)

```csharp
class ModManager
{
    /// <summary>
    /// <para>全部模组信息</para>
    /// <para><see langword="Key"/>: 模组ID</para>
    /// <para><see langword="Value"/>: 模组信息</para>
    /// </summary>
    Dictionary<string, ModInfo> AllModsInfo
    /// <summary>已启用的模组ID</summary>
    HashSet<string> AllEnabledModsId
    /// <summary>已收藏的模组ID</summary>
    HashSet<string> AllCollectedModsId
    /// <summary>
    /// <para>全部用户分组</para>
    /// <para><see langword="Key"/>: 分组名称</para>
    /// <para><see langword="Value"/>: 包含的模组</para>
    /// </summary>
    Dictionary<string, HashSet<string>> AllUserGroups
}
```

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
