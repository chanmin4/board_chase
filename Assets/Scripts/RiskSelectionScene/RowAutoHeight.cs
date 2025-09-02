// RowAutoHeight.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class RowAutoHeight : MonoBehaviour
{
    public float minHeight = 72f;
    public float extraPadding = 0f;

    LayoutElement _le; RectTransform _rt;

    void Awake(){ _le = GetComponent<LayoutElement>(); _rt = (RectTransform)transform; }
    void LateUpdate()
    {
        float maxH = minHeight;
        for (int i = 0; i < _rt.childCount; i++)
        {
            var c = _rt.GetChild(i) as RectTransform;
            if (!c || !c.gameObject.activeInHierarchy) continue;
            // 자식 셀은 VerticalLayoutGroup+Fitter로 이미 preferred가 계산됨
            var h = Mathf.Max(c.rect.height, LayoutUtility.GetPreferredHeight(c));
            if (h > maxH) maxH = h;
        }
        _le.preferredHeight = maxH + extraPadding;
    }
}
