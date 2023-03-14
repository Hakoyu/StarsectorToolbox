# Expanded Tutorial zh-CN

## 环境

工具: **[Visual Studio 2022](https://visualstudio.microsoft.com/zh-hans/vs/)**

工具包: **.NET 桌面开发**

环境: **[.NET6](https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0)**

## 创建项目

[最小演示 Demo](https://github.com/Hakoyu/StarsectorTools/blob/master/Expanded%20Tutorial/WpfLibrary1.7z)  
[基础演示 Demo](https://github.com/Hakoyu/StarsectorToolsExtensionDemo)

### 设置项目

1. 新建项目: **WPF 类库**  
   注意是选择 **C#** 项目而不是 **Visual Basic** 项目  
   ![](https://s2.loli.net/2023/01/09/rKRmBXGDM1UPp8T.png)

2. 创建完成后 为项目添加一个页面(`Page`)  
   原来的**Class1.cs**可以自行选择删除或保留  
   ![](https://s2.loli.net/2023/01/09/y4YUb2EQX9r1RGl.png)

3. 为项目添加引用 **StarsectorTools.dll**  
   你可以在 **[Releases](https://github.com/Hakoyu/StarsectorTools/releases)** 中下载到  
   此外 **StarsectorTools.xml** 文件提供了注释,以便在 IDE 中更好的使用 API,可自行选择下载,与 **StarsectorTools.dll** 放同一目录即可

### 设置拓展信息

在 Debug 目录下(通常位于 **\bin\Debug\net6.0-windows**)  
建立拓展信息文件 **Extension.toml**  
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
Version = "1.0.0.0"
# 拓展支持的工具箱版本
ToolsVersion = "0.8.0.0"
# 拓展的描述
Description = "这是一个测试案例"
# 拓展的入口
ExtensionId = "WpfLibrary1.Page1"
# 拓展的入口文件
ExtensionFile = "WpfLibrary1.dll"
```

工具箱版本可查看 https://github.com/Hakoyu/StarsectorTools/releases

## 测试项目

用来测试项目是否能正确引用

### 测试入口

在 `Page1` 中写入

```csharp
public Page1()
{
    InitializeComponent();
    Logger.Info(GetType().ToString());
}
```

然后使用 StarsectorTools 的拓展调试功能定位拓展的路径即可载入  
![](https://s2.loli.net/2023/01/10/AMEHKxvF4ukg7On.png)

如果操作正确,此时 **StarsectorTools.log** 中会输出 `[Page1] INFO WpfLibrary1.Page1`

### 测试断点调试

在`Logger`处打上断点  
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


## 其它事项

### 日志输出及弹窗使用

#### 标准信息使用默认输出

```csharp
Logger.Info(message);
MessageBoxVM.Show(new(message));
```

#### 使用 `ifelse` 筛选的信息以 `Warn` 等级输出

```csharp
if(isTrue == true)
{
    ...
}
else
{
    Logger.Warn(message);
    MessageBoxVM.Show(new(message) {Icon = MessageBoxVM.Icon.Warning});
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
    Logger.Error(message, ex);
    MessageBoxVM.Show(new(message) {Icon = MessageBoxVM.Icon.Error});
}
```

### 控件风格

控件风格是为了将拓展页面的风格与软件本体风格统一  
下面将演示拓展页面中如何引用及使用本体风格  
_注: xaml 设计器中显示的内容可能与实际显示有误差_

**引用本体风格**

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
        <ResourceDictionary Source="/StarsectorTools;component/ThemeResources/ControlStyles.xaml" />
        <ResourceDictionary Source="/StarsectorTools;component/ThemeResources/ControlViewStyles.xaml" />
        <ResourceDictionary Source="/StarsectorTools;component/ThemeResources/ColorStyles.xaml" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Page.Resources>
  <Grid>
    <Button
      Content="Button"
      Style="{StaticResource ButtonBaseStyle}" />
  </Grid>
</Page>
```

## 打包

将 **WpfLibrary1.dll** 与 **Extension.toml** 放入同一个文件夹  
再将文件夹放入软件根目录的 **Extension** 文件夹即可  
此时启动软件,可在主界面的 **拓展** 下拉列表中看到导入的拓展项  
![](https://s2.loli.net/2023/01/12/IiUpqf9gchNGmAo.png)
