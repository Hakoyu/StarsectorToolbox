# StarsectorToolsChangeLog

## 0.7.8

### ModManager

修改:

- 调整了部分列的 `Columns` 类型, 提高了相应速度
- 为了防止调用 `icon` 时无法删除模组,载入时会将 `icon` 全部载入至内存,会增加一些内存消耗
- 优化了模组选中的方式
- 优化了模组前置的检测方式,现在不会在 `dependencies` 为空时,详情中依旧显示前置列表了
- 优化了 `ModShowInfo`,提高了效率
- 优化了 `ModShowInfo` 中,`ContextMenu` 的载入方式,降低了内存消耗

新增:

- 为 `TextBox` 添加了输入占位符
- 添加确认删除用户分组提示窗

删除:

- 删除了`刷新列表`按钮

### MainWindow

新增:

- 现在点击 `MainWindow` 会取消键盘焦点和事件焦点了
- 为所有菜单添加了右键菜单,可以对菜单进行刷新

### GameSettings

新增:

- 自定义分辨率功能,可以自定义窗口模式下的分辨率,并且可以设置无边框窗口
- 为 `TextBox` 添加了输入占位符
