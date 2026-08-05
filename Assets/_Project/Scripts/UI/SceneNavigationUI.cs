using UnityEngine;

public sealed class SceneNavigationUI : MonoBehaviour
{
    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadScene(string sceneName)
    {
        if (AppRoot.Instance == null
            || AppRoot.Instance.SceneLoader == null)
        {
            Debug.LogError(
                "SceneNavigationUI necesita SceneLoader desde AppRoot. Inicia desde Boot."
            );
            return;
        }

        AppRoot.Instance.SceneLoader.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        if (AppRoot.Instance == null
            || AppRoot.Instance.SceneLoader == null)
        {
            Debug.LogError(
                "SceneNavigationUI necesita SceneLoader desde AppRoot. Inicia desde Boot."
            );
            return;
        }

        AppRoot.Instance.SceneLoader.ReloadCurrentScene();
    }

    public void QuitGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Una build WebGL no puede cerrar la pestaña del usuario.");
#else
        Application.Quit();
#endif
    }
}