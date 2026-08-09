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

        actors ??= new List<KitchenActor>();

        KitchenActor[] sceneActors = FindObjectsByType<KitchenActor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (KitchenActor sceneActor in sceneActors)
        {
            if (sceneActor != null && !actors.Contains(sceneActor))
            {
                actors.Add(sceneActor);
            }
        }

        foreach (KitchenActor actor in actors)
        {
            if (actor == null || string.IsNullOrWhiteSpace(actor.ActorId))
            {
                continue;
            }

            string actorId = actor.ActorId.Trim();

            if (!actorsById.TryGetValue(actorId, out KitchenActor current))
            {
                actorsById.Add(actorId, actor);
                continue;
            }

            if (actor.PresentationPriority > current.PresentationPriority)
            {
                actorsById[actorId] = actor;
            }
            else if (actor.PresentationPriority == current.PresentationPriority)
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

    private void HandleStepStarted(RecipeStep step, int index)
    {
        SelectActor(step, index);
    }

    private void HandleStepCompleted(RecipeStep step, int index)
    {
        if (step?.ReactionType == KitchenReactionType.Collaboration)
        {
            activeActor = null;

            foreach (KitchenActor actor in actors)
            {
                actor?.PlayCelebration();
            }

            return;
        }

        if (activeActor == null)
        {
            SelectActor(step, index);
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
            SelectActor(recipeRunner.CurrentStep, recipeRunner.CurrentStepIndex);
        }
    }

    private void SelectActor(RecipeStep step, int _)
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

        string requestedActorId = step.ActorId.Trim();

        actorsById.TryGetValue(requestedActorId, out activeActor);

        activeActor?.SetTargeted(true);
    }
}
