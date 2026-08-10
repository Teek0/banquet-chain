using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CatVisualState
{
    Hungry,
    Requesting,
    Waiting,
    Receiving,
    Relaxed,
    Satisfied,
    Sleeping
}

public sealed class CatController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private RecipeRunner recipeRunner;

    [Header("Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private GameObject awakeVisual;
    [SerializeField] private GameObject sleepingVisual;
    [SerializeField] private Graphic bodyGraphic;
    [SerializeField] private TMP_Text stateLabel;
    [SerializeField] private TMP_Text requestLabel;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject[] defaultExpressionParts =
        System.Array.Empty<GameObject>();
    [SerializeField] private GameObject[] happyExpressionParts =
        System.Array.Empty<GameObject>();
    [SerializeField] private string visualStateParameter = "VisualState";
    [SerializeField] private Color hungryColor = new(0.43f, 0.34f, 0.48f, 1f);
    [SerializeField] private Color requestingColor = new(0.78f, 0.48f, 0.22f, 1f);
    [SerializeField] private Color relaxedColor = new(0.46f, 0.62f, 0.72f, 1f);
    [SerializeField] private Color satisfiedColor = new(0.38f, 0.72f, 0.48f, 1f);

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float receivingDuration = 0.6f;
    [SerializeField, Min(0f)] private float transitionSpeed = 14f;
    [SerializeField, Min(0f)] private float receivingBounceHeight = 0.45f;
    [SerializeField, Min(0f)] private float satisfiedBounceHeight = 0.3f;
    [SerializeField, Min(0f)] private float sleepingCrossfadeDuration = 1.5f;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private bool hasCapturedBaseTransform;
    private bool isSubscribed;
    private float receivingTimeRemaining;
    private float animationClock;
    private float dishCelebrationTimeRemaining;
    private float sleepingCrossfadeElapsed;
    private bool sleepingCrossfadeActive;
    private SpriteRenderer[] awakeSpriteRenderers =
        System.Array.Empty<SpriteRenderer>();
    private SpriteRenderer[] sleepingSpriteRenderers =
        System.Array.Empty<SpriteRenderer>();
    private Graphic[] awakeGraphics = System.Array.Empty<Graphic>();
    private Graphic[] sleepingGraphics = System.Array.Empty<Graphic>();
    private float[] awakeSpriteAlphas = System.Array.Empty<float>();
    private float[] sleepingSpriteAlphas = System.Array.Empty<float>();
    private float[] awakeGraphicAlphas = System.Array.Empty<float>();
    private float[] sleepingGraphicAlphas = System.Array.Empty<float>();
    private CatVisualState afterReceivingState = CatVisualState.Relaxed;
    private CatVisualState renderedState = (CatVisualState)(-1);

    public CatVisualState State { get; private set; } = CatVisualState.Hungry;
    public int Satisfaction { get; private set; }
    public string CurrentRequest { get; private set; } = string.Empty;
    public bool IsReceiving => State == CatVisualState.Receiving;

    private void Awake()
    {
        ResolveReferences();
        CacheCrossfadeTargets();
        CaptureBaseTransform();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheCrossfadeTargets();
        CaptureBaseTransform();
        Subscribe();
        ApplyVisuals(0f, true);
        RefreshLabels(true);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        AdvanceVisual(Time.deltaTime);
    }

    public void BeginBanquet()
    {
        Satisfaction = 0;
        CurrentRequest = string.Empty;
        receivingTimeRemaining = 0f;
        dishCelebrationTimeRemaining = 0f;
        afterReceivingState = CatVisualState.Relaxed;
        animationClock = 0f;
        CancelSleepingCrossfade();
        State = CatVisualState.Hungry;
        RefreshLabels(true);
        ApplyVisuals(0f, true);
    }

    public void ShowRequest(RecipeData recipe)
    {
        CurrentRequest = recipe?.CatOrder ?? string.Empty;
        receivingTimeRemaining = 0f;
        dishCelebrationTimeRemaining = 0f;
        State = CatVisualState.Requesting;
        RefreshLabels(true);
    }

    public void PlayDishCelebration(float duration)
    {
        dishCelebrationTimeRemaining = Mathf.Max(0.05f, duration);
        RefreshExpression();
    }

    public void SetWaitingForKitchen()
    {
        if (State == CatVisualState.Receiving
            || State == CatVisualState.Satisfied)
        {
            return;
        }

        State = CatVisualState.Waiting;
        RefreshLabels();
    }

    public void PlayReceiving()
    {
        dishCelebrationTimeRemaining = 0f;
        receivingTimeRemaining = Mathf.Max(0.05f, receivingDuration);
        afterReceivingState = Satisfaction >= 3
            ? CatVisualState.Satisfied
            : CatVisualState.Relaxed;
        State = CatVisualState.Receiving;
        RefreshLabels();
    }

    public void RegisterServedDish(int completedDishes, bool isFinalDish)
    {
        Satisfaction = Mathf.Clamp(completedDishes, 0, 3);
        afterReceivingState = isFinalDish || Satisfaction >= 3
            ? CatVisualState.Satisfied
            : CatVisualState.Relaxed;

        if (State != CatVisualState.Receiving
            || receivingTimeRemaining <= 0f)
        {
            State = afterReceivingState;
        }

        RefreshLabels(true);
    }

    public void PlayFinalPurr()
    {
        Satisfaction = 3;
        receivingTimeRemaining = 0f;
        afterReceivingState = CatVisualState.Satisfied;
        State = CatVisualState.Satisfied;
        RefreshLabels(true);
    }

    public void PlaySleeping()
    {
        Satisfaction = 3;
        receivingTimeRemaining = 0f;
        dishCelebrationTimeRemaining = 0f;
        BeginSleepingCrossfade();
        State = CatVisualState.Sleeping;
        RefreshLabels(true);
        ApplyVisuals(0f, false);
    }

    public void AdvanceVisual(float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        animationClock += safeDeltaTime;

        if (dishCelebrationTimeRemaining > 0f)
        {
            dishCelebrationTimeRemaining = Mathf.Max(
                0f,
                dishCelebrationTimeRemaining - safeDeltaTime
            );
            RefreshExpression();
        }

        if (State == CatVisualState.Receiving)
        {
            receivingTimeRemaining = Mathf.Max(
                0f,
                receivingTimeRemaining - safeDeltaTime
            );

            if (receivingTimeRemaining <= 0f)
            {
                State = afterReceivingState;
                RefreshLabels();
            }
        }

        ApplyVisuals(safeDeltaTime, false);
    }

    private void ResolveReferences()
    {
        if (recipeRunner == null)
        {
            recipeRunner = FindFirstObjectByType<RecipeRunner>();
        }

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (bodyGraphic == null)
        {
            bodyGraphic = GetComponent<Graphic>();
        }

        if (awakeVisual == null && visualRoot != null
            && visualRoot.gameObject != gameObject)
        {
            awakeVisual = visualRoot.gameObject;
        }

        if (animator == null && visualRoot != null)
        {
            animator = visualRoot.GetComponentInChildren<Animator>(true);
        }
    }

    private void CaptureBaseTransform()
    {
        if (hasCapturedBaseTransform || visualRoot == null)
        {
            return;
        }

        baseLocalPosition = visualRoot.localPosition;
        baseLocalScale = visualRoot.localScale;
        baseLocalRotation = visualRoot.localRotation;
        hasCapturedBaseTransform = true;
    }

    private void Subscribe()
    {
        if (isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeStarted += ShowRequest;
        recipeRunner.StepStarted += HandleStepStarted;
        recipeRunner.StepCompleted += HandleStepCompleted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeStarted -= ShowRequest;
        recipeRunner.StepStarted -= HandleStepStarted;
        recipeRunner.StepCompleted -= HandleStepCompleted;
        isSubscribed = false;
    }

    private void HandleStepStarted(RecipeStep _, int __)
    {
        SetWaitingForKitchen();
    }

    private void HandleStepCompleted(RecipeStep step, int _)
    {
        if (step != null
            && step.ReactionType == KitchenReactionType.Serving)
        {
            PlayReceiving();
        }
    }

    private void RefreshLabels(bool force = false)
    {
        if (!force && renderedState == State)
        {
            return;
        }

        if (stateLabel != null)
        {
            stateLabel.text = State switch
            {
                CatVisualState.Requesting => GameLocalization.Text("PIDE", "ASKING"),
                CatVisualState.Waiting => GameLocalization.Text("OBSERVANDO", "WATCHING"),
                CatVisualState.Receiving => GameLocalization.Text("PROBANDO...", "TASTING..."),
                CatVisualState.Relaxed => GameLocalization.Text($"MÁS TRANQUILO · {Satisfaction}/3", $"CALMER · {Satisfaction}/3"),
                CatVisualState.Satisfied => GameLocalization.Text("SATISFECHO · RONRONEANDO", "SATISFIED · PURRING"),
                CatVisualState.Sleeping => GameLocalization.Text("DURMIENDO", "SLEEPING"),
                _ => GameLocalization.Text("HAMBRIENTO", "HUNGRY")
            };
        }

        if (requestLabel != null)
        {
            requestLabel.text = string.IsNullOrWhiteSpace(CurrentRequest)
                ? GameLocalization.Text("ESPERANDO EL BANQUETE", "WAITING FOR THE BANQUET")
                : CurrentRequest;
        }

        RefreshExpression();
        SetAnimatorState();
        renderedState = State;
    }

    private void ApplyVisuals(float deltaTime, bool immediate)
    {
        bool isSleeping = State == CatVisualState.Sleeping;

        if (renderedState != State)
        {
            SetAnimatorState();
        }

        if (isSleeping)
        {
            AdvanceSleepingCrossfade(deltaTime);
            return;
        }

        EnsureAwakeVisual();

        if (!hasCapturedBaseTransform || visualRoot == null)
        {
            return;
        }

        float scaleBoost = 0f;
        float rotation = 0f;
        float bounce = 0f;

        switch (State)
        {
            case CatVisualState.Hungry:
                rotation = Mathf.Sin(animationClock * 4f) * 2f;
                break;
            case CatVisualState.Requesting:
                scaleBoost = 0.04f + Mathf.Sin(animationClock * 5f) * 0.02f;
                break;
            case CatVisualState.Waiting:
                scaleBoost = Mathf.Sin(animationClock * 2.5f) * 0.015f;
                break;
            case CatVisualState.Receiving:
                bounce = Mathf.Abs(Mathf.Sin(animationClock * 8f))
                    * receivingBounceHeight;
                rotation = Mathf.Sin(animationClock * 10f) * 4f;
                scaleBoost = 0.06f;
                break;
            case CatVisualState.Relaxed:
                rotation = -3f;
                scaleBoost = 0.035f;
                break;
            case CatVisualState.Satisfied:
                scaleBoost = 0.07f + Mathf.Sin(animationClock * 3f) * 0.025f;
                bounce = Mathf.Abs(Mathf.Sin(animationClock * 2f))
                    * satisfiedBounceHeight;
                break;
        }

        Vector3 desiredPosition = baseLocalPosition + Vector3.up * bounce;
        Vector3 desiredScale = baseLocalScale * (1f + scaleBoost);
        Quaternion desiredRotation = baseLocalRotation
            * Quaternion.Euler(0f, 0f, rotation);
        float blend = immediate
            ? 1f
            : 1f - Mathf.Exp(-Mathf.Max(0f, transitionSpeed) * deltaTime);
        visualRoot.localPosition = Vector3.Lerp(
            visualRoot.localPosition,
            desiredPosition,
            blend
        );
        visualRoot.localScale = Vector3.Lerp(
            visualRoot.localScale,
            desiredScale,
            blend
        );
        visualRoot.localRotation = Quaternion.Slerp(
            visualRoot.localRotation,
            desiredRotation,
            blend
        );

        if (bodyGraphic != null)
        {
            Color desiredColor = State switch
            {
                CatVisualState.Requesting => requestingColor,
                CatVisualState.Receiving => requestingColor,
                CatVisualState.Relaxed => relaxedColor,
                CatVisualState.Satisfied => satisfiedColor,
                _ => hungryColor
            };
            bodyGraphic.color = immediate
                ? desiredColor
                : Color.Lerp(bodyGraphic.color, desiredColor, blend);
        }
    }

    private void SetAnimatorState()
    {
        if (animator == null || string.IsNullOrWhiteSpace(visualStateParameter))
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Int
                && parameter.name == visualStateParameter)
            {
                animator.SetInteger(visualStateParameter, (int)State);
                return;
            }
        }
    }

    private void SetHappyExpression(bool happy)
    {
        SetExpressionPartsActive(defaultExpressionParts, !happy);
        SetExpressionPartsActive(happyExpressionParts, happy);
    }

    private void RefreshExpression()
    {
        SetHappyExpression(
            State == CatVisualState.Satisfied
            || dishCelebrationTimeRemaining > 0f
        );
    }

    private static void SetExpressionPartsActive(
        GameObject[] parts,
        bool active
    )
    {
        if (parts == null)
        {
            return;
        }

        foreach (GameObject part in parts)
        {
            if (part != null && part.activeSelf != active)
            {
                part.SetActive(active);
            }
        }
    }

    private void CacheCrossfadeTargets()
    {
        if (awakeVisual != null && awakeSpriteRenderers.Length == 0
            && awakeGraphics.Length == 0)
        {
            awakeSpriteRenderers =
                awakeVisual.GetComponentsInChildren<SpriteRenderer>(true);
            awakeGraphics = awakeVisual.GetComponentsInChildren<Graphic>(true);
            awakeSpriteAlphas = CaptureAlphas(awakeSpriteRenderers);
            awakeGraphicAlphas = CaptureAlphas(awakeGraphics);
        }

        if (sleepingVisual != null && sleepingSpriteRenderers.Length == 0
            && sleepingGraphics.Length == 0)
        {
            sleepingSpriteRenderers =
                sleepingVisual.GetComponentsInChildren<SpriteRenderer>(true);
            sleepingGraphics =
                sleepingVisual.GetComponentsInChildren<Graphic>(true);
            sleepingSpriteAlphas = CaptureAlphas(sleepingSpriteRenderers);
            sleepingGraphicAlphas = CaptureAlphas(sleepingGraphics);
        }
    }

    private void BeginSleepingCrossfade()
    {
        CacheCrossfadeTargets();
        sleepingCrossfadeElapsed = 0f;
        sleepingCrossfadeActive = sleepingCrossfadeDuration > 0f
            && awakeVisual != null
            && sleepingVisual != null;

        SetVisualActive(awakeVisual, true);
        SetVisualActive(sleepingVisual, true);
        SetAwakeAlpha(1f);
        SetSleepingAlpha(sleepingCrossfadeActive ? 0f : 1f);

        if (!sleepingCrossfadeActive)
        {
            SetVisualActive(awakeVisual, false);
        }
    }

    private void AdvanceSleepingCrossfade(float deltaTime)
    {
        if (!sleepingCrossfadeActive)
        {
            SetVisualActive(awakeVisual, false);
            SetVisualActive(sleepingVisual, true);
            SetSleepingAlpha(1f);
            return;
        }

        sleepingCrossfadeElapsed += Mathf.Max(0f, deltaTime);
        float progress = Mathf.Clamp01(
            sleepingCrossfadeElapsed / Mathf.Max(0.001f, sleepingCrossfadeDuration)
        );
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
        SetAwakeAlpha(1f - easedProgress);
        SetSleepingAlpha(easedProgress);

        if (progress < 1f)
        {
            return;
        }

        sleepingCrossfadeActive = false;
        SetVisualActive(awakeVisual, false);
        SetAwakeAlpha(1f);
        SetSleepingAlpha(1f);
    }

    private void CancelSleepingCrossfade()
    {
        sleepingCrossfadeActive = false;
        sleepingCrossfadeElapsed = 0f;
        SetAwakeAlpha(1f);
        SetSleepingAlpha(1f);
        SetVisualActive(awakeVisual, true);
        SetVisualActive(sleepingVisual, false);
    }

    private void EnsureAwakeVisual()
    {
        if (!sleepingCrossfadeActive
            && (awakeVisual == null || awakeVisual.activeSelf)
            && (sleepingVisual == null || !sleepingVisual.activeSelf))
        {
            return;
        }

        CancelSleepingCrossfade();
    }

    private void SetAwakeAlpha(float multiplier)
    {
        SetAlpha(awakeSpriteRenderers, awakeSpriteAlphas, multiplier);
        SetAlpha(awakeGraphics, awakeGraphicAlphas, multiplier);
    }

    private void SetSleepingAlpha(float multiplier)
    {
        SetAlpha(sleepingSpriteRenderers, sleepingSpriteAlphas, multiplier);
        SetAlpha(sleepingGraphics, sleepingGraphicAlphas, multiplier);
    }

    private static float[] CaptureAlphas(SpriteRenderer[] renderers)
    {
        float[] alphas = new float[renderers.Length];
        for (int index = 0; index < renderers.Length; index++)
        {
            alphas[index] = renderers[index] != null
                ? renderers[index].color.a
                : 1f;
        }

        return alphas;
    }

    private static float[] CaptureAlphas(Graphic[] graphics)
    {
        float[] alphas = new float[graphics.Length];
        for (int index = 0; index < graphics.Length; index++)
        {
            alphas[index] = graphics[index] != null
                ? graphics[index].color.a
                : 1f;
        }

        return alphas;
    }

    private static void SetAlpha(
        SpriteRenderer[] renderers,
        float[] baseAlphas,
        float multiplier
    )
    {
        for (int index = 0; index < renderers.Length; index++)
        {
            SpriteRenderer renderer = renderers[index];
            if (renderer == null)
            {
                continue;
            }

            Color color = renderer.color;
            color.a = baseAlphas[index] * Mathf.Clamp01(multiplier);
            renderer.color = color;
        }
    }

    private static void SetAlpha(
        Graphic[] graphics,
        float[] baseAlphas,
        float multiplier
    )
    {
        for (int index = 0; index < graphics.Length; index++)
        {
            Graphic graphic = graphics[index];
            if (graphic == null)
            {
                continue;
            }

            Color color = graphic.color;
            color.a = baseAlphas[index] * Mathf.Clamp01(multiplier);
            graphic.color = color;
        }
    }

    private void SetVisualActive(GameObject visual, bool active)
    {
        if (visual != null && visual != gameObject
            && visual.activeSelf != active)
        {
            visual.SetActive(active);
        }
    }
}
