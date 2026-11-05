using UnityEditor;
using UnityEngine;

public sealed partial class ItemMakerWindow
{
    private MakerRegistrationIndex.Filter _registrationFilter;
    private GameObject _registrationPrefab;
    private bool _showPrefabRegistration;
    private double _registrationRepaintAt;

    private void OnInspectorUpdate()
    {
        if (EditorApplication.timeSinceStartup < _registrationRepaintAt) return;
        _registrationRepaintAt = EditorApplication.timeSinceStartup + 1;
        Repaint();
    }

    private void DrawRegistrationFilters()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            _registrationFilter = (MakerRegistrationIndex.Filter)EditorGUILayout.Popup("루팅 등록 여부", (int)_registrationFilter, MakerRegistrationIndex.FilterLabels);
            if (GUILayout.Button("등록 새로고침", GUILayout.Width(100))) { MakerRegistrationIndex.Invalidate(); Repaint(); }
        }
        _showPrefabRegistration = EditorGUILayout.Foldout(_showPrefabRegistration, "프리팹 → 연결된 Item SO 확인", true);
        if (_showPrefabRegistration)
        {
            _registrationPrefab = (GameObject)EditorGUILayout.ObjectField("확인할 프리팹", _registrationPrefab, typeof(GameObject), false);
            MakerRegistrationUi.PrefabSummary(_registrationPrefab);
        }
        EditorGUILayout.LabelField("루팅: 상자·적 드롭·공통 귀중품 목록 기준. 상점·퀘스트 사용은 별도 표시합니다.", EditorStyles.wordWrappedMiniLabel);
    }
}
