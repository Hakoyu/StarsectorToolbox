using I18nRes = StarsectorTools.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorTools.Views.ModManagerPage
{
    public partial class ModManagerPage
    {
        public void Save()
        {
            ViewModel.Save();
        }

        public void Close()
        {
            ViewModel.Close();
        }

        public string GetNameI18n()
        {
            return I18nRes.ModManager;
        }

        public string GetDescriptionI18n()
        {
            return I18nRes.ModManagerDescription;
        }
    }
}