using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Model;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows.MainWindow
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// 主菜单展开状态
        /// </summary>
        [ObservableProperty]
        private bool menuIsExpand = false;
        [ObservableProperty]
        private string title = I18n.StarsectorTools;
        [ObservableProperty]
        private ListBoxItemModel? menuSelectedItem;
        [ObservableProperty]
        private object? nowPage;
        [ObservableProperty]
        private object? infoPage;
        [ObservableProperty]
        private object? settingsPage;
        [ObservableProperty]
        private List<ListBoxItemModel> mainPageItems = new();
        [ObservableProperty]
        private List<ListBoxItemModel> expansionPageItems = new();
        [ObservableProperty]
        private bool clearGameLogOnStart = true;

        public MainWindowViewModel()
        {
            InitializeDirectories();
        }

        [RelayCommand]
        private void ChangeMenuExpansionStatus()
        {
            MenuIsExpand = !MenuIsExpand;
        }

        [RelayCommand]
        private void ShowPage(object? page)
        {
            var back = MessageBox.Show(new()
            {
                Message = "114514",
                Icon = MessageBoxModel.Icon.Info,
                Button = MessageBoxModel.Button.OK
            });
            NowPage = page;
        }

        [RelayCommand]
        private void StartGame()
        {
            if (Utils.FileExists(GameInfo.ExeFile))
            {
                SaveAllPages();
                CheckGameStartOption();
                using System.Diagnostics.Process process = new();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                if (process.Start())
                {
                    process.StandardInput.WriteLine($"cd /d {GameInfo.BaseDirectory}");
                    process.StandardInput.WriteLine($"starsector.exe");
                }
            }
        }
    }
}
