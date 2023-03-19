using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 选择器视图模型
/// </summary>
/// <typeparam name="TItem">项目类型</typeparam>
public partial class SelectorVM<TItem> : ItemsCollectionVM<TItem>
{
    /// <summary>
    /// 选中项的索引
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItem))]
    private int selectedIndex = -1;

    /// <summary>
    /// 选中项的值
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedIndex))]
    private TItem? selectedItem;

    /// <summary>
    /// 选中项改变命令
    /// </summary>
    /// <param name="item">选中项</param>
    [RelayCommand]
    private void SelectionChanged(TItem item) => SelectionChangedEvent?.Invoke(item);

    partial void OnSelectedIndexChanged(int value)
    {
        if (value < 0)
            SelectedItem = default;
        else
            SelectedItem = ItemsSource[value];
        SelectionChangedCommand.Execute(SelectedItem);
    }

    partial void OnSelectedItemChanged(TItem? value)
    {
        if (value is null)
            return;
        SelectedIndex = ItemsSource.IndexOf(value);
    }

    /// <summary>
    /// 委托
    /// </summary>
    /// <param name="item">参数</param>
    public delegate void ViewModelHandler(TItem item);

    /// <summary>
    /// 选中项选择改变事件
    /// </summary>
    public event ViewModelHandler? SelectionChangedEvent;
}