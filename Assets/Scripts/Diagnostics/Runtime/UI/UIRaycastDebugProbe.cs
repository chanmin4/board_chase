/*
 * Role hierarchy: Diagnostics > Runtime > UI
 * Primary responsibility: Implements the focused UIRaycastDebugProbe responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIRaycastDebugProbe : MonoBehaviour
{
    [SerializeField] private bool _logOnLeftClick = true;
    [SerializeField] private bool _logOnRightClick = true;
    [SerializeField, Min(1)] private int _maxHits = 20;

    private readonly List<RaycastResult> _results = new();

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        bool shouldLog =
            (_logOnLeftClick && mouse.leftButton.wasPressedThisFrame) ||
            (_logOnRightClick && mouse.rightButton.wasPressedThisFrame);

        if (!shouldLog)
            return;

        LogRaycast(mouse.position.ReadValue());
    }

    // Purpose: Performs the log raycast workflow for this component.
    private void LogRaycast(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogWarning("[UIRaycastDebug] EventSystem.current is null.");
            return;
        }

        _results.Clear();

        PointerEventData data = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };

        eventSystem.RaycastAll(data, _results);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[UIRaycastDebug] mouse={screenPosition}, hits={_results.Count}, currentInputModule={eventSystem.currentInputModule}");

        int count = Mathf.Min(_results.Count, _maxHits);
        for (int i = 0; i < count; i++)
        {
            RaycastResult hit = _results[i];
            GameObject go = hit.gameObject;

            Graphic graphic = go != null ? go.GetComponent<Graphic>() : null;

            sb.AppendLine($"#{i} obj={GetPath(go)}");
            sb.AppendLine($"    module={hit.module}, depth={hit.depth}, sortingOrder={hit.sortingOrder}, distance={hit.distance}");
            sb.AppendLine($"    graphic={graphic?.GetType().Name}, raycastTarget={(graphic != null && graphic.raycastTarget)}");
            AppendCanvasGroups(sb, go);
        }

        Debug.Log(sb.ToString(), this);
    }

    // Purpose: Performs the append canvas groups workflow for this component.
    private static void AppendCanvasGroups(StringBuilder sb, GameObject go)
    {
        if (go == null)
            return;

        Transform current = go.transform;
        while (current != null)
        {
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group != null)
            {
                sb.AppendLine(
                    $"    CanvasGroup at {GetPath(current.gameObject)}: " +
                    $"alpha={group.alpha}, interactable={group.interactable}, " +
                    $"blocksRaycasts={group.blocksRaycasts}, ignoreParentGroups={group.ignoreParentGroups}");
            }

            current = current.parent;
        }
    }

    // Purpose: Returns the current path.
    private static string GetPath(GameObject go)
    {
        if (go == null)
            return "null";

        Stack<string> names = new Stack<string>();
        Transform current = go.transform;

        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }
}
