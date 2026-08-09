using UnityEngine;
using UnityEngine.UI;

public enum WorldRecipeBubbleMode
{
    CatRequest,
    ActorStep
}

public sealed class WorldRecipeBubbleUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private RecipeRunner recipeRunner;
    [SerializeField] private WorldRecipeBubbleMode mode;
    [SerializeField] private string actorId = string.Empty;

    [Header("Scene references")]
    [SerializeField] private Transform worldTarget;
    [SerializeField] private Vector3 worldOffset = new(0f, 1.5f, 0f);
    [SerializeField] private RectTransform bubbleRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Camera worldCamera;

    private bool isSubscribed;

    public bool CanPresentStep(RecipeStep step)
    {
        return isActiveAndEnabled
            && mode == WorldRecipeBubbleMode.ActorStep
            && worldTarget != null
            && iconImage != null
            && step?.Icon != null
            && string.Equals(
                step.ActorId?.Trim(),
                actorId.Trim(),
                System.StringComparison.OrdinalIgnoreCase
            );
    }

    public void Configure(
        RecipeRunner runner,
        WorldRecipeBubbleMode bubbleMode,
        string stepActorId,
        Transform target,
        Vector3 offset,
        RectTransform root,
        CanvasGroup group,
        Image icon,
        Canvas canvas
    )
    {
        Unsubscribe();
        recipeRunner = runner;
        mode = bubbleMode;
        actorId = stepActorId ?? string.Empty;
        worldTarget = target;
        worldOffset = offset;
        bubbleRoot = root;
        canvasGroup = group;
        iconImage = icon;
        parentCanvas = canvas;
        ResolveReferences();
        Subscribe();
        Hide();
    }

    private void Awake()
    {
        ResolveReferences();
        Hide();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        Hide();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void ResolveReferences()
    {
        recipeRunner ??= FindFirstObjectByType<RecipeRunner>();
        bubbleRoot ??= transform as RectTransform;
        canvasGroup ??= GetComponent<CanvasGroup>();
        parentCanvas ??= GetComponentInParent<Canvas>();

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private void Subscribe()
    {
        if (isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeStarted += HandleRecipeStarted;
        recipeRunner.StepStarted += HandleStepStarted;
        recipeRunner.StepCompleted += HandleStepCompleted;
        recipeRunner.RecipeCompleted += HandleRecipeCompleted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeStarted -= HandleRecipeStarted;
        recipeRunner.StepStarted -= HandleStepStarted;
        recipeRunner.StepCompleted -= HandleStepCompleted;
        recipeRunner.RecipeCompleted -= HandleRecipeCompleted;
        isSubscribed = false;
    }

    private void HandleRecipeStarted(RecipeData recipe)
    {
        if (mode == WorldRecipeBubbleMode.CatRequest)
        {
            Show(recipe?.Icon);
        }
        else
        {
            Hide();
        }
    }

    private void HandleStepStarted(RecipeStep step, int _)
    {
        if (mode != WorldRecipeBubbleMode.ActorStep)
        {
            return;
        }

        if (step != null && string.Equals(
            step.ActorId?.Trim(),
            actorId.Trim(),
            System.StringComparison.OrdinalIgnoreCase
        ))
        {
            Show(step.Icon);
        }
        else
        {
            Hide();
        }
    }

    private void HandleStepCompleted(RecipeStep step, int _)
    {
        if (mode == WorldRecipeBubbleMode.ActorStep
            && step != null
            && string.Equals(
                step.ActorId?.Trim(),
                actorId.Trim(),
                System.StringComparison.OrdinalIgnoreCase
            ))
        {
            Hide();
        }
    }

    private void HandleRecipeCompleted(RecipeData _)
    {
        Hide();
    }

    private void Show(Sprite sprite)
    {
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
            iconImage.preserveAspect = true;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = sprite != null ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        FollowTarget();
    }

    private void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void FollowTarget()
    {
        if (worldTarget == null || bubbleRoot == null || parentCanvas == null)
        {
            return;
        }

        Camera canvasCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            worldCamera,
            worldTarget.position + worldOffset
        );

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPoint,
            canvasCamera,
            out Vector2 localPoint
        ))
        {
            bubbleRoot.anchoredPosition = localPoint;
        }
    }
}
