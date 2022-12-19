# API Usage Tutorial

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
