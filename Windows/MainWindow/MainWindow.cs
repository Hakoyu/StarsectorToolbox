using System;
using System.Windows;
using System.Windows.Controls;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Pages.GameSettings;
using StarsectorTools.Pages.Info;
using StarsectorTools.Pages.ModManager;
using StarsectorTools.Pages.Settings;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows.MainWindow
{
    public partial class MainWindow
    {
        /// <summary>StarsectorTools配置文件资源链接</summary>
        private static readonly Uri resourcesConfigUri =
            new("\\Resources\\Config.toml", UriKind.Relative);

        internal static MainWindowViewModel ViewModel =>
            (MainWindowViewModel)((MainWindow)Application.Current.MainWindow).DataContext;
        private void RegisterData()
        {
            // 注册消息窗口
            RegisterMessageBoxModel();
            // 注册打开文件对话框
            RegisterOpenFileDialogModel();
            // 注册保存文件对话框
            RegisterSaveFileDialogModel();
            // 注册主窗口模糊效果触发器
            ViewModel.RegisterChangeWindowEffectEvent(SetBlurEffect, RemoveBlurEffect);
        }

        private void InitializePage()
        {
            // 添加页面
            ViewModel.InfoPage = new InfoPage();
            ViewModel.SettingsPage = new SettingsPage();
            // 主界面必须在View中生成,拓展及调试拓展可以在ViewModel中使用反射
            InitializeMainPage();
            //InitializeExpansionPages();
            //InitializeExpansionDebugPage();
        }

        private void InitializeMainPage()
        {
            //添加主要页面
            ViewModel.AddMainPageItem(new()
            {
                Icon = "🌐",
                Tag = CreatePage(typeof(ModManagerPage)),
            });
            ViewModel.AddMainPageItem(new()
            {
                Icon = "⚙",
                Tag = CreatePage(typeof(GameSettingsPage))
            });
        }

        private Page? CreatePage(Type type)
        {
            try
            {
                return (Page)type.Assembly.CreateInstance(type.FullName!)!;
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageInitializeError}: {type.FullName}", ex);
                Utils.ShowMessageBox($"{I18n.PageInitializeError}:\n{type.FullName}", STMessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// 设置模糊效果
        /// </summary>
        private void SetBlurEffect()
        {
            Dispatcher.Invoke(() => Effect = new System.Windows.Media.Effects.BlurEffect());
        }

        /// <summary>
        /// 取消模糊效果
        /// </summary>
        private void RemoveBlurEffect()
        {
            Dispatcher.Invoke(() => Effect = null);
        }
    }
}