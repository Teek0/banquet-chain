using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TypingTutorialTests
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
    public IEnumerator FirstRecipe_ShowsContextualProgressAndHidesOnCompletion()
    {
        Setup setup = CreateSetup(CreateRecipe("Pan", "pan"));
        setup.Runner.StartRecipe(setup.TutorialRecipe);
        Assert.That(setup.Tutorial.Stage, Is.EqualTo(TypingTutorialStage.Intro));
        yield return WaitForState(setup.Runner, RecipeRunnerState.AwaitingInput);

        setup.Input.ProcessCharacter('p');
        Assert.That(
            setup.Tutorial.Stage,
            Is.EqualTo(TypingTutorialStage.CorrectLetter)
        );

        setup.Input.ProcessCharacter('x');
        Assert.That(
            setup.Tutorial.Stage,
            Is.EqualTo(TypingTutorialStage.ErrorExplained)
        );
        StringAssert.Contains("BACKSPACE", setup.Tutorial.Message);
        Assert.That(setup.Input.TypedText, Is.EqualTo("px"));

        setup.Input.ProcessBackspace();
        setup.Input.ProcessCharacter('a');
        setup.Input.ProcessCharacter('n');
        Assert.That(
            setup.Tutorial.Stage,
            Is.EqualTo(TypingTutorialStage.WordCompleted)
        );
        yield return WaitForState(setup.Runner, RecipeRunnerState.AwaitingInput);
        CompleteCurrentWord(setup.Input);
        yield return WaitForState(setup.Runner, RecipeRunnerState.RecipeCompleted);

        Assert.That(setup.Tutorial.Stage, Is.EqualTo(TypingTutorialStage.Completed));
        Assert.That(setup.Tutorial.IsVisible, Is.False);
    }

    [UnityTest]
    public IEnumerator LaterRecipe_DoesNotShowTutorial()
    {
        RecipeData tutorialRecipe = CreateRecipe("Pan", "pan");
        Setup setup = CreateSetup(tutorialRecipe);
        RecipeData laterRecipe = CreateRecipe("Sopa", "sopa");
        setup.Runner.StartRecipe(laterRecipe);
        yield return null;

        Assert.That(setup.Tutorial.Stage, Is.EqualTo(TypingTutorialStage.Hidden));
        Assert.That(setup.Tutorial.IsVisible, Is.False);
    }

    [UnityTest]
    public IEnumerator RestartingFirstRecipe_ReplaysTutorial()
    {
        Setup setup = CreateSetup(CreateRecipe("Pan", "pan"));
        setup.Runner.StartRecipe(setup.TutorialRecipe);
        yield return WaitForState(setup.Runner, RecipeRunnerState.AwaitingInput);
        CompleteCurrentWord(setup.Input);
        yield return WaitForState(setup.Runner, RecipeRunnerState.AwaitingInput);
        CompleteCurrentWord(setup.Input);
        yield return WaitForState(setup.Runner, RecipeRunnerState.RecipeCompleted);
        Assert.That(setup.Tutorial.Stage, Is.EqualTo(TypingTutorialStage.Completed));

        setup.Runner.RestartRecipe();
        yield return null;

        Assert.That(setup.Tutorial.Stage, Is.EqualTo(TypingTutorialStage.Intro));
        Assert.That(setup.Tutorial.IsVisible, Is.True);
    }

    private Setup CreateSetup(RecipeData tutorialRecipe)
    {
        GameObject runnerObject = new("Runner");
        GameObject tutorialObject = new("Tutorial");
        GameObject labelObject = new("Label");
        objectsToDestroy.Add(runnerObject);
        objectsToDestroy.Add(tutorialObject);
        objectsToDestroy.Add(tutorialRecipe);
        runnerObject.SetActive(false);
        tutorialObject.SetActive(false);
        labelObject.transform.SetParent(tutorialObject.transform);

        TypingInput input = runnerObject.AddComponent<TypingInput>();
        RecipeRunner runner = runnerObject.AddComponent<RecipeRunner>();
        SerializedObject serializedRunner = new(runner);
        serializedRunner.FindProperty("typingInput").objectReferenceValue = input;
        serializedRunner.FindProperty("playOnStart").boolValue = false;
        serializedRunner.FindProperty("orderPresentationDuration").floatValue = 0f;
        serializedRunner.ApplyModifiedPropertiesWithoutUndo();

        CanvasGroup panel = tutorialObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        TypingTutorial tutorial = tutorialObject.AddComponent<TypingTutorial>();
        SerializedObject serializedTutorial = new(tutorial);
        serializedTutorial.FindProperty("recipeRunner").objectReferenceValue
            = runner;
        serializedTutorial.FindProperty("typingInput").objectReferenceValue
            = input;
        serializedTutorial.FindProperty("tutorialRecipe").objectReferenceValue
            = tutorialRecipe;
        serializedTutorial.FindProperty("panel").objectReferenceValue = panel;
        serializedTutorial.FindProperty("messageLabel").objectReferenceValue
            = label;
        serializedTutorial.ApplyModifiedPropertiesWithoutUndo();
        runnerObject.SetActive(true);
        tutorialObject.SetActive(true);

        return new Setup(runner, input, tutorial, tutorialRecipe);
    }

    private RecipeData CreateRecipe(string name, string firstWord)
    {
        RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();
        SerializedObject serializedRecipe = new(recipe);
        serializedRecipe.FindProperty("displayName").stringValue = name;
        serializedRecipe.FindProperty("catOrder").stringValue = "Pedido";
        SerializedProperty steps = serializedRecipe.FindProperty("steps");
        steps.arraySize = 2;
        ConfigureStep(steps.GetArrayElementAtIndex(0), firstWord, false);
        ConfigureStep(steps.GetArrayElementAtIndex(1), "servir", true);
        serializedRecipe.ApplyModifiedPropertiesWithoutUndo();
        return recipe;
    }

    private static void ConfigureStep(
        SerializedProperty step,
        string word,
        bool serving
    )
    {
        step.FindPropertyRelative("expectedWord").stringValue = word;
        step.FindPropertyRelative("actorId").stringValue = "servicio";
        step.FindPropertyRelative("reactionType").enumValueIndex = serving
            ? (int)KitchenReactionType.Serving
            : (int)KitchenReactionType.Ingredient;
        step.FindPropertyRelative("durationBeforeNextStep").floatValue = 0f;
    }

    private static void CompleteCurrentWord(TypingInput input)
    {
        while (!input.IsComplete)
        {
            input.ProcessCharacter(input.ExpectedWord[input.Progress]);
        }
    }

    private static IEnumerator WaitForState(
        RecipeRunner runner,
        RecipeRunnerState expected
    )
    {
        for (int frame = 0; frame < 12; frame++)
        {
            if (runner.State == expected)
            {
                yield break;
            }

            yield return null;
        }

        Assert.That(runner.State, Is.EqualTo(expected));
    }

    private sealed class Setup
    {
        public Setup(
            RecipeRunner runner,
            TypingInput input,
            TypingTutorial tutorial,
            RecipeData tutorialRecipe
        )
        {
            Runner = runner;
            Input = input;
            Tutorial = tutorial;
            TutorialRecipe = tutorialRecipe;
        }

        public RecipeRunner Runner { get; }
        public TypingInput Input { get; }
        public TypingTutorial Tutorial { get; }
        public RecipeData TutorialRecipe { get; }
    }
}
