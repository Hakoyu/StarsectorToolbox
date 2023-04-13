using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;

namespace HKW.ViewModels;

/// <summary>
/// 可观测本地化资源实例
/// <para>创建实例:<code><![CDATA[    public partial class MainWindowViewModel : ObservableObject
/// {
///     [ObservableProperty]
///     public ObservableI18n<MainWindowI18nRes> _i18n = ObservableI18n<MainWindowI18nRes>.Create(new());
/// }
/// ]]></code></para>
/// <para>在xaml中使用:<code><![CDATA[    <Window.DataContext>
///     <local:MainWindowViewModel />
/// </Window.DataContext>
/// <Grid>
///     <Label Content="{Binding I18nRes.I18nRes.Key}" />
///     <Label Content="{Binding LabelContent}" />
/// </Grid>
/// ]]></code></para>
/// <para>在代码中使用:<code><![CDATA[    [ObservableProperty]
/// private string labelContent;
/// public MainWindowViewModel()
/// {
///     I18nRes.AddPropertyChangedAction(() =>
///     {
///        LabelContent = MainWindowI18nRes.Test;
///     });
/// }
/// ]]></code></para>
/// </summary>
/// <typeparam name="TI18nRes">I18n资源</typeparam>
public class ObservableI18n<TI18nRes> : ObservableI18n
    where TI18nRes : notnull
{
    /// <summary>
    /// 资源名称
    /// </summary>
    public string ResName => _resName;

    /// <summary>
    /// 本地化资源
    /// </summary>
    public TI18nRes I18nRes => (TI18nRes)_i18nRes;

    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="i18nRes">本地化资源</param>
    /// <param name="resName">资源名称</param>
    private ObservableI18n(TI18nRes i18nRes, string resName)
        : base(i18nRes, resName) { }

    /// <summary>
    /// 创造可观测本地化资源实例
    /// </summary>
    /// <param name="i18nRes">本地化资源</param>
    /// <returns>若检测到同名的实例,返回已有实例,否则新建实例</returns>
    public static ObservableI18n<TI18nRes> Create(TI18nRes i18nRes)
    {
        var resName = i18nRes.GetType().FullName!;
        return ObservableI18ns.TryGetValue(resName, out var value)
            ? (ObservableI18n<TI18nRes>)value
            : new(i18nRes, resName);
    }

    /// <summary>
    /// 刷新I18n资源
    /// </summary>
    public void Refresh()
    {
        Refresh(ResName);
    }
}

/// <summary>
/// 可观测本地化
/// <c>ObservableI18n.Language = "zh-CN"</c>
/// </summary>
public class ObservableI18n : INotifyPropertyChanged
{
    /// <summary>
    /// 资源名称
    /// </summary>
    protected object _i18nRes;

    /// <summary>
    /// 本地化资源
    /// </summary>
    protected readonly string _resName;

    private static readonly Dictionary<string, object> _observableI18nTSet = new();

    /// <summary>
    /// 本地化资源实例集合
    /// </summary>
    protected static Dictionary<string, object> ObservableI18ns => _observableI18nTSet;

    private static string _language = CultureInfo.CurrentCulture.Name;

    /// <summary>
    /// 当前语言
    /// </summary>
    public static string Language
    {
        get => _language;
        set
        {
            if (_language == value)
                return;
            _language = value;
            var cultureInfo = new CultureInfo(value);
            CultureInfo.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            foreach (var observableI18n in ObservableI18ns)
            {
                if (observableI18n.Value is not ObservableI18n i18n)
                    return;
                i18n.PropertyChanged?.Invoke(i18n, new(null));
            }
        }
    }

    /// <summary>
    /// 刷新I18n资源
    /// </summary>
    /// <param name="resName">资源名称</param>
    protected static void Refresh(string resName)
    {
        if (ObservableI18ns[resName] is not ObservableI18n observableI18n)
            return;
        observableI18n.PropertyChanged?.Invoke(observableI18n, new(null));
    }

    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="i18nRes">本地化资源</param>
    /// <param name="resName">资源名称</param>
    protected ObservableI18n(object i18nRes, string resName)
    {
        _i18nRes = i18nRes;
        _resName = resName;
        ObservableI18ns.TryAdd(resName, this);
    }

    /// <summary>
    /// 添加属性改变委托,并刷新
    /// </summary>
    /// <param name="propertyChangedAction">属性改变委托</param>
    /// <param name="nowRefresh">立刻刷新</param>
    public void AddPropertyChangedAction(Action propertyChangedAction, bool nowRefresh = false)
    {
        if (nowRefresh)
            propertyChangedAction();
        PropertyChanged += (s, e) =>
        {
            propertyChangedAction();
        };
    }

    /// <summary>
    /// 属性更变委托
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}