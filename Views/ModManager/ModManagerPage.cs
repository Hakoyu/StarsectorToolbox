using I18nRes = StarsectorToolbox.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorToolbox.Views.ModManager;

internal partial class ModManagerPage
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