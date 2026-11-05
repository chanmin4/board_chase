using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine
{
    /// <summary>한 계층 실행 단위의 읽기 전용 상태. Unity 컴포넌트 수와 계층 수는 무관합니다.</summary>
    public interface IHfsmScope
    {
        string Label { get; }
        int Level { get; }
        TransitionTableSO Table { get; }
        HfsmRuntime Runtime { get; }
        StateSO CurrentStateSO { get; }
        StateSO PreviousStateSO { get; }
        float CurrentStateEnteredTime { get; }
        float CurrentStateElapsedSeconds { get; }
        int LastTransitionFrame { get; }
        bool IsScopeActive { get; }
    }
}
