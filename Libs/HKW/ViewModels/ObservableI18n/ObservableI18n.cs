using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HKW.ViewModels.ObservableI18n
{
    /// <summary>
    /// 可观测本地化资源实例
    /// <para>示例:
    /// <code>
    /// <![CDATA[
    /// public partial class MainWindowViewModel : ObservableObject
    /// {
    ///     [ObservableProperty]
    ///     public ObservableI18n<MainWindowI18nRes> i18n = new(new());
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
        /// 本地化资源
        /// </summary>
        public TI18nRes I18nRes { get; private set; }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="i18nRes">本地化资源</param>
        public ObservableI18n(TI18nRes i18nRes) : base(i18nRes)
        {
            I18nRes = i18nRes;
        }
    }

    /// <summary>
    /// 可观测本地化
    /// </summary>
    public class ObservableI18n : INotifyPropertyChanged
    {
        private readonly object _i18nRes;
        private static HashSet<ObservableI18n> _observableI18nSet = null!;
        private static string _language = CultureInfo.CurrentCulture.Name;
        /// <summary>
        /// 语言
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
                    observableI18n.PropertyChanged?.Invoke(observableI18n, new(null));
            }
        }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="i18nRes">本地化资源</param>
        public ObservableI18n(object i18nRes)
        {
            _i18nRes = i18nRes;
            _observableI18nSet ??= new();
            _observableI18nSet.Add(this);
        }
        /// <summary>
        /// 属性更变事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
