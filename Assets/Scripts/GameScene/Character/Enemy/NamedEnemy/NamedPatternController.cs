using UnityEngine;

public enum NamedPatternResult
{
    None,
    PlayerSucceeded,
    PlayerFailed
}

public class NamedPatternController : MonoBehaviour
{
    [Header("Runtime")]
    public bool patternReady;
    public bool prepareFinished;
    public bool activeFinished;
    public bool resolved;
    public bool sequenceFinished;
    public NamedPatternResult result = NamedPatternResult.None;

    public void BeginPattern()
    {
        patternReady = false;
        prepareFinished = false;
        activeFinished = false;
        resolved = false;
        sequenceFinished = false;
        result = NamedPatternResult.None;
    }

    public void MarkPrepareFinished()
    {
        prepareFinished = true;
    }

    public void MarkActiveFinished(NamedPatternResult patternResult)
    {
        result = patternResult;
        activeFinished = true;
    }

    public void MarkResolved()
    {
        resolved = true;
        sequenceFinished = true;
    }

    public void SetPatternReady(bool ready)
    {
        patternReady = ready;
    }
}
