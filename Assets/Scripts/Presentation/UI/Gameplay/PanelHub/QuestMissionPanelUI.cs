/*
 * Role hierarchy: Presentation > UI > Gameplay > Panel Hub > Missions
 * Primary responsibility: Renders accepted and offered English quests, and accepts authored Home quest offers.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class QuestMissionPanelUI : MonoBehaviour
{
    private const string StringTable = "UI_Inventory";
    private readonly List<GameObject> _generatedRows = new();

    private HomeQuestNpc _questGiver;
    private RectTransform _rowsRoot;
    private TextMeshProUGUI _titleText;
    private TMP_FontAsset _font;
    private bool _subscribed;
    private Coroutine _localizationRefreshRoutine;

    // Purpose: Creates the runtime owned by this workflow.
    public static QuestMissionPanelUI CreateRuntime(Transform parent, int layer)
    {
        GameObject panelObject = new(
            "QuestMissionPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(QuestMissionPanelUI));

        panelObject.layer = layer;
        panelObject.transform.SetParent(parent, false);

        QuestMissionPanelUI panel = panelObject.GetComponent<QuestMissionPanelUI>();
        panel.BuildRuntimeView();
        return panel;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        BuildRuntimeView();
        SetVisible(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        Subscribe();
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RequestLocalizationRefresh();
        Refresh();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        EndConversation();
        Unsubscribe();
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_localizationRefreshRoutine != null)
        {
            StopCoroutine(_localizationRefreshRoutine);
            _localizationRefreshRoutine = null;
        }
    }

    // Purpose: Updates the quest giver state.
    public void SetQuestGiver(HomeQuestNpc questGiver)
    {
        if (_questGiver != questGiver) EndConversation();
        _questGiver = questGiver;
        Refresh();
    }

    public void EndConversation() => _questGiver?.EndConversation();

    public void SetVisible(bool visible)
    {
        if (visible && !gameObject.activeSelf) gameObject.SetActive(true);
        CanvasGroup group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        group.ignoreParentGroups = false;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        if (!visible) EndConversation();
    }

    private void AcceptQuest(QuestRuntime runtime, QuestDefinitionSO definition)
    {
        bool accepted = runtime.TryAccept(definition);
        _questGiver?.PlayChoiceResponse(accepted);
        if (!accepted) UIAudio.Request(UIAudioCueType.Denied);
    }

    // Purpose: Refreshes the displayed or cached refresh.
    public void Refresh()
    {
        BuildRuntimeView();
        ClearRows();

        QuestRuntime runtime = QuestRuntime.EnsureRuntime();
        _titleText.text = _questGiver != null
            ? Format("Inventory_MissionsWithNameFormat", "Missions - {0}", _questGiver.DisplayName)
            : Get("Inventory_MissionsTitle", "Missions");

        HashSet<QuestDefinitionSO> rendered = new();
        if (_questGiver != null)
        {
            QuestDefinitionSO[] offered = _questGiver.OfferedQuests;
            for (int i = 0; i < offered.Length; i++)
            {
                QuestDefinitionSO definition = offered[i];
                if (definition == null || !_questGiver.CanOffer(definition) || !rendered.Add(definition))
                    continue;

                QuestRuntimeState state = FindState(runtime, definition);
                AddQuestRow(runtime, definition, state, true);
            }
        }

        IReadOnlyList<QuestRuntimeState> states = runtime.States;
        for (int i = 0; i < states.Count; i++)
        {
            QuestRuntimeState state = states[i];
            QuestDefinitionSO definition = state != null ? state.Definition : null;
            if (definition == null || !rendered.Add(definition))
                continue;

            AddQuestRow(runtime, definition, state, false);
        }

        if (_generatedRows.Count == 0)
            AddLabel(Get("Inventory_MissionsEmpty", "No quests are available."), 22, new Color(0.78f, 0.88f, 0.9f, 1f));
    }

    // Purpose: Subscribes to subscribe notifications.
    private void Subscribe()
    {
        if (_subscribed)
            return;

        QuestRuntime runtime = QuestRuntime.EnsureRuntime();
        runtime.OnQuestStateChanged += Refresh;
        _subscribed = true;
    }

    // Purpose: Removes the unsubscribe notification subscription.
    private void Unsubscribe()
    {
        if (!_subscribed || QuestRuntime.Instance == null)
            return;

        QuestRuntime.Instance.OnQuestStateChanged -= Refresh;
        _subscribed = false;
    }

    // Purpose: Adds the requested quest row to this component.
    private void AddQuestRow(QuestRuntime runtime, QuestDefinitionSO definition, QuestRuntimeState state, bool fromGiver)
    {
        GameObject row = new(
            $"Quest_{definition.QuestId}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));

        row.layer = gameObject.layer;
        row.transform.SetParent(_rowsRoot, false);
        _generatedRows.Add(row);

        Image background = row.GetComponent<Image>();
        background.color = state != null && state.IsCompleted
            ? new Color(0.08f, 0.32f, 0.19f, 0.96f)
            : new Color(0.025f, 0.15f, 0.19f, 0.96f);

        VerticalLayoutGroup layout = row.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = row.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddLabel(definition.DisplayName, 25, Color.white, row.transform);

        QuestObjectiveDefinition[] objectives = definition.Objectives;
        for (int i = 0; i < objectives.Length; i++)
        {
            QuestObjectiveDefinition objective = objectives[i];
            if (objective == null)
                continue;

            int current = state != null && i < state.ObjectiveProgress.Count ? state.ObjectiveProgress[i] : 0;
            AddLabel(
                $"{objective.BuildSummary()}  {current}/{objective.RequiredCount}",
                16,
                new Color(0.65f, 0.94f, 0.95f, 1f),
                row.transform);
        }

        QuestRewardDefinition[] rewards = definition.Rewards;
        for (int i = 0; i < rewards.Length; i++)
        {
            QuestRewardDefinition reward = rewards[i];
            if (reward == null)
                continue;

            AddLabel(
                $"{Get("Inventory_MissionsReward", "Reward").ToUpperInvariant()}  {reward.BuildSummary()}",
                16,
                new Color(1f, 0.83f, 0.38f, 1f),
                row.transform);
        }

        if (rewards.Length == 0 && !string.IsNullOrWhiteSpace(definition.PersistentRewardSummary))
        {
            AddLabel(
                $"{Get("Inventory_MissionsReward", "Reward").ToUpperInvariant()}  {definition.PersistentRewardSummary}",
                16,
                new Color(1f, 0.83f, 0.38f, 1f),
                row.transform);
        }

        if (state == null && fromGiver)
        {
            bool canAccept = runtime.CanAccept(definition);
            AddButton(
                canAccept
                    ? Get("Inventory_MissionsAccept", "Accept").ToUpperInvariant()
                    : Get("Inventory_MissionsLocked", "Locked").ToUpperInvariant(),
                canAccept,
                () => AcceptQuest(runtime, definition),
                row.transform);
        }
        if (state != null && state.IsAccepted && !state.IsCompleted && fromGiver)
        {
            if (state.IsReadyToTurnIn) AddButton("임무 완료", true, () =>
            {
                runtime.TryTurnIn(definition, _questGiver, out string message);
                Debug.Log($"[Quest turn-in] {message}");
            }, row.transform);
            bool hasSubmission = false;
            for (int i = 0; i < objectives.Length; i++)
                if (objectives[i]?.ObjectiveType == QuestObjectiveType.SubmitItem &&
                    (i >= state.ObjectiveProgress.Count || state.ObjectiveProgress[i] < objectives[i].RequiredCount)) hasSubmission = true;
            if (hasSubmission) AddButton("필요 아이템 제출", true, () =>
            {
                runtime.TrySubmitItems(definition, out string message);
                Debug.Log($"[Quest submission] {message}");
            }, row.transform);
        }
    }

    // Purpose: Rebuilds dynamic labels after the selected locale and its table are ready.
    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RequestLocalizationRefresh();
    }

    private void RequestLocalizationRefresh()
    {
        if (_localizationRefreshRoutine != null)
            StopCoroutine(_localizationRefreshRoutine);

        _localizationRefreshRoutine = StartCoroutine(RefreshLocalizedTextWhenReady());
    }

    private IEnumerator RefreshLocalizedTextWhenReady()
    {
        yield return UIRuntimeLocalization.EnsureTableLoaded(StringTable, "Inventory_MissionsTitle");

        if (isActiveAndEnabled)
            Refresh();

        _localizationRefreshRoutine = null;
    }

    private static string Get(string key, string fallback)
    {
        return UIRuntimeLocalization.Get(StringTable, key, fallback);
    }

    private static string Format(string key, string fallback, params object[] arguments)
    {
        return UIRuntimeLocalization.Format(StringTable, key, fallback, arguments);
    }

    // Purpose: Adds the requested button to this component.
    private void AddButton(string label, bool interactable, UnityEngine.Events.UnityAction action, Transform parent)
    {
        GameObject buttonObject = new(
            label,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));

        buttonObject.layer = gameObject.layer;
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = interactable
            ? new Color(0.05f, 0.56f, 0.66f, 1f)
            : new Color(0.2f, 0.25f, 0.28f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.interactable = interactable;
        button.onClick.AddListener(action);

        LayoutElement element = buttonObject.GetComponent<LayoutElement>();
        element.preferredHeight = 36f;

        AddLabel(label, 17, Color.white, buttonObject.transform, TextAlignmentOptions.Center);
    }

    // Purpose: Adds the requested label to this component.
    private TextMeshProUGUI AddLabel(string text, float size, Color color, Transform parent = null, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        GameObject textObject = new("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(parent != null ? parent : _rowsRoot, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = _font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.enableWordWrapping = true;
        label.alignment = alignment;
        label.raycastTarget = false;

        LayoutElement element = textObject.GetComponent<LayoutElement>();
        element.minHeight = size + 6f;
        return label;
    }

    // Purpose: Builds the runtime view from the current authored data.
    private void BuildRuntimeView()
    {
        if (_rowsRoot != null)
            return;

        _font = TMP_Settings.defaultFontAsset;
        RectTransform root = transform as RectTransform;
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(270f, -10f);
        root.sizeDelta = new Vector2(560f, 600f);

        Image background = GetComponent<Image>();
        background.color = new Color(0.008f, 0.055f, 0.07f, 0.98f);

        GameObject titleObject = transform.Find("Title")?.gameObject ??
            new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObject.layer = gameObject.layer;
        titleObject.transform.SetParent(transform, false);
        RectTransform titleRect = (RectTransform)titleObject.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -15f);
        titleRect.sizeDelta = new Vector2(-28f, 44f);
        _titleText = titleObject.GetComponent<TextMeshProUGUI>();
        _titleText.font = _font;
        _titleText.fontSize = 28f;
        _titleText.color = Color.white;
        _titleText.alignment = TextAlignmentOptions.Left;

        GameObject rowsObject = transform.Find("Rows")?.gameObject ??
            new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rowsObject.layer = gameObject.layer;
        rowsObject.transform.SetParent(transform, false);
        _rowsRoot = (RectTransform)rowsObject.transform;
        _rowsRoot.anchorMin = Vector2.zero;
        _rowsRoot.anchorMax = Vector2.one;
        _rowsRoot.offsetMin = new Vector2(14f, 14f);
        _rowsRoot.offsetMax = new Vector2(-14f, -68f);

        VerticalLayoutGroup rowsLayout = rowsObject.GetComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 8f;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandHeight = false;
    }

    // Purpose: Clears the current rows state.
    private void ClearRows()
    {
        if (_rowsRoot != null)
        {
            // Refresh can be raised more than once in the same frame. Detach each
            // generated child before destroying it so the next refresh cannot keep
            // a pending-destroy row or the empty-list label in the layout.
            for (int i = _rowsRoot.childCount - 1; i >= 0; i--)
            {
                Transform row = _rowsRoot.GetChild(i);
                row.SetParent(null, false);
                Destroy(row.gameObject);
            }
        }

        _generatedRows.Clear();
    }

    // Purpose: Finds the requested state.
    private static QuestRuntimeState FindState(QuestRuntime runtime, QuestDefinitionSO definition)
    {
        IReadOnlyList<QuestRuntimeState> states = runtime.States;
        for (int i = 0; i < states.Count; i++)
        {
            QuestRuntimeState state = states[i];
            if (state != null && state.Definition == definition)
                return state;
        }

        return null;
    }
}
