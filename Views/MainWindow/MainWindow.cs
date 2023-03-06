using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HKW.Libs.Log4Cs;
using HKW.ViewModels;
using HKW.ViewModels.Dialogs;
using Panuon.WPF.UI;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindowI18nRes;
using StarsectorTools.ViewModels.MainWindow;
using CommunityToolkit.Mvvm.Messaging;
using StarsectorTools.Models.Messages;
using System.Collections.Generic;

namespace StarsectorTools.Views.MainWindow
{
    public partial class MainWindow
    {
        /// <summary>
        /// 消息长度限制
        /// </summary>
        private int _messageLengthLimits = 8192;

        private void RegisterData()
        {
            // 注册消息窗口
            RegisterMessageBox();
            // 注册等待窗口
            RegisterPendingBox();
            // 注册打开文件对话框
            RegisterOpenFileDialog();
            // 注册保存文件对话框
            RegisterSaveFileDialog();

            // 注册页面初始化消息
            WeakReferenceMessenger.Default.Register<GetMainMenuItemsRequestMessage>(
                this,
                GetMainMenuItemsRequestReceive
            );
        }

        private void GetMainMenuItemsRequestReceive(
            object recipient,
            GetMainMenuItemsRequestMessage message
        )
        {
            message.Reply(
                new()
                {
                    new() { Icon = "🌐", Tag = CreatePage(typeof(ModManagerPage.ModManagerPage)), },
                    new()
                    {
                        Icon = "⚙",
                        Tag = CreatePage(typeof(GameSettingsPage.GameSettingsPage))
                    }
                }
            );
        }

        private void RegisterMessageBox()
        {
            // 消息长度限制
            MessageBoxVM.InitializeHandler(
                (d) =>
                {
                    var message =
                        d.Message.Length < _messageLengthLimits
                            ? d.Message
                            : d.Message[.._messageLengthLimits]
                                + $".........{I18n.ExcessivelyLongMessages}.........";
                    var button = ButtonConverter(d.Button);
                    var icon = IconConverter(d.Icon);
                    MessageBoxResult result;
                    if (d.ShowMainWindowBlurEffect is true)
                    {
                        SetBlurEffect(false);
                        result = MessageBoxX.Show(message, d.Caption, button, icon);
                        RemoveBlurEffect();
                    }
                    else
                    {
                        result = MessageBoxX.Show(message, d.Caption, button, icon);
                    }
                    if (message.Length == _messageLengthLimits)
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

        private void RegisterPendingBox()
        {
            PendingBoxVM.InitializeHandler(
                (m, c, cc) =>
                {
                    SetBlurEffect(false);
                    var handler = PendingBox.Show(this, m, c, cc);
                    return new(
                        () =>
                        {
                            handler.Show();
                        },
                        () =>
                        {
                            handler.Hide();
                        },
                        () =>
                        {
                            handler.Close();
                            RemoveBlurEffect();
                        },
                        (s) =>
                        {
                            handler.UpdateMessage(s);
                        }
                    );
                }
            );
        }

        private void InitializePage()
        {
            // 添加页面
            ViewModel.InfoPage = new InfoPage.InfoPage();
            ViewModel.SettingsPage = new SettingsPage.SettingsPage();
        }

        private Page? CreatePage(Type type)
        {
            try
            {
                return (Page)type.Assembly.CreateInstance(type.FullName!)!;
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18n.PageInitializeError}: {type.FullName}", ex);
                MessageBoxVM.Show(
                    new($"{I18n.PageInitializeError}:\n{type.FullName}")
                    {
                        Icon = MessageBoxVM.Icon.Error
                    }
                );
                return null;
            }
        }

        System.Windows.Media.Effects.Effect _blurEffect =
            new System.Windows.Media.Effects.BlurEffect();

        /// <summary>
        /// 设置模糊效果
        /// </summary>
        private void SetBlurEffect(bool isEnabled)
        {
            Dispatcher.Invoke(() =>
            {
                IsEnabled = isEnabled;
                Effect = _blurEffect;
            });
        }

        /// <summary>
        /// 取消模糊效果
        /// </summary>
        private void RemoveBlurEffect()
        {
            Dispatcher.Invoke(() =>
            {
                IsEnabled = true;
                Effect = null;
            });
        }

        /// <summary>
        /// 检测颜色是否为亮色调
        /// </summary>
        /// <param name="color">颜色</param>
        /// <returns>是为<see langword="true"/>,不是为<see langword="false"/></returns>
        private bool IsLightColor(Color color)
        {
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255 > 0.5;
        }
    }
}
