using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class KitchenActorCoordinator : MonoBehaviour
{
    [SerializeField] private RecipeRunner recipeRunner;
    [SerializeField] private TypingInput typingInput;
    [SerializeField] private List<KitchenActor> actors = new();

    private readonly Dictionary<string, KitchenActor> actorsById = new(
        StringComparer.OrdinalIgnoreCase
    );
    private KitchenActor activeActor;
    private bool isSubscribed;

    public KitchenActor ActiveActor => activeActor;

    private void Awake()
    {
        ResolveReferences();
        RebuildActorIndex();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RebuildActorIndex();
        Subscribe();
    }

    private void Start()
    {
        SyncWithCurrentStep();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void RebuildActorIndex()
    {
        actorsById.Clear();

        if (actors == null || actors.Count == 0)
        {
            actors = new List<KitchenActor>(
                GetComponentsInChildren<KitchenActor>(true)
            );
        }

        foreach (KitchenActor actor in actors)
        {
            if (actor == null || string.IsNullOrWhiteSpace(actor.ActorId))
            {
                continue;
            }

            if (!actorsById.TryAdd(actor.ActorId.Trim(), actor))
            {
                Debug.LogWarning(
                    $"Hay más de un KitchenActor con el id '{actor.ActorId}'.",
                    actor
                );
            }
        }
    }

    private void ResolveReferences()
    {
        if (recipeRunner == null)
        {
            recipeRunner = FindFirstObjectByType<RecipeRunner>();
        }

        if (typingInput == null && recipeRunner != null)
        {
            typingInput = recipeRunner.GetComponent<TypingInput>();
        }

        if (typingInput == null)
        {
            typingInput = FindFirstObjectByType<TypingInput>();
        }
    }

    private void Subscribe()
    {
        if (isSubscribed || recipeRunner == null || typingInput == null)
        {
            return;
        }

        recipeRunner.RecipeStarted += HandleRecipeStarted;
        recipeRunner.StepStarted += HandleStepStarted;
        recipeRunner.StepCompleted += HandleStepCompleted;
        recipeRunner.RecipeCompleted += HandleRecipeCompleted;
        typingInput.ProgressChanged += HandleProgressChanged;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (recipeRunner != null)
        {
            recipeRunner.RecipeStarted -= HandleRecipeStarted;
            recipeRunner.StepStarted -= HandleStepStarted;
            recipeRunner.StepCompleted -= HandleStepCompleted;
            recipeRunner.RecipeCompleted -= HandleRecipeCompleted;
        }

        if (typingInput != null)
        {
            typingInput.ProgressChanged -= HandleProgressChanged;
        }

        isSubscribed = false;
    }

    private void HandleRecipeStarted(RecipeData _)
    {
        activeActor = null;

        foreach (KitchenActor actor in actors)
        {
            actor?.ResetActor();
        }
    }

    private void HandleStepStarted(RecipeStep step, int _)
    {
        SelectActor(step);
    }

    private void HandleStepCompleted(RecipeStep step, int _)
    {
        if (activeActor == null)
        {
            SelectActor(step);
        }

        activeActor?.PlayAction(step.ReactionType);
    }

    private void HandleRecipeCompleted(RecipeData _)
    {
        activeActor = null;

        foreach (KitchenActor actor in actors)
        {
            actor?.PlayCelebration();
        }
    }

    private void HandleProgressChanged(int progress, string _)
    {
        if (activeActor == null || typingInput == null)
        {
            return;
        }

        int wordLength = TypingTextNormalizer
            .NormalizeForComparison(typingInput.ExpectedWord)
            .Length;
        activeActor.SetAnticipation(
            wordLength > 0 ? (float)progress / wordLength : 0f
        );
    }

    private void SyncWithCurrentStep()
    {
        if (recipeRunner != null && recipeRunner.CurrentStep != null)
        {
            SelectActor(recipeRunner.CurrentStep);
        }
    }

    private void SelectActor(RecipeStep step)
    {
        foreach (KitchenActor actor in actors)
        {
            actor?.SetTargeted(false);
        }

        activeActor = null;

        if (step == null || string.IsNullOrWhiteSpace(step.ActorId))
        {
            return;
        }

        if (!actorsById.TryGetValue(step.ActorId.Trim(), out activeActor))
        {
            Debug.LogWarning(
                $"No hay un KitchenActor para el id '{step.ActorId}'.",
                this
            );
            return;
        }

        activeActor.SetTargeted(true);
    }
}
