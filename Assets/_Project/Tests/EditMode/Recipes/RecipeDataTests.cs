using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RecipeDataTests
{
    private const string RecipeFolder = "Assets/_Project/Data/Recipes";

    [TestCase("Recipe_PanCaliente.asset")]
    [TestCase("Recipe_SopaVerduras.asset")]
    [TestCase("Recipe_PlatoDelPueblo.asset")]
    public void ProjectRecipe_IsValid(string fileName)
    {
        RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(
            $"{RecipeFolder}/{fileName}"
        );

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe.GetValidationMessages(), Is.Empty);
    }

    [Test]
    public void EmptyRecipe_ReportsUsefulMessage()
    {
        RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();

        try
        {
            Assert.That(
                recipe.GetValidationMessages(),
                Contains.Item("La receta no contiene pasos.")
            );
        }
        finally
        {
            Object.DestroyImmediate(recipe);
        }
    }

    [Test]
    public void InvalidStep_ReportsWordActorAndFinalServing()
    {
        RecipeData recipe = ScriptableObject.CreateInstance<RecipeData>();

        try
        {
            SerializedObject serializedRecipe = new(recipe);
            SerializedProperty steps = serializedRecipe.FindProperty("steps");
            steps.arraySize = 1;
            SerializedProperty step = steps.GetArrayElementAtIndex(0);
            step.FindPropertyRelative("expectedWord").stringValue = string.Empty;
            step.FindPropertyRelative("actorId").stringValue = string.Empty;
            serializedRecipe.ApplyModifiedPropertiesWithoutUndo();

            List<string> messages = recipe.GetValidationMessages();

            Assert.That(
                messages,
                Contains.Item("Paso 1: la palabra esperada está vacía.")
            );
            Assert.That(
                messages,
                Contains.Item("Paso 1: falta el identificador del actor.")
            );
            Assert.That(
                messages,
                Contains.Item("El último paso debería usar la palabra 'servir'.")
            );
        }
        finally
        {
            Object.DestroyImmediate(recipe);
        }
    }
}
