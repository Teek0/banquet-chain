using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecipeRunnerTests
{
    private readonly List<Object> objectsToDestroy = new();

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
    public IEnumerator Recipe_CompletesInOrderAndExactlyOnce()
    {
        RecipeData recipe = CreateRecipe(
            new[] { "pan", "tostar", "servir" },
            0f
        );
        (RecipeRunner runner, TypingInput typingInput) = CreateRunner();
        List<string> startedSteps = new();
        List<string> completedSteps = new();
        int recipeStartedCount = 0;
        int recipeCompletedCount = 0;

        runner.RecipeStarted += _ => recipeStartedCount++;
        runner.RecipeCompleted += _ => recipeCompletedCount++;
        runner.StepStarted += (step, _) => startedSteps.Add(step.ExpectedWord);
        runner.StepCompleted += (step, _) => completedSteps.Add(step.ExpectedWord);

        Assert.That(runner.StartRecipe(recipe), Is.True);
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        for (int index = 0; index < recipe.Steps.Count; index++)
        {
            CompleteCurrentWord(typingInput);

            RecipeRunnerState expectedState = index == recipe.Steps.Count - 1
                ? RecipeRunnerState.ServingDish
                : RecipeRunnerState.ExecutingAction;

            Assert.That(runner.State, Is.EqualTo(expectedState));
            Assert.That(typingInput.IsInputEnabled, Is.False);

            RecipeRunnerState nextState = index == recipe.Steps.Count - 1
                ? RecipeRunnerState.RecipeCompleted
                : RecipeRunnerState.AwaitingInput;
            yield return WaitForState(runner, nextState);
        }

        CompleteCurrentWord(typingInput);

        Assert.That(
            startedSteps,
            Is.EqualTo(new[] { "pan", "tostar", "servir" })
        );
        Assert.That(
            completedSteps,
            Is.EqualTo(new[] { "pan", "tostar", "servir" })
        );
        Assert.That(recipeStartedCount, Is.EqualTo(1));
        Assert.That(recipeCompletedCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RestartRecipe_StartsAgainFromFirstStep()
    {
        RecipeData recipe = CreateRecipe(new[] { "pan", "servir" }, 0f);
        (RecipeRunner runner, TypingInput typingInput) = CreateRunner();
        int recipeCompletedCount = 0;
        runner.RecipeCompleted += _ => recipeCompletedCount++;

        Assert.That(runner.StartRecipe(recipe), Is.True);
        yield return CompleteRecipe(runner, typingInput, recipe.Steps.Count);
        Assert.That(recipeCompletedCount, Is.EqualTo(1));

        Assert.That(runner.RestartRecipe(), Is.True);
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        Assert.That(runner.CurrentStepIndex, Is.Zero);
        Assert.That(runner.CurrentStep.ExpectedWord, Is.EqualTo("pan"));

        yield return CompleteRecipe(runner, typingInput, recipe.Steps.Count);
        Assert.That(recipeCompletedCount, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator DisableDuringWait_CancelsAdvanceAndAllowsNewRun()
    {
        RecipeData recipe = CreateRecipe(new[] { "pan", "servir" }, 1f);
        (RecipeRunner runner, TypingInput typingInput) = CreateRunner();
        int recipeCompletedCount = 0;
        runner.RecipeCompleted += _ => recipeCompletedCount++;

        Assert.That(runner.StartRecipe(recipe), Is.True);
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);
        CompleteCurrentWord(typingInput);

        Assert.That(runner.State, Is.EqualTo(RecipeRunnerState.ExecutingAction));
        runner.gameObject.SetActive(false);
        yield return null;
        yield return null;

        Assert.That(runner.State, Is.EqualTo(RecipeRunnerState.Inactive));
        Assert.That(runner.CurrentStepIndex, Is.EqualTo(-1));
        Assert.That(recipeCompletedCount, Is.Zero);

        runner.gameObject.SetActive(true);
        Assert.That(runner.StartRecipe(recipe), Is.True);
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        Assert.That(runner.CurrentStepIndex, Is.Zero);
    }

    [Test]
    public void StartRecipe_WhenRecipeIsMissing_ReturnsFalse()
    {
        (RecipeRunner runner, _) = CreateRunner();
        LogAssert.Expect(
            LogType.Error,
            "RecipeRunner no tiene una receta asignada."
        );

        Assert.That(runner.StartRecipe(null), Is.False);
        Assert.That(runner.State, Is.EqualTo(RecipeRunnerState.Inactive));
    }

    private (RecipeRunner runner, TypingInput typingInput) CreateRunner()
    {
        GameObject gameObject = new("RecipeRunnerTests");
        objectsToDestroy.Add(gameObject);
        gameObject.SetActive(false);

        TypingInput typingInput = gameObject.AddComponent<TypingInput>();
        RecipeRunner runner = gameObject.AddComponent<RecipeRunner>();
        SerializedObject serializedRunner = new(runner);
        serializedRunner.FindProperty("typingInput").objectReferenceValue
            = typingInput;
        serializedRunner.FindProperty("playOnStart").boolValue = false;
        serializedRunner.FindProperty("orderPresentationDuration").floatValue
            = 0f;
        serializedRunner.ApplyModifiedPropertiesWithoutUndo();
        gameObject.SetActive(true);

        return (runner, typingInput);
    }

    private RecipeData CreateRecipe(string[] words, float stepDuration)
    {
        RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
        objectsToDestroy.Add(recipe);
        SerializedObject serializedRecipe = new(recipe);
        serializedRecipe.FindProperty("displayName").stringValue = "Prueba";
        serializedRecipe.FindProperty("catOrder").stringValue = "Pedido";

        SerializedProperty steps = serializedRecipe.FindProperty("steps");
        steps.arraySize = words.Length;

        for (int index = 0; index < words.Length; index++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(index);
            string word = words[index];
            step.FindPropertyRelative("expectedWord").stringValue = word;
            step.FindPropertyRelative("actorId").stringValue = $"actor_{index}";
            step.FindPropertyRelative("reactionType").enumValueIndex
                = word == "servir"
                    ? (int)KitchenReactionType.Serving
                    : (int)KitchenReactionType.Preparation;
            step.FindPropertyRelative("durationBeforeNextStep").floatValue
                = stepDuration;
        }

        serializedRecipe.ApplyModifiedPropertiesWithoutUndo();
        return recipe;
    }

    private static IEnumerator CompleteRecipe(
        RecipeRunner runner,
        TypingInput typingInput,
        int stepCount
    )
    {
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        for (int index = 0; index < stepCount; index++)
        {
            CompleteCurrentWord(typingInput);
            RecipeRunnerState nextState = index == stepCount - 1
                ? RecipeRunnerState.RecipeCompleted
                : RecipeRunnerState.AwaitingInput;
            yield return WaitForState(runner, nextState);
        }
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
