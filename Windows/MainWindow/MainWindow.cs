using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HKW.Libs.Log4Cs;
using HKW.ViewModels;
using HKW.ViewModels.Dialog;
using Panuon.WPF.UI;
using Panuon.WPF.UI.Configurations;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Pages.GameSettings;
using StarsectorTools.Pages.Info;
using StarsectorTools.Pages.ModManager;
using StarsectorTools.Pages.Settings;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorTools.Windows.MainWindow
{
    public partial class MainWindow
    {
        /// <summary>
        /// 消息长度限制
        /// </summary>
        private int messageLengthLimits = 8192;

        /// <summary>StarsectorTools配置文件资源链接</summary>
        private static readonly Uri resourcesConfigUri =
            new("\\Resources\\Config.toml", UriKind.Relative);

        internal MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

        private void RegisterData()
        {
            // 注册消息窗口
            RegisterMessageBox();
            // 注册打开文件对话框
            RegisterOpenFileDialog();
            // 注册保存文件对话框
            RegisterSaveFileDialog();
            // 注册剪切板视图模型

            // 注册主窗口模糊效果触发器
            ViewModel.RegisterChangeWindowEffectEvent(SetBlurEffect, RemoveBlurEffect);
        }

        private void RegisterMessageBox()
        {
            // 消息长度限制
            MessageBoxVM.InitializeHandler(
                (d) =>
                {
                    var message =
                        d.Message.Length < messageLengthLimits
                            ? d.Message
                            : d.Message[..messageLengthLimits]
                                + $".........{I18n.ExcessivelyLongMessages}.........";
                    var button = ButtonConverter(d.Button);
                    var icon = IconConverter(d.Icon);
                    MessageBoxResult result;
                    if (d.Tag is false)
                    {
                        result = MessageBoxX.Show(message, d.Caption, button, icon);
                    }
                    else
                    {
                        SetBlurEffect();
                        result = MessageBoxX.Show(message, d.Caption, button, icon);
                        RemoveBlurEffect();
                    }
                    if (message.Length == messageLengthLimits)
                        GC.Collect();
                    return ResultConverter(result);
                }
            );
            static MessageBoxButton ButtonConverter(MessageBoxVM.Button? button) =>
                button switch
                {
                    MessageBoxVM.Button.OK => MessageBoxButton.OK,
                    MessageBoxVM.Button.OKCancel => MessageBoxButton.OKCancel,
                    MessageBoxVM.Button.YesNo => MessageBoxButton.YesNo,
                    MessageBoxVM.Button.YesNoCancel => MessageBoxButton.YesNoCancel,
                    _ => MessageBoxButton.OK,
                };
            static MessageBoxIcon IconConverter(MessageBoxVM.Icon? icon) =>
                icon switch
                {
                    MessageBoxVM.Icon.None => MessageBoxIcon.None,
                    MessageBoxVM.Icon.Info => MessageBoxIcon.Info,
                    MessageBoxVM.Icon.Warning => MessageBoxIcon.Warning,
                    MessageBoxVM.Icon.Error => MessageBoxIcon.Error,
                    MessageBoxVM.Icon.Success => MessageBoxIcon.Success,
                    MessageBoxVM.Icon.Question => MessageBoxIcon.Question,
                    _ => MessageBoxIcon.Info,
                };
            static MessageBoxVM.Result ResultConverter(MessageBoxResult result) =>
                result switch
                {
                    MessageBoxResult.None => MessageBoxVM.Result.None,
                    MessageBoxResult.OK => MessageBoxVM.Result.OK,
                    MessageBoxResult.Cancel => MessageBoxVM.Result.Cancel,
                    MessageBoxResult.Yes => MessageBoxVM.Result.Yes,
                    MessageBoxResult.No => MessageBoxVM.Result.No,
                    _ => MessageBoxVM.Result.None,
                };
        }

        private void RegisterOpenFileDialog()
        {
            OpenFileDialogVM.InitializeHandler(
                (d) =>
                {
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog()
                    {
                        Title = d.Title,
                        Filter = d.Filter,
                        Multiselect = d.Multiselect,
                    };
                    openFileDialog.ShowDialog();
                    return openFileDialog.FileNames;
                }
            );
        }

        private void RegisterSaveFileDialog()
        {
            SaveFileDialogVM.InitializeHandler(
                (d) =>
                {
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = d.Title,
                        Filter = d.Filter,
                    };
                    saveFileDialog.ShowDialog();
                    return saveFileDialog.FileName;
                }
            );
        }

        private void RegisterClipboard()
        {
            ClipboardVM.InitializeHandler(s =>
            {
                Clipboard.SetText(s);
            });
        }

        private void InitializePage()
        {
            // 添加页面
            ViewModel.InfoPage = new InfoPage();
            ViewModel.SettingsPage = new SettingsPage();
            // 主界面必须在View中生成,拓展及调试拓展可以在ViewModel中使用反射
            InitializeMainPage();
            //InitializeExtensionPages();
            //InitializeExtensionDebugPage();
        }

        private void InitializeMainPage()
        {
            //添加主要页面
            ViewModel.AddMainPageItem(
                new() { Icon = "🌐", Tag = CreatePage(typeof(ModManagerPage)), }
            );
            ViewModel.AddMainPageItem(
                new() { Icon = "⚙", Tag = CreatePage(typeof(GameSettingsPage)) }
            );
        }

        private Page? CreatePage(Type type)
        {
            try
            {
                return (Page)type.Assembly.CreateInstance(type.FullName!)!;
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18n.PageInitializeError}: {type.FullName}", ex);
                MessageBoxVM.Show(
                    new($"{I18n.PageInitializeError}:\n{type.FullName}")
                    {
                        Icon = MessageBoxVM.Icon.Error
                    }
                );
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
