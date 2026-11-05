using UnityEngine;
namespace VSplatter.StateMachine
{
    /// <summary>공용 phase +200: 기존 Child Update 시점. 호스트들은 컴포넌트 하나로 이 단계를 공유합니다.</summary>
    [DefaultExecutionOrder(200), AddComponentMenu("")]
    public sealed class HfsmBehaviourPhase : MonoBehaviour
    {
        private void Start() => HfsmPhaseScheduler.Behaviours(0, true);
        private void Update() => HfsmPhaseScheduler.Behaviours(Time.deltaTime);
    }
}
