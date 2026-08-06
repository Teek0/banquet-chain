using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PresentationSceneTests
{
    [Test]
    public void MainMenu_ExplainsPremiseAndControls()
    {
        Scene scene = OpenScene("Assets/_Project/Scenes/MainMenu.unity");

        try
        {
            string allText = ReadAllText(scene);
            StringAssert.Contains("LA CADENA DEL BANQUETE", allText);
            StringAssert.Contains("ESCRIBE", allText);
            StringAssert.Contains("BACKSPACE", allText);
            StringAssert.Contains("ESC", allText);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void Credits_ContainEditableAuthorshipAndLicenses()
    {
        Scene scene = OpenScene("Assets/_Project/Scenes/Credits.unity");

        try
        {
            string allText = ReadAllText(scene);
            StringAssert.Contains("AUTORÍA", allText);
            StringAssert.Contains("ARTE", allText);
            StringAssert.Contains("AUDIO", allText);
            StringAssert.Contains("LICENCIAS", allText);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void Playground_FinalCommunicatesCollectiveThemeAndCredits()
    {
        Scene scene = OpenScene("Assets/_Project/Scenes/Playground.unity");

        try
        {
            GameFlow flow = FindInScene<GameFlow>(scene);
            Assert.That(flow, Is.Not.Null);
            SerializedObject serialized = new(flow);
            string message = serialized.FindProperty("finalMessage").stringValue;
            StringAssert.Contains("TODO EL PUEBLO", message);
            StringAssert.Contains("RONRONEO", message);
            Assert.That(
                serialized.FindProperty("finalCelebrationDuration").floatValue,
                Is.GreaterThanOrEqualTo(5f)
            );
            bool hasNavigation = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Any(component => component != null
                    && component.GetType().Name == "GameFlowSceneNavigation");
            Assert.That(hasNavigation, Is.True);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [TestCase("Assets/_Project/Scenes/MainMenu.unity")]
    [TestCase("Assets/_Project/Scenes/Playground.unity")]
    [TestCase("Assets/_Project/Scenes/Credits.unity")]
    public void Scene_UsesTargetReferenceResolution(string scenePath)
    {
        Scene scene = OpenScene(scenePath);

        try
        {
            CanvasScaler scaler = FindInScene<CanvasScaler>(scene);
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(960f, 600f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.01f));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void Playground_ActiveWordIsLargeAndAutoSized()
    {
        Scene scene = OpenScene("Assets/_Project/Scenes/Playground.unity");

        try
        {
            TMP_Text word = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                .First(label => label.name == "WordLabel");
            Assert.That(word.fontSize, Is.GreaterThanOrEqualTo(52f));
            Assert.That(word.enableAutoSizing, Is.True);
            Assert.That(word.fontSizeMin, Is.GreaterThanOrEqualTo(38f));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void Recipes_KeepStepDelaysWithinReadableRhythm()
    {
        string[] paths =
        {
            "Assets/_Project/Data/Recipes/Recipe_PanCaliente.asset",
            "Assets/_Project/Data/Recipes/Recipe_SopaVerduras.asset",
            "Assets/_Project/Data/Recipes/Recipe_PlatoDelPueblo.asset"
        };

        foreach (string path in paths)
        {
            RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);

            foreach (RecipeStep step in recipe.Steps)
            {
                Assert.That(
                    step.DurationBeforeNextStep,
                    Is.InRange(0.2f, 0.6f),
                    $"{recipe.DisplayName}: {step.ExpectedWord}"
                );
            }
        }
    }

    private static Scene OpenScene(string path)
    {
        return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    }

    private static string ReadAllText(Scene scene)
    {
        return string.Join(
            "\n",
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                .Select(label => label.text)
        );
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .Select(root => root.GetComponentInChildren<T>(true))
            .FirstOrDefault(component => component != null);
    }
}
