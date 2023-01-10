# Expanded Tutorial zh-CN

## 环境

工具: **[Visual Studio 2022](https://visualstudio.microsoft.com/zh-hans/vs/)**

工具包: **.NET 桌面开发** 和 **通用 Windows 平台开发**

环境: **[.NET6](https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0)**

## 创建项目

[最小演示 Demo](https://github.com/Hakoyu/StarsectorTools/blob/master/Expanded%20Tutorial/WpfLibrary1.7z)
[基础演示 Demo]()

### 设置项目

1. 新建项目: **WPF 类库**
   注意是选择 **C#** 项目而不是 **Visual Basic** 项目
   ![](https://s2.loli.net/2023/01/09/rKRmBXGDM1UPp8T.png)
2. 创建完成后 为项目添加一个页面(`Page`)
   原来的**Class1.cs**可以自行选择删除或保留
   ![](https://s2.loli.net/2023/01/09/y4YUb2EQX9r1RGl.png)
3. 为项目添加引用 **StarsectorTools.dll**
   你可以在 **[Releases](https://github.com/Hakoyu/StarsectorTools/releases)** 中下载到

### 设置拓展信息

在 Debug 目录下(通常位于 **\bin\Debug\net6.0-windows**)
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

## 测试项目

用来测试项目是否能正确引用

### 测试入口

在 `Page1` 中写入

```csharp
public Page1()
{
    InitializeComponent();
    STLog.WriteLine(GetType().ToString());
}
```

如果你无法使用 `using StarsectorTools.Libs.Utils;`
那可能是引用不正确

然后使用 StarsectorTools 的拓展调试功能定位拓展的路径即可载入
![](https://s2.loli.net/2023/01/10/AMEHKxvF4ukg7On.png)
如果操作正确,此时 **StarsectorTools.log** 中会输出 `[Page1] INFO WpfLibrary1.Page1`

### 测试断点调试

在`STLog`处打上断点
在 VS2022 **调试->附加到进程** 中选择 **StarsectorTools.exe**
也可以通过选择窗口来指定 **StarsectorTools.exe**
![](https://s2.loli.net/2023/01/10/ypz32rQKxX6eu1S.png)
完成后在 StarsectorTools 中右键拓展项,点击 **刷新页面**
如果操作正确,此时会命中断点
![](https://s2.loli.net/2023/01/10/SgXsTzUmwOaW3tN.gif)
如果断点处显示:**无法命中断点,源代码与原始版本不同**
可能需要对拓展进行重新编译,或者检查引用的文件是否正确

与正常的拓展载入不同,调试拓展会将内容载入到内存
你可以对拓展进行修改与编译,完成后使用 **刷新页面** 即可
![](https://s2.loli.net/2023/01/10/zuNfrTocISq62JA.gif)

## 基础 API 一览

**StarsectorTools.dll** 所提供的 API

### [StarsectorTools.Libs.Utils](https://github.com/Hakoyu/StarsectorTools/blob/master/Libs/Utils)

存放一些全局可用的资源

#### [StarsectorTools.Libs.Utils.GameInfo](https://github.com/Hakoyu/StarsectorTools/blob/master/Libs/Utils/GameInfo.cs)

```csharp
/// <summary>游戏信息</summary>
class GameInfo
{
    /// <summary>游戏目录</summary>
    string GameDirectory
    /// <summary>游戏exe文件</summary>
    string ExeFile
    /// <summary>游戏模组文件夹</summary>
    string ModsDirectory
    /// <summary>游戏版本</summary>
    string Version
    /// <summary>游戏存档文件夹</summary>
    string SaveDirectory
    /// <summary>游戏已启用模组文件</summary>
    string EnabledModsJsonFile
    /// <summary>游戏日志文件</summary>
    string LogFile
}
```

#### [StarsectorTools.Libs.Utils.ModInfo](https://github.com/Hakoyu/StarsectorTools/blob/master/Libs/Utils/ModInfo.cs)

```csharp
/// <summary>模组信息</summary>
class ModInfo
{
    /// <summary>ID</summary>
    string Id
    /// <summary>名称</summary>
    string Name
    /// <summary>作者</summary>
    string Author
    /// <summary>版本</summary>
    string Version
    /// <summary>是否为功能性模组</summary>
    bool IsUtility
    /// <summary>描述</summary>
    string Description
    /// <summary>支持的游戏版本</summary>
    string GameVersion
    /// <summary>模组信息</summary>
    string ModPlugin
    /// <summary>前置</summary>
    HashSet<ModInfo>? Dependencies
    /// <summary>本地路径</summary>
    string Path
}
```

#### [StarsectorTools.Libs.Utils.STLog](https://github.com/Hakoyu/StarsectorTools/blob/master/Libs/Utils/STLog.cs)

```csharp
/// <summary>StarsectorTools日志等级</summary>
enum STLogLevel
{
    /// <summary>调试</summary>
    DEBUG
    /// <summary>提示</summary>
    INFO
    /// <summary>警告</summary>
    WARN
    /// <summary>错误</summary>
    ERROR
}
```

```csharp
/// <summary>StarsectorTools日志</summary>
class STLog
{
    /// <summary>日志目录</summary>
    string LogFile
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

#### [StarsectorTools.Libs.Utils.Utils](https://github.com/Hakoyu/StarsectorTools/blob/master/Libs/Utils/Utils.cs)

```csharp
/// <summary>StarsectorTools全局工具</summary>
class Utils
{
    /// <summary>
    /// 检测文件是否存在
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <param name="outputLog">输出日志</param>
    /// <returns>存在为<see langword="true"/>,不存在为<see langword="false"/></returns>
    bool FileExists(string file, bool outputLog = true)
    /// <summary>
    /// 检测文件夹是否存在
    /// </summary>
    /// <param name="directory">目录路径</param>
    /// <param name="outputLog">输出日志</param>
    /// <returns>存在为<see langword="true"/>,不存在为<see langword="false"/></returns>
    bool DirectoryExists(string directory, bool outputLog = true)
    /// <summary>
    /// 格式化Json数据,去除掉注释以及不合规的逗号
    /// </summary>
    /// <param name="jsonData">Json数据</param>
    /// <returns>格式化后的数据</returns>
    string JsonParse(string jsonData)
    /// <summary>
    /// 复制文件夹至目标文件夹
    /// </summary>
    /// <param name="sourceDirectory">原始路径</param>
    /// <param name="destinationDirectory">目标路径</param>
    /// <returns>复制成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool CopyDirectory(string sourceDirectory, string destinationDirectory)
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
    /// <param name="sourceDirectory">原始目录</param>
    /// <param name="destinationDirectory">输出目录</param>
    /// <param name="archiveName">压缩文件名</param>
    /// <returns>压缩成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool ArchiveDirToDir(string sourceDirectory, string destinationDirectory, string? archiveName = null)
    /// <summary>
    /// <para>解压压缩文件至目录</para>
    /// <para>支持: <see langword="Zip"/> <see langword="Rar"/> <see langword="7z"/></para>
    /// </summary>
    /// <param name="sourceFile">原始文件</param>
    /// <param name="destinationDirectory">输出目录</param>
    /// <returns>解压成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    bool UnArchiveFileToDir(string sourceFile, string destinationDirectory)
    /// <summary>
    /// 弹出消息窗口
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="image">显示的图标</param>
    /// <returns>按钮结果: <see cref="MessageBoxResult"/></returns>
    MessageBoxResult ShowMessageBox(string message, IMessageBoxIcon.icon = IMessageBoxIcon.Info, bool setBlurEffect = true)
    /// <summary>
    /// 弹出消息窗口
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="button">显示的按钮</param>
    /// <param name="image">显示的图标</param>
    /// <returns>按钮结果: <see cref="MessageBoxResult"/></returns>
    MessageBoxResult ShowMessageBox(string message, MessageBoxButton button, IMessageBoxIcon.icon, bool setBlurEffect = true)
}
```

### [StarsectorTools.Libs.Utils.ModInfo](https://github.com/Hakoyu/StarsectorTools/blob/master/Libs/Utils/ModInfo.cs)

```csharp
class ModsInfo
{
    /// <summary>
    /// <para>全部模组信息</para>
    /// <para><see langword="Key"/>: 模组ID</para>
    /// <para><see langword="Value"/>: 模组信息</para>
    /// </summary>
    ReadOnlyDictionary<string, ModInfo> AllModsInfo
    /// <summary>已启用的模组ID</summary>
    ExternalReadOnlySet<string> AllEnabledModsId
    /// <summary>已收藏的模组ID</summary>
    ExternalReadOnlySet<string> AllCollectedModsId
    /// <summary>
    /// <para>全部用户分组</para>
    /// <para><see langword="Key"/>: 分组名称</para>
    /// <para><see langword="Value"/>: 包含的模组</para>
    /// </summary>
    ReadOnlyDictionary<string, ExternalReadOnlySet<string>> AllUserGroups
}
```

## 其它事项

### 日志输出及弹窗使用

#### 标准信息使用默认输出

```csharp
STLog.WriteLine(message);
Utils.ShowMessageBox(message);
```

#### 使用 `ifelse` 筛选的信息以 `Warn` 等级输出

```csharp
if(isTrue == true)
{
    ...
}
else
{
    STLog.WriteLine(message, STLogLevel.WARNING);
    Utils.ShowMessageBox(message, IMessageBoxIcon.Warning);
}
```

#### 使用 `trycatch` 捕获的信息以 `Error` 等级输出

```csharp
try
{
    ...
}
catch (Exception ex)
{
    STLog.WriteLine(message, ex);
    Utils.ShowMessageBox(message, IMessageBoxIcon.Error);
}
```

### 控件风格

控件风格是为了将拓展页面的风格与软件本体风格统一
下面将演示拓展页面中如何引用及使用本体风格
_注:xaml 设计器中显示的内容可能与实际显示有误差_

**在页面资源(`Page.Resources`)中添加引用本体风格**

```xaml
  <Page.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/StarsectorTools;component/ThemeResources/ControlsStyle.xaml" />
        <ResourceDictionary Source="/StarsectorTools;component/ThemeResources/ColorStyle.xaml" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Page.Resources>
```

**引用本体风格**

```xaml
<Button Content="Button" Style="{StaticResource Button_Style}" />
```

**完整示例**

```xaml
<Page
  x:Class="WpfLibrary1.Page1"
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
  xmlns:local="clr-namespace:WpfLibrary1"
  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
  Title="Page1"
  d:DesignHeight="450"
  d:DesignWidth="800"
  mc:Ignorable="d">
  <Page.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/StarsectorTools;component/ThemeResources/ControlsStyle.xaml" />
        <ResourceDictionary Source="/StarsectorTools;component/ThemeResources/BlackStyle.xaml" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Page.Resources>
  <Grid>
    <Button
      Content="Button"
      Style="{StaticResource Button_Style}" />
  </Grid>
</Page>
```

### 长任务线程处理

如果要在`Page`中使用**长任务线程**
请为`Page`添加`public void Close()`方法
并在里面销毁创建的所有线程
当程序被关闭时会尝试调用此方法,以确保程序的正常关闭
若未销毁线程,则会导致程序无法正常关闭,此时请使用任务管理器结束任务
_除了销毁线程之外,同样可以在此方法中进行资源回收,设置保存等关闭前操作_

**示例:**

```csharp
namespace WpfLibrary1
{
    public partial class Page1 : Page
    {
        private Thread thread;
        public Page1()
        {
            InitializeComponent();
            thread = new(LongTasks);
            thread.Start();
        }
        public void Close()
        {
            if (thread.ThreadState != ThreadState.Unstarted)
                thread.Join(1);
        }
        private void LongTasks()
        {
            while(thread.ThreadState != ThreadState.Unstarted)
            {
                ...
            }
        }
    }
}
```
