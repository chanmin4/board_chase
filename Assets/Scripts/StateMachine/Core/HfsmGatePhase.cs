using UnityEngine;
namespace VSplatter.StateMachine
{
    /// <summary>공용 phase 0: 기존 Coordinator의 연결 판정 시점. 저장되는 캐릭터 컴포넌트가 아닙니다.</summary>
    [DefaultExecutionOrder(0), AddComponentMenu("")]
    public sealed class HfsmGatePhase : MonoBehaviour
    {
        private void Start() => HfsmPhaseScheduler.Gate();
        private void Update() => HfsmPhaseScheduler.Gate();
    }
}
