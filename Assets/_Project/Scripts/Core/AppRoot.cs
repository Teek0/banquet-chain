using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class AppRoot : MonoBehaviour
{
    public static AppRoot Instance { get; private set; }

    [Header("Servicios")]
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private AudioSettings audioSettings;

    [Header("Arranque")]
    [SerializeField] private string bootSceneName = "Boot";
    [SerializeField] private string firstSceneName = "MainMenu";
    [SerializeField, Min(0f)] private float bootFadeDuration = 1.1f;
    [SerializeField] private int targetFrameRate = 60;

    public SceneLoader SceneLoader => sceneLoader;
    public AudioSettings AudioSettings => audioSettings;

    private void Reset()
    {
        sceneLoader = GetComponent<SceneLoader>();
        audioSettings = GetComponent<AudioSettings>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (targetFrameRate > 0)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        if (sceneLoader == null)
        {
            Debug.LogError("AppRoot no tiene SceneLoader asignado.");
            return;
        }

        if (SceneManager.GetActiveScene().name == bootSceneName)
        {
            sceneLoader.LoadScene(firstSceneName, bootFadeDuration);
            return;
        }

        sceneLoader.RevealCurrentScene();
    }
}
