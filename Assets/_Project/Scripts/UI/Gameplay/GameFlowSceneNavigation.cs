using UnityEngine;

public sealed class GameFlowSceneNavigation : MonoBehaviour
{
    [SerializeField] private GameFlow gameFlow;
    [SerializeField] private string creditsSceneName = "Credits";

    private void Awake()
    {
        if (gameFlow == null)
        {
            gameFlow = GetComponent<GameFlow>();
        }
    }

    private void OnEnable()
    {
        if (gameFlow != null)
        {
            gameFlow.CreditsRequested += LoadCredits;
        }
    }

    private void OnDisable()
    {
        if (gameFlow != null)
        {
            gameFlow.CreditsRequested -= LoadCredits;
        }
    }

    public void LoadCredits()
    {
        if (AppRoot.Instance?.SceneLoader == null)
        {
            Debug.LogError(
                "No se puede abrir Credits: falta AppRoot/SceneLoader.",
                this
            );
            return;
        }

        AppRoot.Instance.SceneLoader.LoadScene(creditsSceneName);
    }
}
