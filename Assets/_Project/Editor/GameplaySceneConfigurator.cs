#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameplaySceneConfigurator
{
    private const string PlaygroundPath = "Assets/_Project/Scenes/Playground.unity";
    private const string SessionKey = "BanquetChain.GameplaySceneConfigured";

    [InitializeOnLoadMethod]
    private static void ConfigureOpenPlaygroundOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode
                || SessionState.GetBool(SessionKey, false)
                || SceneManager.GetActiveScene().path != PlaygroundPath)
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            ConfigurePlayground();
        };
    }

    [MenuItem("Banquet Chain/Configurar gameplay en escenario")]
    public static void ConfigurePlayground()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != PlaygroundPath)
        {
            Debug.LogError("Abre la escena Playground antes de configurarla.");
            return;
        }

        RecipeRunner runner = Object.FindFirstObjectByType<RecipeRunner>();
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameFlow gameFlow = Object.FindFirstObjectByType<GameFlow>();
        KitchenActorCoordinator coordinator =
            Object.FindFirstObjectByType<KitchenActorCoordinator>();

        if (runner == null || canvas == null || gameFlow == null)
        {
            Debug.LogError("Playground no contiene RecipeRunner, Canvas o GameFlow.");
            return;
        }

        KitchenActor chef1 = RequireWorldActor("Chef1", "despensa", "CHEF 1");
        KitchenActor chef2 = RequireWorldActor("Chef2", "horno", "CHEF 2");
        KitchenActor chef3 = RequireWorldActor("Chef3", "servicio", "CHEF 3");

        ConfigureActorBubble(
            "Actor_Despensa", chef1, "despensa", runner, canvas
        );
        ConfigureActorBubble("Actor_Horno", chef2, "horno", runner, canvas);
        ConfigureActorBubble(
            "Actor_Servicio", chef3, "servicio", runner, canvas
        );

        Transform bigCat = FindSceneObject("BigCat")?.transform;
        ConfigureCatRequestBubble(bigCat, runner, canvas);
        CatController catController = ConfigureCatController(bigCat, runner);
        SetObjectReference(gameFlow, "catController", catController);
        SetFloat(gameFlow, "finalEatingDuration", 1.2f);
        SetFloat(gameFlow, "finalSleepingDuration", 1.5f);

        WordBubbleUI wordBubble = Object.FindFirstObjectByType<WordBubbleUI>();
        if (wordBubble != null)
        {
            SetObjectReference(wordBubble, "gameFlow", gameFlow);
            SetString(wordBubble, "initialWord", string.Empty);
            SetInteger(wordBubble, "revealedRecipeCount", 1);
        }

        RecipeHUDUI recipeHud = Object.FindFirstObjectByType<RecipeHUDUI>();
        if (recipeHud != null)
        {
            SetObjectReference(recipeHud, "gameFlow", gameFlow);
            SetInteger(recipeHud, "detailedRecipeCount", 1);
        }

        if (coordinator != null)
        {
            SerializedObject serializedCoordinator = new(coordinator);
            SerializedProperty actors = serializedCoordinator.FindProperty("actors");
            KitchenActor[] worldActors = { chef1, chef2, chef3 };
            actors.arraySize = worldActors.Length;

            for (int index = 0; index < worldActors.Length; index++)
            {
                actors.GetArrayElementAtIndex(index).objectReferenceValue =
                    worldActors[index];
            }

            serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();
            coordinator.RebuildActorIndex();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Playground configurado: chefs, burbujas y gato viven en la escena.");
    }

    private static KitchenActor RequireWorldActor(
        string objectName,
        string actorId,
        string displayName
    )
    {
        GameObject actorObject = FindSceneObject(objectName);

        if (actorObject == null)
        {
            Debug.LogError($"No se encontro {objectName} en Playground.");
            return null;
        }

        KitchenActor actor = actorObject.GetComponent<KitchenActor>();

        if (actor == null)
        {
            actor = Undo.AddComponent<KitchenActor>(actorObject);
        }

        actor.ConfigureIdentity(actorId, displayName);
        SetInteger(actor, "presentationPriority", 10);
        SetFloat(actor, "motionDistance", 0.12f);
        return actor;
    }

    private static void ConfigureActorBubble(
        string panelName,
        KitchenActor actor,
        string actorId,
        RecipeRunner runner,
        Canvas canvas
    )
    {
        GameObject panel = FindSceneObject(panelName);

        if (panel == null || actor == null)
        {
            return;
        }

        KitchenActor oldUiActor = panel.GetComponent<KitchenActor>();

        if (oldUiActor != null)
        {
            Undo.DestroyObjectImmediate(oldUiActor);
        }

        RectTransform root = panel.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = new Vector2(112f, 112f);

        foreach (TMP_Text label in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            label.enabled = false;
        }

        CanvasGroup group = GetOrAdd<CanvasGroup>(panel);
        group.alpha = 0f;
        Image icon = GetOrCreateIcon(panel.transform, "FoodIcon", 82f);
        WorldRecipeBubbleUI bubble = GetOrAdd<WorldRecipeBubbleUI>(panel);
        bubble.Configure(
            runner,
            WorldRecipeBubbleMode.ActorStep,
            actorId,
            actor.transform,
            new Vector3(0f, 1.25f, 0f),
            root,
            group,
            icon,
            canvas
        );
        EditorUtility.SetDirty(panel);
    }

    private static void ConfigureCatRequestBubble(
        Transform bigCat,
        RecipeRunner runner,
        Canvas canvas
    )
    {
        if (bigCat == null)
        {
            return;
        }

        GameObject bubbleObject = FindSceneObject("SunRequestBubble");

        if (bubbleObject == null)
        {
            bubbleObject = new GameObject(
                "SunRequestBubble",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(WorldRecipeBubbleUI)
            );
            Undo.RegisterCreatedObjectUndo(bubbleObject, "Crear burbuja de SUN");
            bubbleObject.transform.SetParent(canvas.transform, false);
        }

        RectTransform root = bubbleObject.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = new Vector2(132f, 132f);
        Image background = bubbleObject.GetComponent<Image>();
        background.color = new Color(0.96f, 0.9f, 0.72f, 0.95f);
        background.raycastTarget = false;
        Image icon = GetOrCreateIcon(bubbleObject.transform, "DishIcon", 98f);
        CanvasGroup group = bubbleObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        bubbleObject.GetComponent<WorldRecipeBubbleUI>().Configure(
            runner,
            WorldRecipeBubbleMode.CatRequest,
            string.Empty,
            bigCat,
            new Vector3(0f, 2.2f, 0f),
            root,
            group,
            icon,
            canvas
        );
        EditorUtility.SetDirty(bubbleObject);
    }

    private static CatController ConfigureCatController(
        Transform bigCat,
        RecipeRunner runner
    )
    {
        if (bigCat == null)
        {
            return null;
        }

        GameObject sleepingCat = FindSceneObject("BigCatSleeping");

        if (sleepingCat == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/BigCatSleeping.prefab"
            );
            sleepingCat = PrefabUtility.InstantiatePrefab(
                prefab,
                bigCat.gameObject.scene
            ) as GameObject;
            Undo.RegisterCreatedObjectUndo(sleepingCat, "Agregar gato dormido");
        }

        sleepingCat.transform.SetParent(bigCat.parent, false);
        sleepingCat.transform.localPosition = bigCat.localPosition;
        sleepingCat.transform.localRotation = bigCat.localRotation;
        sleepingCat.transform.localScale = bigCat.localScale;
        sleepingCat.SetActive(false);

        GameObject host = FindSceneObject("CatFlowActor");

        if (host == null)
        {
            host = new GameObject("CatFlowActor");
            Undo.RegisterCreatedObjectUndo(host, "Crear controlador de SUN");
            host.transform.SetParent(bigCat.parent, false);
        }

        CatController controller = GetOrAdd<CatController>(host);
        SerializedObject serialized = new(controller);
        serialized.FindProperty("recipeRunner").objectReferenceValue = runner;
        serialized.FindProperty("visualRoot").objectReferenceValue = bigCat;
        serialized.FindProperty("awakeVisual").objectReferenceValue = bigCat.gameObject;
        serialized.FindProperty("sleepingVisual").objectReferenceValue = sleepingCat;
        serialized.FindProperty("animator").objectReferenceValue =
            bigCat.GetComponentInChildren<Animator>(true);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(host);
        return controller;
    }

    private static Image GetOrCreateIcon(
        Transform parent,
        string objectName,
        float size
    )
    {
        Transform existing = parent.Find(objectName);
        GameObject iconObject;

        if (existing == null)
        {
            iconObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            Undo.RegisterCreatedObjectUndo(iconObject, $"Crear {objectName}");
            iconObject.transform.SetParent(parent, false);
        }
        else
        {
            iconObject = existing.gameObject;
        }

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        Image image = iconObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        return target.GetComponent<T>() ?? Undo.AddComponent<T>(target);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(candidate =>
                candidate.scene.IsValid()
                && candidate.scene == SceneManager.GetActiveScene()
                && candidate.name == objectName
            );
    }

    private static void SetObjectReference(
        Object target,
        string propertyName,
        Object value
    )
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetInteger(Object target, string propertyName, int value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetString(
        Object target,
        string propertyName,
        string value
    )
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
#endif
