using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class AppRootBootstrapper : MonoBehaviour
{
    [SerializeField] private AppRoot appRootPrefab;

    private void Awake()
    {
        if (AppRoot.Instance != null)
        {
            return;
        }

        if (appRootPrefab == null)
        {
            Debug.LogError(
                "AppRootBootstrapper necesita el prefab AppRoot asignado."
            );
            return;
        }

        Instantiate(appRootPrefab);
    }
}
