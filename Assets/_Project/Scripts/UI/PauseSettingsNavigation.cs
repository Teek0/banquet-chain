using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PauseSettingsNavigation : MonoBehaviour
{
    [SerializeField] private RectTransform pauseContent;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject pauseSelection;
    [SerializeField] private GameObject settingsSelection;

    public void ShowSettings()
    {
        if (pauseContent == null || settingsPanel == null)
        {
            Debug.LogError(
                "PauseSettingsNavigation necesita PauseContent y SettingsPanel.",
                this
            );
            return;
        }

        pauseContent.gameObject.SetActive(false);
        settingsPanel.SetActive(true);
        Select(settingsSelection);
    }

    public void ShowPauseMenu()
    {
        if (pauseContent == null || settingsPanel == null)
        {
            Debug.LogError(
                "PauseSettingsNavigation necesita PauseContent y SettingsPanel.",
                this
            );
            return;
        }

        settingsPanel.SetActive(false);
        pauseContent.gameObject.SetActive(true);
        Select(pauseSelection);
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
}
