using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RecipeHUDUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RecipeRunner recipeRunner;
    [SerializeField] private GameFlow gameFlow;
    [SerializeField] private TMP_Text dishNameLabel;
    [SerializeField] private TMP_Text orderLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text progressLabel;
    [SerializeField] private TMP_Text stepsLabel;
    [SerializeField] private TMP_Text pauseHintLabel;
    [SerializeField, Min(0)] private int detailedRecipeCount = 1;

    [Header("Status Colors")]
    [SerializeField] private Color waitingColor = new(0.67f, 0.71f, 0.82f);
    [SerializeField] private Color activeColor = new(1f, 0.82f, 0.28f);
    [SerializeField] private Color actionColor = new(0.4f, 0.8f, 1f);
    [SerializeField] private Color completedColor = new(0.44f, 0.88f, 0.62f);

    private bool isSubscribed;

    private void Awake()
    {
        gameFlow ??= FindFirstObjectByType<GameFlow>();

        if (recipeRunner == null
            || dishNameLabel == null
            || orderLabel == null
            || statusLabel == null
            || progressLabel == null
            || stepsLabel == null
            || pauseHintLabel == null)
        {
            enabled = false;
            return;
        }

        if (pauseHintLabel != null)
        {
            pauseHintLabel.text = LanguageManager.Text("ESC · PAUSA", "ESC · PAUSE");
        }
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshFromRunner();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (recipeRunner == null || isSubscribed)
        {
            return;
        }

        recipeRunner.RecipeStarted += HandleRecipeStarted;
        recipeRunner.RecipeCompleted += HandleRecipeCompleted;
        recipeRunner.StepStarted += HandleStepStarted;
        recipeRunner.StepCompleted += HandleStepCompleted;
        recipeRunner.StateChanged += HandleStateChanged;
        if (gameFlow != null)
        {
            gameFlow.RecipeActivated += HandleRecipeActivated;
        }
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (recipeRunner != null && isSubscribed)
        {
            recipeRunner.RecipeStarted -= HandleRecipeStarted;
            recipeRunner.RecipeCompleted -= HandleRecipeCompleted;
            recipeRunner.StepStarted -= HandleStepStarted;
            recipeRunner.StepCompleted -= HandleStepCompleted;
            recipeRunner.StateChanged -= HandleStateChanged;
        }

        if (gameFlow != null)
        {
            gameFlow.RecipeActivated -= HandleRecipeActivated;
        }

        isSubscribed = false;
    }

    private void RefreshFromRunner()
    {
        if (recipeRunner == null)
        {
            return;
        }

        RecipeData recipe = recipeRunner.CurrentRecipe;
        int index = recipeRunner.CurrentStepIndex;

        switch (recipeRunner.State)
        {
            case RecipeRunnerState.AwaitingInput:
                Render(recipe, index, index - 1, false);
                break;
            case RecipeRunnerState.ExecutingAction:
            case RecipeRunnerState.ServingDish:
                Render(recipe, -1, index, false);
                break;
            case RecipeRunnerState.RecipeCompleted:
                Render(recipe, -1, GetLastStepIndex(recipe), true);
                break;
            default:
                Render(recipe, -1, -1, false);
                break;
        }

        HandleStateChanged(recipeRunner.State);
    }

    private void HandleRecipeStarted(RecipeData recipe)
    {
        Render(recipe, -1, -1, false);
    }

    private void HandleRecipeActivated(int _, RecipeData __)
    {
        RefreshFromRunner();
    }

    private void HandleRecipeCompleted(RecipeData recipe)
    {
        Render(recipe, -1, GetLastStepIndex(recipe), true);
    }

    private void HandleStepStarted(RecipeStep _, int index)
    {
        Render(recipeRunner.CurrentRecipe, index, index - 1, false);
    }

    private void HandleStepCompleted(RecipeStep _, int index)
    {
        Render(recipeRunner.CurrentRecipe, -1, index, false);
    }

    private void HandleStateChanged(RecipeRunnerState state)
    {
        if (statusLabel == null)
        {
            return;
        }

        switch (state)
        {
            case RecipeRunnerState.PresentingOrder:
                SetStatus(LanguageManager.Text("PEDIDO RECIBIDO", "ORDER RECEIVED"), waitingColor);
                break;
            case RecipeRunnerState.AwaitingInput:
                SetStatus(LanguageManager.Text("ESCRIBE", "TYPE"), activeColor);
                break;
            case RecipeRunnerState.ExecutingAction:
                SetStatus(LanguageManager.Text("PREPARANDO", "PREPARING"), actionColor);
                break;
            case RecipeRunnerState.ServingDish:
                SetStatus(LanguageManager.Text("SIRVIENDO", "SERVING"), actionColor);
                break;
            case RecipeRunnerState.RecipeCompleted:
                SetStatus(LanguageManager.Text("RECETA COMPLETADA", "RECIPE COMPLETE"), completedColor);
                break;
            default:
                SetStatus(LanguageManager.Text("ESPERANDO", "WAITING"), waitingColor);
                break;
        }
    }

    private void Render(
        RecipeData recipe,
        int activeStepIndex,
        int completedThroughIndex,
        bool recipeCompleted
    )
    {
        bool showDetails = gameFlow == null
            || gameFlow.CurrentRecipeIndex < detailedRecipeCount;

        if (!showDetails)
        {
            SetText(dishNameLabel, LanguageManager.Text("PEDIDO DE SUN", "SUN'S ORDER"));
            SetText(orderLabel, string.Empty);
            SetText(stepsLabel, string.Empty);
            SetText(
                progressLabel,
                BuildCompactProgress(
                    recipe,
                    activeStepIndex,
                    completedThroughIndex,
                    recipeCompleted
                )
            );
            return;
        }

        if (recipe == null)
        {
            SetText(dishNameLabel, LanguageManager.Text("SIN RECETA", "NO RECIPE"));
            SetText(orderLabel, LanguageManager.Text("Asigna una receta para comenzar.", "Assign a recipe to begin."));
        }
        else
        {
            SetText(
                dishNameLabel,
                RecipeHUDPresenter.EscapeRichText(recipe.DisplayName)
            );
            SetText(
                orderLabel,
                LanguageManager.Text("PEDIDO · “", "ORDER · “")
                    + RecipeHUDPresenter.EscapeRichText(recipe.CatOrder)
                    + "”"
            );
        }

        SetText(
            progressLabel,
            RecipeHUDPresenter.BuildProgress(
                recipe,
                activeStepIndex,
                completedThroughIndex,
                recipeCompleted
            )
        );
        SetText(
            stepsLabel,
            RecipeHUDPresenter.BuildStepList(
                recipe,
                activeStepIndex,
                completedThroughIndex
            )
        );
    }

    private void SetStatus(string text, Color color)
    {
        statusLabel.text = text;
        statusLabel.color = color;
    }

    private static void SetText(TMP_Text label, string text)
    {
        if (label != null)
        {
            label.text = text;
        }
    }

    private static int GetLastStepIndex(RecipeData recipe)
    {
        return recipe?.Steps == null ? -1 : recipe.Steps.Count - 1;
    }

    private static string BuildCompactProgress(
        RecipeData recipe,
        int activeStepIndex,
        int completedThroughIndex,
        bool recipeCompleted
    )
    {
        int total = recipe?.Steps?.Count ?? 0;

        if (total == 0)
        {
            return string.Empty;
        }

        int current = recipeCompleted
            ? total
            : Mathf.Clamp(
                activeStepIndex >= 0
                    ? activeStepIndex + 1
                    : completedThroughIndex + 1,
                1,
                total
            );
        return LanguageManager.Text($"PASO {current} / {total}", $"STEP {current} / {total}");
    }
}
