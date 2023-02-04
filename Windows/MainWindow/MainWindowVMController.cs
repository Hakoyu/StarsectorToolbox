using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HKW.Model;
using HKW.TomlParse;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows.MainWindow
{
    internal partial class MainWindowViewModel
    {
        private Dictionary<string, ExpansionInfo> allExpansionsInfo = new();
        /// <summary>拓展信息</summary>
        private class ExpansionInfo
        {
            /// <summary>ID</summary>
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

            public Type ExpansionType = null!;

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
        internal void AddPage(string icon, string name, string id, string toolTip, object page)
        {

            MainPageItems.Add(new(SelectPageItem)
            {
                Id = id,
                Icon = icon,
                Content = name,
                ToolTip = toolTip,
                Tag = page
            });
        }

        private void SelectPageItem(ListBoxItemModel item)
        {
            // 若切换选择,可取消原来的选中状态,以此达到多列表互斥
            if (MenuSelectedItem?.IsSelected is true)
                MenuSelectedItem.IsSelected = false;
            MenuSelectedItem = item;
            ShowPage(item.Tag);
        }

        private void CheckGameStartOption()
        {
            if (clearGameLogOnStart)
                ClearGameLogFile();
        }
        private void ClearGameLogFile()
        {
            if (Utils.FileExists(GameInfo.LogFile, false))
                Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
            File.Create(GameInfo.LogFile).Close();
            STLog.WriteLine(I18n.GameLogCleanupCompleted);
        }
        private void InitializeDirectories()
        {
            if (!Utils.DirectoryExists(ST.CoreDirectory, false))
                Directory.CreateDirectory(ST.CoreDirectory);
            if (!Utils.DirectoryExists(ST.ExpansionDirectories, false))
                Directory.CreateDirectory(ST.ExpansionDirectories);
        }
        internal bool SetConfig(string originalConfigData)
        {
            try
            {
                if (Utils.FileExists(ST.ConfigTomlFile, false))
                {
                    // 读取设置
                    TomlTable toml = TOML.Parse(ST.ConfigTomlFile);
                    // 语言
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    // 日志等级
                    STLog.SetLogLevel(STLog.Str2STLogLevel(toml["Extras"]["LogLevel"].AsString));
                    // 游戏目录
                    if (!GameInfo.SetGameData(toml["Game"]["Path"].AsString!))
                    {
                        if (!(MessageBoxModel.Show(new()
                        {
                            Message = I18n.GameNotFound_SelectAgain,
                            Button = MessageBoxModel.Button.YesNo,
                            Icon = MessageBoxModel.Icon.Question,
                        }) == MessageBoxModel.Result.Yes && GameInfo.GetGameDirectory()))
                        {
                            Utils.ShowMessageBox(I18n.GameNotFound_SoftwareExit, STMessageBoxIcon.Error);
                            return false;
                        }
                        toml["Game"]["Path"] = GameInfo.BaseDirectory;
                    }
                    // 拓展调试目录
                    string filePath = toml["Expansion"]["DebugPath"].AsString;
                    if (!string.IsNullOrEmpty(filePath) && CheckExpansionInfo(filePath, true) is ExpansionInfo info)
                    {
                        ST.ExpansionDebugPath = filePath;
                        ST.ExpansionDebugId = info.Id;
                    }
                    else
                        toml["Expansion"]["DebugPath"] = "";
                    clearGameLogOnStart = toml["Game"]["ClearLogOnStart"].AsBoolean;
                    toml.SaveTo(ST.ConfigTomlFile);
                }
                else
                {
                    //if (!(Utils.ShowMessageBox(I18n.FirstStart, MessageBoxButton.YesNo, STMessageBoxIcon.Question) == MessageBoxResult.Yes && GameInfo.GetGameDirectory()))
                    //{
                    //    Utils.ShowMessageBox(I18n.GameNotFound_SoftwareExit, STMessageBoxIcon.Error);
                    //    return false;
                    //}
                    CreateConfigFile(originalConfigData);
                    TomlTable toml = TOML.Parse(ST.ConfigTomlFile);
                    toml["Game"]["Path"] = GameInfo.BaseDirectory;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.ConfigTomlFile);
                }
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.ConfigFileError} {I18n.Path}: {ST.ConfigTomlFile}", ex);
                Utils.ShowMessageBox($"{I18n.ConfigFileError}\n{I18n.Path}: {ST.ConfigTomlFile}", STMessageBoxIcon.Error);
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
            STLog.WriteLine($"{I18n.ConfigFileCreationCompleted} {I18n.Path}: {ST.ConfigTomlFile}");
        }
        private ExpansionInfo? CheckExpansionInfo(string directory, bool loadInMemory = false)
        {
            if (string.IsNullOrEmpty(directory))
            {
                STLog.WriteLine(I18n.ExpansionPathIsEmpty, STLogLevel.WARN);
                Utils.ShowMessageBox(I18n.ExpansionPathIsEmpty, STMessageBoxIcon.Warning);
                return null;
            }
            string tomlFile = $"{directory}\\{ST.ExpansionInfoFile}";
            try
            {
                if (!Utils.FileExists(tomlFile, false))
                {
                    STLog.WriteLine($"{I18n.ExpansionTomlFileNotFound} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    Utils.ShowMessageBox($"{I18n.ExpansionTomlFileNotFound}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Warning);
                    return null;
                }
                var expansionInfo = new ExpansionInfo(TOML.Parse(tomlFile));
                var assemblyFile = $"{directory}\\{expansionInfo.ExpansionFile}";
                if (allExpansionsInfo.ContainsKey(expansionInfo.ExpansionId))
                {
                    STLog.WriteLine($"{I18n.ExpansionAlreadyExists} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    Utils.ShowMessageBox($"{I18n.ExpansionAlreadyExists}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Warning);
                    return null;
                }
                if (!Utils.FileExists(assemblyFile, false))
                {
                    STLog.WriteLine($"{I18n.ExpansionFileError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    Utils.ShowMessageBox($"{I18n.ExpansionFileError}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Warning);
                    return null;
                }
                if (loadInMemory)
                {
                    var bytes = File.ReadAllBytes(assemblyFile);
                    expansionInfo.ExpansionType = Assembly.Load(bytes).GetType(expansionInfo.ExpansionId)!;
                }
                else
                    expansionInfo.ExpansionType = Assembly.LoadFrom(assemblyFile).GetType(expansionInfo.ExpansionId)!;
                if (expansionInfo.ExpansionType is null)
                {
                    STLog.WriteLine($"{I18n.ExpansionIdError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    Utils.ShowMessageBox($"{I18n.ExpansionIdError}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Warning);
                    return null;
                }
                return expansionInfo;
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.ExpansionLoadError} {I18n.Path}: {tomlFile}", ex);
                Utils.ShowMessageBox($"{I18n.ExpansionLoadError}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Error);
                return null;
            }
        }
        #region SavePage
        private void SaveAllPages()
        {
            SaveMainPages();
            SaveExpansionPages();
        }

        private void SaveMainPages()
        {
            foreach (var item in mainPageItems)
                SavePage(item.Tag);
        }
        private void SaveExpansionPages()
        {
            foreach (var item in expansionPageItems)
                SavePage(item.Tag);
        }

        private void SavePage(object? page)
        {
            if (page is null)
                return;
            // 获取page中的Save方法并执行
            // 用于保存page中已修改的数据
            try
            {
                if (page.GetType().GetMethod("Save") is MethodInfo info)
                    _ = info.Invoke(page, null);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageSaveError} {page.GetType().FullName}", ex);
                Utils.ShowMessageBox($"{I18n.PageSaveError} {page.GetType().FullName}\n{STLog.SimplifyException(ex)}", STMessageBoxIcon.Error);
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
            foreach (var page in mainPageItems)
                ClosePage(page.Tag);
        }

        private void CloseExpansionPages()
        {
            foreach (var page in expansionPageItems)
                ClosePage(page.Tag);
        }

        private void ClosePage(object? page)
        {
            if (page is null)
                return;
            // 获取page中的Close方法并执行
            // 用于关闭page中创建的线程
            try
            {
                if (page.GetType().GetMethod("Close") is MethodInfo info)
                    _ = info.Invoke(page, null);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageCloseError} {page.GetType().FullName}", ex);
                Utils.ShowMessageBox($"{I18n.PageCloseError} {page.GetType().FullName}\n{STLog.SimplifyException(ex)}", STMessageBoxIcon.Error);
            }
        }

        #endregion 
    }
}
