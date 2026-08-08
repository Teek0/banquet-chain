using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GameFlowTests
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
    public IEnumerator ThreeRecipesReachFinalInOrder()
    {
        RecipeData[] recipes = CreateRecipes(3);
        (RecipeRunner runner, TypingInput typingInput) = CreateRunner();
        CatController cat = CreateCat(runner);
        GameFlow flow = CreateFlow(runner, cat, recipes);
        List<int> activated = new();
        flow.RecipeActivated += (index, _) => activated.Add(index);

        Assert.That(flow.StartGame(), Is.True);

        for (int index = 0; index < recipes.Length; index++)
        {
            yield return WaitForRunnerState(
                runner,
                RecipeRunnerState.AwaitingInput
            );
            Assert.That(flow.CurrentRecipeIndex, Is.EqualTo(index));
            CompleteCurrentWord(typingInput);

            if (index < recipes.Length - 1)
            {
                yield return WaitForRecipeIndex(flow, index + 1);
            }
        }

        yield return WaitForFlowState(flow, GameFlowState.Completed);

        Assert.That(activated, Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(flow.CompletedDishes, Is.EqualTo(3));
        Assert.That(cat.Satisfaction, Is.EqualTo(3));
        Assert.That(cat.State, Is.EqualTo(CatVisualState.Satisfied));
    }

    [UnityTest]
    public IEnumerator RestartReturnsToFirstRecipeAndClearsProgress()
    {
        RecipeData[] recipes = CreateRecipes(3);
        (RecipeRunner runner, TypingInput typingInput) = CreateRunner();
        CatController cat = CreateCat(runner);
        GameFlow flow = CreateFlow(runner, cat, recipes);

        Assert.That(flow.StartGame(), Is.True);
        yield return WaitForRunnerState(runner, RecipeRunnerState.AwaitingInput);
        CompleteCurrentWord(typingInput);
        yield return WaitForRecipeIndex(flow, 1);
        Assert.That(flow.CompletedDishes, Is.EqualTo(1));

        Assert.That(flow.RestartGame(), Is.True);
        yield return WaitForRunnerState(runner, RecipeRunnerState.AwaitingInput);

        Assert.That(flow.CurrentRecipeIndex, Is.Zero);
        Assert.That(flow.CompletedDishes, Is.Zero);
        Assert.That(cat.Satisfaction, Is.Zero);
        Assert.That(runner.CurrentRecipe, Is.SameAs(recipes[0]));
    }

    [UnityTest]
    public IEnumerator MidGameRestart_RecoversAndStillCompletesFullBanquet()
    {
        RecipeData[] recipes = CreateRecipes(3);
        (RecipeRunner runner, TypingInput typingInput) = CreateRunner();
        CatController cat = CreateCat(runner);
        GameFlow flow = CreateFlow(runner, cat, recipes);

        Assert.That(flow.StartGame(), Is.True);
        yield return WaitForRunnerState(runner, RecipeRunnerState.AwaitingInput);
        CompleteCurrentWord(typingInput);
        yield return WaitForRecipeIndex(flow, 1);
        yield return WaitForRunnerState(runner, RecipeRunnerState.AwaitingInput);

        typingInput.ProcessCharacter('x');
        Assert.That(typingInput.Progress, Is.Zero);
        typingInput.ProcessCharacter('s');
        Assert.That(typingInput.TypedText, Is.EqualTo("xs"));
        Assert.That(typingInput.HasError, Is.True);
        typingInput.ProcessBackspace();
        typingInput.ProcessBackspace();
        typingInput.ProcessCharacter('s');
        typingInput.ProcessCharacter('e');
        typingInput.ProcessBackspace();
        Assert.That(typingInput.Progress, Is.EqualTo(1));

        Assert.That(flow.RestartGame(), Is.True);
        yield return WaitForRunnerState(runner, RecipeRunnerState.AwaitingInput);
        Assert.That(flow.CurrentRecipeIndex, Is.Zero);
        Assert.That(typingInput.Progress, Is.Zero);

        for (int index = 0; index < recipes.Length; index++)
        {
            CompleteCurrentWord(typingInput);

            if (index < recipes.Length - 1)
            {
                yield return WaitForRecipeIndex(flow, index + 1);
                yield return WaitForRunnerState(
                    runner,
                    RecipeRunnerState.AwaitingInput
                );
            }
        }

        yield return WaitForFlowState(flow, GameFlowState.Completed);
        Assert.That(flow.CompletedDishes, Is.EqualTo(3));
        Assert.That(cat.State, Is.EqualTo(CatVisualState.Satisfied));
    }

    private (RecipeRunner runner, TypingInput typingInput) CreateRunner()
    {
        GameObject runnerObject = new("Flow runner");
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

    private CatController CreateCat(RecipeRunner runner)
    {
        GameObject catObject = new("Flow cat");
        objectsToDestroy.Add(catObject);
        catObject.SetActive(false);
        CatController cat = catObject.AddComponent<CatController>();
        SerializedObject serializedCat = new(cat);
        serializedCat.FindProperty("recipeRunner").objectReferenceValue = runner;
        serializedCat.FindProperty("receivingDuration").floatValue = 0.01f;
        serializedCat.ApplyModifiedPropertiesWithoutUndo();
        catObject.SetActive(true);
        return cat;
    }

    private GameFlow CreateFlow(
        RecipeRunner runner,
        CatController cat,
        RecipeData[] recipes
    )
    {
        GameObject flowObject = new("Game flow");
        objectsToDestroy.Add(flowObject);
        flowObject.SetActive(false);
        GameFlow flow = flowObject.AddComponent<GameFlow>();
        SerializedObject serializedFlow = new(flow);
        serializedFlow.FindProperty("recipeRunner").objectReferenceValue = runner;
        serializedFlow.FindProperty("catController").objectReferenceValue = cat;
        serializedFlow.FindProperty("playOnStart").boolValue = false;
        serializedFlow.FindProperty("dishCelebrationDuration").floatValue = 0f;
        serializedFlow.FindProperty("interRecipeDelay").floatValue = 0f;
        serializedFlow.FindProperty("finalCelebrationDuration").floatValue = 0f;
        serializedFlow.FindProperty("loadCreditsOnComplete").boolValue = false;
        SerializedProperty recipeList = serializedFlow.FindProperty("recipes");
        recipeList.arraySize = recipes.Length;

        for (int index = 0; index < recipes.Length; index++)
        {
            recipeList.GetArrayElementAtIndex(index).objectReferenceValue
                = recipes[index];
        }

        serializedFlow.ApplyModifiedPropertiesWithoutUndo();
        flowObject.SetActive(true);
        return flow;
    }

    private RecipeData[] CreateRecipes(int count)
    {
        RecipeData[] recipes = new RecipeData[count];

        for (int index = 0; index < count; index++)
        {
            RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
            objectsToDestroy.Add(recipe);
            SerializedObject serializedRecipe = new(recipe);
            serializedRecipe.FindProperty("displayName").stringValue
                = $"Recipe {index + 1}";
            serializedRecipe.FindProperty("catOrder").stringValue
                = $"Order {index + 1}";
            SerializedProperty steps = serializedRecipe.FindProperty("steps");
            steps.arraySize = 1;
            SerializedProperty step = steps.GetArrayElementAtIndex(0);
            step.FindPropertyRelative("expectedWord").stringValue = "servir";
            step.FindPropertyRelative("actorId").stringValue = "servicio";
            step.FindPropertyRelative("reactionType").enumValueIndex
                = (int)KitchenReactionType.Serving;
            step.FindPropertyRelative("durationBeforeNextStep").floatValue = 0f;
            serializedRecipe.ApplyModifiedPropertiesWithoutUndo();
            recipes[index] = recipe;
        }

        return recipes;
    }

    private static void CompleteCurrentWord(TypingInput typingInput)
    {
        foreach (char character in typingInput.ExpectedWord)
        {
            typingInput.ProcessCharacter(character);
        }
    }

    private static IEnumerator WaitForRunnerState(
        RecipeRunner runner,
        RecipeRunnerState expected
    )
    {
        const int frameLimit = 40;

        for (int frame = 0; frame < frameLimit; frame++)
        {
            if (runner.State == expected)
            {
                yield break;
            }

            yield return null;
        }

        Assert.That(runner.State, Is.EqualTo(expected));
    }

    private static IEnumerator WaitForRecipeIndex(GameFlow flow, int expected)
    {
        const int frameLimit = 40;

        for (int frame = 0; frame < frameLimit; frame++)
        {
            if (flow.CurrentRecipeIndex == expected
                && flow.State == GameFlowState.PlayingRecipe)
            {
                yield break;
            }

            yield return null;
        }

        Assert.That(flow.CurrentRecipeIndex, Is.EqualTo(expected));
    }

    private static IEnumerator WaitForFlowState(
        GameFlow flow,
        GameFlowState expected
    )
    {
        const int frameLimit = 60;

        for (int frame = 0; frame < frameLimit; frame++)
        {
            if (flow.State == expected)
            {
                yield break;
            }

            yield return null;
        }

        Assert.That(flow.State, Is.EqualTo(expected));
    }
}
