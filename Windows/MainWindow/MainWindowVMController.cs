using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HKW.ViewModels.Controls;
using StarsectorTools.Libs.GameInfo;
using HKW.Libs.TomlParse;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;
using System.Xml.Serialization;
using HKW.Libs.Log4Cs;
using HKW.ViewModels.Dialog;

namespace StarsectorTools.Windows.MainWindow
{
    internal partial class MainWindowViewModel
    {
        private Dictionary<string, ExpansionInfo> allExpansionsInfo = new();
        private ListBoxItemVM? previousSelectedPageItem;
        private ExpansionInfo? deubgExpansionInfo;
        /// <summary>拓展信息</summary>
        private class ExpansionInfo
        {
            /// <summary>Id</summary>
            public string Id { get; private set; } = null!;

            /// <summary>名称</summary>
            public string Name { get; private set; } = null!;

            /// <summary>作者</summary>
            public string Author { get; private set; } = null!;

            /// <summary>图标</summary>
            public string Icon { get; private set; } = null!;

            /// <summary>版本</summary>
            public string Version { get; private set; } = null!;

            /// <summary>支持的工具箱版本</summary>
            public string ToolsVersion { get; private set; } = null!;

            /// <summary>描述</summary>
            public string Description { get; private set; } = null!;

            /// <summary>拓展Id</summary>
            public string ExpansionId { get; private set; } = null!;

            /// <summary>拓展文件</summary>
            public string ExpansionFile { get; private set; } = null!;

            /// <summary>拓展页面</summary>
            public object ExpansionPage { get; set; } = null!;

            public ExpansionInfo(TomlTable table)
            {
                foreach (var info in table)
                    SetInfo(info.Key, info.Value.AsString);
            }

            public void SetInfo(string key, string value)
            {
                switch (key)
                {
                    case nameof(Id): Id = value; break;
                    case nameof(Name): Name = value; break;
                    case nameof(Author): Author = value; break;
                    case nameof(Icon): Icon = value; break;
                    case nameof(Version): Version = value; break;
                    case nameof(ToolsVersion): ToolsVersion = value; break;
                    case nameof(Description): Description = value; break;
                    case nameof(ExpansionId): ExpansionId = value; break;
                    case nameof(ExpansionFile): ExpansionFile = value; break;
                }
            }
        }
        internal void Close()
        {
            CloseAllPages();
            CheckAllPagesSave();
            STLog.Close();
        }
        internal void AddMainPageItem(ListBoxItemVM model)
        {
            DetectPageItemData(ref model);
            MainListBox.Add(model);
        }
        private void DetectPageItemData(ref ListBoxItemVM model)
        {
            if (model?.Tag is not ISTPage page)
                return;
            model.Id = page.GetType().FullName;
            model.Content = page.NameI18n;
            model.ToolTip = page.DescriptionI18n;
            model.ContextMenu = CreateItemContextMenu();
        }
        private ContextMenuVM CreateItemContextMenu()
        {
            // 标准模式
            return new()
            {
                new((o) =>
                    {
                        if(o is not ListBoxItemVM model)
                            return;
                        model.Tag = CreatePage(model.Tag!.GetType());
                        if(model.IsSelected)
                            ShowPage(model.Tag);
                    })
                {
                    Icon = "🔄",
                    Header = I18n.RefreshPage,
                }
            };
            // 延迟加载,有问题 暂不使用
            //return new((list) =>
            //{
            //    list.Add(new((o) =>
            //    {
            //        if (o is not ListBoxItemVM model)
            //            return;
            //        model.Tag = CreatePage(model.Tag!.GetType());
            //        if (model.IsSelected)
            //            ShowPage(model.Tag);
            //    })
            //    {
            //        Icon = "🔄",
            //        Header = I18n.RefreshPage,
            //    });
            //});
        }


        private object? CreatePage(Type type)
        {
            try
            {
                return type.Assembly.CreateInstance(type.FullName!)!;
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18n.PageInitializeError}: {type.FullName}", ex);
                MessageBoxVM.Show(new($"{I18n.PageInitializeError}:\n{type.FullName}")
                {
                    Icon = MessageBoxVM.Icon.Error
                });
                return null;
            }
        }

        private void CheckGameStartOption()
        {
            if (clearGameLogOnStart)
                ClearGameLogFile();
        }
        private void ClearGameLogFile()
        {
            if (File.Exists(GameInfo.LogFile))
                Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
            File.Create(GameInfo.LogFile).Close();
            Logger.Record(I18n.GameLogCleanupCompleted);
        }
        private void InitializeDirectories()
        {
            if (!Directory.Exists(ST.CoreDirectory))
                Directory.CreateDirectory(ST.CoreDirectory);
            if (!Directory.Exists(ST.ExpansionDirectories))
                Directory.CreateDirectory(ST.ExpansionDirectories);
        }
        private bool SetConfig(string originalConfigData)
        {
            try
            {
                if (Utils.FileExists(ST.ConfigTomlFile, false))
                {
                    // 读取设置
                    var toml = TOML.Parse(ST.ConfigTomlFile);
                    // 语言
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    // 日志等级
                    Logger.Options.DefaultLevel = Logger.LevelConverter(toml["Extras"]["LogLevel"].AsString);
                    // 游戏目录
                    if (!GameInfo.SetGameData(toml["Game"]["Path"].AsString))
                    {
                        if (!(MessageBoxVM.Show(new(I18n.GameNotFound_SelectAgain)
                        {
                            Button = MessageBoxVM.Button.YesNo,
                            Icon = MessageBoxVM.Icon.Question,
                        }) == MessageBoxVM.Result.Yes && GameInfo.GetGameDirectory()))
                        {
                            MessageBoxVM.Show(new(I18n.GameNotFound_SoftwareExit)
                            {
                                Icon = MessageBoxVM.Icon.Error
                            });
                            return false;
                        }
                        toml["Game"]["Path"] = GameInfo.BaseDirectory;
                    }
                    // 拓展调试目录
                    string filePath = toml["Expansion"]["DebugPath"].AsString;
                    if (!string.IsNullOrEmpty(filePath) && CheckExpansionInfo(filePath, true) is ExpansionInfo info)
                    {
                        deubgExpansionInfo = info;
                        ST.ExpansionDebugPath = filePath;
                        ST.ExpansionDebugId = info.Id;
                    }
                    else
                        toml["Expansion"]["DebugPath"] = "";
                    ClearGameLogOnStart = toml["Game"]["ClearLogOnStart"].AsBoolean;
                    toml.SaveTo(ST.ConfigTomlFile);
                }
                else
                {
                    if (!(MessageBoxVM.Show(new(I18n.FirstStart)
                    {
                        Button = MessageBoxVM.Button.YesNo,
                        Icon = MessageBoxVM.Icon.Question,
                    }) == MessageBoxVM.Result.Yes && GameInfo.GetGameDirectory()))
                    {
                        MessageBoxVM.Show(new(I18n.GameNotFound_SoftwareExit)
                        {
                            Icon = MessageBoxVM.Icon.Error
                        });
                        return false;
                    }
                    CreateConfigFile(originalConfigData);
                    var toml = TOML.Parse(ST.ConfigTomlFile);
                    toml["Game"]["Path"] = GameInfo.BaseDirectory;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.ConfigTomlFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18n.ConfigFileError} {I18n.Path}: {ST.ConfigTomlFile}", ex);
                MessageBoxVM.Show(new($"{I18n.ConfigFileError}\n{I18n.Path}: {ST.ConfigTomlFile}")
                {
                    Icon = MessageBoxVM.Icon.Error,
                });
                CreateConfigFile(originalConfigData);
            }
            return true;
        }
        /// <summary>
        /// 创建配置文件
        /// </summary>
        private void CreateConfigFile(string configData)
        {
            File.WriteAllText(ST.ConfigTomlFile, configData);
            Logger.Record($"{I18n.ConfigFileCreationCompleted} {I18n.Path}: {ST.ConfigTomlFile}");
        }
        private ExpansionInfo? CheckExpansionInfo(string directory, bool loadInMemory = false)
        {
            if (string.IsNullOrEmpty(directory))
            {
                Logger.Record(I18n.ExpansionPathIsEmpty, Logger.Level.WARN);
                MessageBoxVM.Show(new(I18n.ExpansionPathIsEmpty)
                {
                    Icon = MessageBoxVM.Icon.Warning
                });
                return null;
            }
            string tomlFile = $"{directory}\\{ST.ExpansionInfoFile}";
            try
            {
                // 判断文件存在性
                if (!File.Exists(tomlFile))
                {
                    Logger.Record($"{I18n.ExpansionTomlFileNotFound} {I18n.Path}: {tomlFile}", Logger.Level.WARN);
                    MessageBoxVM.Show(new($"{I18n.ExpansionTomlFileNotFound}\n{I18n.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    });
                    return null;
                }
                var expansionInfo = new ExpansionInfo(TOML.Parse(tomlFile));
                var assemblyFile = $"{directory}\\{expansionInfo.ExpansionFile}";
                // 检测是否有相同的拓展
                if (allExpansionsInfo.ContainsKey(expansionInfo.ExpansionId))
                {
                    Logger.Record($"{I18n.ExpansionAlreadyExists} {I18n.Path}: {tomlFile}", Logger.Level.WARN);
                    MessageBoxVM.Show(new($"{I18n.ExpansionAlreadyExists}\n{I18n.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    });
                    return null;
                }
                // 判断组件文件是否存在
                if (!File.Exists(assemblyFile))
                {
                    Logger.Record($"{I18n.ExpansionFileError} {I18n.Path}: {tomlFile}", Logger.Level.WARN);
                    MessageBoxVM.Show(new($"{I18n.ExpansionFileError}\n{I18n.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    });
                    return null;
                }
                // 从内存或外部载入
                Type type;
                if (loadInMemory)
                {
                    var bytes = File.ReadAllBytes(assemblyFile);
                    type = Assembly.Load(bytes).GetType(expansionInfo.ExpansionId)!;
                    expansionInfo.ExpansionPage = type.Assembly.CreateInstance(type.FullName!)!;
                }
                else
                {
                    type = Assembly.LoadFrom(assemblyFile).GetType(expansionInfo.ExpansionId)!;
                    expansionInfo.ExpansionPage = type.Assembly.CreateInstance(type.FullName!)!;
                }
                // 判断是否成功创建了页面
                if (expansionInfo.ExpansionPage is null)
                {
                    Logger.Record($"{I18n.ExpansionIdError} {I18n.Path}: {tomlFile}", Logger.Level.WARN);
                    MessageBoxVM.Show(new($"{I18n.ExpansionIdError}\n{I18n.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    });
                    return null;
                }
                // 判断页面是否实现了接口
                if (expansionInfo.ExpansionPage is not ISTPage)
                {
                    Logger.Record($"{I18n.ExpansionIdError} {I18n.Path}: {tomlFile}", Logger.Level.WARN);
                    MessageBoxVM.Show(new($"{I18n.ExpansionIdError}\n{I18n.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    });
                    return null;
                }
                return expansionInfo;
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18n.ExpansionLoadError} {I18n.Path}: {tomlFile}", ex);
                MessageBoxVM.Show(new($"{I18n.ExpansionLoadError}\n{I18n.Path}: {tomlFile}")
                {
                    Icon = MessageBoxVM.Icon.Error
                });
                return null;
            }
        }

        internal void ChangeLanguage()
        {
            Logger.Record($"{I18n.DisplayLanguageIs} {Thread.CurrentThread.CurrentUICulture.Name}");
            TitleI18n = I18n.StarsectorTools;
            InfoI18n = I18n.Info;
            SettingsI18n = I18n.Settings;
            ClearGameLogOnStartI18n = I18n.ClearGameLogOnStart;
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
            toml.SaveTo(ST.ConfigTomlFile);
        }
        private void InitializeExpansionPage()
        {
            DirectoryInfo dirs = new(ST.ExpansionDirectories);
            foreach (var dir in dirs.GetDirectories())
            {
                if (CheckExpansionInfo(dir.FullName) is ExpansionInfo expansionInfo)
                {
                    var page = expansionInfo.ExpansionPage;
                    //var name = page.
                    ExpansionListBox.Add(new()
                    {
                        Id = expansionInfo.Id,
                        Icon = expansionInfo.Icon,
                        Content = expansionInfo.Name,
                        ToolTip = $"Author: {expansionInfo.Author}\nDescription: {expansionInfo.Description}",
                        Tag = page
                    });
                }
            }
        }
        private void InitializeExpansionDebugPage()
        {
            // 添加拓展调试页面
            if (deubgExpansionInfo is not null)
            {
                AddMainPageItem(new()
                {
                    Icon = deubgExpansionInfo.Icon,
                    Tag = deubgExpansionInfo.ExpansionPage,
                });
            }
        }
        #region CheckPageSave
        private void CheckAllPagesSave()
        {
            CheckMainPagesSave();
            CheckExpansionPagesSave();
        }
        private void CheckMainPagesSave()
        {
            foreach (var item in MainListBox)
                CheckPageSave(item);
        }
        private void CheckExpansionPagesSave()
        {
            foreach (var item in ExpansionListBox)
                CheckPageSave(item);
        }
        private void CheckPageSave(ListBoxItemVM model)
        {
            if (model.Tag is not object page)
                return;
            // 获取page中的Save方法并执行
            // 用于保存page中已修改的数据
            var type = page.GetType();
            try
            {
                if (type.GetProperty("NeedSave") is PropertyInfo info)
                {
                    if (info.GetValue(page) is true && MessageBoxVM.Show(new($"{I18n.Page}: {model.Content} {I18n.PageCheckSave}")
                    {
                        Button = MessageBoxVM.Button.YesNo
                    }) is MessageBoxVM.Result.Yes)
                    {
                        SavePage(model);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18n.PageSaveError} {type.FullName}", ex);
                MessageBoxVM.Show(new($"{I18n.PageSaveError} {type.FullName}\n{STLog.SimplifyException(ex)}")
                {
                    Icon = MessageBoxVM.Icon.Error
                });
            }
        }
        #endregion
        #region SavePage
        private void SaveAllPages()
        {
            SaveMainPages();
            SaveExpansionPages();
        }
        private void SaveMainPages()
        {
            foreach (var item in MainListBox)
                SavePage(item);
        }
        private void SaveExpansionPages()
        {
            foreach (var item in ExpansionListBox)
                SavePage(item);
        }
        private void SavePage(ListBoxItemVM model)
        {
            if (model.Tag is not object page)
                return;
            // 获取page中的Save方法并执行
            // 用于保存page中已修改的数据
            var type = page.GetType();
            try
            {
                if (type.GetMethod("Save") is MethodInfo info)
                    _ = info.Invoke(page, null);
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18n.PageSaveError} {type.FullName}", ex);
                MessageBoxVM.Show(new($"{I18n.PageSaveError} {type.FullName}\n{STLog.SimplifyException(ex)}")
                {
                    Icon = MessageBoxVM.Icon.Error
                });
            }
        }

        #endregion
        #region ClosePage

        private void CloseAllPages()
        {
            CloseMainPages();
            CloseExpansionPages();
        }

        private void CloseMainPages()
        {
            foreach (var page in MainListBox)
                ClosePage(page);
        }

        private void CloseExpansionPages()
        {
            foreach (var page in ExpansionListBox)
                ClosePage(page);
        }

        private void ClosePage(ListBoxItemVM model)
        {
            if (model.Tag is not object page)
                return;
            // 获取page中的Close方法并执行
            // 用于关闭page中创建的线程
            var type = page.GetType();
            try
            {
                if (type.GetMethod("Close") is MethodInfo info)
                    _ = info.Invoke(page, null);
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18n.PageCloseError} {type.FullName}", ex);
                MessageBoxVM.Show(new($"{I18n.PageCloseError} {type.FullName}\n{STLog.SimplifyException(ex)}")
                {
                    Icon = MessageBoxVM.Icon.Error
                });
            }
        }
        #endregion 
    }
}
