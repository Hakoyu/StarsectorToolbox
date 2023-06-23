using System.IO;
using System.Windows;
using StarsectorToolbox.Models.ST;
using StarsectorToolbox.Resources;

namespace StarsectorToolbox;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
internal partial class App : Application
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

    public App()
    {
        // 初始化日志配置文件
        if (File.Exists(STResources.NlogConfigFile) is false)
        {
            STResources.ResourceSave(STResources.NlogConfig, STResources.NlogConfigFile);
        }
        NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(
            STResources.NlogConfigFile
        );
        // TODO: ToolInfo
        //sr_logger.Info($"软件版本: {ST.Version}");
    }
}
