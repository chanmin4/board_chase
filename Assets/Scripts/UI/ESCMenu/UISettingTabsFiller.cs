using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UISettingTabsFiller : MonoBehaviour
{
    public UnityAction<SettingsType> ChooseTab = delegate { };

    [SerializeField] private UISettingTabFiller[] _settingTabsList = default;

    public void FillTabs(List<SettingsType> settingTabs)
    {
        if (settingTabs == null || _settingTabsList == null)
            return;

        int count = Mathf.Min(settingTabs.Count, _settingTabsList.Length);

        for (int i = 0; i < count; i++)
        {
            if (_settingTabsList[i] == null)
                continue;

            _settingTabsList[i].Clicked -= ChangeTab;
            _settingTabsList[i].SetTab(settingTabs[i], i == 0);
            _settingTabsList[i].Clicked += ChangeTab;
        }
    }

    private void OnDisable()
    {
        if (_settingTabsList == null)
            return;

        for (int i = 0; i < _settingTabsList.Length; i++)
        {
            if (_settingTabsList[i] != null)
                _settingTabsList[i].Clicked -= ChangeTab;
        }
    }

    public void SelectTab(SettingsType tabType)
    {
        if (_settingTabsList == null)
            return;

        for (int i = 0; i < _settingTabsList.Length; i++)
        {
            if (_settingTabsList[i] == null)
                continue;

            _settingTabsList[i].SetSelected(_settingTabsList[i].SettingTab == tabType);
        }
    }

    public void ChangeTab(SettingsType tabType)
    {
        ChooseTab.Invoke(tabType);
    }
}