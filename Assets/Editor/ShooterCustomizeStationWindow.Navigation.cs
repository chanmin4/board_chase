using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Scene 스타일 미리보기 카메라. 외형 데이터/씬/Undo에는 손대지 않는다.</summary>
public sealed partial class ShooterCustomizeStationWindow
{
    private readonly HashSet<KeyCode> _flightKeys = new();
    private int _navigationControl;
    private int _navigationButton = -1;
    private bool _flightFast;
    private double _lastNavigationTick;

    private void OnLostFocus() => StopPreviewNavigation();

    private void StopPreviewNavigation()
    {
        if (_navigationControl != 0 && GUIUtility.hotControl == _navigationControl) GUIUtility.hotControl = 0;
        _navigationControl = 0; _navigationButton = -1; _flightKeys.Clear(); _flightFast = false;
        _lastNavigationTick = EditorApplication.timeSinceStartup;
    }

    private void FramePreview()
    {
        StopPreviewNavigation(); _preview?.Frame(); _zoom = 1; _pan = Vector3.zero; Repaint();
    }

    private void SetPreviewView(Vector2 orbit) { FramePreview(); _orbit = orbit; }

    private static bool IsFlightKey(KeyCode key) => key == KeyCode.W || key == KeyCode.A || key == KeyCode.S ||
        key == KeyCode.D || key == KeyCode.Q || key == KeyCode.E;

    private void TickPreviewNavigation()
    {
        double now = EditorApplication.timeSinceStartup;
        float dt = Mathf.Clamp((float)(now - _lastNavigationTick), 0, .05f); _lastNavigationTick = now;
        // hotControl is GUI-view-specific; reading it during EditorApplication.update can return
        // another view's control. MouseUp releases it inside OnGUI, while focus is safe to check here.
        if (_navigationButton >= 0 && focusedWindow != this)
        { StopPreviewNavigation(); return; }
        if (_navigationButton != 1 || _preview?.Root == null || _flightKeys.Count == 0) return;
        // Key input is captured only during RMB navigation; releasing RMB/focus prevents stuck movement.
        Quaternion rotation = Quaternion.Euler(_orbit.y, _orbit.x, 0);
        Vector3 direction = rotation * new Vector3((_flightKeys.Contains(KeyCode.D) ? 1 : 0) - (_flightKeys.Contains(KeyCode.A) ? 1 : 0),
            0, (_flightKeys.Contains(KeyCode.W) ? 1 : 0) - (_flightKeys.Contains(KeyCode.S) ? 1 : 0));
        direction += Vector3.up * ((_flightKeys.Contains(KeyCode.E) ? 1 : 0) - (_flightKeys.Contains(KeyCode.Q) ? 1 : 0));
        float speed = Mathf.Max(.25f, _preview.FramingBounds.size.magnitude * .45f) * (_flightFast ? 3 : 1);
        _pan += Vector3.ClampMagnitude(direction, 1) * speed * dt; Repaint();
    }

    private void HandlePreviewInput(Rect rect)
    {
        Event e = Event.current;
        int id = GUIUtility.GetControlID("ShooterSceneNavigation".GetHashCode(), FocusType.Keyboard, rect);
        bool hovered = rect.Contains(e.mousePosition);
        if (_preview?.Root == null) return;
        if (e.type == EventType.ScrollWheel && hovered)
        { _zoom = Mathf.Clamp(_zoom * Mathf.Exp(-e.delta.y * .06f), .45f, 3.5f); e.Use(); Repaint(); }
        if (e.type == EventType.MouseDown && hovered && (e.button == 1 || e.button == 2 || e.button == 0 && e.alt))
        {
            StopPreviewNavigation(); GUIUtility.hotControl = GUIUtility.keyboardControl = id;
            EditorGUIUtility.editingTextField = false;
            _navigationControl = id; _navigationButton = e.button; _flightFast = e.shift;
            _lastNavigationTick = EditorApplication.timeSinceStartup; Focus(); e.Use();
        }
        if (e.type == EventType.MouseDrag && GUIUtility.hotControl == id)
        {
            _flightFast = e.shift;
            Quaternion oldRotation = Quaternion.Euler(_orbit.y, _orbit.x, 0);
            if (_navigationButton == 0 || _navigationButton == 1)
            {
                // Alt+LMB orbits around the character; RMB looks from the current camera position.
                _orbit.x += e.delta.x * .4f; _orbit.y = Mathf.Clamp(_orbit.y + e.delta.y * .4f, -89, 89);
                if (_navigationButton == 1)
                    _pan += (Quaternion.Euler(_orbit.y, _orbit.x, 0) * Vector3.forward - oldRotation * Vector3.forward) * _preview.ViewDistance(rect, _zoom);
            }
            else
            {
                float distance = _preview.ViewDistance(rect, _zoom);
                float scale = 2 * distance * Mathf.Tan(15 * Mathf.Deg2Rad) / Mathf.Max(1, rect.height);
                _pan += (oldRotation * (-Vector3.right * e.delta.x + Vector3.up * e.delta.y)) * scale;
            }
            e.Use(); Repaint();
        }
        if (e.type == EventType.MouseUp && _navigationButton >= 0 && e.button == _navigationButton)
        { StopPreviewNavigation(); e.Use(); }
        if (e.type != EventType.KeyDown && e.type != EventType.KeyUp) return;
        if (_navigationButton == 1 && GUIUtility.hotControl == id)
        {
            _flightFast = e.shift;
            if (IsFlightKey(e.keyCode))
            { if (e.type == EventType.KeyDown) _flightKeys.Add(e.keyCode); else _flightKeys.Remove(e.keyCode); e.Use(); }
            else if (e.keyCode == KeyCode.LeftShift || e.keyCode == KeyCode.RightShift) e.Use();
            else if (e.keyCode == KeyCode.Escape) { StopPreviewNavigation(); e.Use(); }
        }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && hovered && !EditorGUIUtility.editingTextField && !e.control && !e.command && !e.alt)
        { FramePreview(); GUIUtility.keyboardControl = id; e.Use(); }
    }
}
