/*
 * Role hierarchy: Presentation > UI > Gameplay > EnemyBarUI > InfectionCastBar
 * Primary responsibility: Presents EnemyInfectionCastBarWidget state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.UI;

public class EnemyInfectionCastBarWidget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private RectTransform _fillRect;
    [SerializeField] private Image _fillImage;
    [SerializeField] private UICanvasGroupOpacity _uicanvasgroupopacity;

    private Enemy _enemy;
    private EnemyLootInventoryRuntime _lootInventory;
    private float _fillFullWidth;
    private bool _managerVisible = true;
    public RectTransform Root => _root != null ? _root : (RectTransform)transform;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_root == null)
            _root = (RectTransform)transform;

        if (_fillRect == null && _fillImage != null)
            _fillRect = _fillImage.rectTransform;

        if (_fillRect != null)
        {
            _fillFullWidth = _fillRect.sizeDelta.x;
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.anchorMin = new Vector2(0f, 0.5f);
            _fillRect.anchorMax = new Vector2(0f, 0.5f);
        }

        EnsureFillImageMode();
        RefreshImmediate();
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(EnemyScreenSpaceHPUIAnchor anchor)
    {
        _enemy = anchor != null ? anchor.Enemy : null;
        _lootInventory =
            anchor != null
                ? anchor.GetComponent<EnemyLootInventoryRuntime>() ??
                  anchor.GetComponentInParent<EnemyLootInventoryRuntime>() ??
                  anchor.GetComponentInChildren<EnemyLootInventoryRuntime>(true)
                : null;
        RefreshImmediate();
    }

    // Purpose: Updates the screen position state.
    public void SetScreenPosition(Vector2 anchoredPosition)
    {
        Root.anchoredPosition = anchoredPosition;
    }

    // Purpose: Processes one update step for visual state.
    public void TickVisualState()
    {
        if (_enemy == null || _uicanvasgroupopacity == null)
            return;

        if (!_managerVisible)
        {
            _uicanvasgroupopacity.Hide();
            RefreshImmediate();
            return;
        }

        bool visibleByEnemyCast = _enemy != null && _enemy.IsInfectionCasting;
        bool visibleByLootCast = _lootInventory != null && _lootInventory.IsInteractionCasting;

        if (!visibleByEnemyCast && !visibleByLootCast)
        {
            _uicanvasgroupopacity.Hide();
            RefreshImmediate();
            return;
        }

        _uicanvasgroupopacity.Show();
        RefreshImmediate();
    }

    // Purpose: Refreshes the displayed or cached immediate.
    private void RefreshImmediate()
    {
        float normalized =
            _lootInventory != null && _lootInventory.IsInteractionCasting
                ? _lootInventory.InteractionCastProgress01
                : _enemy != null ? _enemy.InfectionCastProgress01 : 0f;

        if (_fillImage != null)
        {
            EnsureFillImageMode();
            _fillImage.fillAmount = normalized;
        }

        if (_fillRect != null)
        {
            Vector2 size = _fillRect.sizeDelta;
            size.x = _fillFullWidth * normalized;
            _fillRect.sizeDelta = size;
        }
    }


    // Purpose: Ensures fill image mode is ready before the workflow continues.
    private void EnsureFillImageMode()
    {
        if (_fillImage == null)
            return;

        if (_fillImage.type != Image.Type.Filled)
            _fillImage.type = Image.Type.Filled;

        if (_fillImage.fillMethod != Image.FillMethod.Horizontal)
            _fillImage.fillMethod = Image.FillMethod.Horizontal;

        if (_fillImage.fillOrigin != (int)Image.OriginHorizontal.Left)
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }
    // Purpose: Updates the manager visible state.
    public void SetManagerVisible(bool visible)
    {
        _managerVisible = visible;

        if (_uicanvasgroupopacity == null)
            return;

        if (!visible)
            _uicanvasgroupopacity.Hide();
    }
}
