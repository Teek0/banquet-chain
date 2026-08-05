using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MenuPanelsUI : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject mainFirstSelected;
    [SerializeField] private GameObject settingsFirstSelected;

    private void Start()
    {
        ShowMain();
    }

    public void ShowMain()
    {
        ShowPanel(true, false, mainFirstSelected);
    }

    public void ShowSettings()
    {
        ShowPanel(false, true, settingsFirstSelected);
    }

    private void ShowPanel(
        bool showMain,
        bool showSettings,
        GameObject firstSelected
    )
    {
        if (mainPanel == null || settingsPanel == null)
        {
            Debug.LogError("MenuPanelsUI necesita MainPanel y SettingsPanel.");
            return;
        }

        mainPanel.SetActive(showMain);
        settingsPanel.SetActive(showSettings);

        if (EventSystem.current == null || firstSelected == null)
        {
            Debug.LogWarning(
                "MenuPanelsUI no pudo seleccionar el primer control."
            );
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
}