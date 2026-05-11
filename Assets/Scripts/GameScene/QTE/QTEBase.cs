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

    [SerializeField] private KeyCode _keyboardCancelKey = KeyCode.E;

    protected Action<QTEResult> OnComplete;
    protected bool IsRunning;

    public bool Running => IsRunning;

    public abstract void Begin(Action<QTEResult> onComplete);

    public virtual void Cancel()
    {
        Finish(QTEResult.Cancelled);
    }

    protected void BeginInternal(Action<QTEResult> onComplete)
    {
        OnComplete = onComplete;
        IsRunning = true;
        gameObject.SetActive(true);
    }

    protected bool IsKeyboardCancelPressed()
    {
        return _allowKeyboardCancelForTesting &&
               Input.GetKeyDown(_keyboardCancelKey);
    }

    protected void Finish(QTEResult result)
    {
        if (!IsRunning)
            return;

        IsRunning = false;

        Action<QTEResult> callback = OnComplete;
        OnComplete = null;

        gameObject.SetActive(false);
        callback?.Invoke(result);
    }
}
