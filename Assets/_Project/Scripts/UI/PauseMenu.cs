using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseContent;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject firstSelected;
    [SerializeField] private GameObject settingsFirstSelected;
    [SerializeField] private GameObject settingsReturnSelected;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string pauseActionName = "Gameplay/Pause";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private TypingInput typingInput;

    private InputAction pauseAction;
    private bool ownsPauseAction;

    public bool IsPaused { get; private set; }
    public event Action<bool> PauseChanged;

    private void Awake()
    {
        ResolveTypingInput();

        if (pausePanel == null)
        {
            Debug.LogError("PauseMenu no tiene PausePanel asignado.");
            return;
        }

        pausePanel.SetActive(false);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ResolveTypingInput();
        ResolvePauseAction();
    }

    private void Start()
    {
        SetPaused(false);
    }

    private void OnDisable()
    {
        if (IsPaused)
        {
            SetPaused(false);
        }

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
            if (IsPaused && settingsPanel != null && settingsPanel.activeSelf)
            {
                ShowPauseMenu();
            }
            else
            {
                SetPaused(!IsPaused);
            }
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

    public void ShowSettings()
    {
        if (!IsPaused || settingsPanel == null)
        {
            return;
        }

        if (pauseContent != null)
        {
            pauseContent.SetActive(false);
        }

        settingsPanel.SetActive(true);
        Select(settingsFirstSelected);
    }

    public void ShowPauseMenu()
    {
        if (!IsPaused)
        {
            return;
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (pauseContent != null)
        {
            pauseContent.SetActive(true);
        }

        Select(settingsReturnSelected != null
            ? settingsReturnSelected
            : firstSelected);
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

    public void SetPaused(bool paused)
    {
        bool stateChanged = IsPaused != paused;
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (paused)
        {
            typingInput?.SuspendInput();
        }
        else
        {
            typingInput?.ResumeInput();
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (pauseContent != null)
        {
            pauseContent.SetActive(true);
        }

        if (paused)
        {
            Select(firstSelected);
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (stateChanged)
        {
            PauseChanged?.Invoke(IsPaused);
        }
    }

    private void ResolveTypingInput()
    {
        if (typingInput == null)
        {
            typingInput = FindFirstObjectByType<TypingInput>();
        }
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
