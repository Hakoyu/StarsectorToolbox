using System;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HKW.Libs.Log4Cs;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.Messages;
using StarsectorToolbox.Models.ST;
using StarsectorToolbox.Models.System;
using StarsectorToolbox.ViewModels.CrashReporter;
using I18nRes = StarsectorToolbox.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorToolbox.ViewModels.Main;

/// <summary>
/// 主窗口视图模型
/// </summary>
internal partial class MainWindowViewModel : ObservableObject
{
    #region ObservableProperty

    [ObservableProperty]
    private ObservableI18n<I18nRes> _i18n = ObservableI18n<I18nRes>.Create(new());

    [ObservableProperty]
    private CrashReporterWindowViewModel _crashReporterWindow = null!;

    [ObservableProperty]
    private bool _menuIsExpand = false;

    [ObservableProperty]
    private bool _clearGameLogOnStart = true;

    partial void OnClearGameLogOnStartChanged(bool value)
    {
        STSettings.Instance.Game.ClearLogOnStart = value;
        STSettings.Save();
    }

    [ObservableProperty]
    private bool _infoButtonIsChecked = false;

    [ObservableProperty]
    private bool _settingsButtonIsChecked = false;

    #endregion ObservableProperty

    #region Page

    [ObservableProperty]
    private object? _nowPage;

    [ObservableProperty]
    private object? _infoPage;

    [ObservableProperty]
    private object? _settingsPage;

    [ObservableProperty]
    private object? _extensionDebugPage;

    #endregion Page

    #region ListBox

    [ObservableProperty]
    private ListBoxVM _listBox_MainMenu = new();

    [ObservableProperty]
    private ListBoxVM _listBox_ExtensionMenu = new();

    #endregion ListBox

    public MainWindowViewModel()
    {
        //InitializeDirectories();
    }

    public MainWindowViewModel(bool noop)
    {
        Instance = this;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        SystemInfo.Initialize();
        InitializeData();
        InitializeDirectories();
        // 注册日志
        Logger.Initialize(ST.LogFile);
        // 设置全局异常捕获
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        // 初始化设置
        if (InitializeConfig() is false)
            throw new(I18nRes.GameNotFound_SoftwareExit);
        // 初始化消息
        InitializeMessenger();
        I18n.AddPropertyChangedAction(I18nChangedAction);
        if (ListBox_MainMenu.SelectedIndex == -1)
            ListBox_MainMenu.SelectedIndex = 0;
    }

    private void InitializeMessenger()
    {
        // 获取主页面
        var items = WeakReferenceMessenger.Default.Send<GetMainMenuItemsRequestMessage>();
        foreach (var item in items.Response)
            AddMainPageItem(item);
        // 初始化拓展页面
        InitializeExtensionPages();
        // 初始化拓展调试页面
        RefreshExtensionDebugPage();
        WeakReferenceMessenger.Default.Register<ExtensionDebugPathChangedMessage>(
            this,
            ExtensionDebugPathChangeReceive
        );
        WeakReferenceMessenger.Default.Register<ExtensionDebugPathRequestMessage>(
            this,
            ExtensionDebugPathRequestReceive
        );
        WeakReferenceMessenger.Default.Register<ShowCrashReporterMessage>(
            this,
            ShowCrashReporterWindowReceive
        );
    }

    private void I18nChangedAction()
    {
        foreach (var item in ListBox_MainMenu)
        {
            if (item is ISTPage iPage)
            {
                item.Content = iPage.GetNameI18n();
                item.ToolTip = iPage.GetDescriptionI18n();
            }
        }
        foreach (var item in ListBox_ExtensionMenu)
        {
            if (item is ISTPage iPage)
            {
                item.Content = iPage.GetNameI18n();
                item.ToolTip = iPage.GetDescriptionI18n();
            }
        }
    }

    private void InitializeData()
    {
        ListBox_MainMenu.SelectionChangedEvent += ListBox_SelectionChangedEvent;
        ListBox_ExtensionMenu.SelectionChangedEvent += ListBox_SelectionChangedEvent;
    }

    private void ListBox_SelectionChangedEvent(ListBoxItemVM item)
    {
        // 在对ListBoxVM.SelectedItem赋值触发此命令时,item并非ListBoxVM.SelectedItem,原因未知
        if (item is null || _selectedItem == item)
            return;
        // 若切换选择,可取消原来的选中状态,以此达到多列表互斥
        if (_selectedItem?.IsSelected is true)
            _selectedItem.IsSelected = false;
        _selectedItem = item;
        ShowPage(item.Tag);
    }

    #region Message
    private void ExtensionDebugPathChangeReceive(
        object recipient,
        ExtensionDebugPathChangedMessage message
    )
    {
        if (string.IsNullOrEmpty(message.Value))
        {
            _deubgItemExtensionInfo = null;
            _deubgItemPath = string.Empty;
        }
        else
        {
            if (TryGetExtensionInfo(message.Value, true) is not ExtensionInfo extensionInfo)
            {
                WeakReferenceMessenger.Default.Send<ExtensionDebugPathErrorMessage>(new(""));
                return;
            }
            _deubgItemExtensionInfo = extensionInfo;
            _deubgItemPath = message.Value;
        }
        RefreshExtensionDebugPage();
        STSettings.Instance.Extension.DebugPath = message.Value;
        STSettings.Save();
    }

    private void ExtensionDebugPathRequestReceive(
        object recipient,
        ExtensionDebugPathRequestMessage message
    )
    {
        message.Reply(_deubgItemPath!);
    }

    private void ShowCrashReporterWindowReceive(object recipient, ShowCrashReporterMessage message)
    {
        CrashReporterWindow.Show();
        CrashReporterWindow.RefreshCrashReport();
    }
    #endregion
    #region RelayCommand

    [RelayCommand]
    private void MenuExpand(object parameter) => MenuIsExpand = !MenuIsExpand;

    [RelayCommand]
    private void ShowPage(object? page)
    {
        NowPage = page;
        // 取消按钮页面选中状态
        InfoButtonIsChecked = false;
        SettingsButtonIsChecked = false;
        // 设置选中状态
        if (page == InfoPage)
        {
            InfoButtonIsChecked = true;
            _selectedItem =
                ListBox_MainMenu.SelectedItem =
                ListBox_ExtensionMenu.SelectedItem =
                    null;
        }
        else if (page == SettingsPage)
        {
            SettingsButtonIsChecked = true;
            _selectedItem =
                ListBox_MainMenu.SelectedItem =
                ListBox_ExtensionMenu.SelectedItem =
                    null;
        }
        Logger.Info($"{I18nRes.ShowPage}: {page?.GetType().FullName}");
    }

    [RelayCommand]
    private async Task StartGame()
    {
        if (Utils.FileExists(GameInfo.ExeFile) is false)
            return;
        ReminderSaveAllPages();
        CheckGameStartOption();
        using System.Diagnostics.Process process = new();
        process.StartInfo.FileName = "cmd";
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardInput = true;
        if (process.Start())
        {
            process.StandardInput.WriteLine($"cd /d {GameInfo.BaseDirectory}");
            process.StandardInput.WriteLine($"starsector.exe");
            await Task.Delay(3000);
            await CrashReporterWindow.ListeningGameAsync();
        }
    }

    [RelayCommand]
    private void RefreshExtensionMenu()
    {
        CloseExtensionPages();
        InitializeExtensionPages();
    }
    #endregion RelayCommand
}
