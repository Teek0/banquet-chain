using UnityEngine;

public sealed class WorldSpriteRecipeBubbleUI : MonoBehaviour
{
    public static event System.Action BubbleShown;

    [Header("Source")]
    [SerializeField] private RecipeRunner recipeRunner;
    [SerializeField] private WorldRecipeBubbleMode mode;
    [SerializeField] private string actorId = string.Empty;

    [Header("Scene references")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private SpriteRenderer[] bubbleRenderers =
        System.Array.Empty<SpriteRenderer>();
    [SerializeField, Range(0.1f, 0.9f)] private float iconFillRatio = 0.62f;
    [SerializeField, Min(0.01f)] private float iconMaxLocalSize = 6.5f;

    private bool isSubscribed;
    private bool isVisible;

    public bool CanPresentStep(RecipeStep step)
    {
        return isActiveAndEnabled
            && mode == WorldRecipeBubbleMode.ActorStep
            && iconRenderer != null
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
        SpriteRenderer icon,
        SpriteRenderer[] renderers
    )
    {
        Unsubscribe();
        recipeRunner = runner;
        mode = bubbleMode;
        actorId = stepActorId ?? string.Empty;
        iconRenderer = icon;
        bubbleRenderers = renderers ?? System.Array.Empty<SpriteRenderer>();
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

    private void ResolveReferences()
    {
        recipeRunner ??= FindFirstObjectByType<RecipeRunner>();

        if (bubbleRenderers == null || bubbleRenderers.Length == 0)
        {
            bubbleRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (iconRenderer == null)
        {
            Transform icon = transform.Find("Icon");
            iconRenderer = icon != null
                ? icon.GetComponent<SpriteRenderer>()
                : null;
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

        if (CanPresentStep(step))
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
        if (iconRenderer == null || sprite == null)
        {
            Hide();
            return;
        }

        iconRenderer.sprite = sprite;
        FitIcon(sprite);
        SetRenderersEnabled(true);

        if (!isVisible)
        {
            isVisible = true;
            BubbleShown?.Invoke();
        }
    }

    private void Hide()
    {
        SetRenderersEnabled(false);
        isVisible = false;
    }

    private void SetRenderersEnabled(bool visible)
    {
        if (bubbleRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in bubbleRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private void FitIcon(Sprite sprite)
    {
        Vector2 size = sprite.bounds.size;
        float largestSide = Mathf.Max(size.x, size.y);
        float targetSize = ResolveIconTargetSize();
        float scale = largestSide > 0f
            ? targetSize / largestSide
            : 1f;
        Transform iconTransform = iconRenderer.transform;
        iconTransform.localPosition = Vector3.zero;
        iconTransform.localRotation = Quaternion.identity;
        iconTransform.localScale = Vector3.one * scale;
    }

    private float ResolveIconTargetSize()
    {
        if (bubbleRenderers != null)
        {
            foreach (SpriteRenderer renderer in bubbleRenderers)
            {
                if (renderer == null || renderer == iconRenderer
                    || renderer.sprite == null)
                {
                    continue;
                }

                Vector2 bubbleSize = renderer.sprite.bounds.size;
                float innerSize = Mathf.Min(bubbleSize.x, bubbleSize.y)
                    * iconFillRatio;
                return Mathf.Min(iconMaxLocalSize, innerSize);
            }
        }

        return iconMaxLocalSize;
    }
}
