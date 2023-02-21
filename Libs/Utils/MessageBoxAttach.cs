using System.Windows;
using Panuon.WPF.UI;
using Panuon.WPF.UI.Configurations;

namespace StarsectorTools.Libs.Utils
{
    public class MessageBoxXAttach : MessageBoxXSetting
    {
        public bool CheckBoxIsChecked
        {
            get { return (bool)GetValue(CheckBoxIsCheckedProperty); }
            set { SetValue(CheckBoxIsCheckedProperty, value); }
        }

        public static readonly DependencyProperty CheckBoxIsCheckedProperty =
            DependencyProperty.Register("CheckBoxIsChecked", typeof(bool), typeof(MessageBoxX));

        #region CheckBoxMessage

        public string CheckBoxMessage
        {
            get { return (string)GetValue(CheckBoxMessageProperty); }
            set { SetValue(CheckBoxMessageProperty, value); }
        }

        public static readonly DependencyProperty CheckBoxMessageProperty =
            DependencyProperty.Register("CheckBoxMessage", typeof(string), typeof(MessageBoxX));

        #endregion CheckBoxMessage
    }
}