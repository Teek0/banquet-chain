using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RecipeHUDPresenterTests
{
    private RecipeData recipe;

    [TearDown]
    public void TearDown()
    {
        if (recipe != null)
        {
            Object.DestroyImmediate(recipe);
        }
    }

    [Test]
    public void StepList_MarksCompletedActiveAndPendingStepsInOrder()
    {
        recipe = CreateRecipe("pan", "mantequilla", "servir");

        string result = RecipeHUDPresenter.BuildStepList(recipe, 1, 0);

        int completed = result.IndexOf("✓ pan");
        int active = result.IndexOf("▶ mantequilla");
        int pending = result.IndexOf("○ servir");

        Assert.That(completed, Is.GreaterThanOrEqualTo(0));
        Assert.That(active, Is.GreaterThan(completed));
        Assert.That(pending, Is.GreaterThan(active));
    }

    [Test]
    public void Progress_ReportsPresentationActiveAndCompletion()
    {
        recipe = CreateRecipe("pan", "tostar", "servir");

        Assert.That(
            RecipeHUDPresenter.BuildProgress(recipe, -1, -1, false),
            Is.EqualTo("PROGRESO · 0 / 3")
        );
        Assert.That(
            RecipeHUDPresenter.BuildProgress(recipe, 1, 0, false),
            Is.EqualTo("PASO · 2 / 3")
        );
        Assert.That(
            RecipeHUDPresenter.BuildProgress(recipe, -1, 2, true),
            Is.EqualTo("RECETA · 3 / 3")
        );
    }

    [Test]
    public void EmptyRecipe_UsesSafeFallbacks()
    {
        recipe = ScriptableObject.CreateInstance<RecipeData>();

        Assert.That(
            RecipeHUDPresenter.BuildStepList(recipe, 0, -1),
            Is.EqualTo("Sin pasos configurados")
        );
        Assert.That(
            RecipeHUDPresenter.BuildProgress(recipe, 0, -1, false),
            Is.EqualTo("PROGRESO · 0 / 0")
        );
    }

    private static RecipeData CreateRecipe(params string[] words)
    {
        RecipeData result = ScriptableObject.CreateInstance<RecipeData>();
        SerializedObject serializedRecipe = new(result);
        serializedRecipe.FindProperty("displayName").stringValue = "Prueba";
        serializedRecipe.FindProperty("catOrder").stringValue = "Pedido";
        SerializedProperty steps = serializedRecipe.FindProperty("steps");
        steps.arraySize = words.Length;

        for (int index = 0; index < words.Length; index++)
        {
            SerializedProperty step = steps.GetArrayElementAtIndex(index);
            step.FindPropertyRelative("expectedWord").stringValue = words[index];
            step.FindPropertyRelative("actorId").stringValue = $"actor_{index}";
        }

        serializedRecipe.ApplyModifiedPropertiesWithoutUndo();
        return result;
    }
}
