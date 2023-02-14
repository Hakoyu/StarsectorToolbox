using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HKW.ViewModels
{
    /// <summary>
    /// 可观测本地化资源实例
    /// <para>示例:
    /// <code>
    /// <![CDATA[
    /// public partial class MainWindowViewModel : ObservableObject
    /// {
    ///     [ObservableProperty]
    ///     public ObservableI18n<MainWindowI18nRes> i18n = ObservableI18n<MainWindowI18nRes>.Create(new());
    /// }
    /// ]]>
    /// </code>
    /// </para>
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
            return _observableI18nSet.TryGetValue(resName, out var value)
                ? (ObservableI18n<TI18nRes>)value
                : new(i18nRes, resName);
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
        protected readonly object _i18nRes;

        /// <summary>
        /// 本地化资源
        /// </summary>
        protected readonly string _resName;

        /// <summary>
        /// 本地化资源实例集合
        /// </summary>
        protected static Dictionary<string, object> _observableI18nSet = new();
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
                foreach (var observableI18n in _observableI18nSet)
                    ((ObservableI18n)observableI18n.Value).PropertyChanged?.Invoke(
                        (ObservableI18n)observableI18n.Value,
                        new(null)
                    );
            }
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
            _observableI18nSet.TryAdd(resName, this);
        }

        /// <summary>
        /// 属性更变委托
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
