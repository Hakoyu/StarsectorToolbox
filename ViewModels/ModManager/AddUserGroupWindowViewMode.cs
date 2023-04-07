using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.ViewModels.Controls;
using StarsectorToolbox.Libs;

namespace StarsectorToolbox.ViewModels.ModManager;

internal partial class AddUserGroupWindowViewMode : WindowVM
{
    [ObservableProperty]
    private string _userGroupIcon = string.Empty;

    [ObservableProperty]
    private string _userGroupName = string.Empty;

    public ListBoxItemVM? BaseListBoxItem;

    public AddUserGroupWindowViewMode(object window)
        : base(window)
    {
        DataContext = this;
        ShowDialogEvent += () => Utils.SetMainWindowBlurEffect();
        HideEvent += () =>
        {
            UserGroupIcon = string.Empty;
            UserGroupName = string.Empty;
            BaseListBoxItem = null;
            Utils.RemoveMainWindowBlurEffect();
        };
    }

    [RelayCommand]
    private void OK()
    {
        OKEvent?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelEvent?.Invoke();
    }

    public delegate void DelegateHandler();

    public event DelegateHandler? OKEvent;

    public event DelegateHandler? CancelEvent;
}