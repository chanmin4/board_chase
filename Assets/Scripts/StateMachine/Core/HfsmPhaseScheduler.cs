using System.Collections.Generic;
using UnityEngine;

namespace VSplatter.StateMachine
{
    /// <summary>
    /// 모든 호스트가 공유하는 프레임 단계 서비스. 캐릭터 프리팹에는 드라이버를 붙이지 않습니다.
    /// Unity의 기존 -200 / 0 / +200 슬롯을 유지하며 캐릭터별 Update/Coroutine을 새로 늘리지 않습니다.
    /// </summary>
    internal static class HfsmPhaseScheduler
    {
        private static readonly List<HfsmStateMachine> Hosts = new();
        private static readonly List<HfsmStateMachine> Snapshot = new();
        private static GameObject _service;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            if (_service != null) { _service.SetActive(false); Object.Destroy(_service); }
            Hosts.Clear(); Snapshot.Clear(); _service = null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterLoadedHosts()
        {
            // Domain/Scene Reload를 꺼도 기존 씬 객체를 빠뜨리지 않습니다. 비활성 객체는 OnEnable 때 등록합니다.
            foreach (var host in Object.FindObjectsByType<HfsmStateMachine>(FindObjectsSortMode.None))
                if (host.isActiveAndEnabled) Register(host);
        }
        public static void Register(HfsmStateMachine host)
        {
            if (!Application.isPlaying) return;
            if (!Hosts.Contains(host)) Hosts.Add(host);
            if (_service != null) return;
            _service = new GameObject("HFSM frame phases") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(_service);
            _service.AddComponent<HfsmGatePhase>(); _service.AddComponent<HfsmBehaviourPhase>();
        }
        public static void Unregister(HfsmStateMachine host) => Hosts.Remove(host);
        public static void Gate()
        {
            Snapshot.Clear(); Snapshot.AddRange(Hosts);
            foreach (var host in Snapshot) if (host != null && host.isActiveAndEnabled) host.RefreshActiveScopes();
        }
        public static void Behaviours(float deltaTime, bool startOnly = false)
        {
            Snapshot.Clear(); Snapshot.AddRange(Hosts);
            foreach (var host in Snapshot)
                if (host != null && host.isActiveAndEnabled)
                { if (startOnly) host.StartInnerScopes(); else host.TickInnerScopes(deltaTime); }
        }
    }
}
