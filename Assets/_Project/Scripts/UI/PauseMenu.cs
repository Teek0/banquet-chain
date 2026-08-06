using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject firstSelected;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string pauseActionName = "Gameplay/Pause";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private InputAction pauseAction;
    private bool ownsPauseAction;

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

    private void OnEnable()
    {
        ResolvePauseAction();
    }

    private void Start()
    {
        SetPaused(false);
    }

    private void OnDisable()
    {
        if (ownsPauseAction && pauseAction != null)
        {
            pauseAction.Disable();
        }

        ownsPauseAction = false;
        pauseAction = null;
    }

    private void Update()
    {
        bool pausePressed = pauseAction != null
            && pauseAction.WasPressedThisFrame();
        bool keyboardPausePressed = Keyboard.current != null
            && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (pausePressed || keyboardPausePressed)
        {
            SetPaused(!IsPaused);
        }
    }

    private void ResolvePauseAction()
    {
        if (inputActions == null)
        {
            Debug.LogError("PauseMenu necesita un InputActionAsset asignado.");
            return;
        }

        pauseAction = inputActions.FindAction(
            pauseActionName,
            false
        );

        if (pauseAction == null)
        {
            Debug.LogError(
                $"PauseMenu no encontró la acción '{pauseActionName}'."
            );
            return;
        }

        if (!pauseAction.enabled)
        {
            pauseAction.Enable();
            ownsPauseAction = true;
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
