﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Panuon.WPF.UI;

namespace StarsectorToolbox.Views.CrashReporter;

/// <summary>
/// CrashReporterWindow.xaml 的交互逻辑
/// </summary>
internal partial class CrashReporterWindow : WindowX
{
    public CrashReporterWindow()
    {
        InitializeComponent();
    }
    //protected override void OnClosing(CancelEventArgs e)
    //{
    //    if (CloseToHide is false)
    //        return;
    //    e.Cancel = true;
    //    Hide();
    //}
}
