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

    public static void ConfigurePlaygroundAssetBatch()
    {
        EditorSceneManager.OpenScene(PlaygroundPath, OpenSceneMode.Single);
        ConfigurePlayground();
    }

    [MenuItem("Banquet Chain/Configurar cursores de escritura")]
    public static void ConfigureTypingCaretsInOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        PaperWordRenderer[] renderers = Resources
            .FindObjectsOfTypeAll<PaperWordRenderer>()
            .Where(renderer => renderer.gameObject.scene == scene)
            .ToArray();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("La escena abierta no contiene palabras de papel.");
            return;
        }

        foreach (PaperWordRenderer renderer in renderers)
        {
            ConfigureTypingCaret(renderer);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Cursores de escritura configurados: {renderers.Length}.");
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

        RecipeRunner runner = FindFirstInScene<RecipeRunner>(scene);
        Canvas canvas = FindFirstInScene<Canvas>(scene);
        GameFlow gameFlow = FindFirstInScene<GameFlow>(scene);
        KitchenActorCoordinator coordinator =
            FindFirstInScene<KitchenActorCoordinator>(scene);

        if (runner == null || canvas == null || gameFlow == null)
        {
            Debug.LogError("Playground no contiene RecipeRunner, Canvas o GameFlow.");
            return;
        }

        KitchenActor chef1 = RequireWorldActor("Chef1", "despensa", "CHEF 1");
        KitchenActor chef2 = RequireWorldActor("Chef2", "horno", "CHEF 2");
        KitchenActor chef3 = RequireWorldActor("Chef3", "servicio", "CHEF 3");

        if (coordinator == null)
        {
            GameObject coordinatorObject = new("KitchenActorCoordinator");
            coordinatorObject.transform.SetParent(
                chef1 != null ? chef1.transform.parent : null,
                false
            );
            coordinator = coordinatorObject.AddComponent<KitchenActorCoordinator>();
        }

        SetObjectReference(coordinator, "recipeRunner", runner);
        SetObjectReference(
            coordinator,
            "typingInput",
            runner.GetComponent<TypingInput>()
        );

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

        WordBubbleUI wordBubble = FindFirstInScene<WordBubbleUI>(scene);
        if (wordBubble != null)
        {
            SetObjectReference(wordBubble, "recipeRunner", runner);
            SetObjectReference(wordBubble, "gameFlow", gameFlow);
            SetString(wordBubble, "initialWord", string.Empty);
            SetInteger(wordBubble, "revealedRecipeCount", 1);
        }

        foreach (PaperWordRenderer renderer in Resources
            .FindObjectsOfTypeAll<PaperWordRenderer>()
            .Where(renderer => renderer.gameObject.scene == scene))
        {
            ConfigureTypingCaret(renderer);
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

    private static void ConfigureTypingCaret(PaperWordRenderer renderer)
    {
        Transform existing = renderer.transform.Find("TypingCaret");
        GameObject caretObject;

        if (existing == null)
        {
            caretObject = new GameObject(
                "TypingCaret",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            caretObject.transform.SetParent(renderer.transform, false);
        }
        else
        {
            caretObject = existing.gameObject;
        }

        caretObject.layer = renderer.gameObject.layer;
        RectTransform rect = caretObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = GetOrAdd<Image>(caretObject);
        image.color = new Color(0.22f, 0.17f, 0.12f, 1f);
        image.raycastTarget = false;
        image.enabled = false;

        renderer.ConfigureCaret(rect, image);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(caretObject);
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
            actor = actorObject.AddComponent<KitchenActor>();
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

        if (actor == null)
        {
            return;
        }

        if (ConfigureWorldSpriteBubble(
            actor.transform,
            runner,
            WorldRecipeBubbleMode.ActorStep,
            actorId
        ))
        {
            return;
        }

        if (panel == null)
        {
            panel = new GameObject(
                panelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(WorldRecipeBubbleUI)
            );
            panel.transform.SetParent(canvas.transform, false);
            Image newBackground = panel.GetComponent<Image>();
            newBackground.color = new Color(0.96f, 0.9f, 0.72f, 0.95f);
            newBackground.raycastTarget = false;
        }

        KitchenActor oldUiActor = panel.GetComponent<KitchenActor>();

        if (oldUiActor != null)
        {
            oldUiActor.enabled = false;
            SetInteger(oldUiActor, "presentationPriority", -10);
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

        if (ConfigureWorldSpriteBubble(
            bigCat,
            runner,
            WorldRecipeBubbleMode.CatRequest,
            string.Empty
        ))
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

    private static bool ConfigureWorldSpriteBubble(
        Transform actorRoot,
        RecipeRunner runner,
        WorldRecipeBubbleMode mode,
        string actorId
    )
    {
        Transform bubbleRoot = FindDescendant(actorRoot, "IconBubble");

        if (bubbleRoot == null)
        {
            return false;
        }

        Transform iconTransform = FindDescendant(bubbleRoot, "Icon");

        if (iconTransform == null)
        {
            GameObject iconObject = new("Icon");
            iconTransform = iconObject.transform;
            iconTransform.SetParent(bubbleRoot, false);
        }

        SpriteRenderer background = bubbleRoot.GetComponent<SpriteRenderer>();
        SpriteRenderer icon = GetOrAdd<SpriteRenderer>(iconTransform.gameObject);
        iconTransform.localPosition = Vector3.zero;
        iconTransform.localRotation = Quaternion.identity;
        iconTransform.localScale = Vector3.one;
        icon.gameObject.layer = bubbleRoot.gameObject.layer;
        icon.color = Color.white;

        if (background != null)
        {
            icon.sortingLayerID = background.sortingLayerID;
            icon.sortingOrder = background.sortingOrder + 1;
        }

        WorldSpriteRecipeBubbleUI bubble =
            GetOrAdd<WorldSpriteRecipeBubbleUI>(bubbleRoot.gameObject);
        bubble.Configure(
            runner,
            mode,
            actorId,
            icon,
            bubbleRoot.GetComponentsInChildren<SpriteRenderer>(true)
        );
        EditorUtility.SetDirty(bubbleRoot.gameObject);
        EditorUtility.SetDirty(iconTransform.gameObject);
        return true;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => string.Equals(
                candidate.name,
                objectName,
                System.StringComparison.OrdinalIgnoreCase
            ));
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        return target.GetComponent<T>() ?? target.AddComponent<T>();
    }

    private static T FindFirstInScene<T>(Scene scene) where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(candidate =>
                candidate != null && candidate.gameObject.scene == scene
            );
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
