using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DishVisualTests
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
    public IEnumerator RecipeBuildsDishAndServingCompletesIt()
    {
        RecipeData recipe = CreateRecipe();
        (RecipeRunner runner, TypingInput typingInput) = CreateRunner();
        DishVisual dish = CreateDish(runner);

        Assert.That(runner.StartRecipe(recipe), Is.True);
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);
        Assert.That(dish.State, Is.EqualTo(DishVisualState.Empty));
        Assert.That(dish.TotalTransformations, Is.EqualTo(3));

        CompleteCurrentWord(typingInput);
        Assert.That(dish.State, Is.EqualTo(DishVisualState.Building));
        Assert.That(dish.TransformationCount, Is.EqualTo(1));
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        CompleteCurrentWord(typingInput);
        Assert.That(dish.TransformationCount, Is.EqualTo(2));
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        CompleteCurrentWord(typingInput);
        Assert.That(dish.State, Is.EqualTo(DishVisualState.Ready));
        Assert.That(dish.TransformationCount, Is.EqualTo(3));
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);

        CompleteCurrentWord(typingInput);
        Assert.That(dish.State, Is.EqualTo(DishVisualState.Serving));
        yield return WaitForState(runner, RecipeRunnerState.RecipeCompleted);
        dish.AdvanceVisual(1f);

        Assert.That(dish.State, Is.EqualTo(DishVisualState.Completed));
        Assert.That(dish.IsServing, Is.False);
    }

    [UnityTest]
    public IEnumerator RestartClearsPreviousDishProgress()
    {
        RecipeData recipe = CreateRecipe();
        (RecipeRunner runner, TypingInput typingInput) = CreateRunner();
        DishVisual dish = CreateDish(runner);

        Assert.That(runner.StartRecipe(recipe), Is.True);
        yield return WaitForState(runner, RecipeRunnerState.AwaitingInput);
        CompleteCurrentWord(typingInput);
        Assert.That(dish.TransformationCount, Is.EqualTo(1));

        Assert.That(runner.StartRecipe(recipe), Is.True);
        Assert.That(dish.State, Is.EqualTo(DishVisualState.Empty));
        Assert.That(dish.TransformationCount, Is.Zero);
    }

    [Test]
    public void ServingStepDoesNotAddTransformation()
    {
        RecipeData recipe = CreateRecipe();
        DishVisual dish = CreateDish(null);
        dish.BeginRecipe(recipe);
        dish.ApplyStep(recipe.Steps[3]);

        Assert.That(dish.TransformationCount, Is.Zero);
        Assert.That(dish.State, Is.EqualTo(DishVisualState.Serving));
    }

    private (RecipeRunner runner, TypingInput typingInput) CreateRunner()
    {
        GameObject runnerObject = new("Dish runner");
        objectsToDestroy.Add(runnerObject);
        runnerObject.SetActive(false);
        TypingInput typingInput = runnerObject.AddComponent<TypingInput>();
        RecipeRunner runner = runnerObject.AddComponent<RecipeRunner>();
        SerializedObject serializedRunner = new(runner);
        serializedRunner.FindProperty("typingInput").objectReferenceValue
            = typingInput;
        serializedRunner.FindProperty("playOnStart").boolValue = false;
        serializedRunner.FindProperty("orderPresentationDuration").floatValue
            = 0f;
        serializedRunner.ApplyModifiedPropertiesWithoutUndo();
        runnerObject.SetActive(true);
        return (runner, typingInput);
    }

    private DishVisual CreateDish(RecipeRunner runner)
    {
        GameObject dishObject = new("Dish visual");
        objectsToDestroy.Add(dishObject);
        dishObject.SetActive(false);
        DishVisual dish = dishObject.AddComponent<DishVisual>();
        SerializedObject serializedDish = new(dish);
        serializedDish.FindProperty("recipeRunner").objectReferenceValue = runner;
        serializedDish.FindProperty("servingDuration").floatValue = 0.1f;
        serializedDish.ApplyModifiedPropertiesWithoutUndo();
        dishObject.SetActive(true);
        return dish;
    }

    private RecipeData CreateRecipe()
    {
        RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
        objectsToDestroy.Add(recipe);
        SerializedObject serializedRecipe = new(recipe);
        serializedRecipe.FindProperty("displayName").stringValue = "Pan caliente";
        serializedRecipe.FindProperty("catOrder").stringValue = "Pedido";
        SerializedProperty steps = serializedRecipe.FindProperty("steps");
        string[] words = { "pan", "mantequilla", "tostar", "servir" };
        steps.arraySize = words.Length;

        for (int index = 0; index < words.Length; index++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(index);
            step.FindPropertyRelative("expectedWord").stringValue = words[index];
            step.FindPropertyRelative("actorId").stringValue = "actor";
            step.FindPropertyRelative("reactionType").enumValueIndex
                = index == words.Length - 1
                    ? (int)KitchenReactionType.Serving
                    : (int)KitchenReactionType.Preparation;
            step.FindPropertyRelative("durationBeforeNextStep").floatValue = 0f;
            step.FindPropertyRelative("transformsDish").boolValue
                = index < words.Length - 1;
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
