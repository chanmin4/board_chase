/*
 * Role hierarchy: Presentation > UI > Common
 * Primary responsibility: Supplies safe table lookups for UI text generated at runtime.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

/// <summary>
/// A small synchronous facade for UI that is created after localization has initialized.
/// It deliberately returns the supplied fallback while initialization is still in progress;
/// components that stay on screen should also refresh on SelectedLocaleChanged.
/// </summary>
public static class UIRuntimeLocalization
{
    public static bool IsReady => LocalizationSettings.InitializationOperation.IsDone;

    public static string Get(string table, string key, string fallback)
    {
        if (!IsReady || string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(key))
            return fallback ?? string.Empty;

        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key);

        if (!handle.IsDone || handle.Status != AsyncOperationStatus.Succeeded || string.IsNullOrWhiteSpace(handle.Result))
            return fallback ?? string.Empty;

        return handle.Result;
    }

    public static string Format(string table, string key, string fallback, params object[] arguments)
    {
        string format = Get(table, key, fallback);

        try
        {
            return arguments == null || arguments.Length == 0
                ? format
                : string.Format(format, arguments);
        }
        catch (FormatException)
        {
            return fallback ?? string.Empty;
        }
    }

    /// <summary>
    /// Loads one representative entry so later synchronous UI lookups from the same table
    /// can render in the selected language rather than briefly falling back to English.
    /// </summary>
    public static IEnumerator EnsureTableLoaded(string table, string representativeEntryKey)
    {
        yield return LocalizationSettings.InitializationOperation;

        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(representativeEntryKey))
            yield break;

        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, representativeEntryKey);
        yield return handle;
    }
}

/// <summary>
/// Localizes authored, non-item UI labels that are shared by multiple prefabs.
/// Dynamic labels are owned by their presentation component instead of this bootstrap.
/// </summary>
[DefaultExecutionOrder(-9000)]
public sealed class UIStaticTextLocalizationBootstrap : MonoBehaviour
{
    private readonly struct StaticTextEntry
    {
        public readonly string table;
        public readonly string key;

        public StaticTextEntry(string table, string key)
        {
            this.table = table;
            this.key = key;
        }
    }

    private static readonly IReadOnlyDictionary<string, StaticTextEntry> Entries =
        new Dictionary<string, StaticTextEntry>(StringComparer.Ordinal)
        {
            ["Retry"] = new("UI_Popup", "Popup_Retry"),
            ["MainMenu"] = new("UI_Pause", "Pause_MainMenu"),
            ["Main Menu"] = new("UI_Pause", "Pause_MainMenu"),
            ["Survival Complete!"] = new("UI_Popup", "Popup_SurvivalComplete"),
            ["Shop"] = new("UI_Inventory", "Inventory_Shop"),
            ["Click To Buy"] = new("UI_Inventory", "Inventory_ShopInstruction"),
            ["Sell"] = new("UI_Inventory", "Inventory_Sell"),
            ["Equipe"] = new("UI_Inventory", "Inventory_Equip"),
            ["Buy"] = new("UI_Inventory", "Inventory_Buy"),
            ["SOLD OUT"] = new("UI_Inventory", "Inventory_SoldOut"),
            ["PASSIVE"] = new("UI_Inventory", "Inventory_Passive"),
            ["POCKET"] = new("UI_Inventory", "Inventory_Pocket"),
            ["EQUIPMENT"] = new("UI_Inventory", "Inventory_Equipment"),
            ["Equipment"] = new("UI_Inventory", "Inventory_Equipment"),
            ["INVENTORY"] = new("UI_Inventory", "Inventory_Title"),
            ["Player"] = new("UI_Inventory", "Inventory_Player"),
            ["WARNING"] = new("UI_Popup", "Popup_Title"),
            ["Warning"] = new("UI_Popup", "Popup_Title"),
            ["OK"] = new("UI_Popup", "Popup_Ok"),
            ["YES"] = new("UI_Popup", "Popup_Ok"),
            ["NO"] = new("UI_Popup", "Popup_Cancel"),
            ["Cancel"] = new("UI_Popup", "Popup_Cancel"),
            ["Discard?"] = new("UI_Popup", "Popup_Discard"),
            ["really Quit?"] = new("UI_Popup", "Popup_QuitDescription"),
            ["Press Space to Stop"] = new("UI_Popup", "Popup_QtePressSpace"),
            ["Pause"] = new("UI_Pause", "Pause_Title"),
            ["General"] = new("UI_Settings", "Settings_Tab_General"),
            ["Audio"] = new("UI_Settings", "Settings_Tab_Audio"),
            ["Graphics"] = new("UI_Settings", "Settings_Tab_Graphic"),
            ["Key Bindings"] = new("UI_Settings", "Settings_Tab_KeyBindings"),
            ["Language"] = new("UI_Settings", "Settings_Content_LanguageTitle"),
            ["Current Difficulty"] = new("UI_Settings", "Settings_CurrentDifficulty"),
            ["Press A Key   To Bind"] = new("UI_Settings", "Settings_Key_RebindTitle"),
            ["Press ESC to cancel"] = new("UI_Settings", "Settings_Key_RebindCancel"),
            ["Reset Key Bindings"] = new("UI_Settings", "Settings_Action_ResetKeyBindings"),
            ["Setting"] = new("UI_MainMenu", "MainMenu_Settings"),
            ["Settings"] = new("UI_MainMenu", "MainMenu_Settings"),
            ["Achievement"] = new("UI_MainMenu", "MainMenu_Achievement"),
            ["Credit"] = new("UI_MainMenu", "MainMenu_Credit"),
            ["Player Upgrade"] = new("UI_MainMenu", "MainMenu_PlayerUpgrade"),
            ["Skin Inventory"] = new("UI_MainMenu", "MainMenu_PlayerUpgrade"),
        };

    // Some HUDs generate their labels entirely in code and therefore have no authored TMP
    // text for the scan below. Load their tables once with the other always-available UI data.
    private static readonly StaticTextEntry[] PreloadEntries =
    {
        new("UI_Popup", "Popup_Retry"),
        new("UI_Inventory", "Inventory_Title"),
        new("UI_Pause", "Pause_Title"),
        new("UI_MainMenu", "MainMenu_Settings"),
        new("UI_Settings", "Settings_Title"),
        new("UI_PlayerInfo", "PlayerInfo_HealthFormat"),
        new("UI_PlayerTimer", "PlayerTimer_StageFormat"),
        new("UI_Map", "Map_Opacity"),
    };

    private static UIStaticTextLocalizationBootstrap _instance;
    private readonly Dictionary<TMP_Text, StaticTextEntry> _boundTexts = new();
    private Coroutine _refreshRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureBootstrap()
    {
        if (_instance != null)
            return;

        GameObject host = new("UI Static Text Localization");
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<UIStaticTextLocalizationBootstrap>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RequestRefresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_refreshRoutine != null)
        {
            StopCoroutine(_refreshRoutine);
            _refreshRoutine = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RequestRefresh();
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RequestRefresh();
    }

    private void RequestRefresh()
    {
        if (_refreshRoutine != null)
            StopCoroutine(_refreshRoutine);

        _refreshRoutine = StartCoroutine(RefreshRoutine());
    }

    private IEnumerator RefreshRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        for (int index = 0; index < PreloadEntries.Length; index++)
        {
            StaticTextEntry preload = PreloadEntries[index];
            AsyncOperationHandle<string> handle =
                LocalizationSettings.StringDatabase.GetLocalizedStringAsync(preload.table, preload.key);
            yield return handle;
        }

        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.GetComponent<LocalizeStringEvent>() != null)
                continue;

            string authoredText = text.text?.Trim();
            if (string.IsNullOrWhiteSpace(authoredText) ||
                !Entries.TryGetValue(authoredText, out StaticTextEntry entry))
                continue;

            _boundTexts[text] = entry;
        }

        List<TMP_Text> removed = null;
        foreach (KeyValuePair<TMP_Text, StaticTextEntry> pair in _boundTexts)
        {
            TMP_Text text = pair.Key;
            if (text == null)
            {
                removed ??= new List<TMP_Text>();
                removed.Add(text);
                continue;
            }

            StaticTextEntry entry = pair.Value;
            AsyncOperationHandle<string> handle =
                LocalizationSettings.StringDatabase.GetLocalizedStringAsync(entry.table, entry.key);
            yield return handle;

            if (text != null &&
                handle.Status == AsyncOperationStatus.Succeeded &&
                !string.IsNullOrWhiteSpace(handle.Result))
            {
                text.text = handle.Result;
            }
        }

        if (removed != null)
        {
            for (int i = 0; i < removed.Count; i++)
                _boundTexts.Remove(removed[i]);
        }

        _refreshRoutine = null;
    }
}
