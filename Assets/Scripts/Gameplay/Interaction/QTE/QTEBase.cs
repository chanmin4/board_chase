/*
 * Role hierarchy: Gameplay > Interaction > QTE
 * Primary responsibility: Implements the focused QTEType responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum QTEType
{
    KeyPrompt,
    GaugeMash,
    Timing
}

public enum QTEResult
{
    Cancelled,
    Fail,
    Success
}

public abstract class QTEBase : MonoBehaviour
{
    [Header("Debug Cancel")]
    [Tooltip("For keyboard-only testing. In production, interaction code should call Cancel() directly.")]
    [SerializeField] private bool _allowKeyboardCancelForTesting = false;
    [Header("Input Guard")]
    [SerializeField, Min(0)] private int _inputIgnoreFramesAfterBegin = 1;    [SerializeField] private KeyCode _keyboardCancelKey = KeyCode.E;

    [Tooltip("Prevents the same input frame that opened this QTE from instantly cancelling it.")]
    [SerializeField, Min(0)] private int _cancelInputIgnoreFrames = 1;

    protected Action<QTEResult> OnComplete;
    protected bool IsRunning;
    
    private int _startedFrame;

    public bool Running => IsRunning;

    // Purpose: Begins the begin workflow.
    public abstract void Begin(Action<QTEResult> onComplete);

    // Purpose: Checks whether cancel is currently permitted.
    public virtual void Cancel()
    {
        Finish(QTEResult.Cancelled);
    }

    // Purpose: Begins the internal workflow.
    protected void BeginInternal(Action<QTEResult> onComplete)
    {
        OnComplete = onComplete;
        IsRunning = true;
        _startedFrame = Time.frameCount;
    }
    // Purpose: Performs the should ignore input this frame workflow for this component.
    protected bool ShouldIgnoreInputThisFrame()
    {
        return Time.frameCount <= _startedFrame + _inputIgnoreFramesAfterBegin;
    }
    // Purpose: Checks whether keyboard cancel pressed is currently true.
    protected bool IsKeyboardCancelPressed()
    {
        if (!_allowKeyboardCancelForTesting)
            return false;

        if (Time.frameCount <= _startedFrame + _cancelInputIgnoreFrames)
            return false;

        return Input.GetKeyDown(_keyboardCancelKey);
    }

    // Purpose: Finishes the active finish workflow.
    protected void Finish(QTEResult result)
    {
        if (!IsRunning)
            return;

        IsRunning = false;

        Action<QTEResult> callback = OnComplete;
        OnComplete = null;
        callback?.Invoke(result);
    }
}
