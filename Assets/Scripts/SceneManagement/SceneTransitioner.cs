namespace PixeLadder.EasyTransition
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// A singleton manager that controls the entire scene transition process.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance;

        [Header("Configuration")]
        [Tooltip("The screen-covering Image prefab used for transitions.")]
        [SerializeField] private Image transitionImagePrefab;

        [Tooltip("The default transition effect to use if none is provided in the LoadScene call.")]
        [SerializeField] private TransitionEffect defaultTransition;

        // --- Private State ---
        private Image transitionImageInstance;
        private bool isTransitioning = false;

        // Cache shader property ID for performance
        private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");

        public static event System.Action OnSceneLoaded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Create a dedicated, persistent canvas for the transition UI.
            GameObject canvasGO = new GameObject("TransitionCanvas");
            canvasGO.transform.SetParent(this.transform);

            var transitionCanvas = canvasGO.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 999;

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            transitionImageInstance = Instantiate(transitionImagePrefab, canvasGO.transform);

            // Ensure the image stretches to fill the screen
            RectTransform rectT = transitionImageInstance.rectTransform;
            rectT.anchorMin = Vector2.zero;
            rectT.anchorMax = Vector2.one;
            rectT.sizeDelta = Vector2.zero;
            rectT.anchoredPosition = Vector2.zero;

            transitionImageInstance.gameObject.SetActive(false);
        }

        /// <summary>
        /// The main public method to start a scene transition.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="effect">The TransitionEffect ScriptableObject defining the visuals.</param>
        public void LoadScene(string sceneName, TransitionEffect effect = null)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("SceneTransitioner: Transition already in progress.");
                return;
            }

            var effectToUse = effect ?? defaultTransition;
            if (effectToUse == null)
            {
                Debug.LogError("SceneTransitioner: No transition effect specified and no default is set.", this);
                return;
            }

            StartCoroutine(TransitionRoutine(sceneName, effectToUse));
        }

        private IEnumerator TransitionRoutine(string sceneName, TransitionEffect effect)
        {
            isTransitioning = true;
            transitionImageInstance.gameObject.SetActive(true);

            // 1. Create a fresh instance of the material for this specific transition
            Material materialInstance = new Material(effect.transitionMaterial);

            // 2. CRITICAL FIX: Pass the RectSize (Aspect Ratio) to the shader immediately
            Rect rect = transitionImageInstance.rectTransform.rect;
            materialInstance.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0, 0));

            // 3. Apply custom effect properties
            effect.SetEffectProperties(materialInstance);

            // 4. Assign the material to the image
            transitionImageInstance.material = materialInstance;

            // Run the fade-out animation
            yield return effect.AnimateOut(transitionImageInstance);

            // Load the new scene
            yield return SceneManager.LoadSceneAsync(sceneName);

            // Fire event
            OnSceneLoaded?.Invoke();

            // Run the fade-in animation
            yield return effect.AnimateIn(transitionImageInstance);

            // Cleanup
            transitionImageInstance.gameObject.SetActive(false);
            Destroy(materialInstance); // Clean up the material instance to prevent leaks
            isTransitioning = false;
        }
    }
}