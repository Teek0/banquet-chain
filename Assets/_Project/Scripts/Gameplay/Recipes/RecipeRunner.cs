using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RecipeRunnerState
{
    Inactive,
    PresentingOrder,
    AwaitingInput,
    ExecutingAction,
    ServingDish,
    RecipeCompleted
}

[RequireComponent(typeof(TypingInput))]
public sealed class RecipeRunner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RecipeData recipe;
    [SerializeField] private TypingInput typingInput;

    [Header("Flow")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField, Min(0f)] private float orderPresentationDuration = 0.6f;

    private Coroutine flowRoutine;
    private int runVersion;
    private int currentStepIndex = -1;
    private bool stepCompletionHandled;
    private bool recipeCompletionReported;
    private bool isSubscribedToInput;

    public event Action<RecipeData> RecipeStarted;
    public event Action<RecipeData> RecipeCompleted;
    public event Action<RecipeStep, int> StepStarted;
    public event Action<RecipeStep, int> StepCompleted;
    public event Action<RecipeRunnerState> StateChanged;

    public RecipeData CurrentRecipe => recipe;
    public RecipeStep CurrentStep => IsCurrentStepValid()
        ? recipe.Steps[currentStepIndex]
        : null;
    public int CurrentStepIndex => currentStepIndex;
    public RecipeRunnerState State { get; private set; }
        = RecipeRunnerState.Inactive;
    public bool IsRunning => State != RecipeRunnerState.Inactive
        && State != RecipeRunnerState.RecipeCompleted;

    private void Awake()
    {
        if (typingInput == null)
        {
            TryGetComponent(out typingInput);
        }
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartRecipe();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();

        CancelCurrentRun();
    }

    public bool StartRecipe()
    {
        return StartRecipe(recipe);
    }

    public bool StartRecipe(RecipeData recipeToRun)
    {
        CancelCurrentRun();
        recipe = recipeToRun;
        SubscribeToInput();

        if (!CanStartRecipe(recipeToRun))
        {
            return false;
        }

        recipeCompletionReported = false;
        SetState(RecipeRunnerState.PresentingOrder);
        RecipeStarted?.Invoke(recipe);
        int version = runVersion;
        flowRoutine = StartCoroutine(BeginFirstStepAfterOrder(version));
        return true;
    }

    public bool RestartRecipe()
    {
        return StartRecipe(recipe);
    }

    private IEnumerator BeginFirstStepAfterOrder(int version)
    {
        yield return WaitForDuration(orderPresentationDuration);

        if (!CanContinueRun(version))
        {
            yield break;
        }

        flowRoutine = null;
        BeginStep(0);
    }

    private void BeginStep(int index)
    {
        if (recipe == null || index < 0 || index >= recipe.Steps.Count)
        {
            Debug.LogError(
                "RecipeRunner intentó iniciar un paso fuera de la receta.",
                this
            );
            CancelCurrentRun();
            return;
        }

        RecipeStep step = recipe.Steps[index];

        if (step == null || string.IsNullOrWhiteSpace(step.ExpectedWord))
        {
            Debug.LogError(
                $"RecipeRunner no puede iniciar el paso {index + 1}: "
                    + "falta una palabra válida.",
                this
            );
            CancelCurrentRun();
            return;
        }

        currentStepIndex = index;
        stepCompletionHandled = false;
        typingInput.SetExpectedWord(step.ExpectedWord, true);
        SetState(RecipeRunnerState.AwaitingInput);
        StepStarted?.Invoke(step, index);
    }

    private void HandleWordCompleted(string _)
    {
        if (State != RecipeRunnerState.AwaitingInput
            || stepCompletionHandled
            || !IsCurrentStepValid())
        {
            return;
        }

        stepCompletionHandled = true;
        typingInput.SetInputEnabled(false);

        RecipeStep completedStep = CurrentStep;
        int completedIndex = currentStepIndex;
        bool isServingStep = completedStep.ReactionType
            == KitchenReactionType.Serving;

        SetState(
            isServingStep
                ? RecipeRunnerState.ServingDish
                : RecipeRunnerState.ExecutingAction
        );
        StepCompleted?.Invoke(completedStep, completedIndex);

        int version = runVersion;
        flowRoutine = StartCoroutine(
            AdvanceAfterStep(completedStep, completedIndex, version)
        );
    }

    private IEnumerator AdvanceAfterStep(
        RecipeStep completedStep,
        int completedIndex,
        int version
    )
    {
        yield return WaitForDuration(completedStep.DurationBeforeNextStep);

        if (!CanContinueRun(version)
            || completedIndex != currentStepIndex
            || completedStep != CurrentStep)
        {
            yield break;
        }

        flowRoutine = null;
        int nextIndex = completedIndex + 1;

        if (nextIndex < recipe.Steps.Count)
        {
            BeginStep(nextIndex);
            yield break;
        }

        CompleteRecipe();
    }

    private void CompleteRecipe()
    {
        if (recipeCompletionReported)
        {
            return;
        }

        recipeCompletionReported = true;
        typingInput.SetInputEnabled(false);
        SetState(RecipeRunnerState.RecipeCompleted);
        RecipeCompleted?.Invoke(recipe);
    }

    private bool CanStartRecipe(RecipeData recipeToRun)
    {
        if (typingInput == null)
        {
            Debug.LogError("RecipeRunner necesita un TypingInput.", this);
            return false;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "RecipeRunner debe estar activo para iniciar una receta.",
                this
            );
            return false;
        }

        if (recipeToRun == null)
        {
            Debug.LogError("RecipeRunner no tiene una receta asignada.", this);
            return false;
        }

        List<string> validationMessages = recipeToRun.GetValidationMessages();

        if (validationMessages.Count == 0)
        {
            return true;
        }

        Debug.LogError(
            $"RecipeRunner no puede iniciar '{recipeToRun.name}':\n- "
                + string.Join("\n- ", validationMessages),
            recipeToRun
        );
        return false;
    }

    private void SubscribeToInput()
    {
        if (typingInput == null)
        {
            TryGetComponent(out typingInput);
        }

        if (typingInput == null || isSubscribedToInput)
        {
            return;
        }

        typingInput.WordCompleted += HandleWordCompleted;
        isSubscribedToInput = true;
    }

    private void UnsubscribeFromInput()
    {
        if (typingInput != null && isSubscribedToInput)
        {
            typingInput.WordCompleted -= HandleWordCompleted;
        }

        isSubscribedToInput = false;
    }

    private void CancelCurrentRun()
    {
        runVersion++;

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        if (typingInput != null)
        {
            typingInput.SetInputEnabled(false);
        }

        currentStepIndex = -1;
        stepCompletionHandled = false;
        recipeCompletionReported = false;
        SetState(RecipeRunnerState.Inactive);
    }

    private bool CanContinueRun(int version)
    {
        return version == runVersion && isActiveAndEnabled && recipe != null;
    }

    private bool IsCurrentStepValid()
    {
        return recipe != null
            && recipe.Steps != null
            && currentStepIndex >= 0
            && currentStepIndex < recipe.Steps.Count;
    }

    private void SetState(RecipeRunnerState nextState)
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
