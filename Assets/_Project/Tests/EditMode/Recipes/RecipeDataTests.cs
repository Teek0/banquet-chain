using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RecipeDataTests
{
    private const string RecipeFolder = "Assets/_Project/Data/Recipes";
    private static readonly HashSet<string> MountedActorIds = new()
    {
        "despensa",
        "horno",
        "servicio"
    };

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

    [TestCase(
        "Recipe_PanCaliente.asset",
        "pan|mantequilla|tostar|servir"
    )]
    [TestCase(
        "Recipe_SopaVerduras.asset",
        "tomate|zanahoria|cortar|agua|hervir|mezclar|servir"
    )]
    [TestCase(
        "Recipe_PlatoDelPueblo.asset",
        "pescado|limón|verduras|cortar|condimentar|cocinar|masa|frutas|mezclar|hornear|decorar|compartir|servir"
    )]
    public void ProjectRecipe_HasDefinitiveSequence(
        string fileName,
        string expectedSequence
    )
    {
        RecipeData recipe = LoadProjectRecipe(fileName);
        string[] words = recipe.Steps
            .Select(step => step.ExpectedWord)
            .ToArray();

        Assert.That(words, Is.EqualTo(expectedSequence.Split('|')));
        Assert.That(recipe.Steps[^1].ReactionType, Is.EqualTo(KitchenReactionType.Serving));
    }

    [TestCase("Recipe_PanCaliente.asset")]
    [TestCase("Recipe_SopaVerduras.asset")]
    [TestCase("Recipe_PlatoDelPueblo.asset")]
    public void ProjectRecipe_UsesOnlyMountedActors(string fileName)
    {
        RecipeData recipe = LoadProjectRecipe(fileName);

        Assert.That(
            recipe.Steps.Select(step => step.ActorId),
            Is.All.Matches<string>(MountedActorIds.Contains)
        );
    }

    [Test]
    public void FinalRecipe_UsesEveryActorAndCollaborationBeforeServing()
    {
        RecipeData recipe = LoadProjectRecipe("Recipe_PlatoDelPueblo.asset");
        HashSet<string> contributors = recipe.Steps
            .Select(step => step.ActorId)
            .ToHashSet();

        Assert.That(contributors, Is.SupersetOf(MountedActorIds));
        Assert.That(
            recipe.Steps[^2].ReactionType,
            Is.EqualTo(KitchenReactionType.Collaboration)
        );
        Assert.That(recipe.Steps[^2].ExpectedWord, Is.EqualTo("compartir"));
    }

    [Test]
    public void Limon_IsEquivalentWithOrWithoutAccent()
    {
        RecipeData recipe = LoadProjectRecipe("Recipe_PlatoDelPueblo.asset");
        RecipeStep lemon = recipe.Steps.Single(
            step => step.ExpectedWord == "limón"
        );

        Assert.That(
            TypingTextNormalizer.AreEquivalent(lemon.ExpectedWord, "limon"),
            Is.True
        );
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
                Contains.Item("El último paso debería ser de servicio.")
            );
        }
        finally
        {
            Object.DestroyImmediate(recipe);
        }
    }

    private static RecipeData LoadProjectRecipe(string fileName)
    {
        RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(
            $"{RecipeFolder}/{fileName}"
        );
        Assert.That(recipe, Is.Not.Null);
        return recipe;
    }
}
