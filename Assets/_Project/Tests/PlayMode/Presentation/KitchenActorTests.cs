using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class KitchenActorTests
{
    private GameObject actorObject;
    private KitchenActor actor;
    private readonly List<Object> objectsToDestroy = new();

    [SetUp]
    public void SetUp()
    {
        actorObject = new GameObject("Actor under test");
        objectsToDestroy.Add(actorObject);
        actor = actorObject.AddComponent<KitchenActor>();
        actor.ConfigureIdentity("horno", "Horno");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object target in objectsToDestroy)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        objectsToDestroy.Clear();
    }

    [UnityTest]
    public IEnumerator AnticipationUsesLatestAbsoluteProgress()
    {
        actor.SetTargeted(true);
        actor.SetAnticipation(0.2f);
        actor.SetAnticipation(0.8f);
        actor.SetAnticipation(0.35f);

        yield return null;

        Assert.That(actor.State, Is.EqualTo(KitchenActorVisualState.Anticipating));
        Assert.That(actor.AnticipationProgress, Is.EqualTo(0.35f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator ActionReturnsToStableTargetState()
    {
        actor.SetTargeted(true);
        actor.PlayAction(KitchenReactionType.Cooking);

        Assert.That(actor.State, Is.EqualTo(KitchenActorVisualState.Acting));
        actor.AdvanceReaction(1f);
        yield return null;

        Assert.That(actor.HasActiveReaction, Is.False);
        Assert.That(actor.State, Is.EqualTo(KitchenActorVisualState.Targeted));
    }

    [UnityTest]
    public IEnumerator RapidActionsReplaceTheCurrentReaction()
    {
        actor.PlayAction(KitchenReactionType.Ingredient);
        actor.PlayAction(KitchenReactionType.Preparation);
        actor.PlayAction(KitchenReactionType.Serving);

        actor.AdvanceReaction(1f);
        yield return null;

        Assert.That(actor.HasActiveReaction, Is.False);
        Assert.That(actor.State, Is.EqualTo(KitchenActorVisualState.Idle));
    }

    [UnityTest]
    public IEnumerator CelebrationReturnsToIdle()
    {
        actor.SetTargeted(true);
        actor.PlayCelebration();
        Assert.That(actor.State, Is.EqualTo(KitchenActorVisualState.Celebrating));

        actor.AdvanceReaction(2f);
        yield return null;

        Assert.That(actor.HasActiveReaction, Is.False);
        Assert.That(actor.State, Is.EqualTo(KitchenActorVisualState.Idle));
    }

    [UnityTest]
    public IEnumerator RecipeRoutesStepsThroughThreeDifferentActors()
    {
        GameObject runnerObject = new("Runner");
        GameObject actorRoot = new("Actors");
        objectsToDestroy.Add(runnerObject);
        objectsToDestroy.Add(actorRoot);
        runnerObject.SetActive(false);
        actorRoot.SetActive(false);

        TypingInput typingInput = runnerObject.AddComponent<TypingInput>();
        RecipeRunner runner = runnerObject.AddComponent<RecipeRunner>();
        SerializedObject serializedRunner = new(runner);
        serializedRunner.FindProperty("typingInput").objectReferenceValue
            = typingInput;
        serializedRunner.FindProperty("playOnStart").boolValue = false;
        serializedRunner.FindProperty("orderPresentationDuration").floatValue
            = 0f;
        serializedRunner.ApplyModifiedPropertiesWithoutUndo();

        KitchenActor pantry = CreateActor(actorRoot.transform, "despensa");
        KitchenActor oven = CreateActor(actorRoot.transform, "horno");
        KitchenActor service = CreateActor(actorRoot.transform, "servicio");
        KitchenActorCoordinator coordinator = actorRoot.AddComponent<KitchenActorCoordinator>();
        SerializedObject serializedCoordinator = new(coordinator);
        serializedCoordinator.FindProperty("recipeRunner").objectReferenceValue
            = runner;
        serializedCoordinator.FindProperty("typingInput").objectReferenceValue
            = typingInput;
        SerializedProperty actors = serializedCoordinator.FindProperty("actors");
        actors.arraySize = 3;
        actors.GetArrayElementAtIndex(0).objectReferenceValue = pantry;
        actors.GetArrayElementAtIndex(1).objectReferenceValue = oven;
        actors.GetArrayElementAtIndex(2).objectReferenceValue = service;
        serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();

        RecipeData recipe = CreateRecipe();
        runnerObject.SetActive(true);
        actorRoot.SetActive(true);
        Assert.That(runner.StartRecipe(recipe), Is.True);
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        Assert.That(coordinator.ActiveActor, Is.SameAs(pantry));
        CompleteCurrentWord(typingInput);
        Assert.That(pantry.State, Is.EqualTo(KitchenActorVisualState.Acting));
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        Assert.That(coordinator.ActiveActor, Is.SameAs(oven));
        CompleteCurrentWord(typingInput);
        Assert.That(oven.State, Is.EqualTo(KitchenActorVisualState.Acting));
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        Assert.That(coordinator.ActiveActor, Is.SameAs(service));
        CompleteCurrentWord(typingInput);
        Assert.That(service.State, Is.EqualTo(KitchenActorVisualState.Acting));
        yield return WaitForState(runner, RecipeRunnerState.RecipeCompleted);

        Assert.That(pantry.State, Is.EqualTo(KitchenActorVisualState.Celebrating));
        Assert.That(oven.State, Is.EqualTo(KitchenActorVisualState.Celebrating));
        Assert.That(service.State, Is.EqualTo(KitchenActorVisualState.Celebrating));
    }

    private KitchenActor CreateActor(Transform parent, string id)
    {
        GameObject actorGameObject = new(id);
        actorGameObject.transform.SetParent(parent);
        KitchenActor kitchenActor = actorGameObject.AddComponent<KitchenActor>();
        kitchenActor.ConfigureIdentity(id, id);
        return kitchenActor;
    }

    private RecipeData CreateRecipe()
    {
        RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
        objectsToDestroy.Add(recipe);
        SerializedObject serializedRecipe = new(recipe);
        serializedRecipe.FindProperty("displayName").stringValue = "Pan caliente";
        serializedRecipe.FindProperty("catOrder").stringValue = "Pedido";
        SerializedProperty steps = serializedRecipe.FindProperty("steps");
        string[] words = { "pan", "tostar", "servir" };
        string[] actorIds = { "despensa", "horno", "servicio" };
        steps.arraySize = words.Length;

        for (int index = 0; index < words.Length; index++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(index);
            step.FindPropertyRelative("expectedWord").stringValue = words[index];
            step.FindPropertyRelative("actorId").stringValue = actorIds[index];
            step.FindPropertyRelative("reactionType").enumValueIndex
                = index == words.Length - 1
                    ? (int)KitchenReactionType.Serving
                    : (int)KitchenReactionType.Preparation;
            step.FindPropertyRelative("durationBeforeNextStep").floatValue = 0f;
        }

        serializedRecipe.ApplyModifiedPropertiesWithoutUndo();
        return recipe;
    }

    private static void CompleteCurrentWord(TypingInput typingInput)
    {
        foreach (char character in typingInput.ExpectedWord)
        {
            typingInput.ProcessCharacter(character);
        }
    }

    private static IEnumerator WaitForState(
        RecipeRunner runner,
        RecipeRunnerState expectedState
    )
    {
        const int frameLimit = 10;

        for (int frame = 0; frame < frameLimit; frame++)
        {
            if (runner.State == expectedState)
            {
                yield break;
            }

            yield return null;
        }

        Assert.That(runner.State, Is.EqualTo(expectedState));
    }
}
