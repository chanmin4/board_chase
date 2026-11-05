// Editor-only, opt-in observation. Never resets or changes a camera, scene, or save.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class CameraRuntimeTrace
{
    private const string Folder = "Library/CameraDiagnosis";
    private const string ArmedKey = "BoardChase.CameraTrace.Armed";
    private const string PathKey = "BoardChase.CameraTrace.Path";
    private static double _nextControl, _nextSample;
    private static int _sampleFrame = -1, _records;
    private static bool _writing;
    private static readonly Dictionary<int, int> LastBegin = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> LastEnd = new Dictionary<int, int>();

    static CameraRuntimeTrace()
    {
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += PlayState;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
        SceneManager.sceneLoaded += SceneLoaded;
        SceneManager.sceneUnloaded += SceneUnloaded;
    }

    [MenuItem("Tools/Diagnostics/Camera Trace/Start (read-only)")]
    private static void Start()
    {
        Directory.CreateDirectory(Folder);
        string path = Folder + "/trace-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".jsonl";
        SessionState.SetString(PathKey, path);
        SessionState.SetBool(ArmedKey, true);
        _records = 0; _nextSample = 0; LastBegin.Clear(); LastEnd.Clear();
        Write("armed", null);
        Debug.Log("[Camera Trace] Read-only capture armed. Reproduce without toggling the camera first. " + path);
    }

    [MenuItem("Tools/Diagnostics/Camera Trace/Snapshot (read-only)")]
    private static void Snapshot()
    {
        if (!SessionState.GetBool(ArmedKey, false)) Start();
        Write("manual-snapshot", null);
        foreach (Camera camera in Cameras()) Write("manual-camera", camera);
    }

    [MenuItem("Tools/Diagnostics/Camera Trace/Stop")]
    private static void Stop()
    {
        if (!SessionState.GetBool(ArmedKey, false)) return;
        Write("stopped", null);
        SessionState.SetBool(ArmedKey, false);
        Debug.Log("[Camera Trace] Capture stopped. " + SessionState.GetString(PathKey, ""));
    }

    private static void Update()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now >= _nextControl)
        {
            _nextControl = now + 0.5;
            string control = Folder + "/control";
            if (File.Exists(control))
            {
                string command = File.ReadAllText(control).Trim();
                File.Delete(control);
                if (command == "start") Start();
                else if (command == "snapshot") Snapshot();
                else if (command == "stop") Stop();
            }
        }
        if (!SessionState.GetBool(ArmedKey, false) || !EditorApplication.isPlaying ||
            EditorApplication.isCompiling || now < _nextSample) return;
        _nextSample = now + 0.5;
        _sampleFrame = Time.frameCount;
        Write("editor-update", null);
        foreach (Camera camera in Cameras()) Write("camera-update", camera);
        if (_records >= 6000) Stop(); // Bound the observation, even if the user leaves it armed.
    }

    private static void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!SessionState.GetBool(ArmedKey, false) || !Application.isPlaying || !IsRuntime(camera)) return;
        LastBegin[camera.GetInstanceID()] = Time.frameCount;
        if (Time.frameCount == _sampleFrame) Write("render-begin", camera);
    }

    private static void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!SessionState.GetBool(ArmedKey, false) || !Application.isPlaying || !IsRuntime(camera)) return;
        LastEnd[camera.GetInstanceID()] = Time.frameCount;
        if (Time.frameCount == _sampleFrame) Write("render-end", camera);
    }

    private static void PlayState(PlayModeStateChange state)
    { if (SessionState.GetBool(ArmedKey, false)) Write("play-state:" + state, null); }
    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    { if (SessionState.GetBool(ArmedKey, false)) Write("scene-loaded:" + scene.name, null); }
    private static void SceneUnloaded(Scene scene)
    { if (SessionState.GetBool(ArmedKey, false)) Write("scene-unloaded:" + scene.name, null); }

    private static Camera[] Cameras() => UnityEngine.Object.FindObjectsByType<Camera>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
    private static bool IsRuntime(Camera camera) => camera != null && camera.cameraType == CameraType.Game &&
        camera.gameObject.scene.IsValid() && camera.gameObject.scene.isLoaded;

    private static void Write(string phase, Camera camera)
    {
        if (_writing) return;
        string path = SessionState.GetString(PathKey, "");
        if (string.IsNullOrEmpty(path)) return;
        _writing = true;
        try
        {
            var record = new Record { phase = phase, utc = DateTime.UtcNow.ToString("o"),
                frame = Time.frameCount, playing = Application.isPlaying, timeScale = Time.timeScale,
                deltaTime = Time.deltaTime, pipeline = GraphicsSettings.currentRenderPipeline != null
                    ? GraphicsSettings.currentRenderPipeline.name : "Built-in" };
            var scenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++) scenes.Add(SceneManager.GetSceneAt(i).name);
            record.scenes = scenes.ToArray();
            record.mainCameraId = Camera.main != null ? Camera.main.GetInstanceID() : 0;
            record.screenCameraIds = Array.ConvertAll(Camera.allCameras, c => c.GetInstanceID());
            if (camera != null)
            {
                record.id = camera.GetInstanceID(); record.name = camera.name;
                record.scene = camera.gameObject.scene.name; record.type = camera.cameraType.ToString();
                record.enabled = camera.enabled; record.active = camera.gameObject.activeInHierarchy;
                record.position = camera.transform.position; record.rotation = camera.transform.eulerAngles;
                record.scale = camera.transform.lossyScale; record.fov = camera.fieldOfView;
                record.aspect = camera.aspect; record.depth = camera.depth;
                record.rect = camera.rect; record.display = camera.targetDisplay;
                record.texture = camera.targetTexture != null ? camera.targetTexture.name : "screen";
                record.orthographic = camera.orthographic; record.occlusion = camera.useOcclusionCulling;
                record.cullingMask = camera.cullingMask; record.clearFlags = camera.clearFlags.ToString();
                record.near = camera.nearClipPlane; record.far = camera.farClipPlane;
                LastBegin.TryGetValue(record.id, out record.lastRenderBegin);
                LastEnd.TryGetValue(record.id, out record.lastRenderEnd);
                record.view = camera.worldToCameraMatrix; record.projection = camera.projectionMatrix;
                record.nonJitteredProjection = camera.nonJitteredProjectionMatrix;
                record.culling = camera.cullingMatrix;
                record.expectedView = Matrix4x4.Scale(new Vector3(1, 1, -1)) *
                    Matrix4x4.TRS(camera.transform.position, camera.transform.rotation, Vector3.one).inverse;
                record.viewDifference = Difference(record.view, record.expectedView);
                var components = new List<string>();
                foreach (Behaviour component in camera.GetComponents<Behaviour>())
                    if (component != null) components.Add(component.GetType().FullName + ":" + component.enabled);
                record.components = components.ToArray();
            }
            var manager = UnityEngine.Object.FindFirstObjectByType<CameraManager>();
            if (manager != null)
            {
                record.managerEnabled = manager.isActiveAndEnabled;
                var follow = Field(manager, "followTarget") as Transform;
                var output = Field(manager, "mainCamera") as Camera;
                var target = Field(manager, "cameraTarget") as Transform;
                record.managerCameraId = output != null ? output.GetInstanceID() : 0;
                record.managerTarget = target != null ? target.name : "null";
                record.follow = follow != null ? follow.name : "null";
                if (follow != null)
                {
                    record.followPosition = follow.position;
                    if (camera != null) record.followViewport = camera.WorldToViewportPoint(follow.position);
                }
            }
            var loader = SceneLoader.Instance;
            if (loader != null)
            {
                record.loading = Field(loader, "_isLoading") is bool loading && loading;
                record.unloadingHome = loader.IsHomeEnvironmentUnloading;
            }
            File.AppendAllText(path, JsonUtility.ToJson(record) + Environment.NewLine);
            _records++;
        }
        catch (Exception error)
        {
            SessionState.SetBool(ArmedKey, false);
            Debug.LogWarning("[Camera Trace] Observation stopped: " + error.Message);
        }
        finally { _writing = false; }
    }

    private static object Field(object owner, string name) => owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner);
    private static float Difference(Matrix4x4 a, Matrix4x4 b)
    { float difference = 0; for (int i = 0; i < 16; i++) difference = Mathf.Max(difference, Mathf.Abs(a[i] - b[i])); return difference; }

    [Serializable] private sealed class Record
    {
        public string phase, utc, pipeline, name, scene, type, texture, clearFlags, follow, managerTarget;
        public string[] scenes, components;
        public int frame, id, mainCameraId, managerCameraId, display, cullingMask, lastRenderBegin, lastRenderEnd;
        public int[] screenCameraIds;
        public bool playing, enabled, active, orthographic, occlusion, managerEnabled, loading, unloadingHome;
        public float timeScale, deltaTime, fov, aspect, depth, near, far, viewDifference;
        public Vector3 position, rotation, scale, followPosition, followViewport;
        public Rect rect;
        public Matrix4x4 view, expectedView, projection, nonJitteredProjection, culling;
    }
}
