using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class AudioGroundworkTests
{
    private readonly List<Object> _objects = new();
    private readonly List<AudioCueSO> _played = new();
    private readonly List<AudioCueKey> _stopped = new();
    private AudioCueEventChannelSO _channel;
    private AudioConfigurationSO _configuration;
    private object _previousUi;
    private float _previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        _previousUi = typeof(UIAudio).GetField("_active", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        typeof(UIAudio).GetField("_active", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
        _previousTimeScale = Time.timeScale;
        _channel = Asset<AudioCueEventChannelSO>();
        _configuration = Asset<AudioConfigurationSO>();
        _channel.OnAudioCuePlayRequested += (cue, configuration, position) =>
        {
            _played.Add(cue);
            return AudioCueKey.Create();
        };
        _channel.OnAudioCueStopRequested += key => { _stopped.Add(key); return true; };
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = _previousTimeScale;
        for (int i = _objects.Count - 1; i >= 0; i--)
            if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
        _objects.Clear();
        _played.Clear();
        _stopped.Clear();
        typeof(UIAudio).GetField("_active", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, _previousUi);
    }

    [TestCase(true, UIAudioCueType.Equip)]
    [TestCase(true, UIAudioCueType.InventoryMove)]
    [TestCase(true, UIAudioCueType.Unequip)]
    [TestCase(true, UIAudioCueType.Split)]
    [TestCase(false, UIAudioCueType.Equip)]
    public void UiCommand_QueuesOneResultInsteadOfButtonClick(bool success, UIAudioCueType result)
    {
        UIAudio ui = MakeUi();
        AudioCueSO good = Cue(), denied = Cue();
        foreach (string field in new[] { "_equipCue", "_inventoryMoveCue", "_unequipCue", "_splitCue" }) Set(ui, field, good);
        Set(ui, "_deniedCue", denied);
        ui.Play(UIAudioCueType.ButtonClick);
        Assert.That(UIAudio.ReportResult(success, result), Is.EqualTo(success));
        ui.Play(UIAudioCueType.ButtonClick);
        Call(ui, "LateUpdate");
        Call(ui, "LateUpdate");
        Assert.That(_played, Is.EqualTo(new[] { success ? good : denied }));
    }

    [Test]
    public void QuestOverride_ReplacesGenericClick_AndInvalidOverrideFallsBack()
    {
        UIAudio ui = MakeUi();
        AudioCueSO completion = Cue(), custom = Cue();
        Set(ui, "_questCompletedCue", completion);
        UIAudio.PlayQuestNotification(custom, true);
        ui.Play(UIAudioCueType.ButtonClick);
        Call(ui, "LateUpdate");
        AudioCueSO invalid = Asset<AudioCueSO>();
        UIAudio.PlayQuestNotification(invalid, true);
        Call(ui, "LateUpdate");
        Assert.That(_played, Is.EqualTo(new[] { custom, completion }));
    }

    [Test]
    public void EmptyOrLoopingUiCue_IsNotPlayed()
    {
        UIAudio ui = MakeUi();
        Set(ui, "_buttonClickCue", Asset<AudioCueSO>());
        ui.Play(UIAudioCueType.ButtonClick);
        Set(ui, "_equipCue", Cue(true));
        ui.Play(UIAudioCueType.Equip);
        Call(ui, "LateUpdate");
        Assert.That(_played, Is.Empty);
    }

    [Test]
    public void Button_BindsOnce_AndIgnoresRightClick()
    {
        GameObject go = Go("Button");
        Button button = go.AddComponent<Button>();
        UIButtonAudioEventEmitter emitter = go.AddComponent<UIButtonAudioEventEmitter>();
        UIAudioEventChannelSO channel = Asset<UIAudioEventChannelSO>();
        int clicks = 0;
        channel.OnEventRaised += cue => { if (cue == UIAudioCueType.ButtonClick) clicks++; };
        emitter.Bind(button, channel);
        emitter.Bind(button, channel);
        Call(emitter, "OnDisable");
        Call(emitter, "OnEnable");
        button.onClick.Invoke();
        emitter.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Right });
        Assert.That(clicks, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Search_OneLoop_StopsOnce_OnCancelOrCompletion(bool complete)
    {
        InteractionSearchAudio search = MakeSearch(out InteractionCastBarEventChannelSO cast);
        Transform actor = Go("Actor").transform;
        cast.RaiseEvent(Snapshot(actor, 0f));
        cast.RaiseEvent(Snapshot(actor, 0.5f));
        Assert.That(_played.Count, Is.EqualTo(2)); // start + one loop, not one loop per tick
        if (complete) cast.RaiseEvent(Snapshot(actor, 1f));
        cast.Clear();
        cast.Clear();
        Assert.That(_stopped.Count, Is.EqualTo(1));
        Assert.That(_played.Count, Is.EqualTo(3));
        Assert.That(_played[2], Is.SameAs(Get(search, complete ? "_completeCue" : "_cancelCue")));
    }

    [Test]
    public void Search_PauseStopsLoop_ResumeRestarts_AndDisableCleansUp()
    {
        InteractionSearchAudio search = MakeSearch(out InteractionCastBarEventChannelSO cast);
        Transform actor = Go("Actor").transform;
        Time.timeScale = 1f;
        cast.RaiseEvent(Snapshot(actor, 0f));
        Time.timeScale = 0f;
        Call(search, "Update");
        Call(search, "Update");
        Assert.That(_stopped.Count, Is.EqualTo(1));
        Time.timeScale = 1f;
        Call(search, "Update");
        Assert.That(_played.Count, Is.EqualTo(3));
        Call(search, "OnDisable");
        Assert.That(_stopped.Count, Is.EqualTo(2));
    }

    [Test]
    public void Search_DoesNotPlayForReloadOrOtherCasts()
    {
        MakeSearch(out InteractionCastBarEventChannelSO cast);
        Transform actor = Go("Actor").transform;
        cast.RaiseEvent(new InteractionCastBarSnapshot(true, InteractionType.Reload, actor, actor, 0, 1));
        cast.Clear();
        Assert.That(_played, Is.Empty);
    }

    [Test]
    public void Npc_RepeatedOpenCloseAndLineReplacement_DoNotStackVoices()
    {
        HomeQuestNpc npc = Go("Resident").AddComponent<HomeQuestNpc>();
        NpcAudioProfileSO profile = Asset<NpcAudioProfileSO>();
        profile.channel = _channel;
        profile.configuration = _configuration;
        profile.greeting = Cue(); profile.accept = Cue(); profile.farewell = Cue();
        Set(npc, "_conversationAudio", profile);
        npc.BeginConversation();
        npc.BeginConversation();
        npc.PlayChoiceResponse(true);
        npc.EndConversation();
        npc.EndConversation();
        Assert.That(_played, Is.EqualTo(new[] { profile.greeting, profile.accept, profile.farewell }));
        Assert.That(_stopped.Count, Is.EqualTo(2));
        Call(npc, "OnDisable");
        Assert.That(_stopped.Count, Is.EqualTo(3));
    }

    [Test]
    public void AuthoredMixer_ContainsExpectedGroupsAndParameters()
    {
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/Audio/Mixer/Master.mixer");
        Assert.That(mixer, Is.Not.Null);
        foreach (string name in new[] { "MasterVolume", "MusicVolume", "SFXVolume" })
            Assert.That(mixer.GetFloat(name, out _), Is.True, name);
        foreach (string name in new[] { "Music", "SFX", "UI", "Voice", "Ambience" })
            Assert.That(mixer.FindMatchingGroups(name).Length, Is.GreaterThan(0), name);
        foreach (string guid in AssetDatabase.FindAssets("t:AudioConfigurationSO", new[] { "Assets/ScriptableObject/Audio" }))
        {
            AudioConfigurationSO config = AssetDatabase.LoadAssetAtPath<AudioConfigurationSO>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(config.outputMixerGroup, Is.Not.Null, config.name);
        }
    }

    private UIAudio MakeUi()
    {
        UIAudio ui = Go("UI Audio").AddComponent<UIAudio>();
        Set(ui, "_sfxEventChannel", _channel);
        Set(ui, "_audioConfig", _configuration);
        Set(ui, "_buttonClickCue", Cue());
        Call(ui, "OnDisable"); Call(ui, "OnEnable");
        return ui;
    }

    private InteractionSearchAudio MakeSearch(out InteractionCastBarEventChannelSO cast)
    {
        InteractionSearchAudio search = Go("Search Audio").AddComponent<InteractionSearchAudio>();
        cast = Asset<InteractionCastBarEventChannelSO>();
        Set(search, "_castChannel", cast); Set(search, "_sfxChannel", _channel);
        Set(search, "_configuration", _configuration);
        Set(search, "_startCue", Cue()); Set(search, "_loopCue", Cue(true));
        Set(search, "_completeCue", Cue()); Set(search, "_cancelCue", Cue());
        Call(search, "OnDisable"); Call(search, "OnEnable");
        return search;
    }

    private static InteractionCastBarSnapshot Snapshot(Transform actor, float progress) =>
        new(true, InteractionType.Loot, actor, actor, progress, 2f);

    private AudioCueSO Cue(bool loop = false)
    {
        AudioClip clip = AudioClip.Create("Test Clip", 100, 1, 1000, false);
        _objects.Add(clip);
        AudioCueSO cue = Asset<AudioCueSO>();
        cue.looping = loop;
        Set(cue, "_audioClipGroups", new[] { new AudioClipsGroup { audioClips = new[] { clip } } });
        return cue;
    }

    private T Asset<T>() where T : ScriptableObject { T asset = ScriptableObject.CreateInstance<T>(); _objects.Add(asset); return asset; }
    private GameObject Go(string name) { GameObject go = new(name); _objects.Add(go); return go; }
    private static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
