﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.ViewModels.Controls
{
    /// <summary>
    /// 菜单项模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Name},Header = {Header},Count = {ItemsSource.Count}")]
    public partial class MenuItemVM : HeaderedItemsControlVM<MenuItemVM>
    {
        [ObservableProperty]
        private object? icon;
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="handler">委托</param>
        public MenuItemVM(CommandHandler? handler = null)
        {
            CommandEvent += handler;
        }
        [RelayCommand]
        private void MenuItem(object parameter)
        {
            if (CommandEvent is not null)
                CommandEvent(parameter);
        }
        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="parameter">参数</param>
        public delegate void CommandHandler(object parameter);
        /// <summary>
        /// 事件
        /// </summary>
        public event CommandHandler? CommandEvent;
    }
}