using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneLoader : MonoBehaviour
{
    [SerializeField] private ScreenFader screenFader;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    public bool IsLoading { get; private set; }

    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, fadeDuration);
    }

    public void LoadScene(string sceneName, float transitionDuration)
    {
        if (IsLoading)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("SceneLoader recibió un nombre de escena vacío.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"La escena '{sceneName}' no está incluida en el perfil de compilación."
            );
            return;
        }

        StartCoroutine(
            LoadSceneRoutine(sceneName, Mathf.Max(0f, transitionDuration))
        );
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RevealCurrentScene()
    {
        if (screenFader != null)
        {
            StartCoroutine(screenFader.FadeTo(0f, fadeDuration));
        }
    }

    private IEnumerator LoadSceneRoutine(
        string sceneName,
        float transitionDuration
    )
    {
        IsLoading = true;
        Time.timeScale = 1f;

        if (screenFader != null)
        {
            yield return screenFader.FadeTo(1f, transitionDuration);
        }

        AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);

        while (!loading.isDone)
        {
            yield return null;
        }

        if (screenFader != null)
        {
            yield return screenFader.FadeTo(0f, transitionDuration);
        }

        IsLoading = false;
    }
}
