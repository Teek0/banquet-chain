using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
