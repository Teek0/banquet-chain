using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum GameFlowState
{
    Inactive,
    Starting,
    PlayingRecipe,
    CelebratingDish,
    TransitioningRecipe,
    FinalEating,
    FinalCelebration,
    FinalSleeping,
    Completed
}

public sealed class GameFlow : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private RecipeRunner recipeRunner;
    [SerializeField] private CatController catController;
    [SerializeField] private List<RecipeData> recipes = new();
    [SerializeField] private bool playOnStart = true;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float dishCelebrationDuration = 1.2f;
    [SerializeField, Min(0f)] private float interRecipeDelay = 0.45f;
    [SerializeField, Min(0f)] private float finalEatingDuration = 1.2f;
    [SerializeField, Min(0f)] private float finalCelebrationDuration = 3f;
    [SerializeField, Min(0f)] private float finalSleepingDuration = 1.5f;
    [SerializeField, Min(0f)] private float finalMessageDuration = 10f;

    [Header("Final")]
    [SerializeField] private CanvasGroup finalOverlay;
    [SerializeField] private TMP_Text finalLabel;
    [SerializeField] private bool loadCreditsOnComplete = true;
    [SerializeField, TextArea(2, 4)] private string finalMessage =
        "EL BANQUETE ESTÁ COMPLETO\nEl ronroneo vuelve a proteger al pueblo.";

    private Coroutine transitionRoutine;
    private int runVersion;
    private bool isSubscribed;

    public event Action GameStarted;
    public event Action<int, RecipeData> RecipeActivated;
    public event Action<int, RecipeData> DishCompleted;
    public event Action BanquetCompleted;
    public event Action CreditsRequested;
    public event Action<GameFlowState> StateChanged;

    public GameFlowState State { get; private set; } = GameFlowState.Inactive;
    public int CurrentRecipeIndex { get; private set; } = -1;
    public int CompletedDishes { get; private set; }
    public RecipeData CurrentRecipe => CurrentRecipeIndex >= 0
        && CurrentRecipeIndex < recipes.Count
            ? recipes[CurrentRecipeIndex]
            : null;
    public IReadOnlyList<RecipeData> Recipes => recipes;
    public bool IsRunning => State != GameFlowState.Inactive
        && State != GameFlowState.Completed;

    private void Awake()
    {
        ResolveReferences();
        HideFinalOverlay();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartGame();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
        CancelActiveSequence();
    }

    public bool StartGame()
    {
        CancelActiveSequence();
        ResolveReferences();
        Subscribe();

        if (!CanStartGame())
        {
            return false;
        }

        CurrentRecipeIndex = 0;
        CompletedDishes = 0;
        catController?.BeginBanquet();
        HideFinalOverlay();
        SetState(GameFlowState.Starting);
        GameStarted?.Invoke();
        return BeginCurrentRecipe();
    }

    public bool RestartGame()
    {
        return StartGame();
    }

    private void ResolveReferences()
    {
        if (recipeRunner == null)
        {
            recipeRunner = FindFirstObjectByType<RecipeRunner>();
        }

        if (catController == null)
        {
            catController = FindFirstObjectByType<CatController>();
        }
    }

    private void Subscribe()
    {
        if (isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeCompleted += HandleRecipeCompleted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeCompleted -= HandleRecipeCompleted;
        isSubscribed = false;
    }

    private bool BeginCurrentRecipe()
    {
        RecipeData recipe = CurrentRecipe;

        if (recipe == null)
        {
            Debug.LogError("GameFlow no puede iniciar una receta ausente.", this);
            SetState(GameFlowState.Inactive);
            return false;
        }

        SetState(GameFlowState.PlayingRecipe);

        if (!recipeRunner.StartRecipe(recipe))
        {
            SetState(GameFlowState.Inactive);
            return false;
        }

        RecipeActivated?.Invoke(CurrentRecipeIndex, recipe);
        return true;
    }

    private void HandleRecipeCompleted(RecipeData recipe)
    {
        if (State != GameFlowState.PlayingRecipe
            || recipe == null
            || recipe != CurrentRecipe
            || transitionRoutine != null)
        {
            return;
        }

        int version = runVersion;
        transitionRoutine = StartCoroutine(
            AdvanceAfterDish(recipe, CurrentRecipeIndex, version)
        );
    }

    private IEnumerator AdvanceAfterDish(
        RecipeData recipe,
        int completedIndex,
        int version
    )
    {
        SetState(GameFlowState.CelebratingDish);
        CompletedDishes = completedIndex + 1;
        bool isFinalDish = completedIndex >= recipes.Count - 1;
        catController?.RegisterServedDish(CompletedDishes, isFinalDish);
        DishCompleted?.Invoke(completedIndex, recipe);
        yield return WaitForDuration(dishCelebrationDuration);

        if (!CanContinue(version))
        {
            yield break;
        }

        if (isFinalDish)
        {
            SetState(GameFlowState.FinalEating);
            catController?.PlayReceiving();
            yield return WaitForDuration(finalEatingDuration);

            if (!CanContinue(version))
            {
                yield break;
            }

            SetState(GameFlowState.FinalCelebration);
            catController?.PlayFinalPurr();
            yield return WaitForDuration(finalCelebrationDuration);

            if (!CanContinue(version))
            {
                yield break;
            }

            SetState(GameFlowState.FinalSleeping);
            catController?.PlaySleeping();
            yield return WaitForDuration(finalSleepingDuration);

            if (!CanContinue(version))
            {
                yield break;
            }

            ShowFinalOverlay();
            BanquetCompleted?.Invoke();
            SetState(GameFlowState.Completed);

            yield return WaitForDuration(finalMessageDuration);

            transitionRoutine = null;

            if (loadCreditsOnComplete)
            {
                CreditsRequested?.Invoke();
            }

            yield break;
        }

        SetState(GameFlowState.TransitioningRecipe);
        yield return WaitForDuration(interRecipeDelay);

        if (!CanContinue(version))
        {
            yield break;
        }

        CurrentRecipeIndex++;
        transitionRoutine = null;
        BeginCurrentRecipe();
    }

    private bool CanStartGame()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("GameFlow debe estar activo para iniciar.", this);
            return false;
        }

        if (recipeRunner == null)
        {
            Debug.LogError("GameFlow necesita RecipeRunner.", this);
            return false;
        }

        if (recipes == null || recipes.Count == 0)
        {
            Debug.LogError("GameFlow necesita al menos una receta.", this);
            return false;
        }

        for (int index = 0; index < recipes.Count; index++)
        {
            if (recipes[index] == null)
            {
                Debug.LogError(
                    $"GameFlow tiene una receta ausente en la posición {index + 1}.",
                    this
                );
                return false;
            }
        }

        if (recipes.Count != 3)
        {
            Debug.LogWarning(
                $"El flujo objetivo usa 3 recetas; actualmente hay {recipes.Count}.",
                this
            );
        }

        return true;
    }

    private bool CanContinue(int version)
    {
        return version == runVersion && isActiveAndEnabled;
    }

    private void CancelActiveSequence()
    {
        runVersion++;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        CurrentRecipeIndex = -1;
        CompletedDishes = 0;
        SetState(GameFlowState.Inactive);
    }

    private void ShowFinalOverlay()
    {
        if (finalLabel != null)
        {
            finalLabel.text = finalMessage;
        }

        if (finalOverlay != null)
        {
            finalOverlay.alpha = 1f;
            finalOverlay.interactable = true;
            finalOverlay.blocksRaycasts = true;
        }
    }

    private void HideFinalOverlay()
    {
        if (finalOverlay != null)
        {
            finalOverlay.alpha = 0f;
            finalOverlay.interactable = false;
            finalOverlay.blocksRaycasts = false;
        }
    }

    private void SetState(GameFlowState nextState)
    {
        if (State == nextState)
        {
            return;
        }

        State = nextState;
        StateChanged?.Invoke(State);
    }

    private static IEnumerator WaitForDuration(float duration)
    {
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
        else
        {
            yield return null;
        }
    }
}
