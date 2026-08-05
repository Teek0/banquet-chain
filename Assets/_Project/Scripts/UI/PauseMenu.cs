using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject firstSelected;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string pauseActionName = "Gameplay/Pause";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private InputAction pauseAction;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (pausePanel == null)
        {
            Debug.LogError("PauseMenu no tiene PausePanel asignado.");
            return;
        }

        pausePanel.SetActive(false);
    }

    private void Start()
    {
        ResolvePauseAction();
        SetPaused(false);
    }

    private void Update()
    {
        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            SetPaused(!IsPaused);
        }
    }

    private void ResolvePauseAction()
    {
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogError("PauseMenu necesita un PlayerInput en la escena.");
            return;
        }

        pauseAction = playerInput.actions.FindAction(
            pauseActionName,
            false
        );

        if (pauseAction == null)
        {
            Debug.LogError(
                $"PauseMenu no encontró la acción '{pauseActionName}'."
            );
        }
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void RestartScene()
    {
        SetPaused(false);

        if (AppRoot.Instance == null
            || AppRoot.Instance.SceneLoader == null)
        {
            Debug.LogError("PauseMenu necesita SceneLoader desde AppRoot.");
            return;
        }

        AppRoot.Instance.SceneLoader.ReloadCurrentScene();
    }

    public void ReturnToMainMenu()
    {
        SetPaused(false);

        if (AppRoot.Instance == null
            || AppRoot.Instance.SceneLoader == null)
        {
            Debug.LogError("PauseMenu necesita SceneLoader desde AppRoot.");
            return;
        }

        AppRoot.Instance.SceneLoader.LoadScene(mainMenuSceneName);
    }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);

            if (paused && firstSelected != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected);
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
